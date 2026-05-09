using System;
using Avalonia.Data.Converters;

namespace Shop.Converters;

public class AdminRoleVisibilityConverter : IValueConverter
{
    public static readonly AdminRoleVisibilityConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is string roleName)
        {
            return roleName == "admin";
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
