using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FSCenter.Views;

public partial class PaymentsListView : UserControl
{
    public PaymentsListView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}