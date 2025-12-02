using ClosedXML.Excel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FSCenter.Data;
using FSCenter.Models;
using Microsoft.EntityFrameworkCore;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;


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

        [ObservableProperty]
        private string successMessage = "";

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
        private void ExportDayReport()
        {
            try
            {
                logger.Info("Початок експорту");

                if (SelectedDate == null)
                {
                    ErrorMessage = "Оберіть дату";
                    logger.Warn("Не обрано дату для експорту");
                    System.Threading.Tasks.Task.Delay(5000).ContinueWith(_ =>
                    {
                        ErrorMessage = "";
                    }, System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
                    return;
                }

                using var context = new SportDBContext();
                var dateSrt = SelectedDate.Value.ToString("yyyy-MM--dd");

                var dayPayments = context.Payments
                .Include(p => p.Client)
                .Include(p => p.Membership).ThenInclude(m => m.Club)
                .AsEnumerable()
                .Where(p => DateTime.TryParse(p.PaymentDate, out DateTime pd) && pd.Date == SelectedDate.Value.Date)
                .OrderBy(p => p.PaymentDate)
                .ToList();

                if (dayPayments.Count == 0)
                {
                    logger.Warn("Немає оплат за обрану дату");
                    ErrorMessage = ("Немає оплат за обрану дату");
                    return;
                }
                logger.Debug($"Знайдено {dayPayments.Count} платежів за {SelectedDate}");

                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Денний звіт");

                worksheet.Cell(1, 1).Value = $"ФІНАНСОВИЙ ЗВІТ ЗА ДЕНЬ: {SelectedDate.Value:dd.MM.yyyy}";
                worksheet.Range(1, 1, 1, 7).Merge();
                worksheet.Cell(1, 1).Style.Font.Bold = true;
                worksheet.Cell(1, 1).Style.Font.FontSize = 16;
                worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                worksheet.Cell(3, 1).Value = "ID";
                worksheet.Cell(3, 2).Value = "Час";
                worksheet.Cell(3, 3).Value = "Клієнт";
                worksheet.Cell(3, 4).Value = "Телефон";
                worksheet.Cell(3, 5).Value = "Опис";
                worksheet.Cell(3, 6).Value = "Метод";
                worksheet.Cell(3, 7).Value = "Сума (грн)";

                var headerRange = worksheet.Range(3, 1, 3, 7);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                int row = 4;
                double cashTotal = 0, cardTotal = 0, transferTotal = 0;

                foreach (var p in dayPayments)
                {
                    worksheet.Cell(row, 1).Value = p.PaymentId;
                    worksheet.Cell(row, 2).Value = DateTime.TryParse(p.PaymentDate, out DateTime dt) ? dt.ToString("HH:mm") : "";
                    worksheet.Cell(row, 3).Value = p.Client?.FullName ?? "";
                    worksheet.Cell(row, 4).Value = p.Client?.Phone ?? "";
                    worksheet.Cell(row, 5).Value = p.Description;
                    worksheet.Cell(row, 6).Value = p.PaymentMethod;
                    worksheet.Cell(row, 7).Value = p.Amount;

                    switch (p.PaymentMethod)
                    {
                        case "Готівка": cashTotal += p.Amount; break;
                        case "Картка": cardTotal += p.Amount; break;
                        case "Переказ": transferTotal += p.Amount; break;
                    }

                    row++;
                }

                row++;
                worksheet.Cell(row, 6).Value = "ВСЬОГО:";
                worksheet.Cell(row, 6).Style.Font.Bold = true;
                worksheet.Cell(row, 7).Value = dayPayments.Sum(p => p.Amount);
                worksheet.Cell(row, 7).Style.Font.Bold = true;
                worksheet.Cell(row, 7).Style.Fill.BackgroundColor = XLColor.Yellow;

                row++;
                worksheet.Cell(row, 6).Value = "Готівка:";
                worksheet.Cell(row, 7).Value = cashTotal;

                row++;
                worksheet.Cell(row, 6).Value = "Картка:";
                worksheet.Cell(row, 7).Value = cardTotal;

                row++;
                worksheet.Cell(row, 6).Value = "Переказ:";
                worksheet.Cell(row, 7).Value = transferTotal;

                worksheet.Columns().AdjustToContents();
                worksheet.Column(7).Style.NumberFormat.Format = "#,##0.00";

                string baseDir = Path.Combine(AppContext.BaseDirectory, "Reports", "Excel", "Фінанси", "День");

                if (!Directory.Exists(baseDir))
                    Directory.CreateDirectory(baseDir);

                string fileName = $"Оплати_{SelectedDate:yyyy-MM-dd}.xlsx";
                string fullPath = Path.Combine(baseDir, fileName);

                workbook.SaveAs(fullPath);

                SuccessMessage = $"Звіт збережено: {fileName}";
                logger.Info($"Експорт успішний: {fullPath}");
                System.Threading.Tasks.Task.Delay(5000).ContinueWith(_ =>
                {
                   SuccessMessage="";
                }, System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Помилка експорту: {ex.Message}";
                logger.Error(ex, "Помилка експорту денного звіту");
            }
        }

        [RelayCommand]
        private void ExportMonthReport()
        {
            try
            {
                logger.Info("Початок експорту");

                if (SelectedDate == null)
                {
                    ErrorMessage = "Оберіть дату (місяць)";
                    logger.Warn("Не обрано дату для звіту");
                    System.Threading.Tasks.Task.Delay(5000).ContinueWith(_ =>
                    {
                       ErrorMessage="";
                    }, System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
                    return;
                }

                using var context = new SportDBContext();
                var year = SelectedDate.Value.Year;
                var month = SelectedDate.Value.Month;

                var monthPayments = context.Payments
                    .Include(p => p.Client)
                    .Include(p => p.Membership).ThenInclude(m => m.Club)
                    .AsEnumerable()
                    .Where(p => DateTime.TryParse(p.PaymentDate, out DateTime pd) && pd.Year == year && pd.Month == month)
                    .OrderBy(p => p.PaymentDate)
                    .ToList();

                if (monthPayments.Count == 0)
                {
                    ErrorMessage = $"Немає оплат за {SelectedDate.Value:MMMM yyyy}";
                    logger.Warn($"Немає даних для експорту за {year}-{month:00}");
                    return;
                }

                logger.Debug($"Знайдено {monthPayments.Count} платежів за {year}-{month:00}");

                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Місячний звіт");

                worksheet.Cell(1, 1).Value = $"ФІНАНСОВИЙ ЗВІТ ЗА МІСЯЦЬ: {SelectedDate.Value:MMMM yyyy}";
                worksheet.Range(1, 1, 1, 7).Merge();
                worksheet.Cell(1, 1).Style.Font.Bold = true;
                worksheet.Cell(1, 1).Style.Font.FontSize = 16;
                worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                worksheet.Cell(3, 1).Value = "ID";
                worksheet.Cell(3, 2).Value = "Дата";
                worksheet.Cell(3, 3).Value = "Клієнт";
                worksheet.Cell(3, 4).Value = "Телефон";
                worksheet.Cell(3, 5).Value = "Опис";
                worksheet.Cell(3, 6).Value = "Метод";
                worksheet.Cell(3, 7).Value = "Сума (грн)";

                var headerRange = worksheet.Range(3, 1, 3, 7);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                int row = 4;
                double cashTotal = 0, cardTotal = 0, transferTotal = 0;
                var dailyTotals = new Dictionary<int, double>();

                foreach (var p in monthPayments)
                {
                    worksheet.Cell(row, 1).Value = p.PaymentId;
                    worksheet.Cell(row, 2).Value = DateTime.TryParse(p.PaymentDate, out DateTime dt) ? dt.ToString("dd.MM.yyyy HH:mm") : "";
                    worksheet.Cell(row, 3).Value = p.Client?.FullName ?? "";
                    worksheet.Cell(row, 4).Value = p.Client?.Phone ?? "";
                    worksheet.Cell(row, 5).Value = p.Description;
                    worksheet.Cell(row, 6).Value = p.PaymentMethod;
                    worksheet.Cell(row, 7).Value = p.Amount;

                    switch (p.PaymentMethod)
                    {
                        case "Готівка": cashTotal += p.Amount; break;
                        case "Картка": cardTotal += p.Amount; break;
                        case "Переказ": transferTotal += p.Amount; break;
                    }

                    if (DateTime.TryParse(p.PaymentDate, out DateTime date))
                    {
                        int day = date.Day;
                        if (!dailyTotals.ContainsKey(day))
                            dailyTotals[day] = 0;
                        dailyTotals[day] += p.Amount;
                    }

                    row++;
                }

                row++;
                worksheet.Cell(row, 6).Value = "ВСЬОГО ЗА МІСЯЦЬ:";
                worksheet.Cell(row, 6).Style.Font.Bold = true;
                worksheet.Cell(row, 7).Value = monthPayments.Sum(p => p.Amount);
                worksheet.Cell(row, 7).Style.Font.Bold = true;
                worksheet.Cell(row, 7).Style.Fill.BackgroundColor = XLColor.Yellow;

                row++;
                worksheet.Cell(row, 6).Value = "Готівка:";
                worksheet.Cell(row, 7).Value = cashTotal;

                row++;
                worksheet.Cell(row, 6).Value = "Картка:";
                worksheet.Cell(row, 7).Value = cardTotal;

                row++;
                worksheet.Cell(row, 6).Value = "Переказ:";
                worksheet.Cell(row, 7).Value = transferTotal;

                row += 2;
                worksheet.Cell(row, 1).Value = "СТАТИСТИКА ПО ДНЯХ:";
                worksheet.Cell(row, 1).Style.Font.Bold = true;
                worksheet.Range(row, 1, row, 3).Merge();

                row++;
                worksheet.Cell(row, 1).Value = "День";
                worksheet.Cell(row, 2).Value = "Сума (грн)";
                worksheet.Range(row, 1, row, 2).Style.Font.Bold = true;
                worksheet.Range(row, 1, row, 2).Style.Fill.BackgroundColor = XLColor.LightGray;

                foreach (var day in dailyTotals.OrderBy(d => d.Key))
                {
                    row++;
                    worksheet.Cell(row, 1).Value = $"{day.Key:00}.{month:00}.{year}";
                    worksheet.Cell(row, 2).Value = day.Value;
                }

                worksheet.Columns().AdjustToContents();
                worksheet.Column(7).Style.NumberFormat.Format = "#,##0.00";
                worksheet.Column(2).Style.NumberFormat.Format = "#,##0.00";

                string baseDir = Path.Combine(AppContext.BaseDirectory, "Reports", "Excel", "Фінанси", "Місяць");

                if (!Directory.Exists(baseDir))
                    Directory.CreateDirectory(baseDir);

                string fileName = $"Оплати_{SelectedDate:yyyy-MM}.xlsx";
                string fullPath = Path.Combine(baseDir, fileName);

                workbook.SaveAs(fullPath);

                SuccessMessage = $"Звіт збережено: {fileName}";
                logger.Info($"Експорт успішний: {fullPath}");
                System.Threading.Tasks.Task.Delay(5000).ContinueWith(_ =>
                {
                   SuccessMessage="";
                }, System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Помилка експорту: {ex.Message}";
                logger.Error(ex, "Помилка експорту місячного звіту");
            }
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