using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using FSCenter.Data;
using FSCenter.Models;
using Microsoft.EntityFrameworkCore;
using NLog;


namespace FSCenter.ViewModels
{
    public partial class PaymentsListViewModel : ViewModelBase
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        [ObservableProperty]
        private ObservableCollection<Payment> payments = new();

        [ObservableProperty]
        private ObservableCollection<Payment> filteredPayments = new();

        [ObservableProperty]
        private string searchText = "";

        [ObservableProperty]
        private string selectedFilter = "Всі";

        [ObservableProperty]
        private DateTime? selectedDate;

        [ObservableProperty]
        private double totalAmount;

        [ObservableProperty]
        private int paymentsCount;

        [ObservableProperty]
        private string errorMessage = "";

        public ObservableCollection<string> FilterOptions { get; } = new()
        {
            "Всі",
            "Готівка",
            "Картка",
            "Переказ"
        };

        public PaymentsListViewModel()
        {
            LoadPayments();
        }

        private void LoadPayments()
        {
            logger.Info("Було відкрито сторінку 'Всі оплати'");
            try
            {
                using var context = new SportDBContext();
                var paymentsList = context.Payments
                    .Include(p => p.Client)
                    .Include(p => p.Membership)
                        .ThenInclude(m => m.Club)
                    .OrderByDescending(p => p.PaymentDate)
                    .ToList();

                logger.Debug($"Отримано {paymentsList.Count} платежів");

                Payments.Clear();
                foreach (var payment in paymentsList)
                {
                    Payments.Add(payment);
                }

                logger.Debug("Успішно завантажено");
                ApplyFilters();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Помилка завантаження платежів: {ex.Message}";
                logger.Error($"Виникла помилка:{ex.Message}");
            }
        }

        partial void OnSearchTextChanged(string value)
        {
            ApplyFilters();
        }

        partial void OnSelectedFilterChanged(string value)
        {
            ApplyFilters();
        }

        partial void OnSelectedDateChanged(DateTime? value)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            var query = Payments.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                logger.Debug($"Фільтрація за текстом: '{SearchText}'");
                query = query.Where(p =>
                    (p.Client?.FullName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (p.Description?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (p.Client?.Phone?.Contains(SearchText) ?? false) ||
                    (p.Client?.ClientId.ToString().Contains(SearchText) ?? false)
                );
            }

            if (SelectedFilter != "Всі")
            {
                query = query.Where(p => p.PaymentMethod == SelectedFilter);
                logger.Debug($"Фільтрація за методом оплати: '{SelectedFilter}'");
            }

            if (SelectedDate.HasValue)
            {
                logger.Debug($"Фільтрація за датою: {SelectedDate.Value:dd.MM.yyyy}");
                query = query.Where(p =>
                {
                    if (DateTime.TryParse(p.PaymentDate, out DateTime paymentDate))
                    {
                        return paymentDate.Date == SelectedDate.Value.Date;
                    }
                    logger.Warn($"Невірна дата у платежу ID={p.PaymentId}: '{p.PaymentDate}'");
                    return false;
                });
            }

            var filtered = query.ToList();
            logger.Debug($"Після фільтрації залишилось: {filtered.Count} платежів");

            FilteredPayments.Clear();
            foreach (var payment in filtered)
            {
                FilteredPayments.Add(payment);
            }

            TotalAmount = filtered.Sum(p => p.Amount);
            PaymentsCount = filtered.Count;
            logger.Info($"Фільтрація завершена. Кількість: {PaymentsCount}, Сума: {TotalAmount}");
        }

        [RelayCommand]
        private void ClearFilters()
        {
            SearchText = "";
            SelectedFilter = "Всі";
            SelectedDate = null;
            ApplyFilters();
        }
    }
}