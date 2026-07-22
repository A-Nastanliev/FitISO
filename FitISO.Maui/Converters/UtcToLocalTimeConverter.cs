using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FitISO.Maui.Converters
{
    public class UtcToLocalTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not DateTime dt) return string.Empty;

            if (dt.Kind != DateTimeKind.Utc)
                dt = DateTime.SpecifyKind(dt, DateTimeKind.Utc);

            var local = dt.ToLocalTime();
            return local.ToString("HH:mm", culture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
