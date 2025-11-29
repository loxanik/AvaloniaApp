using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Shop.DTOs;
using Shop.Interfaces;
using Shop.Messages;
using Shop.Utils;

namespace Shop.ViewModels;

public partial class ProductDetailsControlViewModel : ViewModelBase, IParameterized, IEditableObject
{
    private readonly IProductService _productService;
    private readonly IUserContext _userContext;
    
    private int _productId;
    private ProductDetailsDTO? _originalProduct;
    private bool _isBusy;
    
    public bool CanEdit => _userContext.CurrentUser?.Role.Name is "manager" or "admin";
    public string SaveButtonText => _isBusy ? "Сохранение..." : "Сохранить";
        
    [ObservableProperty]
    private ProductDetailsDTO? _currentProductDetails;
    
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
        if (param is int productId)
        {
            _productId = productId;
            Task.Run(async () => await LoadProductDetails());
        }
    }

    private async Task LoadProductDetails()
    {
        CurrentProductDetails = await _productService.GetProductDetailsAsync(_productId);
    }

    [RelayCommand]
    private void GoToCatalog()
    {
        if (IsEditing)
            CancelEdit();
        
        WeakReferenceMessenger.Default.Send(new ChangeViewModelMessage(typeof(ProductsCatalogControlViewModel)));
    }
    
    [RelayCommand]
    public void BeginEdit()
    {
        if (IsEditing) return;

        _originalProduct = CloneProduct(CurrentProductDetails);
        IsEditing = true;
        HasChanges = false;
    }

    [RelayCommand]
    public void CancelEdit()
    {
        if (!IsEditing) return; 
        
        CurrentProductDetails = CloneProduct(_originalProduct);
        IsEditing = false;
        HasChanges = false;
    }

    [RelayCommand]
    public void EndEdit()
    {
       if (!IsEditing) return;
       
       IsEditing = false;
       HasChanges = false;

       _ = UpdateProductDetails();
    }

    private async Task UpdateProductDetails()
    {
        try
        {
            //TODO сделать проверку на HasChanges
            if (CurrentProductDetails != null)
            {
                await _productService.UpdateProductDetailsAsync(CurrentProductDetails);
                CurrentProductDetails = await _productService.GetProductDetailsAsync(_productId);
            }
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, $"Error saving product viewmodel: id:{_currentProductDetails.Id}");
        }
    }
    
    partial void OnCurrentProductDetailsChanged(ProductDetailsDTO? value)
    {
        //TODO сделать отдельную модель продукта с полным покрытием propertyChanged
        if (IsEditing && _originalProduct != null)
            HasChanges = !IsProductsEqual(value, _originalProduct);
    }
    
    private ProductDetailsDTO CloneProduct(ProductDetailsDTO product)
    {
        return new ProductDetailsDTO()
        {
            Description = product.Description,
            Price = product.Price,
            Producer = product.Producer,
            Category = product.Category,
            Image = product.Image,
            Id = product.Id,
            Name = product.Name,
            DisplayImage = product.DisplayImage,
            Parameters = product.Parameters?.Select(p => new ParametersDTO()
            {
                Id = p.Id,
                Name = p.Name,
                Value = p.Value,
                Unit = p.Unit
            }).ToList()
        };
    }

    private bool IsProductsEqual(ProductDetailsDTO a, ProductDetailsDTO b)
    {
        return a.Name == b.Name
               && a.Price == b.Price
               && a.Producer == b.Producer
               && a.Category == b.Category
               && a.Description == b.Description;
    }
}