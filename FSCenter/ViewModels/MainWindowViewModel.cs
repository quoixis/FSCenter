using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FSCenter.Data;
using System;
using System.Linq;

namespace FSCenter.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        [ObservableProperty]
        private ViewModelBase currentView;

        [ObservableProperty]
        private string adminName = "Адміністратор";

        [ObservableProperty]
        private string pageTitle = "Головна сторінка";

        [ObservableProperty]
        private string currentDateTime;

        [ObservableProperty]
        private string lastLogin = "Останній вхід: --";

        private DispatcherTimer? _timer;

        public MainViewModel()
        {
            LoadUserInfo();
            CurrentView = new MainPageViewModel();// за замовчуванням гловна сторінка
            CheckAndDeactivateExpiredMemberships();
            StartClock();
            logger.Info("Відкрито головне вікно");
        }

        private void LoadUserInfo()
        {
            if (!string.IsNullOrEmpty(UserSession.FullName))
            {
                AdminName = UserSession.FullName;
            }

            if (UserSession.LastLogin.HasValue)
            {
                LastLogin = $"Останній вхід: {UserSession.LastLogin.Value:dd.MM.yyyy HH:mm:ss}";
            }
            logger.Info("Успішно завантажено");
        }

        private void CheckAndDeactivateExpiredMemberships()
        {
            try
            {
                using var context = new SportDBContext();
                var today = DateTime.Today.ToString("yyyy-MM-dd");

                var activeMemberships = context.Memberships
                    .Where(m => m.Status == "Активний")
                    .ToList();

                int deactivatedCount = 0;

                foreach (var membership in activeMemberships)
                {
                    bool isExpired = false;

                    if (!string.IsNullOrEmpty(membership.ExpiryDate))
                    {
                        if (DateTime.TryParse(membership.ExpiryDate, out DateTime expiryDate))
                        {
                            if (expiryDate.Date < DateTime.Today)
                            {
                                isExpired = true;
                            }
                        }
                    }

                    if (membership.SessionsRemaining <= 0)
                    {
                        isExpired = true;
                    }

                    if (isExpired)
                    {
                        membership.Status = "Завершений";
                        deactivatedCount++;
                    }
                }

                if (deactivatedCount > 0)
                {
                    context.SaveChanges();

                    System.Diagnostics.Debug.WriteLine($"Деактивовано {deactivatedCount} прострочених абонементів");
                }
                logger.Info("Всі прострочені абонементи та абонементи без зайнять позначені як 'Завершені'");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Помилка перевірки абонементів: {ex.Message}");
                logger.Error($"Виникла помилка:{ex.Message}");
            }
        }

        private void StartClock()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += (sender, e) => UpdateDateTime();
            _timer.Start();
            UpdateDateTime();
            logger.Debug("Годинник запущено");
        }

        private void UpdateDateTime()
        {
            CurrentDateTime = DateTime.Now.ToString("dd.MM.yyyy  HH:mm:ss");
        }

        [RelayCommand]
        private void NavigateToMainView()
        {
            CurrentView = new MainPageViewModel();
            PageTitle = "Головна сторінка";
        }

        [RelayCommand]
        private void NavigateToClientCreate()
        {
            CurrentView = new ClientCreateViewModel();
            PageTitle = "Новий клієнт";
        }

        [RelayCommand]
        private void NavigateToAttendance()
        {
            CurrentView = new AttendanceViewModel();
            PageTitle = "Відвідування";
        }

        [RelayCommand]
        private void NavigateToVisits()
        {
            CurrentView = new VisitsListViewModel();
            PageTitle = "Список відвідувань";
        }

        [RelayCommand]
        private void NavigateToClientDetails()
        {
            CurrentView = new ClientDetailsViewModel();
            PageTitle = "Інформація про клієнта";
        }

        [RelayCommand]
        private void NavigateToPayment()
        {
            CurrentView = new PaymentViewModel();
            PageTitle = "Оплата";
        }

        [RelayCommand]
        private void NavigateToPaymentsList()
        {
            CurrentView = new PaymentsListViewModel();
            PageTitle = "Всі оплати";
        }

        [RelayCommand]
        private void NavigateToClubs()
        {
            CurrentView = new ClubsViewModel();
            PageTitle = "Клуби";
        }

        [RelayCommand]
        private void Logout()
        {
            logger.Info($"Адміністратор {UserSession.FullName} виходить з системи {DateTime.Now}");
            _timer?.Stop();

            UserSession.UserId = 0;
            UserSession.Username = "";
            UserSession.FullName = "";
            UserSession.LastLogin = null;

            // відриття вікна логіну
            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var loginWindow = new Views.LoginWindow();
                loginWindow.Show();

                var mainWindow = desktop.Windows.OfType<Views.MainWindow>().FirstOrDefault();
                mainWindow?.Close();
                logger.Info("Головне вікно було закрите");
            }
        }
    }
}