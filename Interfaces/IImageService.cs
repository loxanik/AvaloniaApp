using System.Threading.Tasks;
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
    
    /// <summary>
    /// Загрузить изображение для товара в базу данных
    /// </summary>
    /// <param name="productId">ID товара</param>
    /// <param name="imageData">Данные изображения</param>
    /// <returns>True если успешно</returns>
    Task<bool> UploadProductImageAsync(int productId, byte[] imageData);
}