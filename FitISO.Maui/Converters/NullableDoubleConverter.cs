using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FitISO.Maui.Converters
{
    public class NullableDoubleConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double d)
                return d.ToString(culture);

            return string.Empty;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string s)
            {
                if (string.IsNullOrWhiteSpace(s))
                    return null;

                if (double.TryParse(s, NumberStyles.Any, culture, out var d))
                    return d;
            }

            return null;
        }
    }
}
