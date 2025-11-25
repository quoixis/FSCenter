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
    public partial class PaymentViewModel : ViewModelBase
    {

        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        [ObservableProperty]
        private int selectedTabIndex = 0;

        // 1 
        [ObservableProperty]
        private int clientId;

        [ObservableProperty]
        private string clientInfo = "";

        [ObservableProperty]
        private ObservableCollection<Club> clubs = new();

        [ObservableProperty]
        private Club? selectedClub;

        [ObservableProperty]
        private int sessionsCount = 8;

        [ObservableProperty]
        private double price;

        [ObservableProperty]
        private string paymentMethod = "Готівка";

        [ObservableProperty]
        private string errorMessage = "";

        [ObservableProperty]
        private string successMessage = "";

        // 2 
        [ObservableProperty]
        private int freezeClientId;

        [ObservableProperty]
        private string freezeClientInfo = "";

        [ObservableProperty]
        private ObservableCollection<Membership> activeMemberships = new();

        [ObservableProperty]
        private Membership? selectedMembership;

        [ObservableProperty]
        private int freezeMonths = 1;

        [ObservableProperty]
        private double freezePrice = 100;

        [ObservableProperty]
        private string freezePaymentMethod = "Готівка";

        [ObservableProperty]
        private string freezeErrorMessage = "";

        [ObservableProperty]
        private string freezeSuccessMessage = "";

        [ObservableProperty]
        private string membershipInfo = "";

        // 3 
        [ObservableProperty]
        private int customClientId;

        [ObservableProperty]
        private string customClientInfo = "";

        [ObservableProperty]
        private string customDescription = "";

        [ObservableProperty]
        private double customPrice;

        [ObservableProperty]
        private string customPaymentMethod = "Готівка";

        [ObservableProperty]
        private string customErrorMessage = "";

        [ObservableProperty]
        private string customSuccessMessage = "";

        public ObservableCollection<int> SessionsOptions { get; } = new() { 8, 12 };
        public ObservableCollection<int> FreezeMonthsOptions { get; } = new() { 1, 2, 3 };

        public ObservableCollection<string> PaymentMethods { get; } = new()
        {
            "Готівка",
            "Картка",
            "Переказ"
        };

        public PaymentViewModel()
        {
            LoadClubs();
        }

        // 1

        private void LoadClubs()
        {
            logger.Info("Відкрито сторінку 'Оплата'");
            try
            {
                using var context = new SportDBContext();
                var clubsList = context.Clubs
                    .Where(c => c.IsActive == 1)
                    .OrderBy(c => c.Name)
                    .ToList();

                logger.Debug($"Отримано {clubsList.Count} активних клубів");

                Clubs.Clear();
                foreach (var club in clubsList)
                {
                    Clubs.Add(club);
                }
                logger.Debug("Успішно завантажено");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Помилка завантаження клубів: {ex.Message}";
                logger.Error($"Виникла помилка:{ex.Message}");
            }
        }

        partial void OnClientIdChanged(int value)
        {
            LoadClientInfo(value);
        }

        partial void OnSelectedClubChanged(Club? value)
        {
            UpdatePrice();
        }

        partial void OnSessionsCountChanged(int value)
        {
            UpdatePrice();
        }

        private void LoadClientInfo(int id)
        {
            if (id <= 0)
            {
                ClientInfo = "";
                return;
            }

            logger.Debug($"Пошук клієнта ID={id}");

            try
            {
                using var context = new SportDBContext();
                var client = context.Clients.FirstOrDefault(c => c.ClientId == id);

                if (client != null)
                {
                    ClientInfo = $"{client.FullName} - {client.Phone}";
                    ErrorMessage = "";
                    logger.Info($"Знайдено клієнта: {client.FullName}");
                }
                else
                {
                    ClientInfo = "";
                    ErrorMessage = "Клієнта з таким ID не знайдено!";
                    logger.Warn($"Клієнт ID={id} не знайдений");
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Помилка завантаження клієнта: {ex.Message}";
                ClientInfo = "";
                logger.Error($"Виникла помилка:{ex.Message}");
            }
        }

        private void UpdatePrice()
        {
            if (SelectedClub == null)
            {
                Price = 0;
                return;
            }

            Price = SessionsCount switch
            {
                8 => SelectedClub.Price8Sessions,
                12 => SelectedClub.Price12Sessions,
                _ => 0
            };
            logger.Debug($"Обраховано ціну: {Price}");
        }

        [RelayCommand]
        private void Save()
        {

            logger.Info("Спроба створення абонемента");

            ErrorMessage = "";
            SuccessMessage = "";

            if (ClientId <= 0)
            {
                ErrorMessage = "Введіть ID клієнта!";
                logger.Warn("ID клієнта не введено");
                return;
            }

            if (string.IsNullOrWhiteSpace(ClientInfo))
            {
                ErrorMessage = "Клієнта не знайдено! Перевірте ID.";
                logger.Warn("Спроба створити абонемент без знайденого клієнта");
                return;
            }

            if (SelectedClub == null)
            {
                ErrorMessage = "Оберіть секцію!";
                logger.Warn("Не вибрано клуб");
                return;
            }

            if (SessionsCount != 8 && SessionsCount != 12)
            {
                ErrorMessage = "Оберіть кількість занять (8 або 12)!";
                logger.Warn("Невірна кількість занять");
                return;
            }

            if (Price <= 0)
            {
                ErrorMessage = "Ціна повинна бути більше 0!";
                logger.Warn("Ціна <= 0");
                return;
            }

            try
            {
                using var context = new SportDBContext();

                var client = context.Clients.FirstOrDefault(c => c.ClientId == ClientId);
                if (client == null)
                {
                    ErrorMessage = "Клієнта з таким ID не існує!";
                    logger.Warn($"Клієнт ID={ClientId} не існує");
                    return;
                }

                var startDate = DateTime.Now;
                var expiryDate = startDate.AddMonths(1); // міc

                var membership = new Membership
                {
                    ClientId = ClientId,
                    ClubId = SelectedClub.ClubId,
                    SessionsTotal = SessionsCount,
                    SessionsRemaining = SessionsCount,
                    StartDate = startDate.ToString("yyyy-MM-dd"),
                    ExpiryDate = expiryDate.ToString("yyyy-MM-dd"),
                    Status = "Активний"
                };

                context.Memberships.Add(membership);
                context.SaveChanges();
                logger.Info($"Абонемент створено (ID={membership.MembershipId})");

                var payment = new Payment
                {
                    ClientId = ClientId,
                    MembershipId = membership.MembershipId,
                    Amount = Price,
                    PaymentDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    PaymentMethod = PaymentMethod,
                    Description = $"Оплата абонемента: {SelectedClub.Name} ({SessionsCount} занять)"
                };

                context.Payments.Add(payment);
                context.SaveChanges();
                logger.Info($"Оплату за абонемент збережено: {Price} грн");

                SuccessMessage = $"Абонемент успішно створено! Сума: {Price:F2} грн";

                System.Threading.Tasks.Task.Delay(2000).ContinueWith(_ =>
                {
                    Clear();
                }, System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Помилка при збереженні: {ex.Message}";
                logger.Error($"Виникла помилка:{ex.Message}");
            }
        }

        [RelayCommand]
        private void Clear()
        {
            ClientId = 0;
            ClientInfo = "";
            SelectedClub = null;
            SessionsCount = 8;
            Price = 0;
            PaymentMethod = "Готівка";
            ErrorMessage = "";
            SuccessMessage = "";
        }

        // 2 

        partial void OnFreezeClientIdChanged(int value)
        {
            LoadFreezeClientInfo(value);
        }

        partial void OnFreezeMonthsChanged(int value)
        {
            UpdateFreezePrice();
        }

        partial void OnSelectedMembershipChanged(Membership? value)
        {
            if (value != null)
            {
                MembershipInfo = $"Секція: {value.Club?.Name}\n" +
                                $"Занять: {value.SessionsRemaining}/{value.SessionsTotal}\n" +
                                $"Початок: {value.StartDate}\n" +
                                $"Закінчення: {value.ExpiryDate}";
                logger.Debug($"Обрано абонемент для заморозки (ID={value.MembershipId})");
            }
            else
            {
                MembershipInfo = "";
                logger.Debug("Абонемент для заморозки знято");
            }
        }

        private void LoadFreezeClientInfo(int id)
        {
            logger.Debug($"Пошук клієнта для заморозки, ID={id}");

            FreezeClientInfo = "";
            ActiveMemberships.Clear();
            SelectedMembership = null;
            MembershipInfo = "";
            FreezeErrorMessage = "";

            if (id <= 0) return;

            try
            {
                using var context = new SportDBContext();
                var client = context.Clients
                    .Include(c => c.Memberships)
                        .ThenInclude(m => m.Club)
                    .FirstOrDefault(c => c.ClientId == id);

                if (client != null)
                {
                    FreezeClientInfo = $"{client.FullName} - {client.Phone}";
                    logger.Debug($"Клієнт знайдений: {client.FullName}");

                    var memberships = client.Memberships
                        .Where(m => m.Status == "Активний")
                        .OrderByDescending(m => m.StartDate)
                        .ToList();

                    logger.Debug($"У клієнта {memberships.Count} активних абонементів");
                    foreach (var membership in memberships)
                    {
                        ActiveMemberships.Add(membership);
                    }

                    if (ActiveMemberships.Count == 0)
                    {
                        FreezeErrorMessage = "У клієнта немає активних абонементів.";
                        logger.Warn("Активних абонементів немає");
                    }
                }
                else
                {
                    FreezeErrorMessage = "Клієнта з таким ID не знайдено!";
                    logger.Warn("Клієнт не знайдений");
                }
            }
            catch (Exception ex)
            {
                FreezeErrorMessage = $"Помилка завантаження: {ex.Message}";
                logger.Error($"Виникла помилка:{ex.Message}");
            }
        }

        private void UpdateFreezePrice()
        {
            FreezePrice = FreezeMonths switch
            {
                1 => 100,
                2 => 150,
                3 => 200,
                _ => 0
            };
            logger.Debug($"FreezePrice оновлено: {FreezePrice}");
        }

        [RelayCommand]
        private void FreezeMembership()
        {
            logger.Info("Спроба заморозити абонемент");

            FreezeErrorMessage = "";
            FreezeSuccessMessage = "";

            if (FreezeClientId <= 0)
            {
                FreezeErrorMessage = "Введіть ID клієнта!";
                logger.Warn("FreezeClientId <= 0");
                return;
            }

            if (SelectedMembership == null)
            {
                FreezeErrorMessage = "Оберіть абонемент для заморозки!";
                logger.Warn("Абонемент не вибрано");
                return;
            }

            if (FreezeMonths < 1 || FreezeMonths > 3)
            {
                FreezeErrorMessage = "Термін заморозки може бути від 1 до 3 місяців!";
                logger.Warn("Невірний термін заморозки");
                return;
            }

            try
            {
                using var context = new SportDBContext();

                var membership = context.Memberships
                    .Include(m => m.Client)
                    .Include(m => m.Club)
                    .FirstOrDefault(m => m.MembershipId == SelectedMembership.MembershipId);

                if (membership == null)
                {
                    FreezeErrorMessage = "Абонемент не знайдено!";
                    logger.Warn("Абонемент не знайдено");
                    return;
                }

                DateTime currentExpiry = DateTime.Parse(membership.ExpiryDate ?? DateTime.Now.ToString("yyyy-MM-dd"));
                DateTime newExpiry = currentExpiry.AddMonths(FreezeMonths);

                membership.ExpiryDate = newExpiry.ToString("yyyy-MM-dd");

                var payment = new Payment
                {
                    ClientId = FreezeClientId,
                    MembershipId = membership.MembershipId,
                    Amount = FreezePrice,
                    PaymentDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    PaymentMethod = FreezePaymentMethod,
                    Description = $"Заморозка абонемента: {membership.Club?.Name} на {FreezeMonths} міс."
                };

                context.Payments.Add(payment);
                context.SaveChanges();

                FreezeSuccessMessage = $"Абонемент заморожено на {FreezeMonths} міс. за {FreezePrice} грн! Новий термін: {newExpiry:dd.MM.yyyy}";
                logger.Info($"Абонемент {payment.MembershipId} заморожено, нова дата: {newExpiry:yyyy-MM-dd}");

                SelectedMembership.ExpiryDate = membership.ExpiryDate;
                MembershipInfo = $"Секція: {membership.Club?.Name}\n" +
                                $"Занять: {membership.SessionsRemaining}/{membership.SessionsTotal}\n" +
                                $"Початок: {membership.StartDate}\n" +
                                $"Закінчення: {membership.ExpiryDate}";

                System.Threading.Tasks.Task.Delay(10000).ContinueWith(_ =>
                {
                    ClearFreeze();
                }, System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
            }
            catch (Exception ex)
            {
                FreezeErrorMessage = $"Помилка заморозки: {ex.Message}";
                logger.Error($"Виникла помилка:{ex.Message}");
            }
        }

        [RelayCommand]
        private void ClearFreeze()
        {
            FreezeClientId = 0;
            FreezeClientInfo = "";
            ActiveMemberships.Clear();
            SelectedMembership = null;
            FreezeMonths = 1;
            FreezePrice = 100;
            FreezePaymentMethod = "Готівка";
            FreezeErrorMessage = "";
            FreezeSuccessMessage = "";
            MembershipInfo = "";
        }

        // 3 

        partial void OnCustomClientIdChanged(int value)
        {
            LoadCustomClientInfo(value);
        }

        private void LoadCustomClientInfo(int id)
        {
            logger.Info($"Пошук клієнта для кастомної покупки ID={id}");
            if (id <= 0)
            {
                CustomClientInfo = "";
                return;
            }

            try
            {
                using var context = new SportDBContext();
                var client = context.Clients.FirstOrDefault(c => c.ClientId == id);

                if (client != null)
                {
                    CustomClientInfo = $"{client.FullName} - {client.Phone}";
                    logger.Info("Клієнт знайдений");
                    CustomErrorMessage = "";
                }
                else
                {
                    CustomClientInfo = "";
                    CustomErrorMessage = "Клієнта з таким ID не знайдено!";
                    logger.Warn("Клієнт не знайдений");
                }
            }
            catch (Exception ex)
            {
                CustomErrorMessage = $"Помилка завантаження клієнта: {ex.Message}";
                CustomClientInfo = "";
                logger.Error($"Виникла помилка:{ex.Message}");
            }
        }

        [RelayCommand]
        private void SaveCustomPurchase()
        {
            logger.Info("Спроба створення кастомної покупки");
            CustomErrorMessage = "";
            CustomSuccessMessage = "";

            if (CustomClientId <= 0)
            {
                CustomErrorMessage = "Введіть ID клієнта!";
                logger.Warn("CustomClientId <= 0");
                return;
            }

            if (string.IsNullOrWhiteSpace(CustomClientInfo))
            {
                CustomErrorMessage = "Клієнта не знайдено! Перевірте ID.";
                logger.Warn("Спроба зберегти покупку без знайденого клієнта");
                return;
            }

            if (string.IsNullOrWhiteSpace(CustomDescription))
            {
                CustomErrorMessage = "Введіть опис покупки!";
                logger.Warn("Порожній опис покупки");
                return;
            }

            if (CustomPrice <= 0)
            {
                CustomErrorMessage = "Ціна повинна бути більше 0!";
                logger.Warn("CustomPrice <= 0");
                return;
            }

            try
            {
                using var context = new SportDBContext();

                var client = context.Clients.FirstOrDefault(c => c.ClientId == CustomClientId);
                if (client == null)
                {
                    CustomErrorMessage = "Клієнта з таким ID не існує!";
                    logger.Warn("Клієнт не знайдений");
                    return;
                }

                var payment = new Payment
                {
                    ClientId = CustomClientId,
                    MembershipId = null,
                    Amount = CustomPrice,
                    PaymentDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    PaymentMethod = CustomPaymentMethod,
                    Description = CustomDescription
                };

                context.Payments.Add(payment);
                context.SaveChanges();

                CustomSuccessMessage = $"Покупку успішно збережено! Сума: {CustomPrice:F2} грн";
                logger.Info("Кастомну покупку збережено");

                System.Threading.Tasks.Task.Delay(5000).ContinueWith(_ =>
                {
                    ClearCustom();
                }, System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
            }
            catch (Exception ex)
            {
                CustomErrorMessage = $"Помилка при збереженні: {ex.Message}";
                logger.Error($"Виникла помилка:{ex.Message}");
            }
        }

        [RelayCommand]
        private void ClearCustom()
        {
            CustomClientId = 0;
            CustomClientInfo = "";
            CustomDescription = "";
            CustomPrice = 0;
            CustomPaymentMethod = "Готівка";
            CustomErrorMessage = "";
            CustomSuccessMessage = "";
        }
    }
}