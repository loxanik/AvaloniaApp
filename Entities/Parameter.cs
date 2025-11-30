using System;
using System.Collections.Generic;

namespace Shop.Entities;

public partial class Parameter
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string Name { get; set; } = null!;

    public string Value { get; set; } = null!;

    public int UnitId { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual UnitOfMeasurement Unit { get; set; } = null!;
}
