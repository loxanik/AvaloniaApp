using System.Collections.Generic;
using System.Linq;

namespace Shop.DTOs;

public class CartDTO
{
    public List<CartItemDTO> Items { get; set; } = [];
    public int ItemsCount => Items.Sum(i => i.Quantity);
    public decimal Total => Items.Sum(i => i.LineTotal);
    public decimal DiscountPercentage => Total >= 10000 ? 5 : 0;
    public decimal DiscountAmount => Total * DiscountPercentage / 100;
    public decimal DiscountedTotal => Total - DiscountAmount;
}

