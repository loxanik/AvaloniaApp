using System.Collections.Generic;
using System.Threading.Tasks;
using Shop.DTOs;

namespace Shop.Interfaces;

public interface IProductService
{
    Task<PagedResult<ProductPreviewDTO>?> GetProductsPagedAsync(
        int pageNumber,
        int pageSize,
        string? search = null,
        string? category = null,
        string? producer = null,
        string? sortBy = null,
        decimal? priceFrom = null,
        decimal? priceTo = null);
    Task<List<CategoryDTO>?> GetCategoriesAsync();
    Task<List<ProducerDTO>?> GetProducersAsync();
    Task<ProductDetailsDTO?> GetProductDetailsAsync(int id, bool includeDeleted);
    Task UpdateProductDetailsAsync(ProductDetailsDTO product);
    Task<bool> SoftDeleteProductAsync(int id);
    Task<bool> HardDeleteProductAsync(int id);
    Task<bool> RestoreSoftDeletedProductAsync(int id);
    Task<List<ProductPreviewDTO>?> GetDeletedProductsAsync();
    Task<int> CreateProductAsync(ProductDetailsDTO productDto);
}