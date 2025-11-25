using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FSCenter.Data;
using FSCenter.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using NLog;

namespace FSCenter.ViewModels
{
    public partial class ClientDetailsViewModel : ViewModelBase
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        [ObservableProperty]
        private string searchQuery = "";

        [ObservableProperty]
        private bool hasClientData = false;

        [ObservableProperty]
        private string statusMessage = "Введіть ID або прізвище клієнта для пошуку";

        [ObservableProperty]
        private int clientId;

        [ObservableProperty]
        private string fullName = "";

        [ObservableProperty]
        private string phone = "";

        [ObservableProperty]
        private string email = "";

        [ObservableProperty]
        private int? age;

        [ObservableProperty]
        private string address = "";

        [ObservableProperty]
        private string registeredAt = "";

        [ObservableProperty]
        private string isActiveText = "";

        // Абонементи
        [ObservableProperty]
        private ObservableCollection<MembershipInfo> memberships = new();

        [ObservableProperty]
        private int totalMemberships = 0;

        [ObservableProperty]
        private int activeMemberships = 0;

        // Платежі
        [ObservableProperty]
        private ObservableCollection<PaymentInfo> payments = new();

        [ObservableProperty]
        private double totalPaid = 0;

        [ObservableProperty]
        private int totalPayments = 0;

        // Відвідування
        [ObservableProperty]
        private int totalVisits = 0;

        [ObservableProperty]
        private string lastVisitDate = "";

        [RelayCommand]
        private void Search()
        {
            logger.Info("Відкрито сторінку 'Оплата'");

            if (string.IsNullOrWhiteSpace(SearchQuery))
            {
                StatusMessage = "Введіть ID або прізвище для пошуку";
                HasClientData = false;
                logger.Warn("Пошук з порожнім запитом");
                return;
            }

            try
            {
                using var context = new SportDBContext();
                Client? client = null;

                if (int.TryParse(SearchQuery.Trim(), out int clientId))
                {
                    logger.Debug($"Пошук по ID: {clientId}");
                    client = context.Clients
                        .Include(c => c.Memberships)
                            .ThenInclude(m => m.Club)
                        .Include(c => c.Memberships)
                            .ThenInclude(m => m.Visits)
                        .Include(c => c.Payments)
                        .FirstOrDefault(c => c.ClientId == clientId);
                }
                else
                {
                    logger.Debug($"Пошук по імені: {SearchQuery}");
                    var nameLower = SearchQuery.Trim().ToLower();
                    var allClients = context.Clients
                        .Include(c => c.Memberships)
                            .ThenInclude(m => m.Club)
                        .Include(c => c.Memberships)
                            .ThenInclude(m => m.Visits)
                        .Include(c => c.Payments)
                        .ToList();

                    client = allClients.FirstOrDefault(c =>
                    {
                        if (string.IsNullOrWhiteSpace(c.FullName)) return false;
                        var firstName = c.FullName.Split(' ')[0].ToLower();
                        return firstName.Contains(nameLower) || c.FullName.ToLower().Contains(nameLower);
                    });
                }

                if (client == null)
                {
                    StatusMessage = "Клієнта не знайдено";
                    HasClientData = false;
                    logger.Warn("Клієнта не знайдено");
                    return;
                }

                logger.Info($"Клієнт знайдений: {client.FullName} (ID: {client.ClientId})");

                ClientId = client.ClientId;
                FullName = client.FullName;
                Phone = client.Phone;
                Email = client.Email ?? "Не вказано";
                Age = client.Age;
                Address = client.Address ?? "Не вказано";
                RegisteredAt = client.RegisteredAt?.ToString("dd.MM.yyyy HH:mm") ?? "Невідомо";
                IsActiveText = client.IsActive == 1 ? "Активний" : "Неактивний";

                LoadMemberships(client);

                LoadPayments(client);

                LoadVisits(client);

                HasClientData = true;
                StatusMessage = $"Знайдено клієнта: {client.FullName}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Помилка пошуку: {ex.Message}";
                HasClientData = false;
                logger.Error($"Виникла помилка:{ex.Message}");
            }
        }

