using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FSCenter.Views;

public partial class AttendanceView : UserControl
{
    public AttendanceView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}