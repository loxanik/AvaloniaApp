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
    private ObservableCollection<string>? _categories = [];
    
    [ObservableProperty]
    private ObservableCollection<string>? _producers = [];
    
    [ObservableProperty]
    private ObservableCollection<string>? _sortBy;
    
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
    private string? _selectedCategory;
    
    [ObservableProperty]
    private string? _selectedProducer;
    
    [ObservableProperty]
    private string? _searchText;
    
    [ObservableProperty]
    private decimal? _priceFrom;
    
    [ObservableProperty]
    private decimal? _priceTo;
    
    [ObservableProperty]
    private string? _selectedSortBy;
    
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
            string? producer = null, category = null;
            
            if (SelectedCategory != "Все категории")
                category = SelectedCategory;
            
            if (SelectedProducer != "Все производители")
                producer = SelectedProducer;
            
            Products?.Clear();
        
            var products =  await _productService.GetProductsPagedAsync(
                CurrentPage,
                PageSize,
                SearchText,
                category, 
                producer,
                SelectedSortBy,
                PriceFrom,
                PriceTo);

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
                Categories = new ObservableCollection<string>(categories);
            }

            Categories?.Insert(0, "Все категории");
            
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
                Producers = new ObservableCollection<string>(producers);
            }
            
            Producers?.Insert(0,"Все производители");

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

        SearchText = null;
        SelectedSortBy = SortBy?[0];
        PriceFrom = null;
        PriceTo = null;
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

        SortBy =
        [
            "По возрастанию имени",
            "По убыванию имени",
            "По возрастанию цены",
            "По убыванию цены"
        ];

        SelectedSortBy = SortBy[0];
    }
}