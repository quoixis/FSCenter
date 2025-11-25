using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using FSCenter.Data;
using FSCenter.Models;
using NLog;

namespace FSCenter.ViewModels
{
    public partial class ClientCreateViewModel : ViewModelBase
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

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
        private string errorMessage = "";

        [ObservableProperty]
        private string successMessage = "";

        public ClientCreateViewModel()
        {
            logger.Info("Відкрито вікно 'Новий клієнт'");
        }

        [RelayCommand]
        private void Save()
        {
            logger.Info("Початок збереження клієнта");
            logger.Debug($"Початок збереження клієнта: {FullName}");

            ErrorMessage = "";
            SuccessMessage = "";

            // валідація
            if (string.IsNullOrWhiteSpace(FullName))
            {
                ErrorMessage = "ПІБ є обов'язковим полем!";
                logger.Warn("Помилка збереження: ПІБ порожнє");
                return;
            }

            if (string.IsNullOrWhiteSpace(Phone))
            {
                ErrorMessage = "Телефон є обов'язковим полем!";
                logger.Warn("Помилка збереження: телефон порожній");
                return;
            }

            // базовий телефон може потім зроблю типу авто +380 щоб не приходилося руками набирати
            if (!Phone.StartsWith("+380") || Phone.Length < 13)
            {
                ErrorMessage = "Невірний формат телефону! Приклад: +380501234567";
                logger.Warn($"Помилка збереження: невірний формат телефону {Phone}");
                return;
            }

            try
            {
                using var context = new SportDBContext();
                var client = new Client
                {
                    FullName = FullName,
                    Phone = Phone,
                    Email = Email,
                    Age = Age,
                    Address = Address,
                    RegisteredAt = DateTime.Now,
                    IsActive = 1
                };
                context.Clients.Add(client);
                context.SaveChanges();

                SuccessMessage = $"Клієнта '{FullName}' успішно додано!";
                logger.Info("Клієнтa успішно додано");
                logger.Info($"Клієнт '{FullName}' успішно додано з ID {client.ClientId}");

                // очищення за 2 секунди  працює
                System.Threading.Tasks.Task.Delay(2000).ContinueWith(_ =>
                {
                    Clear();
                }, System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Помилка при збереженні клієнта {ex.Message}!";
                logger.Error($"Виникла помилка:{ex.Message}");
            }
        }

        [RelayCommand]
        private void Clear()
        {
            FullName = "";
            Phone = "";
            Email = "";
            Age = null;
            Address = "";
            ErrorMessage = "";
            SuccessMessage = "";
        }
    }
}