using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FitISO.Maui.Messages;
using FitISO.Maui.Models;
using FitISO.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Maui.Alerts;

namespace FitISO.Maui.ViewModels
{
    public partial class WorkoutFormViewModel : ObservableObject, IQueryAttributable
    {
        [ObservableProperty]
        Workout workout = new();

        [ObservableProperty]
        Workout navigationWorkout = new();

        [ObservableProperty]
        ExerciseCollection exerciseCollection;

        [ObservableProperty]
        Exercise selectedExercise;

        bool isEditMode;

        readonly WorkoutService workoutService;

        public WorkoutFormViewModel(WorkoutService workoutService, ExerciseCollection exerciseCollection)
        {
            this.workoutService = workoutService;
            ExerciseCollection = exerciseCollection;
        }

        [RelayCommand]
        public async Task Save()
        {
            if (Workout.Name?.Length < 4 || String.IsNullOrWhiteSpace(Workout.Name) || Workout.WorkoutExercises?.Count == 0) return;

            try
            {
                if (!isEditMode)
                {
                    List<FitISO.Data.Models.WorkoutExercise> workoutExercises = new();
                    foreach (var we in Workout.WorkoutExercises)
                    {
                        FitISO.Data.Models.WorkoutExercise workoutExercise = new FitISO.Data.Models.WorkoutExercise
                        {
                            ExerciseId = we.Exercise.Id,
                            Sets = new()
                        };
                        for (int i = 0; i < we.SetCount; i++)
                        {
                            workoutExercise.Sets.Add(new FitISO.Data.Models.Set());
                        }
                        workoutExercises.Add(workoutExercise);
                    }
                    var workout = await workoutService.CreateAsync(Workout.Name, workoutExercises);
                    WeakReferenceMessenger.Default.Send(new WorkoutTemplateCreatedMessage(new Workout(workout)));
                    _ = Toast.Make($"{workout.Name} created").Show();
                    await NavigateBack();
                }
                else
                {
                    var workoutExercises = Workout.WorkoutExercises.Select(we => new FitISO.Data.Models.WorkoutExercise
                    {
                        Id = we.Id,
                        ExerciseId = we.Exercise.Id,
                        Note = we.Note,
                        Sets = we.Sets.Select(s => new FitISO.Data.Models.Set
                        {
                            Id = s.Id,
                            Weight = s.Weight,
                            Reps = s.Reps
                        }).ToList()
                    }).ToList();

                    var updated = await workoutService.UpdateAsync(Workout.Id, Workout.Name, workoutExercises);
                    NavigationWorkout.Name = Workout.Name;
                    Workout updatedWorkout = new Workout(updated);
                    NavigationWorkout.WorkoutExercises = updatedWorkout.WorkoutExercises;
                    _ = Toast.Make($"{updated.Name} updated").Show();
                    await NavigateBack();
                }
            }
            catch(Exception ex)
            {
                await Shell.Current.DisplayAlertAsync(ex.Message, ex.InnerException?.ToString(), "ok");
            }
        }


        [RelayCommand]
        public void IncreaseSets(WorkoutExercise workoutExercise)
        {
            workoutExercise.Sets.Add(new Set());
            workoutExercise.SetCount = workoutExercise.Sets.Count;
        }


        [RelayCommand]
        public void DecreaseSets(WorkoutExercise workoutExercise)
        {
            workoutExercise.Sets.Remove(workoutExercise.Sets[^1]);
            workoutExercise.SetCount = workoutExercise.Sets.Count;
            if (workoutExercise.SetCount == 0)
            {
                Workout.WorkoutExercises.Remove(workoutExercise);
            }
        }

        [RelayCommand]
        public async Task NavigateBack()
        {
            await Shell.Current.GoToAsync("..");
        }


        partial void OnSelectedExerciseChanged(Exercise value)
        {
            if(value is not null)
            {
                Workout.WorkoutExercises.Add(new WorkoutExercise
                {
                    Exercise = value,
                    Sets = new ObservableCollection<Set> { new Set() },
                    SetCount = 1
                });
                SelectedExercise = null;
            }
        }


        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue($"{nameof(NavigationWorkout)}", out var obj) && obj is Workout workout)
            {
                NavigationWorkout = workout;
                isEditMode = true;
                foreach (var we in NavigationWorkout.WorkoutExercises)
                {
                    var workoutExercise = new WorkoutExercise
                    {
                        Id = we.Id,
                        Exercise = we.Exercise,
                        Note = we.Note,
                        Sets = new ObservableCollection<Set>()
                    };

                    foreach (var set in we.Sets)
                    {
                        workoutExercise.Sets.Add(new Set { Id = set.Id });
                    }
                    workoutExercise.SetCount = workoutExercise.Sets.Count;

                    Workout.WorkoutExercises.Add(workoutExercise);
                }
                Workout.Id = workout.Id;
                Workout.Name = workout.Name;
            }
        }
    }
}
