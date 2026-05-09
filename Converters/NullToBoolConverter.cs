using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Shop.Converters
{
    public class NullToBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value == null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue && !boolValue)
                return null;
            return value;
        }
    }
}
