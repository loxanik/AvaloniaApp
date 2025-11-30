using System;
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
    
    public bool CanEdit => _userContext.CurrentUser?.Role.Name is "manager" or "admin";
    public string SaveButtonText => IsBusy ? "Сохранение..." : "Сохранить";
    
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
        Task.Run(async () => await LoadProductDetails());
    }

    private async Task LoadProductDetails()
    {
        try
        {
            var dto = await _productService.GetProductDetailsAsync(_productId);
            
            if (dto == null) return;
            
            CurrentProductDetails = new ProductDetailsModel(dto);
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, $"Error on load product details viewmodel: id: {_productId} ");
        }
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
        if (IsEditing || CurrentProductDetails == null) return;

        _originalProduct = CloneProduct(CurrentProductDetails);
        IsEditing = true;
        HasChanges = false;
        
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
        
        CurrentProductDetails.Name = _originalProduct.Name;
        CurrentProductDetails.Description = _originalProduct.Description;
        CurrentProductDetails.Price = _originalProduct.Price;
        CurrentProductDetails.Producer = _originalProduct.Producer;
        CurrentProductDetails.Category = _originalProduct.Category;
        CurrentProductDetails.Country = _originalProduct.Country;
        
        IsEditing = false;
        HasChanges = false;
    }

    [RelayCommand]
    public void EndEdit()
    {
       if (!IsEditing || CurrentProductDetails == null) return;
       
       bool hasChanges = HasChanges;
       
       CurrentProductDetails.PropertyChanged -= OnProductPropertyChanged;

       foreach (var parameter in CurrentProductDetails.Parameters)
       {
           parameter.PropertyChanged -= OnProductPropertyChanged;
       }
        
       CurrentProductDetails.Parameters.CollectionChanged -= OnParametersCollectionChanged;

       IsEditing = false;
       HasChanges = false;

       _ = UpdateProductDetails(hasChanges);
    }

    private async Task UpdateProductDetails(bool hasChanges)
    {
        IsBusy = true;

        try
        {
            //TODO сделать проверку на HasChanges
            if (CurrentProductDetails != null && hasChanges)
            {
                await _productService.UpdateProductDetailsAsync(CurrentProductDetails.ToDto());

                var msg = MessageBoxManager.GetMessageBoxStandard("Сохранение",
                    "Изменения успешно сохранены.",
                    icon: Icon.Info);
                await msg.ShowAsync();
            }
            else
            {
                var msg = MessageBoxManager.GetMessageBoxStandard("Сохранение",
                    "Изменения не были обнаружены.",
                    icon: Icon.Info);
                await msg.ShowAsync();
            }
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, $"Error saving product viewmodel: id:{CurrentProductDetails?.Id}");
            var msg = MessageBoxManager.GetMessageBoxStandard("Сохранение",
                "Произошла ошибка сохранения." +
                "\nИзменения не были сохранены.",
                icon: Icon.Error);
            await msg.ShowAsync();
        }
        finally
        {
            await LoadProductDetails();
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
    private void RemoveParameter(ParametersModel parameter)
    {
        if (CurrentProductDetails == null) return;

        CurrentProductDetails.Parameters.Remove(parameter);
    }
    
    private ProductDetailsModel CloneProduct(ProductDetailsModel product)
    {
        return new ProductDetailsModel(product.ToDto());
    }
}