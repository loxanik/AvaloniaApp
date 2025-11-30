using System.Collections.Generic;

namespace Shop.Entities;

public partial class Address
{
    public int Id { get; set; }

    public string City { get; set; } = null!;

    public string Street { get; set; } = null!;

    public string Construction { get; set; } = null!;

    public virtual ICollection<Entities.Shop> Shops { get; set; } = new List<Entities.Shop>();
}
