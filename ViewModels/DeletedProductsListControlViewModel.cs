using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Shop.DTOs;
using Shop.Interfaces;
using Shop.Utils;

namespace Shop.ViewModels;

public partial class DeletedProductsListControlViewModel : ViewModelBase
{
    private readonly IProductService _productService;

    [ObservableProperty]
    private ObservableCollection<ProductPreviewDTO> _deletedProducts = [];
    
    [ObservableProperty]
    private ProductPreviewDTO? _selectedProduct;
    
    public DeletedProductsListControlViewModel(IProductService productService)
    {
        _productService = productService;
    }

    [RelayCommand]
    private async Task GetDeletedProducts()
    {
        try
        {
            var products = await _productService.GetDeletedProductsAsync();
            
            if (products != null)
                DeletedProducts = new ObservableCollection<ProductPreviewDTO>(products);
            
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    [RelayCommand]
    private async Task RestoreSelectedProduct(ProductPreviewDTO? selectedProduct)
    {
        if (selectedProduct == null) return;
        
        try
        {
            bool isSuccess = await _productService.RestoreSoftDeletedProductAsync(selectedProduct.Id);

            if (isSuccess)
            {
                var msg = MessageBoxManager.GetMessageBoxStandard("Восстановление",
                    $"Продукт \"{selectedProduct.Name}\" был успешно восстановлен",
                    icon: Icon.Success);

                await msg.ShowAsync();
                await GetDeletedProducts();
            }
            else
            {
                var msg = MessageBoxManager.GetMessageBoxStandard("Восстановление",
                    $"При восстановлении продукта \"{selectedProduct.Name}\" произошла ошибка",
                    icon: Icon.Error);
                await msg.ShowAsync();
            }
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, "Restore product error viewmodel");
            var msg = MessageBoxManager.GetMessageBoxStandard("Восстановление",
                $"При восстановлении продукта произошла ошибка",
                icon: Icon.Error);
            await msg.ShowAsync();
        }
    }
}