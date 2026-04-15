using System.Collections.Generic;
using System.Threading.Tasks;
using Shop.DTOs;

namespace Shop.Interfaces;

public interface IOrderService
{
    Task<bool> CreateOrderFromMyCartAsync();
    Task<List<OrderSummaryDTO>> GetMyOrdersAsync();
    Task<List<OrderSummaryDTO>> GetAllOrdersAsync();
    Task<bool> ConfirmPaymentAsync(int orderId);
    Task<bool> CancelOrderAsync(int orderId);
}
