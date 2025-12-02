using Avalonia.Media.Imaging;

namespace Shop.DTOs;

public class ProductPreviewDTO
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Producer { get; set; }
    public decimal Price { get; set; }
    public string Category { get; set; }
    public bool IsDeleted { get; set; }
    public byte[]? Image { get; set; }
    public Bitmap? DisplayImage { get; set; }
}