using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shop.DTOs;
using Shop.Interfaces;
using Shop.Utils;

namespace Shop.ViewModels;

public partial class UserManagementControlViewModel : ViewModelBase
{
    private readonly IUserService _userService;

    [ObservableProperty]
    private ObservableCollection<UserManagementDTO> _users = [];

    [ObservableProperty]
    private ObservableCollection<RoleOptionDTO> _roles = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private int? _selectedRoleId;

    [ObservableProperty]
    private RoleOptionDTO? _selectedRole;

    [ObservableProperty]
    private bool? _selectedDismissedFilter;

    
    [ObservableProperty]
    private string _selectedSortBy = "Login";

    [ObservableProperty]
    private bool _sortAscending = true;

    public ObservableCollection<string> SortOptions { get; } = new()
    {
        "Логин",
        "Полное имя", 
        "Роль",
        "Email",
        "Статус"
    };

    [ObservableProperty]
    private bool _isEmpty = true;

    [ObservableProperty]
    private string _actionInfo = string.Empty;

    [ObservableProperty]
    private UserManagementDTO? _editingUser;

    public UserManagementControlViewModel(IUserService userService)
    {
        _userService = userService;
        _ = LoadRolesAsync();
        _ = LoadUsersAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadUsersAsync();
    }

    [RelayCommand]
    private async Task ApplyFiltersAsync()
    {
        await LoadUsersAsync();
    }

    [RelayCommand]
    private async Task FilterAllUsersAsync()
    {
        SelectedDismissedFilter = null;
        await LoadUsersAsync();
    }

    [RelayCommand]
    private async Task FilterActiveUsersAsync()
    {
        SelectedDismissedFilter = false;
        await LoadUsersAsync();
    }

    [RelayCommand]
    private async Task FilterDismissedUsersAsync()
    {
        SelectedDismissedFilter = true;
        await LoadUsersAsync();
    }

    [RelayCommand]
    private async Task ClearFiltersAsync()
    {
        SearchText = string.Empty;
        SelectedRoleId = null;
        SelectedRole = null;
        SelectedDismissedFilter = null;
        await LoadUsersAsync();
    }

    [RelayCommand]
    private void SetEditingUser(UserManagementDTO? user)
    {
        EditingUser = user;
    }

    [RelayCommand]
    private async Task UpdateUserRoleAsync(UserManagementDTO? user)
    {
        if (user == null || user.SelectedRole == null)
            return;

        var success = await _userService.UpdateUserRoleAsync(user.Id, user.SelectedRole.Id);
        ActionInfo = success 
            ? "Роль пользователя успешно обновлена." 
            : "Не удалось обновить роль пользователя.";
        
        await LoadUsersAsync();
    }

    [RelayCommand]
    private async Task DismissUserAsync(object? parameter)
    {
        if (parameter is not int userId)
            return;

        var success = await _userService.DismissUserAsync(userId);
        ActionInfo = success 
            ? "Сотрудник уволен." 
            : "Не удалось уволить сотрудника.";
        
        await LoadUsersAsync();
    }

    [RelayCommand]
    private async Task ReinstateUserAsync(object? parameter)
    {
        if (parameter is not int userId)
            return;

        var success = await _userService.ReinstateUserAsync(userId);
        ActionInfo = success 
            ? "Сотрудник восстановлен." 
            : "Не удалось восстановить сотрудника.";
        
        await LoadUsersAsync();
    }

    [RelayCommand]
    private void ToggleSortOrder()
    {
        SortAscending = !SortAscending;
        _ = LoadUsersAsync();
    }

    partial void OnSelectedSortByChanged(string value)
    {
        _ = LoadUsersAsync();
    }

    private string GetSortByField(string sortBy)
    {
        return sortBy switch
        {
            "Логин" => "Login",
            "Полное имя" => "FullName",
            "Роль" => "Role",
            "Email" => "Email",
            "Статус" => "IsDismissed",
            _ => "Login"
        };
    }

    partial void OnSortAscendingChanged(bool value)
    {
        _ = LoadUsersAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        _ = LoadUsersAsync();
    }

    partial void OnSelectedRoleIdChanged(int? value)
    {
        _ = LoadUsersAsync();
    }

    partial void OnSelectedRoleChanged(RoleOptionDTO? value)
    {
        SelectedRoleId = value?.Id;
        _ = LoadUsersAsync();
    }

    partial void OnSelectedDismissedFilterChanged(bool? value)
    {
        _ = LoadUsersAsync();
    }

    
    private async Task LoadRolesAsync()
    {
        try
        {
            var roles = await _userService.GetRolesAsync();
            Roles = new ObservableCollection<RoleOptionDTO>(roles);
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, "Load roles viewmodel error");
        }
    }

    private async Task LoadUsersAsync()
    {
        try
        {
            var users = await _userService.GetUsersAsync(
                search: string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,
                roleId: SelectedRoleId,
                isDismissed: SelectedDismissedFilter,
                sortBy: GetSortByField(SelectedSortBy),
                ascending: SortAscending);

            var usersWithSelectedRole = users.Select(u => new UserManagementDTO
            {
                Id = u.Id,
                Login = u.Login,
                FullName = u.FullName,
                RoleName = u.RoleName,
                RoleId = u.RoleId,
                SelectedRole = Roles.FirstOrDefault(r => r.Id == u.RoleId),
                IsDismissed = u.IsDismissed,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber
            });

            Users = new ObservableCollection<UserManagementDTO>(usersWithSelectedRole);
            IsEmpty = !Users.Any();
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, "Load users viewmodel error");
        }
    }
}
