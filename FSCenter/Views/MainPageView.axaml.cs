using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FSCenter.Views;

public partial class MainPageView : UserControl
{
    public MainPageView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}