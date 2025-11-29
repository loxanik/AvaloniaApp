using System;
using System.Collections.Generic;

namespace Shop.Models;

public partial class UnitOfMeasurement
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Parameter> Parameters { get; set; } = new List<Parameter>();
}
