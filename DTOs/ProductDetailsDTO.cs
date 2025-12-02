using System.Collections.Generic;
using Avalonia.Media.Imaging;

namespace Shop.DTOs;

public class ProductDetailsDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public decimal Price { get; set; }
    public string Producer { get; set; } = null!;
    public string Country { get; set; } = null!;
    public string Category { get; set; } = null!;
    public bool IsDeleted { get; set; }
    public int Count {get; set;}
    public byte[]? Image { get; set; }
    public Bitmap? DisplayImage { get; set; }
    public List<ParametersDTO>? Parameters { get; set; } = [];
}