using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Shop.DTOs;
using Shop.Interfaces;
using Shop.Messages;
using Shop.Utils;

namespace Shop.ViewModels;

public partial class DeletedProductsListControlViewModel : ViewModelBase
{
    private readonly IProductService _productService;

    [ObservableProperty]
    private ObservableCollection<ProductPreviewDTO> _deletedProducts = [];
    
    [ObservableProperty]
    private ProductPreviewDTO? _selectedProduct;

    [ObservableProperty]
    private int _deletedProductCount = 0;
    public bool IsProductSelected => SelectedProduct != null;

    
    public DeletedProductsListControlViewModel(IProductService productService)
    {
        _productService = productService;

        _ = InitializeAsync();
    }

    [RelayCommand]
    private async Task GetDeletedProductsAsync()
    {
        SelectedProduct = null;
        
        try
        {
            var products = await _productService.GetDeletedProductsAsync();

            if (products != null)
                DeletedProducts = new ObservableCollection<ProductPreviewDTO>(products);

            DeletedProductCount = products.Count;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    [RelayCommand(CanExecute = nameof(IsProductSelected))]
    private async Task RestoreSelectedProductAsync(ProductPreviewDTO? selectedProduct)
    {
        try
        {
            bool isSuccess = await _productService.RestoreSoftDeletedProductAsync(selectedProduct.Id);

            if (isSuccess)
            {
                var msg = MessageBoxManager.GetMessageBoxStandard("Восстановление",
                    $"Продукт \"{selectedProduct.Name}\" был успешно восстановлен",
                    icon: Icon.Success);

                await msg.ShowAsync();
                await GetDeletedProductsAsync();
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

    [RelayCommand(CanExecute = nameof(IsProductSelected))]
    private async Task DeleteSelectedProductAsync(ProductPreviewDTO? selectedProduct)
    {
        var msg = MessageBoxManager.GetMessageBoxStandard("Удаление",
            $"Вы действительно хотите полностью удалить товар \"{selectedProduct.Name}\" ?",
            ButtonEnum.OkCancel,
            icon: Icon.Question);
        await msg.ShowAsync();
    }

    [RelayCommand(CanExecute = nameof(IsProductSelected))]
    private void OpenDeletedProductDetails(object parameter)
    {
        if (parameter is int productId)
            WeakReferenceMessenger.Default.Send(new ChangeViewModelMessage(typeof(ProductDetailsControlViewModel),
                productId));
    }
    
    private async Task InitializeAsync()
    {
        await GetDeletedProductsAsync();
    }
}