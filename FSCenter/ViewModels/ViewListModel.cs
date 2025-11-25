using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FSCenter.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using NLog;

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