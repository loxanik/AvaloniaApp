using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shop.DTOs;
using Shop.Interfaces;
using Shop.Utils;

namespace Shop.ViewModels;

public partial class OrdersHistoryControlViewModel : ViewModelBase
{
    private readonly IOrderService _orderService;

    [ObservableProperty]
    private ObservableCollection<OrderSummaryDTO> _orders = [];

    [ObservableProperty]
    private bool _isEmpty = true;

    public OrdersHistoryControlViewModel(IOrderService orderService)
    {
        _orderService = orderService;
        _ = RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            var orders = await _orderService.GetMyOrdersAsync();
            Orders = new ObservableCollection<OrderSummaryDTO>(orders);
            IsEmpty = Orders.Count == 0;
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, "Refresh my orders error");
        }
    }
}
