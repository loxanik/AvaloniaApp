using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
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
    private bool _isEditing;
    
    [ObservableProperty]
    private bool _hasChanges;
    
    public ProductDetailsControlViewModel(IProductService productService, IUserContext userContext)
    {
        _productService = productService;
        _userContext = userContext;
    }
    
    public void InitializeParam(object param)
    {
        if (param is not int productId) return;
        
        _productId = productId;
        _isNewProduct = productId == 0;
        
        if (_isNewProduct)
        {
            InitializeNewProduct();
        }
        else
        {
            _productId = productId;
            _ = LoadProductDetails();
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
            Parameters = new List<ParametersDTO>()
        };

        CurrentProductDetails = new ProductDetailsModel(newProductDto);

        if (CanEdit)
        {
            BeginEdit();
        }
    }
    private async Task LoadProductDetails()
    {
        try
        {
            ProductDetailsDTO? dto = null;
            
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
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, $"Error on load product details viewmodel: id: {_productId} ");
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
        if (IsEditing && CurrentProductDetails != null && _originalProduct != null)
        {
            HasChanges = CurrentProductDetails.HasChanges(_originalProduct);
        }
    }

    private void OnProductPropertyChanged(object? sender, PropertyChangedEventArgs e)
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
        }
        else if (_originalProduct != null)
        {
            CurrentProductDetails.Name = _originalProduct.Name;
            CurrentProductDetails.Description = _originalProduct.Description;
            CurrentProductDetails.Price = _originalProduct.Price;
            CurrentProductDetails.Producer = _originalProduct.Producer;
            CurrentProductDetails.Category = _originalProduct.Category;
            CurrentProductDetails.Country = _originalProduct.Country;
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

       _ = SaveProductDetails(hasChanges);
    }

    private async Task SaveProductDetails(bool hasChanges)
    {
        IsBusy = true;

        try
        {
            if (CurrentProductDetails != null && hasChanges)
            {
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
                        
                        await LoadProductDetails();
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
                await LoadProductDetails();
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
        
        CurrentProductDetails.Parameters.Add(new ParametersModel(new ParametersDTO()
        {
            Name = "Новый параметр",
            Value = "Значение",
            Unit = "Единица"
        }));
    }

    [RelayCommand]
    private async Task RemoveParameterAsync(ParametersModel parameter)
    {
        if (CurrentProductDetails == null) return;
        
        try
        {
            var dialog = MessageBoxManager.GetMessageBoxStandard(
                "Подтверждение удаления",
                $"Вы действительно хотите удалить параметр:" +
                $" \"{SelectedParameter?.Name}" +
                $" {SelectedParameter?.Value}" +
                $" {SelectedParameter?.Unit}\" ?",
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
    
    private ProductDetailsModel CloneProduct(ProductDetailsModel product)
    {
        return new ProductDetailsModel(product.ToDto());
    }
}