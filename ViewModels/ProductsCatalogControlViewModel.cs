using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Shop.DTOs;
using Shop.Interfaces;
using Shop.Messages;
using Shop.Utils;

namespace Shop.ViewModels;

public partial class ProductsCatalogControlViewModel : ViewModelBase
{
    private readonly IProductService _productService;

    private bool _isResetting;
    
    [ObservableProperty]
    private ObservableCollection<ProductPreviewDTO>? _products = [];
    
    [ObservableProperty]
    private ObservableCollection<CategoryDTO>? _categories = [];
    
    [ObservableProperty]
    private ObservableCollection<ProducerDTO>? _producers = [];
    
    [ObservableProperty]
    private int _currentPage = 1;
    
    [ObservableProperty]
    private int _pageSize = 9;
    
    [ObservableProperty]
    private int _totalCount;
    
    [ObservableProperty]
    private bool _canGoBack;
    
    [ObservableProperty]
    private bool _canGoNext;

    [ObservableProperty]
    private int _totalPages;
    
    [ObservableProperty]
    private CategoryDTO? _selectedCategory;
    
    [ObservableProperty]
    private ProducerDTO? _selectedProducer;
    
    public ProductsCatalogControlViewModel(IProductService productService)
    {
        _productService = productService;

        _ = InitializeAsync();
    }

    [RelayCommand]
    private async Task GetProductsAsync()
    {
        try
        {
            Products?.Clear();
        
            var products =  await _productService.GetProductsPagedAsync(CurrentPage, PageSize);

            if (products != null)
            {
                if (products?.Items != null)
                    Products = new ObservableCollection<ProductPreviewDTO>(products.Items);

                CanGoBack = products.CanGoBack;
                CanGoNext = products.CanGoNext;
                TotalCount = products.TotalCount;
                TotalPages = products.TotalPages;
            }
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, $"Get products viewmodel error:" +
                                  $" CurrentPage: {CurrentPage}," +
                                  $" PageSize:{PageSize}," +
                                  $" CanGoBack:{CanGoBack}," +
                                  $" CanGoNext:{CanGoNext}," +
                                  $" TotalCount:{TotalCount}," +
                                  $" TotalPages{TotalPages}");
        }
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        if (CanGoBack)
        {
            CurrentPage--;
            await GetProductsAsync();
        }
    }

    [RelayCommand]
    private async Task GoNextAsync()
    {
        if (CanGoNext)
        {
            CurrentPage++;
            await GetProductsAsync();
        }
    }

    private async Task GetCategoriesAsync()
    {
        try
        {
            Categories?.Clear();
        
        
            var categories = await _productService.GetCategoriesAsync();

            if (categories != null)
            {
                Categories = new ObservableCollection<CategoryDTO>(categories);
            }

            Categories?.Insert(0, new CategoryDTO() {Id = 0, Name = "Все категории"});
            
            SelectedCategory = Categories?[0];
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, $"Get categories viewmodel error");
        }
        
    }

    private async Task GetProducersAsync()
    {
        try
        {
            Producers?.Clear();


            var producers = await _productService.GetProducersAsync();

            if (producers != null)
            {
                Producers = new ObservableCollection<ProducerDTO>(producers);
            }
            
            Producers?.Insert(0,new ProducerDTO() { Id = 0, Name = "Все производители" });

            SelectedProducer = Producers?[0];
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, $"Get producers viewmodel error");
        }
    }

    [RelayCommand]
    private void RemoveFilters()
    {
        if (Producers?.Count != 0)
            SelectedProducer = Producers?[0];
        
        if (Categories?.Count != 0)
            SelectedCategory = Categories?[0];
    }
    
    [RelayCommand]
    private void OpenDetails(object parameter)
    {
        if (parameter is int productId)
            WeakReferenceMessenger.Default.Send(new ChangeViewModelMessage(typeof(ProductDetailsControlViewModel), productId));
    }

    public async Task ResetForNewUserAsync()
    {
        if (_isResetting) return;
        
        _isResetting = true;

        try
        {
            RemoveFilters();

            CurrentPage = 1;

            await GetCategoriesAsync();
            await GetProducersAsync();
            await GetProductsAsync();
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, $"Reset for new user viewmodel error");
        }
        finally
        {
            _isResetting = false;
        }
    }

    private async Task InitializeAsync()
    {
        await GetCategoriesAsync();
        await GetProducersAsync();
        await GetProductsAsync();
    }
}