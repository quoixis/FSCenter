using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FSCenter.ViewModels;

namespace FSCenter.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            AvaloniaXamlLoader.Load(this);
            DataContext = new LoginViewModel(this);
        }
    }
}