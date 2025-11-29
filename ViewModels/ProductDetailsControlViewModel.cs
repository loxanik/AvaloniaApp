using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Shop.DTOs;
using Shop.Interfaces;
using Shop.Messages;

namespace Shop.ViewModels;

public partial class ProductDetailsControlViewModel : ViewModelBase, IParameterized
{
    private readonly IProductService _productService;
    private readonly IUserContext _userContext;
    
    private int _productId;
    
    public bool CanEdit => _userContext.CurrentUser?.Role.Name is "manager" or "admin";

    [ObservableProperty]
    private ProductDetailsDTO? _currentProductDetails;
    
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
        WeakReferenceMessenger.Default.Send(new ChangeViewModelMessage(typeof(ProductsCatalogControlViewModel)));
    }
}