using System.Linq;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Shop.Entities;
using Shop.Interfaces;
using Shop.Messages;

namespace Shop.ViewModels;

public partial class UserProfileControlViewModel : ViewModelBase
{
    private readonly IUserContext _userContext;
    private readonly ILocalizationHelper _localizationHelper;
    public User? User => _userContext.CurrentUser;
    public PersonalInfo? PersonalInfo => User?.PersonalInfos.FirstOrDefault();
    public string LocalizedRole => _localizationHelper.LocalizateRole(User?.Role.Name);
    public string Patronymic => string.IsNullOrEmpty(PersonalInfo?.Patronymic) ? "не указано" : PersonalInfo.Patronymic;
    public bool CanEdit => User?.Role.Name is "admin";
    public bool CanAdd => User?.Role.Name is "manager" or "admin";
    
    public UserProfileControlViewModel(IUserContext userContext, ILocalizationHelper localizationHelper)
    {
        _userContext = userContext;
        _localizationHelper = localizationHelper;
        
        _userContext.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(_userContext.CurrentUser)) return;
            
            OnPropertyChanged(nameof(User));
            OnPropertyChanged(nameof(PersonalInfo));
            OnPropertyChanged(nameof(Patronymic));
            OnPropertyChanged(nameof(LocalizedRole));
            OnPropertyChanged(nameof(CanEdit));
            OnPropertyChanged(nameof(CanAdd));
        };
    }

    [RelayCommand]
    private void Logout()
    {
        _userContext.UserLogout();
        WeakReferenceMessenger.Default.Send(new ChangeViewModelMessage(typeof(LoginControlViewModel)));
    }

    [RelayCommand]
    private void OpenDeletedProducts()
    {
        WeakReferenceMessenger.Default.Send(new ChangeViewModelMessage(typeof(DeletedProductsListControlViewModel)));
    }

    [RelayCommand]
    private void AddNewProduct()
    {
        WeakReferenceMessenger.Default.Send(new ChangeViewModelMessage(typeof(ProductDetailsControlViewModel), 0));
    }
}