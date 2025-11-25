using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FSCenter.Data;
using FSCenter.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NLog;

namespace FSCenter.ViewModels
{
    public partial class AttendanceViewModel : ViewModelBase
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [ObservableProperty]
        private string searchText = "";

        [ObservableProperty]
        private ObservableCollection<MembershipItem> memberships = new();

        [ObservableProperty]
        private string statusMessage = "Введіть ID або прізвище для пошуку";

        [ObservableProperty]
        private int totalVisitsToday = 0;

        [ObservableProperty]
        private bool hasResults = false;

        public AttendanceViewModel()
        {
            LoadTotalVisits();
            logger.Info("Відкрито сторінку 'Відвідування'");
        }

        private void LoadTotalVisits()
        {
            try
            {
                using var context = new SportDBContext();
                var today = DateTime.Today.ToString("yyyy-MM-dd");
                TotalVisitsToday = context.Visits.Count(v => v.VisitDate != null && v.VisitDate.StartsWith(today));
                logger.Debug($"Сьогодні відвідувань: {TotalVisitsToday}");
            }
            catch(Exception ex)
            {
                TotalVisitsToday = 0;
                logger.Error($"Виникла помилка:{ex.Message}");
            }
        }

        [RelayCommand]
        private void Search()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                StatusMessage = "Введіть дані для пошуку";
                Memberships.Clear();
                HasResults = false;
                return;
            }

            logger.Info($"Пошук: '{SearchText}'");

            try
            {
                using var context = new SportDBContext();
                var search = SearchText.Trim().ToLower();
                var today = DateTime.Today.ToString("yyyy-MM-dd");

                var clubs = context.Clubs.ToDictionary(c => c.ClubId, c => c.Name);
                var visits = context.Visits
                    .Where(v => v.VisitDate != null && v.VisitDate.StartsWith(today))
                    .ToDictionary(v => v.MembershipId, v => new { v.VisitDate, v.Notes });

                var query = context.Memberships
                    .Include(m => m.Client)
                    .Where(m => m.Status == "Активний");

                if (int.TryParse(search, out int id))
                {
                    logger.Debug($"Пошук по ID: {id}");
                    query = query.Where(m => m.Client.ClientId == id);
                }
                else
                {
                    logger.Debug($"Пошук по імені: {search}");
                    var all = query.ToList();
                    query = all.Where(m => m.Client?.FullName?.ToLower().Contains(search) == true).AsQueryable();
                }

                var results = query.ToList();
                Memberships.Clear();

                foreach (var m in results)
                {
                    var visit = visits.GetValueOrDefault(m.MembershipId);
                    Memberships.Add(new MembershipItem
                    {
                        MembershipId = m.MembershipId,
                        ClientId = m.Client?.ClientId ?? 0,
                        ClientName = m.Client?.FullName ?? "Невідомо",
                        ClientPhone = m.Client?.Phone ?? "",
                        ClubName = clubs.GetValueOrDefault(m.ClubId, "Невідомо"),
                        SessionsRemaining = m.SessionsRemaining,
                        IsPresent = visit != null,
                        VisitTime = visit?.VisitDate ?? "",
                        Notes = visit?.Notes ?? "",
                        StartDate = m.StartDate ?? "",
                        EndDate = m.ExpiryDate ?? ""
                    });
                }

                HasResults = Memberships.Count > 0;
                StatusMessage = HasResults ? $"Знайдено: {Memberships.Count}" : "Нічого не знайдено";
                logger.Info($"Пошук завершено. Результатів: {Memberships.Count}");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Помилка: {ex.Message}";
                Memberships.Clear();
                HasResults = false;
            }
        }

        [RelayCommand]
        private void Mark(MembershipItem item)
        {
            if (item == null) return;

            try
            {
                using var context = new SportDBContext();
                var today = DateTime.Today.ToString("yyyy-MM-dd");

                if (item.IsPresent)
                {
                    logger.Info($"Скасування відмітки");
                    logger.Debug($"Скасування відмітки присутності: MemberID={item.MembershipId}");
                    var visit = context.Visits.FirstOrDefault(v =>
                        v.MembershipId == item.MembershipId &&
                        v.VisitDate.StartsWith(today));

                    if (visit != null)
                    {
                        context.Visits.Remove(visit);
                        var membership = context.Memberships.Find(item.MembershipId);
                        if (membership != null) membership.SessionsRemaining++;
                        context.SaveChanges();

                        item.IsPresent = false;
                        item.SessionsRemaining++;
                        item.VisitTime = "";
                        item.Notes = "";
                        TotalVisitsToday--;
                        StatusMessage = "Скасовано";
                        logger.Info("Відмітка скасована");
                    }
                }
                else
                {
                    if (item.SessionsRemaining <= 0)
                    {
                        StatusMessage = "Немає занять!";
                        logger.Warn($"Немає занять для MemberID={item.MembershipId}");
                        return;
                    }

                    var visitTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    logger.Info($"Відмітка присутності");
                    logger.Debug($"Відмітка присутності: MemberID={item.MembershipId}, Time={visitTime}");
                    context.Visits.Add(new Visit
                    {
                        MembershipId = item.MembershipId,
                        VisitDate = visitTime,
                        Notes = item.Notes ?? ""
                    });

                    var membership = context.Memberships.Find(item.MembershipId);
                    if (membership != null) membership.SessionsRemaining--;
                    context.SaveChanges();

                    item.IsPresent = true;
                    item.SessionsRemaining--;
                    item.VisitTime = visitTime;
                    TotalVisitsToday++;
                    StatusMessage = "Відмічено";
                    logger.Info("Відвідування успішно зареєстровано");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Помилка: {ex.Message}";
                logger.Error($"Виникла помилка:{ex.Message}");
            }
        }

        [RelayCommand]
        private void SaveNotes(MembershipItem item)
        {
            if (item == null || !item.IsPresent)
            {
                logger.Warn("Спроба зберегти коментар для неактивного або null item");
                return;
            }

            try
            {
                logger.Info($"Збереження коментаря");
                logger.Debug($"Збереження коментаря для MemberID={item.MembershipId}");

                using var context = new SportDBContext();
                var today = DateTime.Today.ToString("yyyy-MM-dd");
                var visit = context.Visits.FirstOrDefault(v =>
                    v.MembershipId == item.MembershipId &&
                    v.VisitDate.StartsWith(today));

                if (visit != null)
                {
                    visit.Notes = item.Notes ?? "";
                    context.SaveChanges();
                    StatusMessage = "Коментар збережено";
                    logger.Info("Коментар збережено");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Виникла помилка:{ex.Message}");
                StatusMessage = $"Помилка: {ex.Message}";
            }
        }

        [RelayCommand]
        private void Clear()
        {
            SearchText = "";
            Memberships.Clear();
            HasResults = false;
            StatusMessage = "Введіть дані для пошуку";
            LoadTotalVisits();
        }
    }

    public partial class MembershipItem : ObservableObject
    {
        public int MembershipId { get; set; }

        [ObservableProperty] private int clientId;
        [ObservableProperty] private string clientName = "";
        [ObservableProperty] private string clientPhone = "";
        [ObservableProperty] private string clubName = "";
        [ObservableProperty] private int sessionsRemaining;
        [ObservableProperty] private bool isPresent;
        [ObservableProperty] private string visitTime = "";
        [ObservableProperty] private string notes = "";
        [ObservableProperty] private string startDate = "";
        [ObservableProperty] private string endDate = "";

        public string ButtonText => " "; // ХАЙ ПОКИ ТАК ПРОСТО 

        public string TimeFormatted
        {
            get
            {
                if (string.IsNullOrWhiteSpace(VisitTime)) return "";
                try { return DateTime.Parse(VisitTime).ToString("HH:mm"); }
                catch { return ""; }
            }
        }

        public string MembershipPeriod
        {
            get
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(StartDate) || string.IsNullOrWhiteSpace(EndDate))
                        return "";

                    var start = DateTime.Parse(StartDate).ToString("dd.MM.yyyy");
                    var end = DateTime.Parse(EndDate).ToString("dd.MM.yyyy");
                    return $"{start} - {end}";
                }
                catch
                {
                    return "";
                }
            }
        }
    }
}