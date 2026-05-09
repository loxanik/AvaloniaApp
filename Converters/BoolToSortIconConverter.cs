using System;
using Avalonia.Data.Converters;
using Material.Icons;

namespace Shop.Converters;

public class BoolToSortIconConverter : IValueConverter
{
    public static readonly BoolToSortIconConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool ascending)
        {
            return ascending ? MaterialIconKind.SortAscending : MaterialIconKind.SortDescending;
        }
        return MaterialIconKind.SortAscending;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
