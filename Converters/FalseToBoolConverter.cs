using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Shop.Converters
{
    public class FalseToBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is bool boolValue && boolValue == false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue && !boolValue)
                return false;
            return value;
        }
    }
}
