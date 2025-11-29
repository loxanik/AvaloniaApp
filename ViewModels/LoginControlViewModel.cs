using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Shop.Interfaces;
using Shop.Messages;
using Shop.Utils;

namespace Shop.ViewModels;

public partial class LoginControlViewModel(IAuthService authService) : ViewModelBase
{
    public LoginControlViewModel() : this(null!)
    {
    }

    private readonly IAuthService _authService = authService;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LoginButtonText))]
    private bool _isBusy;
    
    [ObservableProperty]
    private string _selectedRole;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Заполните поле")]
    [MinLength(5, ErrorMessage = "Минимальная длина 5"), MaxLength(15, ErrorMessage = "Максимальная длина 15")]
    private string _login;
    
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Заполните поле")]
    private string _password;
    
    public bool HasErrors => GetErrors().Any() ? true : false;
    
    public bool CanLogin => !HasErrors && !IsBusy;
    
    public string LoginButtonText => IsBusy ? "Вход..." : "Войти";
    
    partial void OnLoginChanged(string value)
    {
        LoginCommand.NotifyCanExecuteChanged();
    }

    partial void OnPasswordChanged(string value)
    {
        LoginCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void SwitchToRegister()
    {
        WeakReferenceMessenger.Default.Send(new ChangeViewModelMessage(typeof(RegistrationControlViewModel)));
    }
    
    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync()
    {
        IsBusy = true;
        try
        {
            var isSuccess = await _authService.UserLoginAsync(Login, Password);

            if (isSuccess)
            {
                WeakReferenceMessenger.Default.Send(new ChangeViewModelMessage(typeof(ProductsCatalogControlViewModel)));
            }
            else
            {
                var msg = MessageBoxManager.GetMessageBoxStandard("Логин", "Ошибка входа.\nПроверьте данные", icon: Icon.Error);
                await msg.ShowAsync();
            }
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, $"Login viewmodel error");
        }
        finally
        {
            IsBusy = false;
        }
        
    }
    
}