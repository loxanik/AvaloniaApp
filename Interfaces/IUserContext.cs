using System.ComponentModel;
using Shop.Entities;

namespace Shop.Interfaces;

public interface IUserContext : INotifyPropertyChanged
{
    User? CurrentUser { get; set; }
    bool IsLoggedIn { get; }
    
    void UserLogout();
}