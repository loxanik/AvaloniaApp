using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Shop.Enums;
using Shop.Interfaces;
using Shop.Messages;
using Shop.Services;
using Shop.Utils;

namespace Shop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IUserContext _userContext;
    
    [ObservableProperty]
    private object? _currentViewModel;
    
    [ObservableProperty]
    private object? _currentProfileViewModel;

    [ObservableProperty] 
    private bool _isProfileVisible;

    [ObservableProperty]
    private NavigationSection _selectedNavigationSection = NavigationSection.None;
        
    partial void OnIsProfileVisibleChanged(bool value)
    {
        if (!_userContext.IsLoggedIn && value)
        {
            IsProfileVisible = false;
        }
    }
    public bool IsNavVisible => _userContext.IsLoggedIn;

    public bool IsCatalogActive => SelectedNavigationSection == NavigationSection.Catalog;

    public bool IsCartActive => SelectedNavigationSection == NavigationSection.Cart;

    public GridLength NavHeight => IsNavVisible ? new GridLength(0.1, GridUnitType.Star) : new GridLength(0);
    public MainWindowViewModel(IUserContext userContext)
    {
        _userContext = userContext;
        
        CurrentViewModel = Ioc.Default.GetRequiredService<LoginControlViewModel>();
        CurrentProfileViewModel = Ioc.Default.GetRequiredService<UserProfileControlViewModel>();

        UpdateActiveStates();

        PropertyChanged += OnPropertyChanged;

        WeakReferenceMessenger.Default.Register<ChangeViewModelMessage>(this, OnViewModelChanged);

        
        _userContext.PropertyChanged += userContextOnPropertyChanged;
    }

    private void OnViewModelChanged(object recipient, ChangeViewModelMessage message)
    {
        _ = ChangeViewModelAsync(message);
    }
    private async Task ChangeViewModelAsync(ChangeViewModelMessage message)
    {
        try
        {
            var viewModel = Ioc.Default.GetRequiredService(message.ViewModelType);

            if (message.Parameter != null && viewModel is IParameterized parameterized)
            {
                await parameterized.InitializeParam(message.Parameter);
            }
            
            CurrentViewModel = viewModel;
            SyncView();
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, $"Error in ChangeViewModelAsync: {message.ViewModelType.FullName}");
        }
    }

    private async void userContextOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        try
        {
            if (e.PropertyName == nameof(UserContext.IsLoggedIn)){
                OnPropertyChanged(nameof(NavHeight));
                OnPropertyChanged(nameof(IsNavVisible));

                if (!_userContext.IsLoggedIn)
                {
                    IsProfileVisible = false;
                    SelectedNavigationSection = NavigationSection.None;
                    UpdateActiveStates();

                    var catalogVm = Ioc.Default.GetRequiredService<ProductsCatalogControlViewModel>();
                    await catalogVm.ResetForNewUserAsync();
                }
            }
        }
        catch (Exception ex)
        {
            AppLogger.LogError(ex, $"Delegate error");
        }
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectedNavigationSection))
        {
            UpdateView();
            UpdateActiveStates();
        }

        if (e.PropertyName == nameof(CurrentViewModel))
        {
            SyncView();
        }
    }

    private void UpdateView()
    {
        CurrentViewModel = SelectedNavigationSection switch
        {
            NavigationSection.Cart => Ioc.Default.GetRequiredService<CartControlViewModel>(),
            NavigationSection.Catalog => Ioc.Default.GetRequiredService<ProductsCatalogControlViewModel>(),
            _ => CurrentViewModel
        };
    }
    
    [RelayCommand]
    private void SelectCatalog()
    {
        SelectedNavigationSection = NavigationSection.Catalog;
    }

    [RelayCommand]
    private void SelectCart()
    {
        SelectedNavigationSection = NavigationSection.Cart;
    }

    private void SyncView()
    {
        var newSelection = CurrentViewModel switch
        {
            ProductsCatalogControlViewModel => NavigationSection.Catalog,
            CartControlViewModel => NavigationSection.Cart,
            _ => NavigationSection.None
        };

        if (SelectedNavigationSection != newSelection)
        {
            SelectedNavigationSection = newSelection;
            UpdateActiveStates();
        }
    }
    
    private void UpdateActiveStates()
    {
        OnPropertyChanged(nameof(IsCatalogActive));
        OnPropertyChanged(nameof(IsCartActive));
    }

}