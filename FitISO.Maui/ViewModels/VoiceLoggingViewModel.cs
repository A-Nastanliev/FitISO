using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FitISO.Maui.Messages;
using FitISO.Maui.Models;
using FitISO.Maui.Services;
using System.ComponentModel;
using System.Globalization;

namespace FitISO.Maui.ViewModels
{
    public partial class VoiceLoggingViewModel : ObservableObject, IRecipient<VoiceLoggingEnabledChangedMessage>
    {
        static readonly CultureInfo VoiceCulture = new("en-US");

        readonly VoiceRecognitionService voiceRecognitionService;
        readonly IActiveWorkoutProvider activeWorkout;

        CancellationTokenSource? voiceCts;
        WorkoutExercise? lastExercise;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsAvailable))]
        bool isEnabled;

        [ObservableProperty]
        bool isListening;

        public bool IsAvailable => IsEnabled && !ActiveWorkoutState.Instance.IsViewingPartner;

        public VoiceLoggingViewModel(VoiceLoggingSettingsService voiceLoggingSettingsService,
            VoiceRecognitionService voiceRecognitionService, IActiveWorkoutProvider activeWorkout)
        {
            this.voiceRecognitionService = voiceRecognitionService;
            this.activeWorkout = activeWorkout;

            IsEnabled = voiceLoggingSettingsService.VoiceLoggingEnabled;

            WeakReferenceMessenger.Default.RegisterAll(this);
            ActiveWorkoutState.Instance.PropertyChanged += ActiveWorkoutState_PropertyChanged;
        }

        public void Receive(VoiceLoggingEnabledChangedMessage message) => IsEnabled = message.Value;

        void ActiveWorkoutState_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is not (nameof(ActiveWorkoutState.IsInPipMode) or nameof(ActiveWorkoutState.IsViewingPartner)))
                return;

            var state = ActiveWorkoutState.Instance;

            if (state.IsInPipMode || state.IsViewingPartner)
                Cancel();

            OnPropertyChanged(nameof(IsAvailable));
        }

        public void Cancel() => voiceCts?.Cancel();

        [RelayCommand(AllowConcurrentExecutions = true)]
        async Task Toggle()
        {
            if (IsListening)
            {
                voiceCts?.Cancel();
                return;
            }

            var cts = voiceCts = new CancellationTokenSource();
            IsListening = true;
            try
            {
                string? text = await voiceRecognitionService.ListenOnceAsync(VoiceCulture, cts.Token);

                if (!cts.IsCancellationRequested && !string.IsNullOrWhiteSpace(text))
                    Apply(text);
            }
            finally
            {
                IsListening = false;
                if (ReferenceEquals(voiceCts, cts)) voiceCts = null;
                cts.Dispose();
            }
        }

        void Apply(string text)
        {
            if (VoiceCommandParser.Parse(text) is not { } command)
            {
                _ = Toast.Make($"Didn't understand \"{text}\"").Show();
                return;
            }

            var exercises = activeWorkout.Workout.WorkoutExercises;
            WorkoutExercise? workoutExercise;

            if (command.ExerciseName is null)
            {
                workoutExercise = lastExercise is not null && exercises.Contains(lastExercise)
                    ? lastExercise
                    : null;

                if (workoutExercise is null)
                {
                    _ = Toast.Make("Say the exercise first, e.g. \"bench press 100 kg 5 reps\"").Show();
                    return;
                }
            }
            else
            {
                workoutExercise = ExerciseNameMatcher.FindBest(
                    command.ExerciseName, exercises, we => we.Exercise.Name);

                if (workoutExercise is null)
                {
                    _ = Toast.Make($"No exercise matching \"{command.ExerciseName}\" in this workout").Show();
                    return;
                }

                lastExercise = workoutExercise;
            }

            var set = workoutExercise.Sets.FirstOrDefault(s => !(s.Weight is >= 0 && s.Reps is > 0));

            if (set is null)
            {
                _ = Toast.Make($"All sets of {workoutExercise.Exercise.Name} are already filled").Show();
                return;
            }

            if (command.Weight is double weight)
                set.Weight = weight;

            set.Reps = command.Reps;

            int setNumber = workoutExercise.Sets.IndexOf(set) + 1;
            string weightText = set.Weight is double w ? $"{w:0.##} kg" : "no weight";
            _ = Toast.Make($"{workoutExercise.Exercise.Name} · set {setNumber}: {weightText} × {set.Reps}").Show();
        }
    }
}