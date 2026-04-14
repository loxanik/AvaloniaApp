using Avalonia.Media.Imaging;

namespace Shop.DTOs;

public class CartItemDTO
{
    public int ProductId { get; set; }
    public string Name { get; set; } = null!;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
    public byte[]? Image { get; set; }
    public Bitmap? DisplayImage { get; set; }
    public int AvailableQuantity { get; set; }
    public bool IsInStock => AvailableQuantity > 0;
    public bool CanIncrease => Quantity < AvailableQuantity;
    public bool CanDecrease => Quantity > 1;
}

