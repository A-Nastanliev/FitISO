using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FitISO.Maui.Messages;
using FitISO.Maui.Models;
using FitISO.Maui.Resources.Styles.AccentThemes;
using FitISO.Maui.Services;
using Microsoft.Data.Sqlite;
using System.Collections.ObjectModel;

namespace FitISO.Maui.ViewModels
{
    public partial class SettingsPageViewModel : ObservableObject, IRecipient<AutoBackupCompletedMessage>, IRecipient<RestStopwatchEnabledChangedMessage>,
        IRecipient<ProgressCardEnabledChangedMessage>
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        bool isBusy;

        public bool IsNotBusy => !IsBusy;
        public ObservableCollection<AccentTheme> AccentThemes { get; } = new()
        {
            new AccentTheme(nameof(Default), new Default()),
            new AccentTheme(nameof(Slate), new Slate()),
            new AccentTheme(nameof(DarkRed), new DarkRed()),
            new AccentTheme(nameof(Rust), new Rust()),
            new AccentTheme(nameof(Espresso), new Espresso()),
            new AccentTheme(nameof(Amber), new Amber()),
            new AccentTheme(nameof(Olive), new Olive()),
            new AccentTheme(nameof(Forest), new Forest()),
            new AccentTheme(nameof(DeepTeal), new DeepTeal()),
            new AccentTheme(nameof(DarkBlue), new DarkBlue()),
            new AccentTheme(nameof(Ink), new Ink()),
            new AccentTheme(nameof(Midnight), new Midnight()),
            new AccentTheme(nameof(Plum), new Plum()),
            new AccentTheme(nameof(Mauve), new Mauve()),
            new AccentTheme(nameof(Wine), new Wine())
        };

        [ObservableProperty]
        AccentTheme selectedAccentTheme;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasLastBackup))]
        DateTime? lastBackupUtc;

        [ObservableProperty]
        bool autoSaveEnabled;

        [ObservableProperty]
        bool autoStartRestOnExerciseFinish;

        [ObservableProperty]
        bool restStopwatchEnabled;

        [ObservableProperty]
        bool progressCardEnabled;

        public bool HasLastBackup => LastBackupUtc is not null;

        readonly WorkoutSettingsService workoutSettingsService;

        readonly AccentThemeService accentThemeService;

        readonly AutoBackupService autoBackupService;

        public SettingsPageViewModel(WorkoutSettingsService workoutSettingsService, AccentThemeService accentThemeService,
            AutoBackupService autoBackupService)
        {
            this.workoutSettingsService = workoutSettingsService;
            this.accentThemeService = accentThemeService;
            this.autoBackupService = autoBackupService;

            WeakReferenceMessenger.Default.RegisterAll(this);

            var savedTheme = accentThemeService.AccentThemeName;
            selectedAccentTheme = AccentThemes.FirstOrDefault(t => t.Name == savedTheme) ?? AccentThemes[0];

            lastBackupUtc = autoBackupService.LastBackupUtc;
            AutoSaveEnabled = autoBackupService.AutoSaveEnabled;
            AutoStartRestOnExerciseFinish = workoutSettingsService.AutoStartRestOnExerciseFinish;
            RestStopwatchEnabled = workoutSettingsService.RestStopwatchEnabled;
            ProgressCardEnabled = workoutSettingsService.ProgressCardEnabled;
        }

        public void Receive(AutoBackupCompletedMessage message) => LastBackupUtc = message.Value;

        partial void OnAutoSaveEnabledChanged(bool value) => autoBackupService.AutoSaveEnabled = value;

        partial void OnAutoStartRestOnExerciseFinishChanged(bool value) => workoutSettingsService.AutoStartRestOnExerciseFinish = value;

        partial void OnRestStopwatchEnabledChanged(bool value) => workoutSettingsService.RestStopwatchEnabled = value;

        partial void OnProgressCardEnabledChanged(bool value) => workoutSettingsService.ProgressCardEnabled = value;

        public void Receive(RestStopwatchEnabledChangedMessage message) => RestStopwatchEnabled = message.Value;

        public void Receive(ProgressCardEnabledChangedMessage message) => ProgressCardEnabled = message.Value;

        partial void OnSelectedAccentThemeChanged(AccentTheme value)
        {
            var existing = Application.Current.Resources.MergedDictionaries.FirstOrDefault(d => d.ContainsKey("Gray100"));
            Application.Current.Resources.MergedDictionaries.Remove(existing);
            Application.Current.Resources.MergedDictionaries.Add(value.Theme);
            accentThemeService.AccentThemeName = value.Name;
        }

        [RelayCommand]
        private async Task ExportDatabaseAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;

                var dbPath = App.DatabasePath;
                if (!File.Exists(dbPath))
                {
                    await Shell.Current.DisplayAlertAsync("Export failed", "No database file was found.", "OK");
                    return;
                }

                SqliteConnection.ClearAllPools();

                var fileName = $"fitiso_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db3";

                using var stream = File.OpenRead(dbPath);
                var result = await FileSaver.Default.SaveAsync(fileName, stream, CancellationToken.None);

                if (result.IsSuccessful)
                {
                    LastBackupUtc = DateTime.UtcNow;
                    autoBackupService.LastBackupUtc = LastBackupUtc;

                    _ = Toast.Make("Database exported").Show();
                }
                else if (result.Exception is not null)
                {
                    if (result.Exception is OperationCanceledException)
                        return;

                    await Shell.Current.DisplayAlertAsync("Export failed", result.Exception.Message, "OK");
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Export failed", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task ImportDatabaseAsync()
        {
            if (IsBusy)
                return;

            var confirmed = await Shell.Current.DisplayAlertAsync(
                "Import database",
                "This will replace all current data with the selected backup. The app will close automatically once the import finishes. Continue?",
                "Continue",
                "Cancel");

            if (!confirmed)
                return;

            try
            {
                var pickResult = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Select a FitISO backup file"
                });

                if (pickResult is null)
                    return;

                IsBusy = true;

                var dbPath = App.DatabasePath;

                SqliteConnection.ClearAllPools();

                foreach (var suffix in new[] { "-wal", "-shm" })
                {
                    var sidecarPath = dbPath + suffix;
                    if (File.Exists(sidecarPath))
                        File.Delete(sidecarPath);
                }

                await using (var sourceStream = await pickResult.OpenReadAsync())
                await using (var destinationStream = File.Create(dbPath))
                {
                    await sourceStream.CopyToAsync(destinationStream);
                }

                LastBackupUtc = null;
                autoBackupService.LastBackupUtc = LastBackupUtc;

                _ = Toast.Make($"Database imported").Show();
                WeakReferenceMessenger.Default.Send(new DbImportedMessage());
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Import failed", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}