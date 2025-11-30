using System;
using System.Collections.Generic;

namespace Shop.Entities;

public partial class ShopProduct
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public DateOnly DateOfManufacture { get; set; }

    public int Count { get; set; }

    public virtual ICollection<HistoryCost> HistoryCosts { get; set; } = new List<HistoryCost>();

    public virtual Product Product { get; set; } = null!;

    public virtual ICollection<ProductOrder> ProductOrders { get; set; } = new List<ProductOrder>();
}