        private void LoadMemberships(Client client)
        {
            logger.Debug($"Завантаження абонементів для клієнта ID={client.ClientId}");
            Memberships.Clear();

            foreach (var membership in client.Memberships.OrderByDescending(m => m.StartDate))
            {
                var clubName = membership.Club?.Name ?? "Невідомо";
                var startDate = string.IsNullOrWhiteSpace(membership.StartDate) ? "?" : membership.StartDate;
                var expiryDate = string.IsNullOrWhiteSpace(membership.ExpiryDate) ? "?" : membership.ExpiryDate;

                Memberships.Add(new MembershipInfo
                {
                    ClubName = clubName,
                    Status = membership.Status,
                    SessionsTotal = membership.SessionsTotal,
                    SessionsRemaining = membership.SessionsRemaining,
                    StartDate = startDate,
                    ExpiryDate = expiryDate,
                    VisitsCount = membership.Visits.Count
                });
            }

            TotalMemberships = Memberships.Count;
            ActiveMemberships = Memberships.Count(m => m.Status == "Активний");
            logger.Debug($"Абонементів: {TotalMemberships}, активних: {ActiveMemberships}");
        }

        private void LoadPayments(Client client)
        {
            logger.Debug($"Завантаження платежів для клієнта ID={client.ClientId}");
            Payments.Clear();

            foreach (var payment in client.Payments.OrderByDescending(p => p.PaymentDate))
            {
                var date = string.IsNullOrWhiteSpace(payment.PaymentDate) ? "Невідомо" : payment.PaymentDate;

                try
                {
                    var dt = DateTime.Parse(date);
                    date = dt.ToString("dd.MM.yyyy HH:mm");
                }
                catch { }

                Payments.Add(new PaymentInfo
                {
                    Amount = payment.Amount,
                    PaymentDate = date,
                    PaymentMethod = payment.PaymentMethod,
                    Description = payment.Description
                });
            }

            TotalPayments = Payments.Count;
            TotalPaid = Payments.Sum(p => p.Amount);
            logger.Debug($"Платежів: {TotalPayments}, сума: {TotalPaid} грн");
        }

        private void LoadVisits(Client client)
        {
            logger.Debug($"Завантаження відвідувань для клієнта ID={client.ClientId}");
            var allVisits = client.Memberships
                .SelectMany(m => m.Visits)
                .OrderByDescending(v => v.VisitDate)
                .ToList();

            TotalVisits = allVisits.Count;

            if (allVisits.Any())
            {
                var lastVisit = allVisits.First();
                if (!string.IsNullOrWhiteSpace(lastVisit.VisitDate))
                {
                    try
                    {
                        var dt = DateTime.Parse(lastVisit.VisitDate);
                        LastVisitDate = dt.ToString("dd.MM.yyyy HH:mm:ss");
                    }
                    catch
                    {
                        LastVisitDate = lastVisit.VisitDate;
                    }
                }
                else
                {
                    LastVisitDate = "Невідомо";
                }
            }
            else
            {
                LastVisitDate = "Ще не було";
            }
            logger.Debug($"Відвідувань: {TotalVisits}, останнє: {LastVisitDate}");
        }

        [RelayCommand]
        private void Clear()
        {
            SearchQuery = "";
            HasClientData = false;
            StatusMessage = "Введіть ID або прізвище клієнта для пошуку";

            Memberships.Clear();
            Payments.Clear();

            ClientId = 0;
            FullName = "";
            Phone = "";
            Email = "";
            Age = null;
            Address = "";
            RegisteredAt = "";
            IsActiveText = "";
            TotalMemberships = 0;
            ActiveMemberships = 0;
            TotalPayments = 0;
            TotalPaid = 0;
            TotalVisits = 0;
            LastVisitDate = "";
        }
    }

    public partial class MembershipInfo : ObservableObject
    {
        [ObservableProperty]
        private string clubName = "";

        [ObservableProperty]
        private string status = "";

        [ObservableProperty]
        private int sessionsTotal;

        [ObservableProperty]
        private int sessionsRemaining;

        [ObservableProperty]
        private string startDate = "";

        [ObservableProperty]
        private string expiryDate = "";

        [ObservableProperty]
        private int visitsCount;

        public string SessionsInfo => $"{SessionsRemaining}/{SessionsTotal}";
    }

    public partial class PaymentInfo : ObservableObject
    {
        [ObservableProperty]
        private double amount;

        [ObservableProperty]
        private string paymentDate = "";

        [ObservableProperty]
        private string paymentMethod = "";

        [ObservableProperty]
        private string description = "";

        public string AmountFormatted => $"{Amount:F2} грн";
    }
}