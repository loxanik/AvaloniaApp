using System;
using System.Collections.Generic;

namespace Shop.Models;

public partial class Order
{
    public int Id { get; set; }

    public DateOnly Date { get; set; }

    public int ClientId { get; set; }

    public int StatusId { get; set; }

    public virtual User Client { get; set; } = null!;

    public virtual ICollection<ProductOrder> ProductOrders { get; set; } = new List<ProductOrder>();

    public virtual Status Status { get; set; } = null!;
}
