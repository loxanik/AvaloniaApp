using System.Collections.Generic;

namespace Shop.Entities;

public partial class Country
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Producer> Producers { get; set; } = new List<Producer>();
}
