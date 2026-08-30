using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FitISO.Maui.Messages;
using FitISO.Maui.Models;
using FitISO.Maui.Services;
using FitISO.Services;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Collections.ObjectModel;

namespace FitISO.Maui.ViewModels
{
    public partial class ExerciseDetailsPopupViewModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowDeleteButton))]
        Exercise exercise = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        bool busy;

        public bool IsNotBusy => !Busy;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotEditingName))]
        [NotifyPropertyChangedFor(nameof(ShowDeleteButton))]
        bool isEditingName;

        public bool IsNotEditingName => !IsEditingName;

        string nameBeforeEdit;

        public bool ShowDeleteButton => Deletable && IsNotEditingName;

        public bool Deletable;

        public bool WasDeleted { get; private set; }
        public bool WasRenamed { get; private set; }

        [ObservableProperty]
        ISeries[] chartSeries = Array.Empty<ISeries>();

        [ObservableProperty]
        Axis[] chartXAxes = Array.Empty<Axis>();

        SKColor AccentColor
        {
            get
            {
                if (Application.Current?.Resources.TryGetValue("ChartAccentColor", out var value) == true
                    && value is Color color)
                {
                    return new SKColor(
                        (byte)(color.Red * 255),
                        (byte)(color.Green * 255),
                        (byte)(color.Blue * 255),
                        (byte)(color.Alpha * 255));
                }

                return new SKColor(205, 92, 92);
            }
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanFavourite))]
        bool hasHistory;

        [ObservableProperty]
        bool isFavourite;

        public bool CanFavourite => HasHistory;

        readonly TaskCompletionSource closedSource = new();
        readonly ExerciseService exerciseService;
        readonly SetService setService;
        readonly FavoriteExerciseService favoriteExerciseService;

        public Task Closed => closedSource.Task;

        const double RepsInfluence = 0.01;
        const double MaxRepsOffset = 0.5;

        public ExerciseDetailsPopupViewModel(ExerciseService exerciseService, SetService setService, FavoriteExerciseService favoriteExerciseService)
        {
            this.exerciseService = exerciseService;
            this.setService = setService;
            this.favoriteExerciseService = favoriteExerciseService;
        }

        static double WeightWithRepsTiebreak(double weight, double reps) =>
            weight + Math.Min(reps * RepsInfluence, MaxRepsOffset);

        public async Task LoadChartData()
        {
            if (Exercise.History.Count == 0)
            {
                var sets = await setService.GetBestSetPerWorkoutAsync(Exercise.Id);
                foreach (var s in sets)
                    Exercise.History.Add(new ExerciseHistoryPoint(s.WorkoutExercise.Workout.StartTime!.Value, s.Weight!.Value, s.Reps!.Value));
            }
            BuildChart();
        }

        void BuildChart()
        {
            var history = Exercise.History;
            HasHistory = history is { Count: > 0 };

            if (!HasHistory)
            {
                ChartSeries = Array.Empty<ISeries>();
                ChartXAxes = Array.Empty<Axis>();
                return;
            }

            ChartSeries = new ISeries[]
            {
                new LineSeries<ExerciseHistoryPoint>
                {
                    Values = history,
                    Mapping = (point, index) => new(index, WeightWithRepsTiebreak(point.Weight, point.Reps)),
                    Name = "Weight",

                    GeometrySize = 10,                                                         
                    Stroke = new SolidColorPaint(AccentColor) { StrokeThickness = 3 },
                    Fill = new SolidColorPaint(AccentColor.WithAlpha(60)),
                    GeometryFill = new SolidColorPaint(SKColors.White),                            
                    GeometryStroke = new SolidColorPaint(AccentColor) { StrokeThickness = 3 },      

                    DataLabelsPaint = new SolidColorPaint(SKColors.White)
                    {
                        SKTypeface = SKFontManager.Default.MatchFamily("OpenSans-Semibold") 
                    },
                    DataLabelsSize = 14,                                                              
                    DataLabelsPosition = DataLabelsPosition.Top,
                    DataLabelsFormatter = point => $"{point.Model!.Weight:0.##}×{point.Model!.Reps:0.##}",
                    YToolTipLabelFormatter = point => $"{point.Model!.Weight:0.##} kg × {point.Model!.Reps:0.##} reps",
                }
            };

            ChartXAxes = new Axis[]
            {
                new Axis
                {
                    Labels = history!.Select(h => h.Date.ToString("MMM d")).ToArray(),
                    LabelsRotation = 15,
                }
            };
        }

        public async Task LoadFavouriteStatus()
        {
            if (!HasHistory)
            {
                IsFavourite = false;
                return;
            }

            IsFavourite = await favoriteExerciseService.IsFavoriteAsync(Exercise.Id);
        }

        public async Task CheckIfDeletable()
        {
            Deletable = await exerciseService.IsDeletable(Exercise.Id);
            OnPropertyChanged(nameof(ShowDeleteButton));
        }

        public void SetExercise(Exercise exercise)
        {
            Exercise = exercise;
            IsEditingName = false;
            WasRenamed = false;
        }

        [RelayCommand]
        async Task Delete()
        {
            if (Busy) return;

            Busy = true;

            try
            {
                await exerciseService.DeleteAsync(Exercise.Id); 
                WasDeleted = true;
                closedSource.TrySetResult();
                await Shell.Current.Navigation.PopModalAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Couldn't delete", $"{Exercise.Name} couldn't be deleted. Try again.", "OK");
            }
            finally
            {
                Busy = false;
            }
        }

        public void CompleteIfNotAlready()
            => closedSource.TrySetResult();


        [RelayCommand]
        async Task ToggleFavourite()
        {
            if (!CanFavourite) return;

            if (IsFavourite)
            {
                favoriteExerciseService.ClearFavorite();
                IsFavourite = false;
            }
            else
            {
                await favoriteExerciseService.SetFavoriteAsync(Exercise);
                IsFavourite = true;
            }
        }

        [RelayCommand]
        void StartRename()
        {
            nameBeforeEdit = Exercise.Name;
            IsEditingName = true;
        }

        [RelayCommand]
        void CancelRename()
        {
            Exercise.Name = nameBeforeEdit;
            IsEditingName = false;
        }

        [RelayCommand]
        async Task SaveRename()
        {
            if (Busy) return;

            if (string.IsNullOrWhiteSpace(Exercise.Name))
            {
                Exercise.Name = nameBeforeEdit;
                IsEditingName = false;
                return;
            }

            Busy = true;

            try
            {
                await exerciseService.UpdateNameAsync(Exercise.Id, Exercise.Name.Trim());
                WasRenamed = true;
                IsEditingName = false;
                WeakReferenceMessenger.Default.Send(new ExerciseUpdatedMessage(Exercise));
            }
            catch (Exception ex)
            {
                Exercise.Name = nameBeforeEdit;
                await Shell.Current.DisplayAlertAsync("Couldn't rename", $"The exercise couldn't be renamed. Try again.", "OK");
            }
            finally
            {
                Busy = false;
            }
        }
    }
}