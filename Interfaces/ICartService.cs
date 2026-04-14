using System.Threading.Tasks;
using Shop.DTOs;

namespace Shop.Interfaces;

public interface ICartService
{
    Task<CartDTO?> GetMyCartAsync();
    Task<CartDTO?> AddToMyCartAsync(int productId, int quantity = 1);
    Task<CartDTO?> SetMyCartItemQuantityAsync(int productId, int quantity);
    Task<CartDTO?> RemoveFromMyCartAsync(int productId);
    Task<CartDTO?> ClearMyCartAsync();
}

