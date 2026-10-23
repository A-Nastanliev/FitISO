using System.ComponentModel;
using FitISO.Maui.ViewModels;

namespace FitISO.Maui.Views;

public partial class ActiveWorkoutPage : ContentPage
{
    readonly ActiveWorkoutViewModel viewModel;
    readonly TrainTogetherViewModel? trainTogether;

    public ActiveWorkoutPage(ActiveWorkoutViewModel activeWorkoutViewModel)
    {
        InitializeComponent();
        BindingContext = activeWorkoutViewModel;
        viewModel = activeWorkoutViewModel;

        trainTogether = IPlatformApplication.Current?.Services.GetService<TrainTogetherViewModel>();
        if (trainTogether is not null)
        {
            TrainTogetherCard.BindingContext = trainTogether;
            PartnerList.BindingContext = trainTogether;
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        viewModel.EnsureTimerRunning();
        ActiveWorkoutState.Instance.IsActiveWorkoutPageVisible = true;

        if (trainTogether is not null)
        {
            trainTogether.PropertyChanged += OnTrainTogetherPropertyChanged;
            ApplyTrainTogether();
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        viewModel.Stop();
        ActiveWorkoutState.Instance.IsActiveWorkoutPageVisible = false;

        if (trainTogether is not null)
            trainTogether.PropertyChanged -= OnTrainTogetherPropertyChanged;
    }

    void OnTrainTogetherPropertyChanged(object? sender, PropertyChangedEventArgs e) => ApplyTrainTogether();

    void ApplyTrainTogether()
    {
        if (trainTogether is null) return;

        TrainTogetherButton.IsVisible = trainTogether.IsOff;
        TrainTogetherCard.IsVisible = trainTogether.IsCardVisible;
        viewModel.HasExtraTopCard = trainTogether.IsCardVisible;
        viewModel.SetPartner(trainTogether.PartnerSnapshot, trainTogether.PartnerTimeToLocal);
        viewModel.IsViewingPartner = trainTogether.ViewingPartner;
    }

    async void OnTrainTogetherClicked(object? sender, EventArgs e)
    {
        if (trainTogether is not null)
            await trainTogether.StartCommand.ExecuteAsync(null);
    }
}