using System.Collections.Generic;
using System.Threading.Tasks;
using Shop.DTOs;

namespace Shop.Interfaces;

public interface IUserService
{
    Task<List<UserManagementDTO>> GetAllUsersAsync();
    Task<List<UserManagementDTO>> GetUsersAsync(string? search = null, int? roleId = null, bool? isDismissed = null, string sortBy = "Login", bool ascending = true);
    Task<List<RoleOptionDTO>> GetRolesAsync();
    Task<bool> UpdateUserRoleAsync(int userId, int roleId);
    Task<bool> DismissUserAsync(int userId);
    Task<bool> ReinstateUserAsync(int userId);
}
