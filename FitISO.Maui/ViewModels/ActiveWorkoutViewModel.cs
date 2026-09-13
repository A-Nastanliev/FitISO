using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FitISO.Maui.Messages;
using FitISO.Maui.Models;
using FitISO.Services;
using FitISO.Maui.Services;
using FitISO.Maui.Views;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace FitISO.Maui.ViewModels
{
    public partial class ActiveWorkoutViewModel : ObservableObject, IRecipient<WorkoutStartedMessage>, IRecipient<DbImportedMessage>, 
        IRecipient<ExerciseUpdatedMessage>, IRecipient<RestStopwatchEnabledChangedMessage>
    {
        [ObservableProperty]
        Workout workout = new();

        [ObservableProperty]
        TimeSpan duration;

        [ObservableProperty]
        int completedSets;

        [ObservableProperty]
        int totalSets;

        [ObservableProperty]
        bool restStopwatchEnabled;

        double progress;
        public double Progress
        {
            get => progress;
            private set => SetProperty(ref progress, value);
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowStartRest))]
        [NotifyPropertyChangedFor(nameof(ShowRunningRest))]
        [NotifyPropertyChangedFor(nameof(ShowStoppedRest))]
        DateTime? restStartTime;

        [ObservableProperty]
        TimeSpan restElapsed;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowRunningRest))]
        [NotifyPropertyChangedFor(nameof(ShowStoppedRest))]
        bool restIsStopped;

        partial void OnRestStartTimeChanged(DateTime? value)
        {
            if (_restoringRest) return;

            if (value is DateTime dt)
            {
                Preferences.Default.Set(RestPrefWorkoutId, Workout.Id);
                Preferences.Default.Set(RestPrefStartTimeTicks, dt.Ticks);
                Preferences.Default.Set(RestPrefIsStopped, RestIsStopped);
            }
            else
            {
                ClearPersistedRest();
            }
        }

        void ClearPersistedRest()
        {
            Preferences.Default.Remove(RestPrefWorkoutId);
            Preferences.Default.Remove(RestPrefStartTimeTicks);
            Preferences.Default.Remove(RestPrefIsStopped);
            Preferences.Default.Remove(RestPrefTriggerExerciseId);
            Preferences.Default.Remove(RestPrefTriggerSetId);
        }

        void SetRestTrigger(WorkoutExercise? exercise, Set? set)
        {
            _restTriggerExercise = exercise;
            _restTriggerSet = set;

            if (RestStartTime is null) return;

            Preferences.Default.Set(RestPrefTriggerExerciseId, exercise?.Id ?? 0);
            Preferences.Default.Set(RestPrefTriggerSetId, set?.Id ?? 0);
        }

        partial void OnRestIsStoppedChanged(bool value)
        {
            if (_restoringRest || RestStartTime is null) return;

            Preferences.Default.Set(RestPrefIsStopped, value);
        }

        public bool ShowStartRest => RestStartTime is null;
        public bool ShowRunningRest => RestStartTime is not null && !RestIsStopped;
        public bool ShowStoppedRest => RestStartTime is not null && RestIsStopped;
        WorkoutExercise? _restTriggerExercise;
        Set? _restTriggerSet;
        const string RestPrefWorkoutId = "ActiveRest_WorkoutId";
        const string RestPrefStartTimeTicks = "ActiveRest_StartTimeTicks";
        const string RestPrefIsStopped = "ActiveRest_IsStopped";
        const string RestPrefTriggerExerciseId = "ActiveRest_TriggerExerciseId";
        const string RestPrefTriggerSetId = "ActiveRest_TriggerSetId";
        bool _restoringRest;

        IDispatcherTimer? _timer;

        static readonly TimeSpan NameDebounceDelay = TimeSpan.FromSeconds(1.2);
        CancellationTokenSource? _nameDebounceCts;

        readonly IServiceProvider serviceProvider;

        readonly SetService setService;
        readonly WorkoutExerciseService workoutExerciseService;
        readonly WorkoutService workoutService;
        readonly WorkoutSettingsService workoutSettingsService;

        public ActiveWorkoutViewModel(SetService setService, WorkoutService workoutService, WorkoutExerciseService workoutExerciseService,
            WorkoutSettingsService workoutSettingsService, IServiceProvider serviceProvider)
        {
            WeakReferenceMessenger.Default.RegisterAll(this);
            this.setService = setService;
            this.workoutExerciseService = workoutExerciseService;
            this.workoutService = workoutService;
            this.workoutSettingsService = workoutSettingsService;
            this.serviceProvider = serviceProvider;
            RestStopwatchEnabled = workoutSettingsService.RestStopwatchEnabled;
        }

        public void Receive(RestStopwatchEnabledChangedMessage message) => RestStopwatchEnabled = message.Value;

        partial void OnRestStopwatchEnabledChanged(bool value)
        {
            if (!value)
                ResetRest();
        }

        public void Receive(WorkoutStartedMessage message)
        {
            Workout = message.Value;
            StartTimer();
        }

        partial void OnWorkoutChanged(Workout oldValue, Workout newValue)
        {
            if (oldValue is not null)
            {
                oldValue.PropertyChanged -= Workout_PropertyChanged;
                oldValue.WorkoutExercises.CollectionChanged -= WorkoutExercises_CollectionChanged;
                foreach (var we in oldValue.WorkoutExercises)
                    UnwireWorkoutExercise(we);
            }

            newValue.PropertyChanged += Workout_PropertyChanged;
            newValue.WorkoutExercises.CollectionChanged += WorkoutExercises_CollectionChanged;
            foreach (var we in newValue.WorkoutExercises)
                WireWorkoutExercise(we);

            RestoreRestState(newValue);

            RecalculateProgress();
        }

        void RestoreRestState(Workout newValue)
        {
            int savedWorkoutId = Preferences.Default.Get(RestPrefWorkoutId, 0);
            long savedTicks = Preferences.Default.Get(RestPrefStartTimeTicks, 0L);

            if (savedWorkoutId != 0 && savedWorkoutId == newValue.Id && savedTicks > 0)
            {
                _restoringRest = true;
                RestStartTime = new DateTime(savedTicks, DateTimeKind.Utc);
                RestIsStopped = Preferences.Default.Get(RestPrefIsStopped, false);
                _restoringRest = false;

                int triggerExerciseId = Preferences.Default.Get(RestPrefTriggerExerciseId, 0);
                int triggerSetId = Preferences.Default.Get(RestPrefTriggerSetId, 0);

                _restTriggerExercise = triggerExerciseId != 0
                    ? newValue.WorkoutExercises.FirstOrDefault(we => we.Id == triggerExerciseId)
                    : null;
                _restTriggerSet = _restTriggerExercise is not null && triggerSetId != 0
                    ? _restTriggerExercise.Sets.FirstOrDefault(s => s.Id == triggerSetId)
                    : null;
                return;
            }

            ResetRest();
        }

        void WorkoutExercises_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems is not null)
                foreach (WorkoutExercise we in e.NewItems)
                    WireWorkoutExercise(we);

            if (e.OldItems is not null)
                foreach (WorkoutExercise we in e.OldItems)
                    UnwireWorkoutExercise(we);

            RecalculateProgress();
        }

        void WireWorkoutExercise(WorkoutExercise we)
        {
            we.Sets.CollectionChanged += Sets_CollectionChanged;
            foreach (var s in we.Sets)
                WireSet(we, s);
        }

        void UnwireWorkoutExercise(WorkoutExercise we)
        {
            we.Sets.CollectionChanged -= Sets_CollectionChanged;
            foreach (var s in we.Sets)
                UnwireSet(s);
        }

        void Sets_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            var owner = Workout.WorkoutExercises.FirstOrDefault(we => ReferenceEquals(we.Sets, sender));

            if (e.NewItems is not null && owner is not null)
                foreach (Set s in e.NewItems)
                    WireSet(owner, s);

            if (e.OldItems is not null)
                foreach (Set s in e.OldItems)
                    UnwireSet(s);

            RecalculateProgress();
        }

        void RecalculateProgress()
        {
            int total = 0, completed = 0;

            foreach (var we in Workout.WorkoutExercises)
            {
                foreach (var s in we.Sets)
                {
                    total++;
                    if (s.Weight is >= 0 && s.Reps is > 0)
                        completed++;
                }
            }

            TotalSets = total;
            CompletedSets = completed;
            Progress = total == 0 ? 0 : (double)completed / total;
        }

        void Workout_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(Models.Workout.Name)) return;

            _nameDebounceCts?.Cancel();
            _nameDebounceCts = new CancellationTokenSource();
            _ = DebounceSaveNameAsync(Workout.Name, _nameDebounceCts.Token);
        }

        async Task DebounceSaveNameAsync(string name, CancellationToken token)
        {
            try
            {
                await Task.Delay(NameDebounceDelay, token);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            if (token.IsCancellationRequested) return;
            if (string.IsNullOrWhiteSpace(name)) return;

            Workout.Name = Workout.Name.Trim();
            await workoutService.UpdateNameAsync(Workout.Id, name);
        }

        private void StartTimer()
        {
            _timer?.Stop();

            _timer = Application.Current!.Dispatcher.CreateTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (s, e) => UpdateRemaining();
            _timer.Start();

            UpdateRemaining();
        }

        private void UpdateRemaining()
        {
            if (Workout.StartTime is DateTime start)
            {
                if (start.Kind != DateTimeKind.Utc)
                    start = DateTime.SpecifyKind(start, DateTimeKind.Utc);

                Duration = DateTime.UtcNow - start;
            }

            if (RestStartTime is DateTime restStart)
                RestElapsed = DateTime.UtcNow - restStart;
        }

        [RelayCommand]
        void StartRest()
        {
            RestElapsed = TimeSpan.Zero;
            RestIsStopped = false;
            RestStartTime = DateTime.UtcNow;
            SetRestTrigger(null, null);
        }

        [RelayCommand]
        void StopRest()
        {
            RestIsStopped = true;
        }

        [RelayCommand]
        void ResumeRest()
        {
            RestIsStopped = false;
        }

        [RelayCommand]
        void ManualResetRest()
        {
            RestElapsed = TimeSpan.Zero;
            RestIsStopped = false;
            RestStartTime = DateTime.UtcNow;
            SetRestTrigger(null, null);
        }

        void ResetRest()
        {
            RestElapsed = TimeSpan.Zero;
            RestIsStopped = false;
            RestStartTime = null;
            _restTriggerExercise = null;
            _restTriggerSet = null;
        }

        public void EnsureTimerRunning()
        {
            if (_timer is not null && _timer.IsRunning) return;
            StartTimer();
        }
        public void Stop()
        {
            _timer?.Stop();
            _nameDebounceCts?.Cancel();
        }

        void WireSet(WorkoutExercise workoutExercise, Set set)
        {
            set.SaveAction = s => setService.UpdateAsync(s.Id, s.Weight, s.Reps);
            set.PropertyChanged += Set_PropertyChanged;
            set.JustCompleted += Set_JustCompleted;
        }

        void UnwireSet(Set set)
        {
            set.PropertyChanged -= Set_PropertyChanged;
            set.JustCompleted -= Set_JustCompleted;
        }

        void Set_JustCompleted(object? sender, EventArgs e)
        {
            if (!RestStopwatchEnabled) return;

            if (sender is not Set set) return;

            var owner = FindOwner(set);
            if (owner is null) return;

            int index = owner.Sets.IndexOf(set);
            bool isLastSetOfExercise = index == owner.Sets.Count - 1;

            if (isLastSetOfExercise && !workoutSettingsService.AutoStartRestOnExerciseFinish)
            {
                ResetRest();
                return;
            }

            RestStartTime = DateTime.UtcNow;
            RestIsStopped = false;

            SetRestTrigger(isLastSetOfExercise ? null : owner, isLastSetOfExercise ? null : set);
        }

        WorkoutExercise? FindOwner(Set set) =>
            Workout.WorkoutExercises.FirstOrDefault(we => we.Sets.Contains(set));

        void Set_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not Set set) return;

            if (e.PropertyName == nameof(Set.Reps) && set.Reps is not null && set.Weight is null)
            {
                TryFillWeightFromHistory(set);
                return;
            }

            if (e.PropertyName == nameof(Set.Weight) || e.PropertyName == nameof(Set.Reps))
                RecalculateProgress();
        }

        void TryFillWeightFromHistory(Set set)
        {
            var workoutExercise = FindOwner(set);
            if (workoutExercise is null) return;

            int index = workoutExercise.Sets.IndexOf(set);
            if (index < 0) return;

            for (int i = index - 1; i >= 0; i--)
            {
                var prior = workoutExercise.Sets[i];
                if (prior.Weight is double w && prior.Reps is not null)
                {
                    set.Weight = w;
                    return;
                }
            }

            var suggested = workoutExercise.Exercise?.LastSets?
                .Where(s => s.Weight is not null && s.Reps is not null)
                .Max(s => s.Weight);

            if (suggested is double lastWeight)
                set.Weight = lastWeight;
        }

        [RelayCommand]
        public async Task IncreaseSets(WorkoutExercise workoutExercise)
        {
            Set set = new Set(await setService.CreateAsync(workoutExercise.Id, null, null));
            workoutExercise.Sets.Add(set);
            workoutExercise.SetCount = workoutExercise.Sets.Count;
        }

        [RelayCommand]
        public async Task DecreaseSets(WorkoutExercise workoutExercise)
        {
            var lastSet = workoutExercise.Sets[^1];
            lastSet.CancelPendingSave();

            await setService.DeleteAsync(lastSet.Id);
            workoutExercise.Sets.Remove(lastSet);
            workoutExercise.SetCount = workoutExercise.Sets.Count;

            if (ReferenceEquals(_restTriggerExercise, workoutExercise))
            {
                bool triggerWasRemoved = ReferenceEquals(_restTriggerSet, lastSet);
                bool triggerIsNowLast = !triggerWasRemoved && workoutExercise.Sets.Count > 0
                    && ReferenceEquals(workoutExercise.Sets[^1], _restTriggerSet);

                if (triggerWasRemoved || triggerIsNowLast)
                    ResetRest();
            }

            if (workoutExercise.SetCount == 0)
            {
                await workoutExerciseService.DeleteAsync(workoutExercise.Id);
                Workout.WorkoutExercises.Remove(workoutExercise);

                if (ReferenceEquals(_restTriggerExercise, workoutExercise))
                    ResetRest();
            }

            if (Workout.WorkoutExercises.Count == 0)
            {
                Stop();
                ResetRest();
                await workoutService.DeleteAsync(Workout.Id);
                ActiveWorkoutState.Instance.HasActiveWorkout = false;
                _ = Toast.Make($"{Workout.Name} terminated").Show();
                Workout = new();
            }
        }

        [RelayCommand]
        public async Task FinishWorkout()
        {
            if (string.IsNullOrWhiteSpace(Workout.Name) || Workout.Name.Length < 4) return;

            if (CompletedSets != TotalSets)
            {
                _ = Toast.Make($"All sets need a weight and reps").Show();
                return;
            }

            await workoutService.EndWorkoutAsync(Workout.Id);
            Workout.EndTime = DateTime.UtcNow;
            _ = Toast.Make($"{Workout.Name} finished").Show();
            ResetRest();
            Stop();
            await Shell.Current.GoToAsync("//main/history");
            WeakReferenceMessenger.Default.Send(new WorkoutFinishedMessage(Workout));
            ActiveWorkoutState.Instance.HasActiveWorkout = false;
        }

        [RelayCommand]
        public async Task AddExercise()
        {
            var popup = serviceProvider.GetRequiredService<SelectExercisePopupPage>();
            var viewModel = (SelectExercisePopupViewModel)popup.BindingContext;

            await Shell.Current.Navigation.PushModalAsync(popup);

            var exercise = await viewModel.Result;

            await Task.Yield();

            AppShellTabBar.Current?.RefreshSelectedButtonCommand();

            if (exercise is null) return;

            var workoutExerciseDto = await workoutExerciseService.CreateAsync(Workout.Id, exercise.Id);

            var workoutExercise = new WorkoutExercise
            {
                Id = workoutExerciseDto.Id,
                Exercise = exercise
            };

            var set = new Set(await setService.CreateAsync(workoutExercise.Id, null, null));
            workoutExercise.Sets.Add(set);
            workoutExercise.SetCount = workoutExercise.Sets.Count;

            Workout.WorkoutExercises.Add(workoutExercise);
        }

        public async void Receive(DbImportedMessage message)
        {
            ClearPersistedRest();

            var activeWorkout = await workoutService.GetActiveWorkoutAsync();

            if (activeWorkout != null && activeWorkout?.Id != 0)
            {
                Workout = new Workout(activeWorkout);
                ActiveWorkoutState.Instance.HasActiveWorkout = true;
            }
            else
            {
                ActiveWorkoutState.Instance.HasActiveWorkout = false;
            }
        }

        public void Receive(ExerciseUpdatedMessage message)
        {
            Exercise exercise = message.Value;
            foreach (var we in Workout.WorkoutExercises)
            {
                if (we.Exercise.Id == exercise.Id)
                {
                    we.Exercise.Name = exercise.Name;
                }
            }
        }
    }
}