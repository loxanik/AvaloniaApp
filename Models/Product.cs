using System;
using System.Collections.Generic;

namespace Shop.Models;

public partial class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public int CategoryId { get; set; }

    public int ProducerId { get; set; }

    public int? ImageId { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual Image? Image { get; set; }

    public virtual ICollection<Parameter> Parameters { get; set; } = new List<Parameter>();

    public virtual Producer Producer { get; set; } = null!;

    public virtual ICollection<ShopProduct> ShopProducts { get; set; } = new List<ShopProduct>();
}
