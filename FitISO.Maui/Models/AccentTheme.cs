namespace FitISO.Maui.Models
{
    public class AccentTheme
    {
        public string Name { get; }

        public Color Swatch { get; }

        public ResourceDictionary Theme { get; }

        public AccentTheme(string name, ResourceDictionary resources)
        {
            Name = name;
            Theme = resources;
            Swatch = (Color)resources["Gray500"];
        }
    }
}
