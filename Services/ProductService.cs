using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Shop.DTOs;
using Shop.Entities;
using Shop.Interfaces;
using Shop.Utils;

namespace Shop.Services;

public class ProductService : IProductService
{
    private readonly ShopContext _shopContext;
    private readonly IImageService _imageService;
    
    public ProductService(ShopContext shopContext, IImageService imageService)
    {
        _shopContext = shopContext;
        _imageService = imageService;
    }
    
    public async Task<PagedResult<ProductPreviewDTO>?> GetProductsPagedAsync(
        int pageNumber,
        int pageSize,
        string? search = null,
        string? category = null,
        string? producer = null,
        string? sortBy = null,
        decimal? priceFrom = null,
        decimal? priceTo = null)
    {
        try
        {
            var query = _shopContext.Products
                .Where(p => !p.IsDeleted)
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Image)
                .Include(p => p.Producer)
                .Include(p => p.ShopProducts)
                    .ThenInclude(sp => sp.HistoryCosts)
                .AsQueryable();
            
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.Name.Contains(search));
            
            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(p => p.Category.Name == category);
            
            if (!string.IsNullOrWhiteSpace(producer))
                query = query.Where(p => p.Producer.Name == producer);

            var queryWithPrice = query.Select(p => new
            {
                Product = p,
                Price = p.ShopProducts
                    .SelectMany(sp => sp.HistoryCosts)
                    .OrderByDescending(ph => ph.Id)
                    .Select(ph => ph.NewCost)
                    .FirstOrDefault()
            });
            
            if (priceFrom.HasValue)
                queryWithPrice = queryWithPrice.Where(p => p.Price >= priceFrom);
            
            if (priceTo.HasValue)
                queryWithPrice = queryWithPrice.Where(p => p.Price <= priceTo);
            
            queryWithPrice = sortBy switch
            {
                "По возрастанию цены" => queryWithPrice.OrderBy(x => x.Price),
                "По убыванию цены" => queryWithPrice.OrderByDescending(x => x.Price),
                "По убыванию имени" => queryWithPrice.OrderByDescending(x => x.Product.Name),
                _ => queryWithPrice.OrderBy(x => x.Product.Name),
            };
            
            var totalCount = await queryWithPrice.CountAsync();
            
