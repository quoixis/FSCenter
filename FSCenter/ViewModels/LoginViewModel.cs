using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FSCenter.Data;
using FSCenter.Views;
using NLog;
using System;
using System.Linq;

namespace FSCenter.ViewModels
{
    public partial class LoginViewModel : ViewModelBase
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private LoginWindow _loginWindow;

        [ObservableProperty]
        private string username = "";

        [ObservableProperty]
        private string password = "";

        [ObservableProperty]
        private string errorMessage = "";

        public LoginViewModel(LoginWindow loginWindow)
        {
            logger.Info($"Відкрито вікно авторизації: {DateTime.Now}");
            _loginWindow = loginWindow;
        }

        [RelayCommand]
        private void Login()
        {
            ErrorMessage = "";

            if (string.IsNullOrWhiteSpace(Username))
            {
                ErrorMessage = "Введіть логін!";
                logger.Warn("Не введено логін");
                return;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Введіть пароль!";
                logger.Warn("Не введено пароль");
                return;
            }

            try
            {
                using var context = new SportDBContext();

                var user = context.Users.FirstOrDefault(u =>
                    u.Username == Username && u.PasswordHash == Password);

                if (user == null)
                {
                    ErrorMessage = "Невірний логін або пароль!";
                    logger.Warn("Невірний логін або пароль");
                    return;
                }

                UserSession.UserId = user.UserId;
                UserSession.Username = user.Username;
                UserSession.FullName = user.FullName;
                UserSession.LastLogin = DateTime.Now;

                logger.Info($"Адміністратор {UserSession.FullName} увійшов у систему {UserSession.LastLogin}");

                var mainWindow = new MainWindow // відкривається мейн
                {
                    DataContext = new MainViewModel()
                };
                mainWindow.Show();

                _loginWindow.Close();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Помилка: {ex.Message}";
                logger.Error($"Виникла помилка:{ex.Message}");
            }
        }

        [RelayCommand]
        private void Exit()
        {
            logger.Info($"Програма була закрита {DateTime.Now}");
            _loginWindow.Close();
        }
    }

    public static class UserSession // хай тут буже
    {
        public static int UserId { get; set; }
        public static string Username { get; set; } = "";
        public static string FullName { get; set; } = "";
        public static DateTime? LastLogin { get; set; }
    }
}