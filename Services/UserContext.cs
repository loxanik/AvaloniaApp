using CommunityToolkit.Mvvm.ComponentModel;
using Shop.Entities;
using Shop.Interfaces;

namespace Shop.Services;

public partial class UserContext : ObservableObject, IUserContext
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLoggedIn))]
    private User? _currentUser;
    public bool IsLoggedIn => CurrentUser != null;
    public void UserLogout()
    {
        CurrentUser = null;
    }
}