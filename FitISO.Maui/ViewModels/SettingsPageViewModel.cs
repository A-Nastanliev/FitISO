using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FitISO.Maui.Messages;
using Microsoft.Data.Sqlite;
using FitISO.Maui.Models;
using System.Collections.ObjectModel;
using FitISO.Maui.Resources.Styles.AccentThemes;

namespace FitISO.Maui.ViewModels
{
    public partial class SettingsPageViewModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        bool isBusy;

        public bool IsNotBusy => !IsBusy;
        public ObservableCollection<AccentTheme> AccentThemes { get; } = new();

        [ObservableProperty]
        AccentTheme selectedAccentTheme;

        public SettingsPageViewModel()
        {
            AccentThemes.Add(new AccentTheme(nameof(Default), new Default()));
            AccentThemes.Add(new AccentTheme(nameof(DarkBlue), new DarkBlue()));
            AccentThemes.Add(new AccentTheme(nameof(DarkRed), new DarkRed()));
            AccentThemes.Add(new AccentTheme(nameof(Olive), new Olive()));

            var savedTheme = Preferences.Get("accent_theme", nameof(Default));
            selectedAccentTheme = AccentThemes.FirstOrDefault(t => t.Name == savedTheme) ?? AccentThemes[0];
        }

        partial void OnSelectedAccentThemeChanged(AccentTheme value)
        {
            var existing = Application.Current.Resources.MergedDictionaries.FirstOrDefault(d => d.ContainsKey("Gray100"));
            Application.Current.Resources.MergedDictionaries.Remove(existing);
            Application.Current.Resources.MergedDictionaries.Add(value.Theme);
            Preferences.Set("accent_theme", value.Name);
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
                    await Shell.Current.DisplayAlertAsync("Export complete", $"Backup saved to:\n{result.FilePath}", "OK");
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