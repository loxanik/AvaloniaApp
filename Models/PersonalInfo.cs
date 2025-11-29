using System;
using System.Collections.Generic;

namespace Shop.Models;

public partial class PersonalInfo
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Surname { get; set; } = null!;

    public string? Patronymic { get; set; }

    public string PhoneNumber { get; set; } = null!;

    public string Email { get; set; } = null!;

    public int UserId { get; set; }

    public virtual User User { get; set; } = null!;
}
