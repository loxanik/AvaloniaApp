using System;
using System.Collections.Generic;

namespace Shop.Entities;

public partial class User
{
    public int Id { get; set; }

    public string Login { get; set; } = null!;

    public string Password { get; set; } = null!;

    public int RoleId { get; set; }

    public bool IsDismissed { get; set; }

    public virtual Cart? Cart { get; set; }

    public virtual ICollection<Order> OrderClients { get; set; } = new List<Order>();

    public virtual ICollection<Order> OrderEmployees { get; set; } = new List<Order>();

    public virtual ICollection<PersonalInfo> PersonalInfos { get; set; } = new List<PersonalInfo>();

    public virtual Role Role { get; set; } = null!;
}
