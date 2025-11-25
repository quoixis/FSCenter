using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FSCenter.Views;

public partial class ClientDetailsView : UserControl
{
    public ClientDetailsView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}