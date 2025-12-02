using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Shop.DTOs;
using Shop.Interfaces;
using Shop.Messages;
using Shop.Models;
using Shop.Utils;

namespace Shop.ViewModels;

public partial class ProductDetailsControlViewModel : ViewModelBase, IParameterized, IEditableObject
{
    private readonly IProductService _productService;
    private readonly IUserContext _userContext;
    
    private int _productId;
    private ProductDetailsModel? _originalProduct;
    private bool _isNewProduct;
    
    public bool CanEdit => _userContext.CurrentUser?.Role.Name is "manager" or "admin";
    public string SaveButtonText => IsBusy ? (_isNewProduct ? "Создание..." : "Сохранение...")
        : (_isNewProduct ? "Создать" : "Сохранить");
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SaveButtonText))]
    private bool _isBusy;
    
    [ObservableProperty]
    private ProductDetailsModel? _currentProductDetails;

    [ObservableProperty]
    private ParametersModel? _selectedParameter;
    
    [ObservableProperty]
    private string? _selectedCountry;
    
    [ObservableProperty]
    private string? _selectedProducer;
    
    [ObservableProperty]
    private string? _selectedCategory;
    
    [ObservableProperty]
    private ObservableCollection<string>? _countryList = [];
    
    [ObservableProperty]
    private ObservableCollection<string>? _unitList = [];
    
    [ObservableProperty]
    private ObservableCollection<string>? _producerList = []; 
    
    [ObservableProperty]
    private ObservableCollection<string>? _categoryList = [];
    
    [ObservableProperty]
    private bool _isEditing;
    
    [ObservableProperty]
    private bool _hasChanges;
    
    public ProductDetailsControlViewModel(IProductService productService, IUserContext userContext)
    {
        _productService = productService;
        _userContext = userContext;
    }
    
    public async Task InitializeParam(object param)
    {
        try
        {
            if (param is not int productId) return;
        
            _productId = productId;
            _isNewProduct = productId == 0;
        
            await LoadAllReferenceDataAsync();
        
            if (_isNewProduct)
            {
                InitializeNewProduct();
            }
            else
            {
                await LoadProductDetailsAsync();
            }
        }
        catch (Exception ex)
        {
            AppLogger.LogError(ex, "Error initializing product details viewmodel");
            WeakReferenceMessenger.Default.Send(new ChangeViewModelMessage(typeof(ProductsCatalogControlViewModel)));
        }
    }

    private void InitializeNewProduct()
    {
        var newProductDto = new ProductDetailsDTO()
        {
            Name = "Новый товар",
            Description = "Описание товара",
            Price = 0m,
            Producer = "",
            Country = "",
            Category = "",
            IsDeleted = false,
            Count = 0,
            Parameters = []
        };

        CurrentProductDetails = new ProductDetailsModel(newProductDto);

        if (CanEdit)
        {
            BeginEdit();
        }
    }
    
    private void SetSelectedItemsFromProduct(ProductDetailsDTO dto)
    {
        // Устанавливаем выбранную страну
        if (CountryList != null && !string.IsNullOrEmpty(dto.Country))
        {
            // Ищем точное совпадение
            var country = CountryList.FirstOrDefault(c => 
                string.Equals(c, dto.Country, StringComparison.OrdinalIgnoreCase));
            
            SelectedCountry = country ?? dto.Country;
            
            // Если страны нет в списке, добавляем ее
            if (country == null && !CountryList.Contains(dto.Country))
            {
                CountryList.Add(dto.Country);
            }
        }
        
        // Устанавливаем выбранную категорию
        if (CategoryList != null && !string.IsNullOrEmpty(dto.Category))
        {
            var category = CategoryList.FirstOrDefault(c => 
                string.Equals(c, dto.Category, StringComparison.OrdinalIgnoreCase));
            
            SelectedCategory = category ?? dto.Category;
            
            if (category == null && !CategoryList.Contains(dto.Category))
            {
                CategoryList.Add(dto.Category);
            }
        }
        
        // Устанавливаем выбранного производителя
        if (ProducerList != null && !string.IsNullOrEmpty(dto.Producer))
        {
            var producer = ProducerList.FirstOrDefault(p => 
                string.Equals(p, dto.Producer, StringComparison.OrdinalIgnoreCase));
            
            SelectedProducer = producer ?? dto.Producer;
            
            if (producer == null && !ProducerList.Contains(dto.Producer))
            {
                ProducerList.Add(dto.Producer);
            }
        }
    }
    
    private async Task LoadProductDetailsAsync()
    {
        try
        {
            ProductDetailsDTO? dto;
            
            if (CanEdit)
                dto = await _productService.GetProductDetailsAsync(_productId, true);
            else
                dto = await _productService.GetProductDetailsAsync(_productId, false);
            
            if (dto == null)
            {
                WeakReferenceMessenger.Default.Send(
                        new ChangeViewModelMessage(typeof(ProductsCatalogControlViewModel)));
                return;
            }
            
            CurrentProductDetails = new ProductDetailsModel(dto);
            SetSelectedItemsFromProduct(dto);
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, $"Error on load product details viewmodel: id: {_productId} ");
        }
    }

    partial void OnSelectedCountryChanged(string? value)
    {
        if (CurrentProductDetails != null && value != null && IsEditing)
        {
            CurrentProductDetails.Country = value;
            CheckForChanges();
        }
    }
    
    partial void OnSelectedCategoryChanged(string? value)
    {
        if (CurrentProductDetails != null && value != null && IsEditing)
        {
            CurrentProductDetails.Category = value;
            CheckForChanges();
        }
    }
    
    partial void OnSelectedProducerChanged(string? value)
    {
        if (CurrentProductDetails != null && value != null && IsEditing)
        {
            CurrentProductDetails.Producer = value;
            CheckForChanges();
        }
    }
    
    [RelayCommand]
    private async Task GoToCatalogAsync()
    {
        if (IsEditing)
        {
            var dialog = MessageBoxManager.GetMessageBoxStandard(
                "Подтверждение действия",
                "Вы действительно хотите отменить изменения и перейти в каталог?",
                ButtonEnum.YesNo,
                Icon.Question
            );

            var result = await dialog.ShowAsync();

            if (result == ButtonResult.Yes)
            {
                CancelEdit();
            }
            else
            {
                return;
            }
        }
        
        WeakReferenceMessenger.Default.Send(new ChangeViewModelMessage(typeof(ProductsCatalogControlViewModel)));
    }
    
    [RelayCommand]
    public void BeginEdit()
    {
        if (IsEditing || CurrentProductDetails == null) return;

        _originalProduct = CloneProduct(CurrentProductDetails);
        IsEditing = true;
        HasChanges = _isNewProduct;
        
        CurrentProductDetails.PropertyChanged += OnProductPropertyChanged;

        foreach (var parameter in CurrentProductDetails.Parameters)
        {
            parameter.PropertyChanged += OnProductPropertyChanged;
        }
        
        CurrentProductDetails.Parameters.CollectionChanged += OnParametersCollectionChanged;
    }

    private void OnParametersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        CheckForChanges();
    }

    private void OnProductPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        CheckForChanges();
    }

    private void CheckForChanges()
    {
        if (IsEditing && CurrentProductDetails != null && _originalProduct != null)
        {
            HasChanges = CurrentProductDetails.HasChanges(_originalProduct);
        }
    }

    [RelayCommand]
    public void CancelEdit()
    {
        if (!IsEditing || _originalProduct == null || CurrentProductDetails == null) return; 
        
        CurrentProductDetails.PropertyChanged -= OnProductPropertyChanged;

        foreach (var parameter in CurrentProductDetails.Parameters)
        {
            parameter.PropertyChanged -= OnProductPropertyChanged;
        }
        
        CurrentProductDetails.Parameters.CollectionChanged -= OnParametersCollectionChanged;

        if (_isNewProduct)
        {
            CurrentProductDetails = null;
            SelectedCountry = null;
            SelectedProducer = null;
            SelectedCategory = null;
            SelectedParameter = null;

            _ = GoToCatalogAsync();
        }
        else if (_originalProduct != null)
        {
            CurrentProductDetails.Name = _originalProduct.Name;
            CurrentProductDetails.Description = _originalProduct.Description;
            CurrentProductDetails.Price = _originalProduct.Price;
            CurrentProductDetails.Producer = _originalProduct.Producer;
            CurrentProductDetails.Category = _originalProduct.Category;
            CurrentProductDetails.Country = _originalProduct.Country;
            
            SetSelectedItemsFromProduct(_originalProduct.ToDto());
        }
        
        IsEditing = false;
        HasChanges = false;
    }

    [RelayCommand]
    public void EndEdit()
    {
       if (!IsEditing || CurrentProductDetails == null) return;
       
       bool hasChanges = HasChanges || _isNewProduct;
       
       CurrentProductDetails.PropertyChanged -= OnProductPropertyChanged;

       foreach (var parameter in CurrentProductDetails.Parameters)
       {
           parameter.PropertyChanged -= OnProductPropertyChanged;
       }
        
       CurrentProductDetails.Parameters.CollectionChanged -= OnParametersCollectionChanged;

       IsEditing = false;
       HasChanges = false;

       _ = SaveProductDetailsAsync(hasChanges);
    }

    private async Task SaveProductDetailsAsync(bool hasChanges)
    {
        IsBusy = true;

        try
        {
            if (CurrentProductDetails != null && hasChanges)
            {
                SyncProductWithSelectedItems();
                
                if (_isNewProduct)
                {
                    var createdProductId = await _productService.CreateProductAsync(CurrentProductDetails.ToDto());

                    if (createdProductId > 0)
                    {
                        _productId = createdProductId;
                        _isNewProduct = false;
                        
                        var msg = MessageBoxManager.GetMessageBoxStandard("Создание",
                            "Товар успешно создан.",
                            icon: Icon.Success);
                        await msg.ShowAsync();
                        
                        await LoadProductDetailsAsync();
                    }
                }
                else
                {
                    await _productService.UpdateProductDetailsAsync(CurrentProductDetails.ToDto());

                    var msg = MessageBoxManager.GetMessageBoxStandard("Сохранение",
                        "Изменения успешно сохранены.",
                        icon: Icon.Success);
                    await msg.ShowAsync();
                }
            }
            else if (!_isNewProduct)
            {
                var msg = MessageBoxManager.GetMessageBoxStandard("Сохранение",
                    "Изменения не были обнаружены.",
                    icon: Icon.Info);
                await msg.ShowAsync();
            }
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, 
                _isNewProduct 
                    ? $"Error creating product viewmodel" 
                    : $"Error saving product viewmodel: id:{CurrentProductDetails?.Id}");
                    
            var msg = MessageBoxManager.GetMessageBoxStandard(_isNewProduct ? "Создание" : "Сохранение",
                _isNewProduct 
                    ? "Произошла ошибка создания товара." 
                    : "Произошла ошибка сохранения.\nИзменения не были сохранены.",
                icon: Icon.Error);
            await msg.ShowAsync();
            
            if (_isNewProduct)
            {
                IsEditing = true;
            }
            else
            {
                await LoadProductDetailsAsync();
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void AddParameter()
    {
        if (CurrentProductDetails == null) return;
        
        var defaultUnit = UnitList?.FirstOrDefault() ?? "шт";
        
        var newParameter = new ParametersModel(new ParametersDTO()
        {
            Name = "Новый параметр",
            Value = "Значение",
            Unit = defaultUnit
        });
        
        CurrentProductDetails.Parameters.Add(newParameter);
        SelectedParameter = newParameter;
    }

    [RelayCommand]
    private async Task RemoveParameterAsync(ParametersModel parameter)
    {
        if (CurrentProductDetails == null || parameter == null) return;
        
        try
        {
            var dialog = MessageBoxManager.GetMessageBoxStandard(
                "Подтверждение удаления",
                $"Вы действительно хотите удалить параметр:" +
                $" \"{parameter.Name}" +
                $" {parameter.Value}" +
                $" {parameter.Unit}\" ?",
                ButtonEnum.YesNo,
                icon: Icon.Question);
            
            var result = await dialog.ShowAsync();

            if (result == ButtonResult.Yes)
            {
                CurrentProductDetails.Parameters.Remove(parameter);
                SelectedParameter = null;
                
                var successMsg = MessageBoxManager.GetMessageBoxStandard(
                    "Успех",
                    "Параметр удален",
                    ButtonEnum.Ok,
                    Icon.Success
                );
                await successMsg.ShowAsync();
            }
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, $"Error removing parameter on viewmodel: {parameter.Name}");
        }
        
    }

    [RelayCommand]
    private async Task DeleteProductAsync()
    {
        try
        {
            var dialog = MessageBoxManager.GetMessageBoxStandard("Подтверждение действия",
                $"Вы действительно хотите удалить товар: \"{CurrentProductDetails?.Name}\"?",
                ButtonEnum.YesNo,
                icon: Icon.Warning);
            
            var result = await dialog.ShowAsync();

            if (result == ButtonResult.Yes)
            {
                bool isSuccess = await _productService.SoftDeleteProductAsync(_productId);

                if (isSuccess)
                {
                    var msg = MessageBoxManager.GetMessageBoxStandard("Удаление",
                        "Товар был успешно удален",
                        icon: Icon.Success);
                    await msg.ShowAsync();
                    
                    IsEditing = false;
                    HasChanges = false;
                    await GoToCatalogAsync();
                }
                else
                {
                    var msg = MessageBoxManager.GetMessageBoxStandard("Ошибка",
                        "Произошла ошибка удаления",
                        icon: Icon.Error);
                    await msg.ShowAsync();
                }
            }
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, $"Error removing product viewmodel: id: {_productId}");
        }
    }

    private void SyncProductWithSelectedItems()
    {
        if (CurrentProductDetails == null) return;
        
        if (SelectedCountry != null && CurrentProductDetails.Country != SelectedCountry)
            CurrentProductDetails.Country = SelectedCountry;
        
        if (SelectedCategory != null && CurrentProductDetails.Category != SelectedCategory)
            CurrentProductDetails.Category = SelectedCategory;
        
        if (SelectedProducer != null && CurrentProductDetails.Producer != SelectedProducer)
            CurrentProductDetails.Producer = SelectedProducer;
        
    }
    private async Task LoadProducersAsync()
    {
        try
        {
            var producers = await _productService.GetProducersAsync();

            if (producers != null)
            {
                ProducerList = new ObservableCollection<string>(producers);
            }
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, $"Error loading producers viewmodel");
        }
    }
    
    private async Task LoadCategoriesAsync()
    {
        try
        {
            var categories = await _productService.GetCategoriesAsync();

            if (categories != null)
            {
                CategoryList = new ObservableCollection<string>(categories);
            }
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, $"Error loading categories viewmodel");
        }
    }
    
    private async Task LoadCountriesAsync()
    {
        try
        {
            var countries = await _productService.GetCountriesAsync();

            if (countries != null)
            {
                CountryList = new ObservableCollection<string>(countries);
            }
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, $"Error loading countries viewmodel");
        }
    }
    
    private async Task LoadUnitsAsync()
    {
        try
        {
            var units = await _productService.GetUnitsAsync();

            if (units != null)
            {
                UnitList = new ObservableCollection<string>(units);
            }
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, $"Error loading units viewmodel");
        }
    }
    private async Task LoadAllReferenceDataAsync()
    {
        try
        {
            await LoadCountriesAsync();
            await LoadUnitsAsync();
            await LoadCategoriesAsync();
            await LoadProducersAsync();
        }
        catch (Exception ex)
        {
            AppLogger.LogError(ex, "Error loading all reference data");
        }
    }
    private ProductDetailsModel CloneProduct(ProductDetailsModel product)
    {
        return new ProductDetailsModel(product.ToDto());
    }
}