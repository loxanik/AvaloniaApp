using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Input;

namespace Shop.Views;

public partial class ProductDetailsControlView : UserControl
{
    public ProductDetailsControlView()
    {
        InitializeComponent();
    }
    
    private void ImageBorder_PointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is Border border)
        {
            border.Opacity = 1;
        }
    }
    
    private void ImageBorder_PointerExited(object? sender, PointerEventArgs e)
    {
        if (sender is Border border)
        {
            border.Opacity = 0.7;
        }
    }
}