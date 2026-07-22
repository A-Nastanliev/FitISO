using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FitISO.Maui.Messages;
using FitISO.Maui.Models;
using FitISO.Services;
using System;
using System.Collections.Generic;
using System.Text;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace FitISO.Maui.ViewModels
{
    public partial class ActiveWorkoutViewModel : ObservableObject, IRecipient<WorkoutStartedMessage>
    {
        [ObservableProperty]
        Workout workout = new();

        [ObservableProperty]
        TimeSpan duration;

        IDispatcherTimer? _timer;

        readonly SetService setService;
        readonly WorkoutExerciseService workoutExerciseService;
        readonly WorkoutService workoutService;

        public ActiveWorkoutViewModel(SetService setService, WorkoutService workoutService, WorkoutExerciseService workoutExerciseService)
        {
            WeakReferenceMessenger.Default.RegisterAll(this);
            this.setService = setService;
            this.workoutExerciseService = workoutExerciseService;
            this.workoutService = workoutService;
        }

        public void Receive(WorkoutStartedMessage message)
        {
            Workout = message.Value;
            StartTimer();
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

        public void Stop() => _timer?.Stop();

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
            await setService.DeleteAsync(workoutExercise.Sets[^1].Id);
            workoutExercise.Sets.Remove(workoutExercise.Sets[^1]);
            workoutExercise.SetCount = workoutExercise.Sets.Count;

            if (workoutExercise.SetCount == 0)
            {
                await workoutExerciseService.DeleteAsync(workoutExercise.Id);
                Workout.WorkoutExercises.Remove(workoutExercise);
            }

            if(Workout.WorkoutExercises.Count == 0)
            {
                Stop();
                await workoutService.DeleteAsync(Workout.Id);    
                ActiveWorkoutState.Instance.HasActiveWorkout = false;
                _ = Toast.Make($"{Workout.Name} terminated").Show();
                Workout = new();
            }
        }
    }
}
