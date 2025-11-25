using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FSCenter.Data;
using NLog;
using System;
using System.Linq;

namespace FSCenter.ViewModels
{
    public partial class MainPageViewModel : ViewModelBase
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [ObservableProperty]
        private int visitsToday;

        [ObservableProperty]
        private double paymentsToday;

        [ObservableProperty]
        private int activeMemberships;

        [ObservableProperty]
        private int totalClients;

        [ObservableProperty]
        private string currentDate;

        [ObservableProperty]
        private string errorMessage = "";

        public MainPageViewModel()
        {
            logger.Info("Відкрито головну сторінку");
            CurrentDate = DateTime.Now.ToString("dddd, dd MMMM yyyy", new System.Globalization.CultureInfo("uk-UA"));
            LoadStatistics();
        }

        private void LoadStatistics()
        {
            try
            {
                using var context = new SportDBContext();
                var today = DateTime.Today.ToString("yyyy-MM-dd");

                VisitsToday = context.Visits.Count(v => v.VisitDate != null && v.VisitDate.StartsWith(today));

                PaymentsToday = context.Payments
                    .Where(p => p.PaymentDate != null && p.PaymentDate.StartsWith(today))
                    .Sum(p => (double?)p.Amount) ?? 0;

                ActiveMemberships = context.Memberships.Count(m => m.Status == "Активний");

                TotalClients = context.Clients.Count(c => c.IsActive == 1); // активні клієнти
                logger.Info("Успішно завантажено");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Помилка завантаження статистики: {ex.Message}";
                logger.Error($"Виникла помилка:{ex.Message}");
            }
        }

        [RelayCommand]
        private void Refresh()
        {
            LoadStatistics();
        }
    }
}