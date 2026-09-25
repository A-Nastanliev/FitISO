using CommunityToolkit.Mvvm.ComponentModel;

namespace FitISO.Maui.Models
{
    public partial class AccentTheme : ObservableObject
    {
        public string Name { get; }

        [ObservableProperty]
        Color swatch;
        [ObservableProperty] 
        Color chartAccentColor;
        [ObservableProperty] 
        Color chartGridColor;
        [ObservableProperty] 
        Color chartBackgroundColor;
        [ObservableProperty] 
        Color heatmapRestColor;
        [ObservableProperty] 
        Color heatmapFutureColor;

        public ResourceDictionary Theme { get; private set; }

        public AccentTheme(string name, ResourceDictionary resources)
        {
            Name = name;
            SetFromResources(resources);
        }

        public void RefreshFromTheme(ResourceDictionary resources) => SetFromResources(resources);

        void SetFromResources(ResourceDictionary resources)
        {
            Theme = resources;
            Swatch = (Color)resources["Gray500"];
            ChartAccentColor = (Color)resources["ChartAccentColor"];
            ChartGridColor = (Color)resources["Gray400"];
            ChartBackgroundColor = (Color)resources["Gray950"];
            HeatmapRestColor = (Color)resources["Gray700"];
            HeatmapFutureColor = (Color)resources["Gray900"];
        }
    }
}
