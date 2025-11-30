using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Shop.Entities;
using Shop.Interfaces;
using Shop.Utils;

namespace Shop.Services;

public class ImageService : IImageService
{
    private readonly ShopContext _shopContext;
    private Bitmap _defaultImage;
    private readonly Dictionary<int, Bitmap> _imageCache = [];
    private readonly object _cacheLock = new object();
    
    public ImageService(ShopContext context)
    {
        _shopContext = context;
        _defaultImage = LoadDefaultImage();
    }
    
    public Bitmap? GetProductImage(int productId, byte[]? imageData = null)
    {
        lock (_cacheLock)
        {
            if (_imageCache.TryGetValue(productId, out var cachedImage))
                return cachedImage;
        }
        
        Bitmap? image = null;
        
        if (imageData != null && imageData.Length > 0)
        {
            image = ConvertToBitmap(imageData);
        }
        else
        {
            image = LoadImageFromDb(productId);
        }

        if (image != null)
        {
            _imageCache[productId] = image;
        }
        
        return image ?? _defaultImage;
    }

    private Bitmap? LoadImageFromDb(int productId)
    {
        try
        {
            var dbImage = _shopContext.Products
                .Where(p => p.Id == productId)
                .Select(p => p.Image.Image1)
                .FirstOrDefault();

            return dbImage != null ? ConvertToBitmap(dbImage) : null;
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, $"Load db image error: {productId}");

            return _defaultImage;
        }
        
    }

    public Bitmap GetDefaultImage() => _defaultImage;
    
    private Bitmap? ConvertToBitmap(byte[] imageData)
    {
        try
        {
            using var stream = new MemoryStream(imageData);
            return new Bitmap(stream);
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, $"Convert error");
            return null;
        }
    }
    
    private Bitmap LoadDefaultImage()
    {
        try
        {
            var assets = AssetLoader.Open(new Uri("avares://Shop/Resources/Images/default_image.png"));
            return new Bitmap(assets);
        } 
        catch (Exception e)
        {
            AppLogger.LogError(e, $"GetDefaultImage error");

            return new WriteableBitmap(
                new PixelSize(400, 300), 
                new Vector(96, 96), 
                PixelFormat.Bgra8888);
        }
    }
}