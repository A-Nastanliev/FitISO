namespace FitISO.Maui.Models
{
    public class AccentTheme
    {
        public string Name { get; }

        public Color Swatch { get; }

        public Color ChartAccentColor { get; }
        public Color ChartGridColor { get; }
        public Color ChartBackgroundColor { get; }

        public ResourceDictionary Theme { get; }

        public AccentTheme(string name, ResourceDictionary resources)
        {
            Name = name;
            Theme = resources;
            Swatch = (Color)resources["Gray500"];
            ChartAccentColor = (Color)resources["ChartAccentColor"];
            ChartGridColor = (Color)resources["Gray400"];
            ChartBackgroundColor = (Color)resources["Gray950"];
        }
    }
}
