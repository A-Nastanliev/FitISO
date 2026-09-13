using System.Globalization;

namespace FitISO.Maui.Converters
{
    public class VisibilityAwareHeightConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] is not bool visible || !visible)
                return 0d;

            var height = values[1] is double h && h > 0 ? h : 0d;
            var extraMargin = ParseMargin(parameter, culture);

            return height + extraMargin;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();

        static double ParseMargin(object parameter, CultureInfo culture)
        {
            return parameter switch
            {
                double d => d,
                int i => i,
                string s when double.TryParse(s, NumberStyles.Float, culture, out var parsed) => parsed,
                _ => 0d
            };
        }
    }
}