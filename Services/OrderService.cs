using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Shop.DTOs;
using Shop.Entities;
using Shop.Interfaces;
using Shop.Utils;

namespace Shop.Services;

public class OrderService(ShopContext shopContext, IUserContext userContext) : IOrderService
{
    private const string PendingStatus = "pending";
    private const string PaidStatus = "paid";
    private const string CancelledStatus = "cancelled";

    private readonly ShopContext _shopContext = shopContext;
    private readonly IUserContext _userContext = userContext;

    public async Task<bool> CreateOrderFromMyCartAsync()
    {
        await using var transaction = await _shopContext.Database.BeginTransactionAsync();
        try
        {
            var userId = _userContext.CurrentUser?.Id;
            if (userId is null)
                return false;

            var cart = await _shopContext.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId.Value);

            if (cart == null || cart.CartItems.Count == 0)
                return false;

            var statusId = await EnsureStatusAsync(PendingStatus);
            if (statusId == 0)
                return false;

            var cartItems = cart.CartItems.ToList();
            var productIds = cartItems.Select(i => i.ProductId).Distinct().ToList();

            var shopProducts = await _shopContext.ShopProducts
                .Where(sp => productIds.Contains(sp.ProductId))
                .ToListAsync();

            var shopProductByProductId = shopProducts
                .GroupBy(sp => sp.ProductId)
                .ToDictionary(g => g.Key, g => g.First());

            foreach (var item in cartItems)
            {
                if (!shopProductByProductId.TryGetValue(item.ProductId, out var shopProduct))
                    return false;

                if (shopProduct.Count < item.Quantity)
                    return false;
            }

            var shopProductIds = shopProducts.Select(sp => sp.Id).ToList();
            var latestCosts = await _shopContext.HistoryCosts
                .Where(h => shopProductIds.Contains(h.ShopProductId))
                .GroupBy(h => h.ShopProductId)
                .Select(g => g.OrderByDescending(h => h.Id).First())
                .ToListAsync();

            var latestCostByShopProductId = latestCosts.ToDictionary(c => c.ShopProductId, c => c);

            foreach (var shopProduct in shopProducts)
            {
                if (latestCostByShopProductId.ContainsKey(shopProduct.Id))
                    continue;

                var initialCost = new HistoryCost
                {
                    ShopProductId = shopProduct.Id,
                    OldCost = 0m,
                    NewCost = 0m
                };
                _shopContext.HistoryCosts.Add(initialCost);
                await _shopContext.SaveChangesAsync();
                latestCostByShopProductId[shopProduct.Id] = initialCost;
            }

            var order = new Order
            {
                ClientId = userId.Value,
                Date = DateOnly.FromDateTime(DateTime.Today),
                StatusId = statusId
            };
            _shopContext.Orders.Add(order);
            await _shopContext.SaveChangesAsync();

            foreach (var item in cartItems)
            {
                var shopProduct = shopProductByProductId[item.ProductId];
                var latestCost = latestCostByShopProductId[shopProduct.Id];

                var productOrder = new ProductOrder
                {
                    OrderId = order.Id,
                    CostId = latestCost.Id,
                    ShopProductId = shopProduct.Id,
                    Count = item.Quantity
                };
                _shopContext.ProductOrders.Add(productOrder);

                shopProduct.Count -= item.Quantity;
            }

            _shopContext.CartItems.RemoveRange(cartItems);
            cart.UpdatedAt = DateTime.UtcNow;

            await _shopContext.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, "Create order from cart error");
            return false;
        }
    }

    public async Task<List<OrderSummaryDTO>> GetMyOrdersAsync()
    {
        try
        {
            var userId = _userContext.CurrentUser?.Id;
            if (userId is null)
                return [];

            var orders = await _shopContext.Orders
                .AsNoTracking()
                .Where(o => o.ClientId == userId.Value)
                .Include(o => o.Status)
                .Include(o => o.Client)
                .Include(o => o.ProductOrders)
                    .ThenInclude(po => po.Cost)
                .Include(o => o.ProductOrders)
                    .ThenInclude(po => po.ShopProduct)
                        .ThenInclude(sp => sp.Product)
                .OrderByDescending(o => o.Date)
                .ThenByDescending(o => o.Id)
                .ToListAsync();

            return orders.Select(MapToSummaryDto).ToList();
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, "Get my orders error");
            return [];
        }
    }

    public async Task<List<OrderSummaryDTO>> GetAllOrdersAsync()
    {
        try
        {
            if (!IsManagerOrAdmin())
                return [];

            var orders = await _shopContext.Orders
                .AsNoTracking()
                .Include(o => o.Status)
                .Include(o => o.Client)
                .Include(o => o.ProductOrders)
                    .ThenInclude(po => po.Cost)
                .Include(o => o.ProductOrders)
                    .ThenInclude(po => po.ShopProduct)
                        .ThenInclude(sp => sp.Product)
                .OrderByDescending(o => o.Date)
                .ThenByDescending(o => o.Id)
                .ToListAsync();

            return orders.Select(MapToSummaryDto).ToList();
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, "Get all orders error");
            return [];
        }
    }

    public Task<bool> ConfirmPaymentAsync(int orderId)
    {
        return ChangeOrderStatusAsync(orderId, PaidStatus);
    }

    public Task<bool> CancelOrderAsync(int orderId)
    {
        return ChangeOrderStatusAsync(orderId, CancelledStatus);
    }

    private async Task<bool> ChangeOrderStatusAsync(int orderId, string statusName)
    {
        try
        {
            if (!IsManagerOrAdmin())
                return false;

            var order = await _shopContext.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null)
                return false;

            var statusId = await EnsureStatusAsync(statusName);
            if (statusId == 0)
                return false;

            order.StatusId = statusId;
            await _shopContext.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, $"Change order status error: orderId={orderId}, status={statusName}");
            return false;
        }
    }

    private bool IsManagerOrAdmin()
    {
        var role = _userContext.CurrentUser?.Role?.Name;
        return role is "manager" or "admin";
    }

    private async Task<int> EnsureStatusAsync(string statusName)
    {
        var status = await _shopContext.Statuses
            .FirstOrDefaultAsync(s => EF.Functions.ILike(s.Name, statusName));
        if (status != null)
            return status.Id;

        var created = new Status { Name = statusName };
        _shopContext.Statuses.Add(created);
        await _shopContext.SaveChangesAsync();
        return created.Id;
    }

    private static OrderSummaryDTO MapToSummaryDto(Order order)
    {
        var statusName = order.Status?.Name ?? string.Empty;
        return new OrderSummaryDTO
        {
            OrderId = order.Id,
            Date = order.Date,
            StatusName = statusName,
            StatusDisplayName = LocalizeStatus(statusName),
            ClientLogin = order.Client?.Login ?? string.Empty,
            Items = order.ProductOrders.Select(po => new OrderItemDTO
            {
                ProductName = po.ShopProduct?.Product?.Name ?? "Товар",
                Quantity = po.Count,
                UnitPrice = po.Cost?.NewCost ?? 0m
            }).ToList()
        };
    }

    private static string LocalizeStatus(string statusName)
    {
        return statusName.ToLower(CultureInfo.InvariantCulture) switch
        {
            PendingStatus => "Ожидает подтверждения",
            PaidStatus => "Оплачен",
            CancelledStatus => "Отменен",
            _ => statusName
        };
    }
}
