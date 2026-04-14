using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Shop.DTOs;
using Shop.Entities;
using Shop.Interfaces;
using Shop.Utils;

namespace Shop.Services;

public class CartService(ShopContext shopContext, IUserContext userContext, IImageService imageService) : ICartService
{
    private readonly ShopContext _shopContext = shopContext;
    private readonly IUserContext _userContext = userContext;
    private readonly IImageService _imageService = imageService;

    public async Task<CartDTO?> GetMyCartAsync()
    {
        try
        {
            var userId = _userContext.CurrentUser?.Id;
            if (userId is null) return new CartDTO();

            var cartId = await _shopContext.Carts
                .AsNoTracking()
                .Where(c => c.UserId == userId.Value)
                .Select(c => (int?)c.Id)
                .FirstOrDefaultAsync();

            if (cartId is null) return new CartDTO();

            return await BuildCartDtoAsync(cartId.Value);
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, "Get cart error");
            return null;
        }
    }

    public async Task<CartDTO?> AddToMyCartAsync(int productId, int quantity = 1)
    {
        if (quantity <= 0) return await GetMyCartAsync();

        await using var transaction = await _shopContext.Database.BeginTransactionAsync();
        try
        {
            var userId = _userContext.CurrentUser?.Id;
            if (userId is null) return new CartDTO();

            var cart = await _shopContext.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId.Value);

            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = userId.Value,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _shopContext.Carts.Add(cart);
                await _shopContext.SaveChangesAsync();
            }

            var available = await GetAvailableQuantityAsync(productId);
            if (available <= 0) return await BuildCartDtoAsync(cart.Id);

            var existing = cart.CartItems.FirstOrDefault(i => i.ProductId == productId);
            if (existing == null)
            {
                var toAdd = Math.Min(quantity, available);
                cart.CartItems.Add(new CartItem
                {
                    CartId = cart.Id,
                    ProductId = productId,
                    Quantity = toAdd,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            else
            {
                existing.Quantity = Math.Min(existing.Quantity + quantity, available);
                existing.UpdatedAt = DateTime.UtcNow;
            }

            cart.UpdatedAt = DateTime.UtcNow;
            await _shopContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return await BuildCartDtoAsync(cart.Id);
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, $"Add to cart error: productId={productId}");
            return null;
        }
    }

    public async Task<CartDTO?> SetMyCartItemQuantityAsync(int productId, int quantity)
    {
        await using var transaction = await _shopContext.Database.BeginTransactionAsync();
        try
        {
            var userId = _userContext.CurrentUser?.Id;
            if (userId is null) return new CartDTO();

            var cart = await _shopContext.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId.Value);

            if (cart == null) return new CartDTO();

            var item = cart.CartItems.FirstOrDefault(i => i.ProductId == productId);
            if (item == null) return await BuildCartDtoAsync(cart.Id);

            if (quantity <= 0)
            {
                _shopContext.CartItems.Remove(item);
            }
            else
            {
                var available = await GetAvailableQuantityAsync(productId);
                item.Quantity = Math.Min(quantity, available);
                item.UpdatedAt = DateTime.UtcNow;
            }

            cart.UpdatedAt = DateTime.UtcNow;
            await _shopContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return await BuildCartDtoAsync(cart.Id);
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, $"Set quantity error: productId={productId}, quantity={quantity}");
            return null;
        }
    }

    public async Task<CartDTO?> RemoveFromMyCartAsync(int productId)
    {
        await using var transaction = await _shopContext.Database.BeginTransactionAsync();
        try
        {
            var userId = _userContext.CurrentUser?.Id;
            if (userId is null) return new CartDTO();

            var cart = await _shopContext.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId.Value);

            if (cart == null) return new CartDTO();

            var item = cart.CartItems.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
                _shopContext.CartItems.Remove(item);

            cart.UpdatedAt = DateTime.UtcNow;
            await _shopContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return await BuildCartDtoAsync(cart.Id);
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, $"Remove from cart error: productId={productId}");
            return null;
        }
    }

    public async Task<CartDTO?> ClearMyCartAsync()
    {
        await using var transaction = await _shopContext.Database.BeginTransactionAsync();
        try
        {
            var userId = _userContext.CurrentUser?.Id;
            if (userId is null) return new CartDTO();

            var cart = await _shopContext.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId.Value);

            if (cart == null) return new CartDTO();

            if (cart.CartItems.Count != 0)
                _shopContext.CartItems.RemoveRange(cart.CartItems);

            cart.UpdatedAt = DateTime.UtcNow;
            await _shopContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return new CartDTO();
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, "Clear cart error");
            return null;
        }
    }

    private async Task<int> GetAvailableQuantityAsync(int productId)
    {
        return await _shopContext.ShopProducts
            .AsNoTracking()
            .Where(sp => sp.ProductId == productId)
            .Select(sp => (int?)sp.Count)
            .FirstOrDefaultAsync() ?? 0;
    }

    private async Task<CartDTO> BuildCartDtoAsync(int cartId)
    {
        var items = await _shopContext.CartItems
            .AsNoTracking()
            .Where(i => i.CartId == cartId)
            .Join(_shopContext.Products.AsNoTracking().Include(p => p.Image).Include(p => p.ShopProducts).ThenInclude(sp => sp.HistoryCosts),
                i => i.ProductId,
                p => p.Id,
                (i, p) => new { i, p })
            .Select(x => new
            {
                x.i.ProductId,
                x.i.Quantity,
                x.p.Name,
                Image = x.p.Image != null ? x.p.Image.Image1 : null,
                Price = x.p.ShopProducts
                    .SelectMany(sp => sp.HistoryCosts)
                    .OrderByDescending(h => h.Id)
                    .Select(h => h.NewCost)
                    .FirstOrDefault(),
                Available = x.p.ShopProducts
                    .Select(sp => (int?)sp.Count)
                    .FirstOrDefault() ?? 0
            })
            .ToListAsync();

        var dto = new CartDTO
        {
            Items = items.Select(x => new CartItemDTO
            {
                ProductId = x.ProductId,
                Name = x.Name,
                Quantity = x.Quantity,
                UnitPrice = x.Price,
                Image = x.Image,
                AvailableQuantity = x.Available
            }).ToList()
        };

        foreach (var item in dto.Items)
        {
            item.DisplayImage = _imageService.GetProductImage(item.ProductId, item.Image);
        }

        return dto;
    }
}

