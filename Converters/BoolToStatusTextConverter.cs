using System;
using Avalonia.Data.Converters;

namespace Shop.Converters;

public class BoolToStatusTextConverter : IValueConverter
{
    public static readonly BoolToStatusTextConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool isDismissed)
        {
            return isDismissed ? "Уволен" : "Активен";
        }
        return "Неизвестно";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
