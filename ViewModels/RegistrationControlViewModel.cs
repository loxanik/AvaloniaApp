using System;
using System.ComponentModel;
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
using Shop.Services;
using Shop.Utils;

namespace Shop.ViewModels;

public partial class RegistrationControlViewModel(IAuthService authService) : ViewModelBase
{
    private readonly IAuthService _authService = authService;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Заполните поле")]
    [MinLength(5, ErrorMessage = "Минимальная длина 5"), MaxLength(15, ErrorMessage = "Максимальная длина 15")]
    private string _login;
    
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Заполните поле")]
    [MaxLength(15, ErrorMessage = "Максимальная длина 15")]
    private string _password;
    
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Заполните поле")]
    [property:Compare("Password", ErrorMessage = "Пароли не совпадают")]
    private string _confirmPassword;
    
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Заполните поле")]
    [MinLength(5, ErrorMessage = "Минимальная длина 5"), MaxLength(30, ErrorMessage = "Максимальная длина 30")]
    [EmailAddress(ErrorMessage = "Неккоректная почта")]
    private string _email;
    
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Заполните поле")]
    [MinLength(2, ErrorMessage = "Минимальная длина 2"), MaxLength(15, ErrorMessage = "Максимальная длина 15")]
    private string _firstName;
    
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Заполните поле")]
    [MinLength(2, ErrorMessage = "Минимальная длина 2"), MaxLength(15, ErrorMessage = "Максимальная длина 20")]
    private string _lastName;
    
    [ObservableProperty]
    private string? _patronymic;
    
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Заполните поле")]
    [Length(11, 11, ErrorMessage = "Длина 11 символов")]
    private string _phoneNumber;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RegisterButtonText))]
    private bool _isBusy;
    
    public bool HasErrors => GetErrors().Any() ? true : false;
    
    public bool CanRegister => !HasErrors && !IsBusy;
    
    public string RegisterButtonText => IsBusy ? "Регистрация..." : "Зарегистрироваться";
    
    public RegistrationControlViewModel() : this(null!)
    {
        this.PropertyChanged += OnAnyPropertyChanged;
    }

    private void OnAnyPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Login) ||
            e.PropertyName == nameof(Password) ||
            e.PropertyName == nameof(ConfirmPassword) ||
            e.PropertyName == nameof(Email) ||
            e.PropertyName == nameof(FirstName) ||
            e.PropertyName == nameof(LastName) ||
            e.PropertyName == nameof(Patronymic) ||
            e.PropertyName == nameof(PhoneNumber))
        {
            RegisterCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand(CanExecute = nameof(CanRegister))]
    private async Task RegisterAsync()
    {
        IsBusy = true;
        try
        {
            bool isSuccess = await _authService.ClientRegisterAsync(Login, Password, FirstName, LastName, Patronymic, PhoneNumber,
                Email);

            if (isSuccess)
            {
                var msg = MessageBoxManager.GetMessageBoxStandard("Регистрация",
                    "Успех",
                    icon: Icon.Success);
                await msg.ShowAsync();
                ChangeToLogin();
            }
            else
            {
                var msg = MessageBoxManager.GetMessageBoxStandard("Регистрация",
                    "Ошибка регистрации",
                    icon: Icon.Error);
                await msg.ShowAsync();
            }
        }
        catch (Exception e)
        {
            AppLogger.LogError(e, $"Registration viewmodel error: {Login}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ChangeToLogin()
    {
        WeakReferenceMessenger.Default.Send(new ChangeViewModelMessage(typeof(LoginControlViewModel)));
    }
}