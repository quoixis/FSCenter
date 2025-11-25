using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FSCenter.Views;

public partial class VisitsListView : UserControl
{
    public VisitsListView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}