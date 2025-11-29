using System;
using System.Collections.Generic;

namespace Shop.Models;

public partial class Image
{
    public int Id { get; set; }

    public byte[]? Image1 { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
