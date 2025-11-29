using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Shop.DTOs;
using Shop.Interfaces;
using Shop.Models;
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
    
    public async Task<PagedResult<ProductPreviewDTO>?> GetProductsPagedAsync(int pageNumber, int pageSize)
    {
        try
        {
            var query = _shopContext.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .AsQueryable();
            
            var totalCount = await query.CountAsync();
            
            var pagedQuery = query.
                OrderBy(p => p.Category.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize);

            var items = await pagedQuery
                .Select(p => new ProductPreviewDTO
                {
                    Id = p.Id,
                    Name = p.Name,
                    Category = p.Category.Name,
                    Description = p.Description,
                    Image = p.Image.Image1,
                    Price = p.ShopProducts
                        .SelectMany(sp => sp.HistoryCosts)
                        .OrderByDescending(sp => sp.Id)
                        .Select(sp => sp.NewCost)
                        .FirstOrDefault()
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

    public async Task<List<CategoryDTO>?> GetCategoriesAsync()
    {
        try
        {
            var categories = await _shopContext.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new CategoryDTO
                {
                    Id = c.Id,
                    Name = c.Name
                }).ToListAsync();
            
            return categories;
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, $"Get categories error");
            return null;
        }
    }

    public async Task<List<ProducerDTO>?> GetProducersAsync()
    {
        try
        {
            var producers = await _shopContext.Producers
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new ProducerDTO
                {
                    Id = c.Id,
                    Name = c.Name
                }).ToListAsync();
            
            return producers;
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, $"Get producers error");
            return null;
        }
    }

    public async Task<ProductDetailsDTO?> GetProductDetailsAsync(int id)
    {
        try
        {
            var product = await _shopContext.Products
                .Where(p => p.Id == id)
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
                        }).ToList()
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
        try
        {
            var product = await _shopContext.Products
                .Include(p => p.Category)
                .Include(p => p.Producer)
                    .ThenInclude(producer => producer.Country)
                .Include(p => p.Parameters)
                .Include(p => p.Image)
                .Include(p => p.ShopProducts)
                    .ThenInclude(sp => sp.HistoryCosts)
                .FirstOrDefaultAsync(p => p.Id == productDto.Id);
            
            if (product == null) return;
            
            product.Name = productDto.Name;
            product.Description = productDto.Description;
            product.Producer.Name = productDto.Producer;
            product.Category.Name = productDto.Category;
            product.Producer.Country.Name = productDto.Country;
            
            //TODO сделать изменение фотографии

            var shopProduct = product.ShopProducts.FirstOrDefault();
            if (shopProduct != null)
            {
                var lastHistoryCost = shopProduct.HistoryCosts
                    .OrderByDescending(sp => sp.Id)
                    .FirstOrDefault();

                decimal oldCost = lastHistoryCost?.NewCost ?? 0;
                decimal newCost = productDto?.Price ?? oldCost;

                if (newCost != oldCost)
                {
                    var historyCost = new HistoryCost()
                    {
                        Id = shopProduct.Id,
                        OldCost = oldCost,
                        NewCost = newCost,
                    };
                    
                    _shopContext.HistoryCosts.Add(historyCost);
                }
                
                //TODO сделать обновление параметров
                
            }
            
            await _shopContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, $"Update product details error service: id: {productDto.Id}");
        }
    }
}