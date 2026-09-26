using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using FitISO.Maui.Messages;
using FitISO.Maui.Models;
using FitISO.Maui.Resources.Styles.AccentThemes;
using FitISO.Maui.Services;
using System.Collections.ObjectModel;

namespace FitISO.Maui.ViewModels
{
    public partial class AccentThemeSettingsViewModel : ObservableObject, IRecipient<DynamicAccentColorsChangedMessage>
    {
        public ObservableCollection<AccentTheme> AccentThemes { get; } = BuildAccentThemes();

        static ObservableCollection<AccentTheme> BuildAccentThemes()
        {
            var themes = new ObservableCollection<AccentTheme>
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

            if (DynamicAccentTheme.IsAvailable)
                themes.Add(new AccentTheme("Dynamic", new DynamicAccentTheme()));

            return themes;
        }

        [ObservableProperty]
        AccentTheme selectedAccentTheme;

        readonly AccentThemeService accentThemeService;

        public AccentThemeSettingsViewModel(AccentThemeService accentThemeService)
        {
            this.accentThemeService = accentThemeService;

            WeakReferenceMessenger.Default.RegisterAll(this);

            var savedTheme = accentThemeService.AccentThemeName;
            selectedAccentTheme = AccentThemes.FirstOrDefault(t => t.Name == savedTheme) ?? AccentThemes[0];
        }

        partial void OnSelectedAccentThemeChanged(AccentTheme value)
        {
            var existing = Application.Current.Resources.MergedDictionaries.FirstOrDefault(d => d.ContainsKey("Gray100"));
            Application.Current.Resources.MergedDictionaries.Remove(existing);
            Application.Current.Resources.MergedDictionaries.Add(value.Theme);
            accentThemeService.AccentThemeName = value.Name;
        }

        public void Receive(DynamicAccentColorsChangedMessage message)
        {
            if (accentThemeService.AccentThemeName != "Dynamic")
                return;

            var dynamicEntry = AccentThemes.FirstOrDefault(t => t.Name == "Dynamic");
            if (dynamicEntry is null)
                return;

            MainThread.BeginInvokeOnMainThread(() => dynamicEntry.RefreshFromTheme(new DynamicAccentTheme()));
        }
    }
}
