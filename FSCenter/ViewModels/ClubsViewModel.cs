using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FSCenter.Data;
using Microsoft.EntityFrameworkCore;
using NLog;
using System;
using System.Collections.ObjectModel;
using System.Linq;


namespace FSCenter.ViewModels
{
    public partial class ClubsViewModel : ViewModelBase
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        [ObservableProperty]
        private ObservableCollection<ClubItem> clubs = new();

        [ObservableProperty]
        private ObservableCollection<ClubItem> filteredClubs = new();

        [ObservableProperty]
        private string searchText = "";

        [ObservableProperty]
        private string errorMessage = "";

        public ClubsViewModel()
        {
            LoadClubs();
        }

        private void LoadClubs()
        {
            logger.Info("Відкрито вкладку 'Клуби'");
            try
            {
                using var context = new SportDBContext();
                var clubsList = context.Clubs
                    .Include(c => c.Trainer)
                    .Include(c => c.Room)
                    .Where(c => c.IsActive == 1)
                    .OrderBy(c => c.Name)
                    .ToList();

                logger.Debug($"Отримано з БД {clubsList.Count} клубів");

                Clubs.Clear();
                foreach (var club in clubsList)
                {
                    Clubs.Add(new ClubItem
                    {
                        ClubId = club.ClubId,
                        Name = club.Name,
                        Description = club.Description ?? "",
                        TrainerName = club.Trainer?.FullName ?? "Не призначено",
                        TrainerPhone = club.Trainer?.Phone ?? "",
                        TrainerEmail = club.Trainer?.Email ?? "",
                        TrainerSpecialization = club.Trainer?.Specialization ?? "",
                        RoomNumber = club.Room?.RoomNumber ?? "",
                        RoomName = club.Room?.Name ?? "",
                        RoomCapacity = club.Room?.Capacity ?? 0,
                        Schedule = club.Schedule ?? "Не вказано",
                        Price8Sessions = club.Price8Sessions,
                        Price12Sessions = club.Price12Sessions
                    });
                }

                ApplyFilter();
                logger.Debug("Успішно завантажено");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Помилка завантаження клубів: {ex.Message}";
                logger.Error($"Виникла помилка:{ex.Message}");
            }
        }

        partial void OnSearchTextChanged(string value)
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            logger.Debug($"Фільтрація за рядком: '{SearchText}'");
            var query = Clubs.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var search = SearchText.ToLower();
                query = query.Where(c =>
                    c.Name.ToLower().Contains(search) ||
                    c.Description.ToLower().Contains(search) ||
                    c.TrainerName.ToLower().Contains(search)
                );
            }

            var list = query.ToList();
            logger.Debug($"Після фільтрації залишилось {list.Count} клубів");

            FilteredClubs.Clear();
            foreach (var club in query)
            {
                FilteredClubs.Add(club);
            }
        }

        [RelayCommand]
        private void ClearSearch()
        {
            SearchText = "";
        }
    }

    public partial class ClubItem : ObservableObject
    {
        public int ClubId { get; set; }

        [ObservableProperty]
        private string name = "";

        [ObservableProperty]
        private string description = "";

        [ObservableProperty]
        private string trainerName = "";

        [ObservableProperty]
        private string trainerPhone = "";

        [ObservableProperty]
        private string trainerEmail = "";

        [ObservableProperty]
        private string trainerSpecialization = "";

        [ObservableProperty]
        private string roomNumber = "";

        [ObservableProperty]
        private string roomName = "";

        [ObservableProperty]
        private int roomCapacity;

        [ObservableProperty]
        private string schedule = "";

        [ObservableProperty]
        private double price8Sessions;

        [ObservableProperty]
        private double price12Sessions;

        public string RoomInfo => string.IsNullOrWhiteSpace(RoomNumber)
            ? "Зал не вказано"
            : $"Зал {RoomNumber} - {RoomName}";

        public string TrainerContact => string.IsNullOrWhiteSpace(TrainerPhone)
            ? "Контакт не вказано"
            : TrainerPhone;
    }
}