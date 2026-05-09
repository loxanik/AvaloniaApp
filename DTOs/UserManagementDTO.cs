namespace Shop.DTOs;

public class UserManagementDTO
{
    public int Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public RoleOptionDTO? SelectedRole { get; set; }
    public bool IsDismissed { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
}

public class RoleOptionDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
