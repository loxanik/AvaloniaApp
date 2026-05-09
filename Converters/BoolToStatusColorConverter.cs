using System;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Shop.Converters;

public class BoolToStatusColorConverter : IValueConverter
{
    public static readonly BoolToStatusColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool isDismissed)
        {
            return isDismissed ? Brushes.LightCoral : Brushes.LightGreen;
        }
        return Brushes.LightGray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
