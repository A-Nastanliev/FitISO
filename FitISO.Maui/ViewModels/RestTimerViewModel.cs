using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FitISO.Maui.Messages;
using FitISO.Maui.Models;
using FitISO.Maui.Services;
using System;
using System.ComponentModel;
using System.Linq;

namespace FitISO.Maui.ViewModels
{
    public partial class RestTimerViewModel : ObservableObject,
        IRecipient<RestStopwatchEnabledChangedMessage>, IRecipient<SetCompletedMessage>, IRecipient<SetRemovedMessage>,
        IRecipient<WorkoutExerciseRemovedMessage>, IRecipient<WorkoutFinishedMessage>, IRecipient<DbImportedMessage>
    {
        readonly RestStopwatchStateService stateService;
        readonly WorkoutSettingsService settings;
        readonly IActiveWorkoutProvider activeWorkout;

        IDispatcherTimer? timer;
        WorkoutExercise? triggerExercise;
        Set? triggerSet;
        bool restoring;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsAvailable))]
        bool isEnabled;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowStart))]
        [NotifyPropertyChangedFor(nameof(ShowRunning))]
        [NotifyPropertyChangedFor(nameof(ShowStopped))]
        DateTime? startTime;

        [ObservableProperty]
        TimeSpan elapsed;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowRunning))]
        [NotifyPropertyChangedFor(nameof(ShowStopped))]
        bool isStopped;

        public bool IsAvailable => IsEnabled && !ActiveWorkoutState.Instance.IsViewingPartner;
        public bool ShowStart => StartTime is null;
        public bool ShowRunning => StartTime is not null && !IsStopped;
        public bool ShowStopped => StartTime is not null && IsStopped;

        public RestTimerViewModel(RestStopwatchStateService stateService, WorkoutSettingsService settings, IActiveWorkoutProvider activeWorkout)
        {
            this.stateService = stateService;
            this.settings = settings;
            this.activeWorkout = activeWorkout;

            IsEnabled = settings.RestStopwatchEnabled;

            WeakReferenceMessenger.Default.RegisterAll(this);
            ActiveWorkoutState.Instance.PropertyChanged += ActiveWorkoutState_PropertyChanged;
            activeWorkout.PropertyChanged += ActiveWorkout_PropertyChanged;

            RestoreFor(activeWorkout.Workout);
        }

        void ActiveWorkoutState_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ActiveWorkoutState.IsViewingPartner))
                OnPropertyChanged(nameof(IsAvailable));
        }

        void ActiveWorkout_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IActiveWorkoutProvider.Workout))
                RestoreFor(activeWorkout.Workout);
        }

        partial void OnIsEnabledChanged(bool value)
        {
            if (!value)
                Clear();
        }

        partial void OnStartTimeChanged(DateTime? value)
        {
            UpdateTimer(value is not null);

            if (restoring) return;

            if (value is DateTime dt)
            {
                stateService.WorkoutId = activeWorkout.Workout.Id;
                stateService.StartTimeUtc = dt;
                stateService.IsStopped = IsStopped;
            }
            else
            {
                stateService.Clear();
            }
        }

        partial void OnIsStoppedChanged(bool value)
        {
            if (restoring || StartTime is null) return;

            stateService.IsStopped = value;
        }

        void UpdateTimer(bool run)
        {
            if (!run)
            {
                timer?.Stop();
                return;
            }

            if (timer is null)
            {
                var dispatcher = Application.Current?.Dispatcher;
                if (dispatcher is null) return;

                timer = dispatcher.CreateTimer();
                timer.Interval = TimeSpan.FromSeconds(1);
                timer.Tick += (s, e) => Tick();
            }

            if (!timer.IsRunning)
                timer.Start();

            Tick();
        }

        void Tick()
        {
            if (StartTime is DateTime start)
                Elapsed = DateTime.UtcNow - start;
        }

        [RelayCommand]
        void Start() => Begin();

        [RelayCommand]
        void Restart() => Begin();

        [RelayCommand]
        void Stop() => IsStopped = true;

        [RelayCommand]
        void Resume() => IsStopped = false;

        void Begin()
        {
            Elapsed = TimeSpan.Zero;
            IsStopped = false;
            StartTime = DateTime.UtcNow;
            SetTrigger(null, null);
        }

        void Clear()
        {
            Elapsed = TimeSpan.Zero;
            IsStopped = false;
            StartTime = null;
            triggerExercise = null;
            triggerSet = null;
        }

        void SetTrigger(WorkoutExercise? exercise, Set? set)
        {
            triggerExercise = exercise;
            triggerSet = set;

            if (StartTime is null) return;

            stateService.TriggerExerciseId = exercise?.Id ?? 0;
            stateService.TriggerSetId = set?.Id ?? 0;
        }

        void RestoreFor(Workout workout)
        {
            int savedWorkoutId = stateService.WorkoutId;
            DateTime? savedStartTime = stateService.StartTimeUtc;

            if (savedWorkoutId != 0 && savedWorkoutId == workout.Id && savedStartTime is DateTime savedDt)
            {
                restoring = true;
                StartTime = savedDt;
                IsStopped = stateService.IsStopped;
                restoring = false;

                int triggerExerciseId = stateService.TriggerExerciseId;
                int triggerSetId = stateService.TriggerSetId;

                triggerExercise = triggerExerciseId != 0
                    ? workout.WorkoutExercises.FirstOrDefault(we => we.Id == triggerExerciseId)
                    : null;
                triggerSet = triggerExercise is not null && triggerSetId != 0
                    ? triggerExercise.Sets.FirstOrDefault(s => s.Id == triggerSetId)
                    : null;
                return;
            }

            Clear();
        }

        public void Receive(RestStopwatchEnabledChangedMessage message) => IsEnabled = message.Value;

        public void Receive(SetCompletedMessage message)
        {
            if (!IsEnabled) return;

            var set = message.Value;
            var owner = activeWorkout.Workout.WorkoutExercises.FirstOrDefault(we => we.Sets.Contains(set));
            if (owner is null) return;

            int index = owner.Sets.IndexOf(set);
            bool isLastSetOfExercise = index == owner.Sets.Count - 1;

            if (isLastSetOfExercise && !settings.AutoStartRestOnExerciseFinish)
            {
                Clear();
                return;
            }

            StartTime = DateTime.UtcNow;
            IsStopped = false;

            SetTrigger(isLastSetOfExercise ? null : owner, isLastSetOfExercise ? null : set);
        }

        public void Receive(SetRemovedMessage message)
        {
            var exercise = message.Value;
            if (!ReferenceEquals(triggerExercise, exercise)) return;

            bool triggerWasRemoved = triggerSet is not null && !exercise.Sets.Contains(triggerSet);
            bool triggerIsNowLast = !triggerWasRemoved && exercise.Sets.Count > 0
                && ReferenceEquals(exercise.Sets[^1], triggerSet);

            if (triggerWasRemoved || triggerIsNowLast)
                Clear();
        }

        public void Receive(WorkoutExerciseRemovedMessage message)
        {
            if (ReferenceEquals(triggerExercise, message.Value))
                Clear();
        }

        public void Receive(WorkoutFinishedMessage message) => Clear();

        public void Receive(DbImportedMessage message)
        {
            Clear();
            stateService.Clear();
        }
    }
}