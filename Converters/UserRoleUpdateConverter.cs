using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Shop.DTOs;

namespace Shop.Converters;

public class UserRoleUpdateConverter : IMultiValueConverter
{
    public static readonly UserRoleUpdateConverter Instance = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (values.Count >= 2 && values[0] is UserManagementDTO user && values[1] is int newRoleId)
        {
            return new object[] { user.Id, newRoleId };
        }
        return null;
    }

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
