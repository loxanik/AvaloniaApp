using System.Collections.Generic;
using System.Linq;

namespace Shop.DTOs;

public class CartDTO
{
    public List<CartItemDTO> Items { get; set; } = [];
    public int ItemsCount => Items.Sum(i => i.Quantity);
    public decimal Total => Items.Sum(i => i.LineTotal);
}

