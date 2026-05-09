using System;
using System.Linq;
using Avalonia.Data.Converters;
using Shop.DTOs;

namespace Shop.Converters;

public class RoleIdToRoleConverter : IValueConverter
{
    public static readonly RoleIdToRoleConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is int roleId && parameter is System.Collections.Generic.IEnumerable<RoleOptionDTO> roles)
        {
            return roles.FirstOrDefault(r => r.Id == roleId);
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is RoleOptionDTO role)
        {
            return role.Id;
        }
        return null;
    }
}