            var items =  await queryWithPrice
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new ProductPreviewDTO
                {
                    Id = x.Product.Id,
                    Name = x.Product.Name,
                    Category = x.Product.Category.Name,
                    Producer = x.Product.Producer.Name,
                    Description = x.Product.Description,
                    Image = x.Product.Image != null ? x.Product.Image.Image1 : null,
                    Price = x.Price,
                    AvailableQuantity = x.Product.ShopProducts
                        .Select(sp => (int?)sp.Count)
                        .FirstOrDefault() ?? 0
                }).ToListAsync();

            foreach (var product in items)
            {
                product.DisplayImage = _imageService.GetProductImage(product.Id, product.Image);
            }
            
            return new PagedResult<ProductPreviewDTO>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            AppLogger.LogError(ex, $"Product page exception: pageNumber: {pageNumber}, pageSize: {pageSize}");
            return null;
        }
    }

    public async Task<List<string>?> GetCategoriesAsync()
    {
        try
        {
            var categories = await _shopContext.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => c.Name)
                .ToListAsync();
            
            return categories;
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, $"Get categories error");
            return null;
        }
    }

    public async Task<List<string>?> GetProducersAsync()
    {
        try
        {
            var producers = await _shopContext.Producers
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => c.Name)
                .ToListAsync();
            
            return producers;
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, $"Get producers error");
            return null;
        }
    }

    public async Task<List<string>?> GetCountriesAsync()
    {
        try
        {
            var countries = await _shopContext.Countries
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => c.Name)
                .ToListAsync();
            
            return countries;
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, $"Get countries error");
            return null;
        }
    }

    public async Task<List<string>?> GetUnitsAsync()
    {
        try
        {
            var units = await _shopContext.UnitOfMeasurements
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => c.Name)
                .ToListAsync();
            
            return units;
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, $"Get units error");
            return null;
        }
    }

    public async Task<ProductDetailsDTO?> GetProductDetailsAsync(int id, bool includeDeleted = false)
    {
        try
        {
            var query = _shopContext.Products.Where(p => p.Id == id);

            if (!includeDeleted)
            {
                query = query.Where(p => !p.IsDeleted);
            }
            
            var product = await query
                .Include(p => p.Image)
                .Select(p => new ProductDetailsDTO
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Category = p.Category.Name,
                    Producer = p.Producer.Name,
                    Country = p.Producer.Country.Name,
                    Image = p.Image.Image1,
                    Price = p.ShopProducts
                        .SelectMany(sp => sp.HistoryCosts)
                        .OrderByDescending(sp => sp.Id)
                        .Select(sp => sp.NewCost)
                        .FirstOrDefault(),
                    Parameters = p.Parameters
                        .Select(param => new ParametersDTO
                        {
                            Id = param.Id,
                            Name = param.Name,
                            Value = param.Value,
                            Unit = param.Unit.Name
                        }).ToList(),
                    IsDeleted = p.IsDeleted,
                    Count = p.ShopProducts.FirstOrDefault()!.Count
                }).FirstOrDefaultAsync();

            if (product != null)
            {
                product.DisplayImage = _imageService.GetProductImage(product.Id, product.Image);
            }
            
            return product;
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, $"Get product details error: id: {id}");
            return null;
        }
    }

    public async Task UpdateProductDetailsAsync(ProductDetailsDTO productDto)
    {
        await using var transaction = await _shopContext.Database.BeginTransactionAsync();
        
        try
        {
            var entity = await _shopContext.Products
                .Include(p => p.Category)
                .Include(p => p.Producer)
                    .ThenInclude(producer => producer.Country)
                .Include(p => p.Parameters)
                    .ThenInclude(p => p.Unit)
                .Include(p => p.Image)
                .Include(p => p.ShopProducts)
                    .ThenInclude(sp => sp.HistoryCosts)
                .FirstOrDefaultAsync(p => p.Id == productDto.Id);
            
            if (entity == null) return;
            
            entity.Name = productDto.Name;
            entity.Description = productDto.Description;
            entity.IsDeleted = productDto.IsDeleted;

            var newCategory = await FindOrCreateCategoryAsync(productDto.Category);
            if (entity.Category.Id != newCategory.Id)
            {
                entity.Category = newCategory;
            }
            
            var newCountry = await FindOrCreateCountryAsync(productDto.Country);
        
            var newProducer = await FindOrCreateProducerAsync(productDto.Producer, newCountry);
            if (entity.Producer.Id != newProducer.Id)
            {
                entity.Producer = newProducer;
            }
            else if (entity.Producer.Country.Id != newCountry.Id)
            {
                entity.Producer.Country = newCountry;
            }
            
            //TODO сделать изменение фотографии

            await UpdateParametersAsync(entity, productDto?.Parameters);
            
            var shopProduct = entity.ShopProducts.FirstOrDefault();
            
            if (shopProduct != null)
            {
                shopProduct.Count = productDto.Count;
                
                var lastHistoryCost = shopProduct.HistoryCosts
                    .OrderByDescending(sp => sp.Id)
                    .FirstOrDefault();

                decimal oldCost = lastHistoryCost?.NewCost ?? 0;
                decimal newCost = productDto?.Price ?? oldCost;

                if (Math.Abs(newCost - oldCost) > 0.01m)
                {
                    var historyCost = new HistoryCost()
                    {
                        ShopProductId = shopProduct.Id,
                        OldCost = oldCost,
                        NewCost = newCost,
                    };
                    
                    _shopContext.HistoryCosts.Add(historyCost);
                }
            }
            
            await _shopContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, $"Update product details error service: id: {productDto.Id}");
        }
    }

    public async Task<bool> SoftDeleteProductAsync(int id)
    {
        try
        {
            var product = await _shopContext.Products
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return false;
            }

            if (product.IsDeleted)
            {
                return false;
            }

            product.IsDeleted = true;
        
            await _shopContext.SaveChangesAsync();
        
            return true;
        }
        catch (Exception ex)
        {
            AppLogger.LogError(ex, $"Error deleting product: {id}");
            return false;
        }
    }

    public async Task<bool> HardDeleteProductAsync(int id)
    {
        try
        {
            // Находим товар со всеми зависимостями
            var product = await _shopContext.Products
                .Include(p => p.Parameters)
                .Include(p => p.ShopProducts)
                .ThenInclude(sp => sp.HistoryCosts)
                .Include(p => p.Image)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return false;
            }

            // Удаляем зависимости в правильном порядке
            if (product.ShopProducts?.Any() == true)
            {
                foreach (var shopProduct in product.ShopProducts.ToList())
                {
                    if (shopProduct.HistoryCosts?.Any() == true)
                    {
                        _shopContext.HistoryCosts.RemoveRange(shopProduct.HistoryCosts);
                    }
                    _shopContext.ShopProducts.Remove(shopProduct);
                }
            }

            if (product.Parameters?.Any() == true)
            {
                _shopContext.Parameters.RemoveRange(product.Parameters);
            }

            if (product.Image != null)
            {
                _shopContext.Images.Remove(product.Image);
            }

            // Удаляем сам товар
            _shopContext.Products.Remove(product);

            await _shopContext.SaveChangesAsync();
        
            return true;
        }
        catch (Exception ex)
        {
            AppLogger.LogError(ex, $"Error hard-deleting product: {id}");
            return false;
        }
    }

    public async Task<bool> RestoreSoftDeletedProductAsync(int id)
    {
        try
        {
            var product = await _shopContext.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.IsDeleted);

            if (product == null)
            {
                return false;
            }

            product.IsDeleted = false;

            await _shopContext.SaveChangesAsync();
        
            return true;
        }
        catch (Exception ex)
        {
            AppLogger.LogError(ex, $"Error restoring product: {id}");
            return false;
        }
    }

    public async Task<List<ProductPreviewDTO>?> GetDeletedProductsAsync()
    {
        try
        {
            var deletedProducts = await _shopContext.Products
                .Where(p => p.IsDeleted)
                .Include(p => p.Category)
                .OrderBy(p => p.Id)
                .Select(p => new ProductPreviewDTO
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Category = p.Category.Name,
                    Price = p.ShopProducts
                        .SelectMany(sp => sp.HistoryCosts)
                        .OrderByDescending(sp => sp.Id)
                        .Select(sp => sp.NewCost)
                        .FirstOrDefault(),
                    IsDeleted = p.IsDeleted // Показываем статус
                })
                .ToListAsync();

            return deletedProducts;
        }
        catch (Exception ex)
        {
            AppLogger.LogError(ex, "Error getting deleted products");
            return null;
        }
    }

    public async Task<int> CreateProductAsync(ProductDetailsDTO productDto)
    {
         try
         {
             await using var transaction = await _shopContext.Database.BeginTransactionAsync();
            
             // 1. Находим или создаем категорию
             var category = await FindOrCreateCategoryAsync(productDto.Category);
            
             // 2. Находим или создаем страну
             var country = await FindOrCreateCountryAsync(productDto.Country);
            
             // 3. Находим или создаем производителя
             var producer = await FindOrCreateProducerAsync(productDto.Producer, country);
            
             // 4. Создаем изображение (если есть)
             Image? image = null;
             if (productDto.Image != null && productDto.Image.Length > 0)
             {
                 image = new Image { Image1 = productDto.Image };
                 await _shopContext.Images.AddAsync(image);
                 await _shopContext.SaveChangesAsync();
             }
            
             // 5. Создаем продукт
             var product = new Product
             {
                 Name = productDto.Name,
                 Description = productDto.Description,
                 Category = category,
                 Producer = producer,
                 Image = image,
                 IsDeleted = false,
                 Parameters = new List<Parameter>()
             };
            
             await _shopContext.Products.AddAsync(product);
             await _shopContext.SaveChangesAsync();
            
             // 6. Создаем ShopProduct с начальной стоимостью
             var shopProduct = new ShopProduct
             {
                 ProductId = product.Id,
                 Count = productDto.Count,
                 DateOfManufacture = DateOnly.FromDateTime(DateTime.Now),
                 HistoryCosts = new List<HistoryCost>()
             };
            
             await _shopContext.ShopProducts.AddAsync(shopProduct);
             await _shopContext.SaveChangesAsync();
            
             // 7. Добавляем начальную стоимость
             if (productDto.Price > 0)
             {
                 var historyCost = new HistoryCost
                 {
                     ShopProductId = shopProduct.Id,
                     OldCost = 0,
                     NewCost = productDto.Price
                 };
                
                 await _shopContext.HistoryCosts.AddAsync(historyCost);
                 await _shopContext.SaveChangesAsync();
             }
            
             // 8. Добавляем параметры
             if (productDto.Parameters != null && productDto.Parameters.Any())
             {
                 await AddParametersToProductAsync(product, productDto.Parameters);
             }
            
             await transaction.CommitAsync();
            
             return product.Id;
         }
         catch (Exception ex)
         {
             AppLogger.LogError(ex, "Error creating product");
             return 0;
         }
    }
    
    private async Task<Category> FindOrCreateCategoryAsync(string categoryName)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
            throw new ArgumentException("Category name cannot be empty");
        
        var category = await _shopContext.Categories
            .FirstOrDefaultAsync(c => c.Name == categoryName);
        
        if (category == null)
        {
            category = new Category { Name = categoryName };
            await _shopContext.Categories.AddAsync(category);
            await _shopContext.SaveChangesAsync();
        }
        
        return category;
    }
    
    private async Task<Country> FindOrCreateCountryAsync(string countryName)
    {
        if (string.IsNullOrWhiteSpace(countryName))
            throw new ArgumentException("Country name cannot be empty");
        
        var country = await _shopContext.Countries
            .FirstOrDefaultAsync(c => c.Name == countryName);
        
        if (country == null)
        {
            country = new Country { Name = countryName };
            await _shopContext.Countries.AddAsync(country);
            await _shopContext.SaveChangesAsync();
        }
        
        return country;
    }
    
    private async Task<Producer> FindOrCreateProducerAsync(string producerName, Country country)
    {
        if (string.IsNullOrWhiteSpace(producerName))
            throw new ArgumentException("Producer name cannot be empty");
    
        // Ищем производителя с указанной страной
        var producer = await _shopContext.Producers
            .Include(p => p.Country)
            .FirstOrDefaultAsync(p => p.Name == producerName && p.Country.Id == country.Id);
    
        if (producer == null)
        {
            // Ищем производителя только по имени (без учета страны)
            producer = await _shopContext.Producers
                .Include(p => p.Country)
                .FirstOrDefaultAsync(p => p.Name == producerName);
        
            if (producer != null)
            {
                // Производитель найден, но страна другая - обновляем страну
                producer.Country = country;
                await _shopContext.SaveChangesAsync();
            }
            else
            {
                // Создаем нового производителя
                producer = new Producer
                {
                    Name = producerName,
                    Country = country
                };
                await _shopContext.Producers.AddAsync(producer);
                await _shopContext.SaveChangesAsync();
            }
        }
    
        return producer;
    }
    
    private async Task AddParametersToProductAsync(Product product, List<ParametersDTO> parameters)
    {
        foreach (var paramDto in parameters)
        {
            // Находим или создаем единицу измерения
            var unit = await _shopContext.UnitOfMeasurements
                .FirstOrDefaultAsync(u => u.Name == paramDto.Unit);
            
            if (unit == null)
            {
                unit = new UnitOfMeasurement { Name = paramDto.Unit };
                await _shopContext.UnitOfMeasurements.AddAsync(unit);
                await _shopContext.SaveChangesAsync();
            }
            
            var parameter = new Parameter
            {
                Name = paramDto.Name,
                Value = paramDto.Value,
                Unit = unit,
                Product = product
            };
            
            await _shopContext.Parameters.AddAsync(parameter);
        }
        
        await _shopContext.SaveChangesAsync();
    }

    private async Task UpdateParametersAsync(Product entity, List<ParametersDTO>? newParameters)
    {
        try
        {
            if (newParameters == null) 
            {
                // Если новые параметры null - удаляем все старые
                _shopContext.Parameters.RemoveRange(entity.Parameters);
                return;
            }
        
            var existingParams = entity.Parameters.ToList();
            
            // 1. ОБНОВЛЯЕМ существующие параметры и УДАЛЯЕМ лишние
            foreach (var existingParam in existingParams.ToList()) // ToList() для копии
            {
                var newParamDto = newParameters.FirstOrDefault(np => np.Id == existingParam.Id);
                
                if (newParamDto != null)
                {
                    // ОБНОВЛЯЕМ существующий параметр
                    existingParam.Name = newParamDto.Name;
                    existingParam.Value = newParamDto.Value;
                    
                    // Обновляем Unit если нужно
                    if (existingParam.Unit.Name != newParamDto.Unit)
                    {
                        var unit = await _shopContext.UnitOfMeasurements
                            .FirstOrDefaultAsync(u => u.Name == newParamDto.Unit) 
                            ?? new UnitOfMeasurement { Name = newParamDto.Unit };
                        existingParam.Unit = unit;
                    }
                }
                else
                {
                    // УДАЛЯЕМ параметр которого нет в новых данных
                    _shopContext.Parameters.Remove(existingParam);
                    existingParams.Remove(existingParam); // Убираем из локальной коллекции
                }
            }
        
            // 2. ДОБАВЛЯЕМ новые параметры (без Id или с Id = 0/null)
            foreach (var newParamDto in newParameters)
            {
                // Если параметр новый (нет Id или Id = 0)
                if (newParamDto.Id == null || newParamDto.Id == 0)
                {
                    var unit = await _shopContext.UnitOfMeasurements
                                   .FirstOrDefaultAsync(u => u.Name == newParamDto.Unit)
                               ?? new UnitOfMeasurement { Name = newParamDto.Unit };
    
                    var newParameter = new Parameter
                    {
                        Name = newParamDto.Name,
                        Value = newParamDto.Value,
                        Unit = unit,
                        Product = entity
                    };
    
                    entity.Parameters.Add(newParameter);
                }
            }
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, $"Update parameters error service: id: {entity.Id}");
        }
    }
}