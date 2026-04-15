using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Shop.DTOs;

public class OrderSummaryDTO
{
    public int OrderId { get; set; }
    public DateOnly Date { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string StatusDisplayName { get; set; } = string.Empty;
    public string ClientLogin { get; set; } = string.Empty;
    public List<OrderItemDTO> Items { get; set; } = [];
    public string DateDisplay => Date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
    public int ItemsCount => Items.Sum(i => i.Quantity);
    public decimal Total => Items.Sum(i => i.LineTotal);
}
