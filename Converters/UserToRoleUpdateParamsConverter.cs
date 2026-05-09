using System;
using Avalonia.Data.Converters;
using Shop.DTOs;

namespace Shop.Converters;

public class UserToRoleUpdateParamsConverter : IValueConverter
{
    public static readonly UserToRoleUpdateParamsConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is UserManagementDTO user)
        {
            return new object[] { user.Id, user.RoleId };
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
