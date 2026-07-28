using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FitISO.Maui.Messages;
using FitISO.Maui.Models;
using FitISO.Services;
using FitISO.Maui.Views;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;

namespace FitISO.Maui.ViewModels
{
    public partial class ActiveWorkoutViewModel : ObservableObject, IRecipient<WorkoutStartedMessage>
    {
        [ObservableProperty]
        Workout workout = new();

        [ObservableProperty]
        TimeSpan duration;

        [ObservableProperty]
        int completedSets;

        [ObservableProperty]
        int totalSets;

        double progress;
        public double Progress
        {
            get => progress;
            private set => SetProperty(ref progress, value);
        }

        IDispatcherTimer? _timer;

        static readonly TimeSpan NameDebounceDelay = TimeSpan.FromSeconds(1.2);
        CancellationTokenSource? _nameDebounceCts;

        readonly IServiceProvider serviceProvider;

        readonly SetService setService;
        readonly WorkoutExerciseService workoutExerciseService;
        readonly WorkoutService workoutService;

        public ActiveWorkoutViewModel(SetService setService, WorkoutService workoutService, WorkoutExerciseService workoutExerciseService, IServiceProvider serviceProvider)
        {
            WeakReferenceMessenger.Default.RegisterAll(this);
            this.setService = setService;
            this.workoutExerciseService = workoutExerciseService;
            this.workoutService = workoutService;
            this.serviceProvider = serviceProvider;
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

            RecalculateProgress();
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
                WireSet(s);
        }

        void UnwireWorkoutExercise(WorkoutExercise we)
        {
            we.Sets.CollectionChanged -= Sets_CollectionChanged;
            foreach (var s in we.Sets)
                UnwireSet(s);
        }

        void Sets_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems is not null)
                foreach (Set s in e.NewItems)
                    WireSet(s);

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
                    if (s.Weight is > 0 && s.Reps is > 0)
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
            if (Workout.StartTime is not DateTime start) return;

            if (start.Kind != DateTimeKind.Utc)
                start = DateTime.SpecifyKind(start, DateTimeKind.Utc);

            Duration = DateTime.UtcNow - start;
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

        void WireSet(Set set)
        {
            set.SaveAction = s => setService.UpdateAsync(s.Id, s.Weight, s.Reps);
            set.PropertyChanged += Set_PropertyChanged;
        }

        void UnwireSet(Set set)
        {
            set.PropertyChanged -= Set_PropertyChanged;
        }

        void Set_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Set.Weight) || e.PropertyName == nameof(Set.Reps))
                RecalculateProgress();
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

            if (workoutExercise.SetCount == 0)
            {
                await workoutExerciseService.DeleteAsync(workoutExercise.Id);
                Workout.WorkoutExercises.Remove(workoutExercise);
            }

            if (Workout.WorkoutExercises.Count == 0)
            {
                Stop();
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
            WeakReferenceMessenger.Default.Send(new WorkoutFinishedMessage(Workout));
            ActiveWorkoutState.Instance.HasActiveWorkout = false;
            await Shell.Current.GoToAsync("//main/history");
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
    }
}