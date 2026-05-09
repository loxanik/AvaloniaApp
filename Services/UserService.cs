using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Shop.DTOs;
using Shop.Entities;
using Shop.Helpers;
using Shop.Interfaces;
using Shop.Utils;

namespace Shop.Services;

public class UserService(ShopContext shopContext, IUserContext userContext, ILocalizationHelper localizationHelper) : IUserService
{
    private readonly ShopContext _shopContext = shopContext;
    private readonly IUserContext _userContext = userContext;
    private readonly ILocalizationHelper _localizationHelper = localizationHelper;

    public async Task<List<UserManagementDTO>> GetAllUsersAsync()
    {
        return await GetUsersAsync();
    }

    public async Task<List<UserManagementDTO>> GetUsersAsync(string? search = null, int? roleId = null, bool? isDismissed = null, string sortBy = "Login", bool ascending = true)
    {
        try
        {
            var query = _shopContext.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .Include(u => u.PersonalInfos)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u => u.Login.Contains(search) 
                    || u.PersonalInfos.Any(pi => pi.Name.Contains(search) || pi.Surname.Contains(search)));
            }

            if (roleId.HasValue)
            {
                query = query.Where(u => u.RoleId == roleId.Value);
            }

            if (isDismissed.HasValue)
            {
                query = query.Where(u => u.IsDismissed == isDismissed.Value);
            }

            // Apply sorting
            query = sortBy.ToLowerInvariant() switch
            {
                "login" => ascending ? query.OrderBy(u => u.Login) : query.OrderByDescending(u => u.Login),
                "fullname" => ascending ? query.OrderBy(u => u.PersonalInfos.FirstOrDefault().Name).ThenBy(u => u.PersonalInfos.FirstOrDefault().Surname) : query.OrderByDescending(u => u.PersonalInfos.FirstOrDefault().Name).ThenByDescending(u => u.PersonalInfos.FirstOrDefault().Surname),
                "role" => ascending ? query.OrderBy(u => u.Role.Name) : query.OrderByDescending(u => u.Role.Name),
                "email" => ascending ? query.OrderBy(u => u.PersonalInfos.FirstOrDefault().Email) : query.OrderByDescending(u => u.PersonalInfos.FirstOrDefault().Email),
                "isdismissed" => ascending ? query.OrderBy(u => u.IsDismissed) : query.OrderByDescending(u => u.IsDismissed),
                _ => ascending ? query.OrderBy(u => u.Login) : query.OrderByDescending(u => u.Login)
            };

            var users = await query.ToListAsync();

            return users.Select(u => new UserManagementDTO
            {
                Id = u.Id,
                Login = u.Login,
                FullName = u.PersonalInfos.FirstOrDefault() != null 
                    ? $"{u.PersonalInfos.First().Name} {u.PersonalInfos.First().Surname}"
                    : u.Login,
                RoleName = _localizationHelper.LocalizateRole(u.Role?.Name) ?? string.Empty,
                RoleId = u.RoleId,
                IsDismissed = u.IsDismissed,
                Email = u.PersonalInfos.FirstOrDefault()?.Email ?? string.Empty,
                PhoneNumber = u.PersonalInfos.FirstOrDefault()?.PhoneNumber ?? string.Empty
            }).ToList();
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, "Get users error");
            return [];
        }
    }

    public async Task<List<RoleOptionDTO>> GetRolesAsync()
    {
        try
        {
            return await _shopContext.Roles
                .AsNoTracking()
                .OrderBy(r => r.Name)
                .Select(r => new RoleOptionDTO
                {
                    Id = r.Id,
                    Name = r.Name == "admin" ? "Администратор" : r.Name == "manager" ? "Менеджер" : r.Name == "client" ? "Клиент" : r.Name
                })
                .ToListAsync();
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, "Get roles error");
            return [];
        }
    }

    public async Task<bool> UpdateUserRoleAsync(int userId, int roleId)
    {
        try
        {
            if (!IsAdmin())
                return false;

            var user = await _shopContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return false;

            var roleExists = await _shopContext.Roles.AnyAsync(r => r.Id == roleId);
            if (!roleExists)
                return false;

            user.RoleId = roleId;
            await _shopContext.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, $"Update user role error: userId={userId}, roleId={roleId}");
            return false;
        }
    }

    public async Task<bool> DismissUserAsync(int userId)
    {
        try
        {
            if (!IsAdmin())
                return false;

            var user = await _shopContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return false;

            user.IsDismissed = true;
            await _shopContext.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, $"Dismiss user error: userId={userId}");
            return false;
        }
    }

    public async Task<bool> ReinstateUserAsync(int userId)
    {
        try
        {
            if (!IsAdmin())
                return false;

            var user = await _shopContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return false;

            user.IsDismissed = false;
            await _shopContext.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, $"Reinstate user error: userId={userId}");
            return false;
        }
    }

    private bool IsAdmin()
    {
        var role = _userContext.CurrentUser?.Role?.Name;
        return role == "admin";
    }
}
