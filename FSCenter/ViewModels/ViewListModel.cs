using ClosedXML.Excel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FSCenter.Data;
using Microsoft.EntityFrameworkCore;
using NLog;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace FSCenter.ViewModels
{
    public partial class VisitsListViewModel : ViewModelBase
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [ObservableProperty]
        private DateTime selectedDate = DateTime.Today;

        [ObservableProperty]
        private string searchQuery = "";

        [ObservableProperty]
        private ObservableCollection<VisitItem> visits = new();

        [ObservableProperty]
        private int totalVisits = 0;

        [ObservableProperty]
        private string reportError = "";

        public VisitsListViewModel()
        {
            LoadVisits();
            logger.Info("Відкрито сторінку 'Відвідування'");
        }

        [RelayCommand]
        private void LoadVisits()
        {
            
            logger.Debug($"Завантаження відвідувань на дату {SelectedDate:yyyy-MM-dd}, пошук: '{SearchQuery}'");

            try
            {
                using var context = new SportDBContext();
                var dateStr = SelectedDate.ToString("yyyy-MM-dd");

                var query = context.Visits
                    .Include(v => v.Membership).ThenInclude(m => m.Client)
                    .Include(v => v.Membership).ThenInclude(m => m.Club)
                    .Where(v => v.VisitDate != null && v.VisitDate.StartsWith(dateStr))
                    .OrderByDescending(v => v.VisitDate);

                var results = query.ToList();
                logger.Debug($"Отримано {results.Count} записів перед фільтрацією");

                if (!string.IsNullOrWhiteSpace(SearchQuery))
                {
                    var search = SearchQuery.ToLower();
                    results = results.Where(v =>
                        (v.Membership?.Client?.FullName ?? "").ToLower().Contains(search) ||
                        (v.Membership?.Club?.Name ?? "").ToLower().Contains(search)
                    ).ToList();
                    logger.Debug($"Після фільтрації залишилось {results.Count} записів");
                }

                Visits.Clear();
                foreach (var v in results)
                {
                    Visits.Add(new VisitItem
                    {
                        ClientName = v.Membership?.Client?.FullName ?? "Невідомо",
                        ClubName = v.Membership?.Club?.Name ?? "Невідомо",
                        VisitTime = v.VisitDate ?? "",
                        Notes = v.Notes ?? ""
                    });
                }

                TotalVisits = Visits.Count;
                logger.Debug($"Успішно завантажено {TotalVisits} відвідувань");
            }
            catch(Exception ex)
            {
                logger.Error($"Виникла помилка:{ex.Message}");
                Visits.Clear();
                TotalVisits = 0;
            }
        }

        [RelayCommand]
        private void Search()
        {
            LoadVisits();
        }

        [RelayCommand]
        private void Clear()
        {
            SearchQuery = "";
            LoadVisits();
        }

        partial void OnSelectedDateChanged(DateTime value)
        {
            LoadVisits();
        }

        [RelayCommand]
        private void ExportToExcel()
        {
            try
            {
                logger.Info("Початок експорту");

                if (Visits.Count == 0)
                {
                    ReportError = "Немає даних для експорту!";
                    logger.Warn("Спроба експорту без даних");
                    return;
                }

                using var workbook = new XLWorkbook();
                var ws = workbook.AddWorksheet("Відвідування");

                ws.Cell(1, 1).Value = "Клієнт";
                ws.Cell(1, 2).Value = "Клуб";
                ws.Cell(1, 3).Value = "Час";
                ws.Cell(1, 4).Value = "Примітки";

                var header = ws.Range(1, 1, 1, 4);
                header.Style.Font.Bold = true;
                header.Style.Fill.BackgroundColor = XLColor.LightGray;
                header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                int row = 2;
                foreach (var v in Visits)
                {
                    ws.Cell(row, 1).Value = v.ClientName;
                    ws.Cell(row, 2).Value = v.ClubName;

                    if (DateTime.TryParse(v.VisitTime, out var dateTime))
                        ws.Cell(row, 3).Value = dateTime.ToString("HH:mm:ss");
                    else
                        ws.Cell(row, 3).Value = v.VisitTime;

                    ws.Cell(row, 4).Value = v.Notes;
                    row++;
                }

                ws.Cell(row + 1, 1).Value = "Всього відвідувань:";
                ws.Cell(row + 1, 2).Value = Visits.Count;
                ws.Cell(row + 1, 1).Style.Font.Bold = true;
                ws.Cell(row + 1, 2).Style.Font.Bold = true;

                ws.Columns().AdjustToContents();

                string baseDir = Path.Combine(AppContext.BaseDirectory, "Reports", "Excel", "Відвідування", "День");

                if (!Directory.Exists(baseDir))
                    Directory.CreateDirectory(baseDir);

                string fileName = $"Відвідування_{SelectedDate:yyyy-MM-dd}.xlsx";
                string fullPath = Path.Combine(baseDir, fileName);

                workbook.SaveAs(fullPath);

                ReportError = $"Звіт збережено: {fileName}";
                logger.Info($"Експорт успішний: {fullPath}");

            }
            catch (Exception ex)
            {
                ReportError = "Помилка експорту";
                logger.Error($"Виникла помилка:{ex.Message}");
            }
        }
    }

    public partial class VisitItem : ObservableObject
    {
        [ObservableProperty] private string clientName = "";
        [ObservableProperty] private string clubName = "";
        [ObservableProperty] private string visitTime = "";
        [ObservableProperty] private string notes = "";

        public string TimeFormatted
        {
            get
            {
                if (string.IsNullOrWhiteSpace(VisitTime)) return "";
                try { return DateTime.Parse(VisitTime).ToString("HH:mm:ss"); }
                catch { return VisitTime; }
            }
        }
    }
}