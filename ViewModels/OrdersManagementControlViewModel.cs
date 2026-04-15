using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shop.DTOs;
using Shop.Interfaces;
using Shop.Utils;

namespace Shop.ViewModels;

public partial class OrdersManagementControlViewModel : ViewModelBase
{
    private readonly IOrderService _orderService;

    [ObservableProperty]
    private ObservableCollection<OrderSummaryDTO> _orders = [];

    [ObservableProperty]
    private bool _isEmpty = true;

    [ObservableProperty]
    private string _actionInfo = string.Empty;

    public OrdersManagementControlViewModel(IOrderService orderService)
    {
        _orderService = orderService;
        _ = RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            var orders = await _orderService.GetAllOrdersAsync();
            Orders = new ObservableCollection<OrderSummaryDTO>(orders);
            IsEmpty = Orders.Count == 0;
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, "Refresh orders management error");
        }
    }

    [RelayCommand]
    private async Task ConfirmPaymentAsync(object? parameter)
    {
        if (parameter is not int orderId)
            return;

        var success = await _orderService.ConfirmPaymentAsync(orderId);
        ActionInfo = success ? "Статус заказа обновлен: оплата подтверждена." : "Не удалось обновить статус заказа.";
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task CancelOrderAsync(object? parameter)
    {
        if (parameter is not int orderId)
            return;

        var success = await _orderService.CancelOrderAsync(orderId);
        ActionInfo = success ? "Заказ отменен." : "Не удалось отменить заказ.";
        await RefreshAsync();
    }
}
