using Avalonia.Media.Imaging;

namespace Shop.Interfaces;

public interface IImageService
{
    /// <summary>
    /// Получить изображение товара
    /// </summary>
    /// <param name="productId">ID товара</param>
    /// <param name="imageData">Данные изображения (опционально)</param>
    /// <returns>Bitmap изображения или заглушка</returns>
    Bitmap? GetProductImage(int productId, byte[]? imageData = null);
    
    /// <summary>
    /// Получить изображение-заглушку по умолчанию
    /// </summary>
    Bitmap GetDefaultImage();
}