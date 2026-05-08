using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Shop.DTOs;
using Shop.Entities;
using Shop.Interfaces;
using Shop.Utils;

namespace Shop.Services;

public class CartService : ICartService
{
    private readonly IDbContextFactory<ShopContext> _contextFactory;
    private readonly IUserContext _userContext;
    private readonly IImageService _imageService;

    public CartService(IDbContextFactory<ShopContext> contextFactory, IUserContext userContext, IImageService imageService)
    {
        _contextFactory = contextFactory;
        _userContext = userContext;
        _imageService = imageService;
    }

    public async Task<CartDTO?> GetMyCartAsync()
    {
        try
        {
            await using var shopContext = await _contextFactory.CreateDbContextAsync();
            var userId = _userContext.CurrentUser?.Id;
            if (userId is null) return new CartDTO();

            var cartId = await shopContext.Carts
                .AsNoTracking()
                .Where(c => c.UserId == userId.Value)
                .Select(c => (int?)c.Id)
                .FirstOrDefaultAsync();

            if (cartId is null) return new CartDTO();

            return await BuildCartDtoAsync(shopContext, cartId.Value);
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

        await using var shopContext = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await shopContext.Database.BeginTransactionAsync();
        try
        {
            var userId = _userContext.CurrentUser?.Id;
            if (userId is null) return new CartDTO();

            var cart = await GetOrCreateCartAsync(shopContext, userId.Value);
            if (cart == null)
            {
                AppLogger.LogError(new InvalidOperationException("Cart was not resolved"), $"Add to cart error: userId={userId}, productId={productId}");
                return new CartDTO();
            }

            var available = await GetAvailableQuantityAsync(shopContext, productId);
            if (available <= 0) return await BuildCartDtoAsync(shopContext, cart.Id);

            var existing = await shopContext.CartItems
                .FirstOrDefaultAsync(i => i.CartId == cart.Id && i.ProductId == productId);
            if (existing == null)
            {
                var toAdd = Math.Min(quantity, available);
                shopContext.CartItems.Add(new CartItem
                {
                    CartId = cart.Id,
                    ProductId = productId,
                    Quantity = toAdd,
                    CreatedAt = GetDbTimestamp(),
                    UpdatedAt = GetDbTimestamp()
                });
            }
            else
            {
                existing.Quantity = Math.Min(existing.Quantity + quantity, available);
                existing.UpdatedAt = GetDbTimestamp();
            }

            cart.UpdatedAt = GetDbTimestamp();
            try
            {
                await shopContext.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Handle unique(cartId, productId) race on fast double-clicks.
                var item = await shopContext.CartItems
                    .FirstOrDefaultAsync(i => i.CartId == cart.Id && i.ProductId == productId);
                if (item == null) throw;

                item.Quantity = Math.Min(item.Quantity + quantity, available);
                item.UpdatedAt = GetDbTimestamp();
                await shopContext.SaveChangesAsync();
            }
            await transaction.CommitAsync();

            return await BuildCartDtoAsync(shopContext, cart.Id);
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, $"Add to cart error: productId={productId}; inner={e.InnerException?.Message}");
            return null;
        }
    }

    public async Task<CartDTO?> SetMyCartItemQuantityAsync(int productId, int quantity)
    {
        await using var shopContext = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await shopContext.Database.BeginTransactionAsync();
        try
        {
            var userId = _userContext.CurrentUser?.Id;
            if (userId is null) return new CartDTO();

            var cart = await shopContext.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId.Value);

            if (cart == null) return new CartDTO();

            var item = cart.CartItems.FirstOrDefault(i => i.ProductId == productId);
            if (item == null) return await BuildCartDtoAsync(shopContext, cart.Id);

            if (quantity <= 0)
            {
                shopContext.CartItems.Remove(item);
            }
            else
            {
                var available = await GetAvailableQuantityAsync(shopContext, productId);
                item.Quantity = Math.Min(quantity, available);
                item.UpdatedAt = GetDbTimestamp();
            }

            cart.UpdatedAt = GetDbTimestamp();
            await shopContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return await BuildCartDtoAsync(shopContext, cart.Id);
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, $"Set quantity error: productId={productId}, quantity={quantity}");
            return null;
        }
    }

    public async Task<CartDTO?> RemoveFromMyCartAsync(int productId)
    {
        await using var shopContext = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await shopContext.Database.BeginTransactionAsync();
        try
        {
            var userId = _userContext.CurrentUser?.Id;
            if (userId is null) return new CartDTO();

            var cart = await shopContext.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId.Value);

            if (cart == null) return new CartDTO();

            var item = cart.CartItems.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
                shopContext.CartItems.Remove(item);

            cart.UpdatedAt = GetDbTimestamp();
            await shopContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return await BuildCartDtoAsync(shopContext, cart.Id);
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, $"Remove from cart error: productId={productId}");
            return null;
        }
    }

    public async Task<CartDTO?> ClearMyCartAsync()
    {
        await using var shopContext = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await shopContext.Database.BeginTransactionAsync();
        try
        {
            var userId = _userContext.CurrentUser?.Id;
            if (userId is null) return new CartDTO();

            var cart = await shopContext.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId.Value);

            if (cart == null) return new CartDTO();

            if (cart.CartItems.Count != 0)
                shopContext.CartItems.RemoveRange(cart.CartItems);

            cart.UpdatedAt = GetDbTimestamp();
            await shopContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return new CartDTO();
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, "Clear cart error");
            return null;
        }
    }

    private static async Task<int> GetAvailableQuantityAsync(ShopContext shopContext, int productId)
    {
        return await shopContext.ShopProducts
            .AsNoTracking()
            .Where(sp => sp.ProductId == productId)
            .SumAsync(sp => (int?)sp.Count) ?? 0;
    }

    private static async Task<Cart?> GetOrCreateCartAsync(ShopContext shopContext, int userId)
    {
        try
        {
            await shopContext.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "Cart" ("userId")
                VALUES ({userId})
                ON CONFLICT ("userId") DO NOTHING;
                """);

            return await shopContext.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, $"GetOrCreateCartAsync failed: userId={userId}; inner={e.InnerException?.Message}");
            return null;
        }
    }

    private static DateTime GetDbTimestamp()
    {
        return DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
    }

    private async Task<CartDTO> BuildCartDtoAsync(ShopContext shopContext, int cartId)
    {
        var items = await shopContext.CartItems
            .AsNoTracking()
            .Where(i => i.CartId == cartId)
            .Join(shopContext.Products.AsNoTracking().Include(p => p.Image).Include(p => p.ShopProducts).ThenInclude(sp => sp.HistoryCosts),
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
                    .Sum(sp => (int?)sp.Count) ?? 0
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

