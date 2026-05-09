using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Shop.Converters
{
    public class TrueToBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is bool boolValue && boolValue == true;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue && !boolValue)
                return true;
            return value;
        }
    }
}
