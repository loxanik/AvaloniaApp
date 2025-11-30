using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Shop.ViewModels;
using Shop.Views;
using Microsoft.Extensions.DependencyInjection;
using Shop.Entities;
using Shop.Helpers;
using Shop.Interfaces;
using Shop.Services;

namespace Shop;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        this.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Light;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();

        services.AddDbContext<ShopContext>(options =>
            options.UseNpgsql("Host=localhost;Database=Shop;Username=postgres;Password=0000"));
        
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IProductService, ProductService>();
        services.AddSingleton<IUserContext, UserContext>();
        services.AddSingleton<ILocalizationHelper, LocalizationHelper>();
        services.AddSingleton<IImageService, ImageService>();
        
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<ProductsCatalogControlViewModel>();
        services.AddSingleton<UserProfileControlViewModel>();
        services.AddSingleton<CartControlViewModel>();
        
        services.AddTransient<LoginControlViewModel>();
        services.AddTransient<RegistrationControlViewModel>();
        services.AddTransient<ProductDetailsControlViewModel>();
        
        Ioc.Default.ConfigureServices(services.BuildServiceProvider());
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = new MainWindow
            {
                DataContext = Ioc.Default.GetRequiredService<MainWindowViewModel>(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}