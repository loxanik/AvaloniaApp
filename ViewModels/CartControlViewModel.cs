using System;
using System.Collections.ObjectModel;
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

public partial class CartControlViewModel : ViewModelBase
{
    private readonly ICartService _cartService;
    private readonly IOrderService _orderService;
    private readonly IUserContext _userContext;

    [ObservableProperty]
    private ObservableCollection<CartItemDTO> _items = [];

    [ObservableProperty]
    private decimal _total;

    [ObservableProperty]
    private bool _isEmpty = true;

    [ObservableProperty]
    private string _checkoutInfo = string.Empty;

    [ObservableProperty]
    private ObservableCollection<PaymentMethodOptionDTO> _paymentMethods = [];

    [ObservableProperty]
    private int _selectedPaymentMethodId;

    public CartControlViewModel(ICartService cartService, IOrderService orderService, IUserContext userContext)
    {
        _cartService = cartService;
        _orderService = orderService;
        _userContext = userContext;
        WeakReferenceMessenger.Default.Register<CartChangedMessage>(this, (_, _) => _ = RefreshAsync());
        _userContext.PropertyChanged += UserContextOnPropertyChanged;
        _ = LoadPaymentMethodsAsync();
        _ = RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            var cart = await _cartService.GetMyCartAsync() ?? new CartDTO();
            Items = new ObservableCollection<CartItemDTO>(cart.Items);
            Total = cart.Total;
            IsEmpty = !Items.Any();
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, "Refresh cart error viewmodel");
        }
    }

    private void UserContextOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(IUserContext.CurrentUser))
            return;

        CheckoutInfo = string.Empty;
        _ = RefreshAsync();
    }

    [RelayCommand]
    private async Task IncreaseAsync(object? parameter)
    {
        if (parameter is not int productId) return;
        await _cartService.AddToMyCartAsync(productId, 1);
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task DecreaseAsync(object? parameter)
    {
        if (parameter is not int productId) return;

        var item = Items.FirstOrDefault(i => i.ProductId == productId);
        if (item == null) return;

        await _cartService.SetMyCartItemQuantityAsync(productId, item.Quantity - 1);
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task RemoveAsync(object? parameter)
    {
        if (parameter is not int productId) return;
        await _cartService.RemoveFromMyCartAsync(productId);
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task ClearAsync()
    {
        await _cartService.ClearMyCartAsync();
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task CheckoutAsync()
    {
        CheckoutInfo = string.Empty;
        if (SelectedPaymentMethodId <= 0)
        {
            CheckoutInfo = "Выберите способ оплаты.";
            return;
        }

        var success = await _orderService.CreateOrderFromMyCartAsync(SelectedPaymentMethodId);
        CheckoutInfo = success
            ? "Заказ оформлен. Ожидайте подтверждения оплаты менеджером."
            : "Не удалось оформить заказ. Проверьте корзину и остатки товара.";
        await RefreshAsync();
    }

    private async Task LoadPaymentMethodsAsync()
    {
        try
        {
            var methods = await _orderService.GetPaymentMethodsAsync();
            PaymentMethods = new ObservableCollection<PaymentMethodOptionDTO>(methods);

            if (SelectedPaymentMethodId == 0 && PaymentMethods.Count > 0)
                SelectedPaymentMethodId = PaymentMethods[0].Id;
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, "Load payment methods viewmodel error");
        }
    }
}