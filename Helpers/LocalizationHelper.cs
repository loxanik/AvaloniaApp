using System.Collections.Generic;
using Shop.Interfaces;

namespace Shop.Helpers;

public class LocalizationHelper : ILocalizationHelper
{
    private readonly Dictionary<string, string> _localizedRoles = new()
    {
        ["client"] = "Клиент",
        ["manager"] = "Менеджер",
        ["admin"] = "Администратор",
    };

    public string LocalizateRole(string? role)
    {
        return _localizedRoles.GetValueOrDefault(role?.ToLower() ?? "", "неизвестно");
    }
}