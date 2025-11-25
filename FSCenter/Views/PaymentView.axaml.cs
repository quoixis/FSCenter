using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FSCenter.Views;

public partial class PaymentView : UserControl
{
    public PaymentView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}