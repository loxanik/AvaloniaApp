using System.Collections.Generic;

namespace Shop.Entities;

public partial class HistoryCost
{
    public int Id { get; set; }

    public int ShopProductId { get; set; }

    public decimal NewCost { get; set; }

    public decimal OldCost { get; set; }

    public virtual ICollection<ProductOrder> ProductOrders { get; set; } = new List<ProductOrder>();

    public virtual ShopProduct ShopProduct { get; set; } = null!;
}
