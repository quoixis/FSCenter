using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FSCenter.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}