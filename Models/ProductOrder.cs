using System;
using System.Collections.Generic;

namespace Shop.Models;

public partial class ProductOrder
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int CostId { get; set; }

    public int ShopProductId { get; set; }

    public int Count { get; set; }

    public virtual HistoryCost Cost { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;

    public virtual ShopProduct ShopProduct { get; set; } = null!;
}
