using System.Collections.Generic;

namespace Shop.Entities;

public partial class Shop
{
    public int Id { get; set; }

    public int AddressId { get; set; }

    public virtual Address Address { get; set; } = null!;

    public virtual ICollection<ShopProduct> ShopProducts { get; set; } = new List<ShopProduct>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
