using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using UP01_Chernenkaya_Book.Helpers;
using UP01_Chernenkaya_Book.Models;

namespace UP01_Chernenkaya_Book.Pages;

public partial class CatalogPage : Page
{
    private List<Accommodation> _allAccommodations = new();
    private bool _isInitialized = false;

    public CatalogPage()
    {
        InitializeComponent();
        LoadFiltersData();
        RefreshCatalog();

        // Кнопка перехода в админку видна только администраторам
        BtnAdminPanel.Visibility = SessionContext.IsAdministrator ? Visibility.Visible : Visibility.Collapsed;
    }

    private void LoadFiltersData()
    {
        try
        {
            using var db = new BookingDbContext();

            // Города
            var cities = db.Cities.OrderBy(c => c.CityName).ToList();
            var cityList = new List<string> { "Все города" };
            cityList.AddRange(cities.Select(c => c.CityName));
            CmbCity.ItemsSource = cityList;
            CmbCity.SelectedIndex = 0;

            // Типы размещения
            var types = db.AccommodationTypes.OrderBy(t => t.TypeName).ToList();
            var typeList = new List<string> { "Все типы жилья" };
            typeList.AddRange(types.Select(t => t.TypeName));
            CmbType.ItemsSource = typeList;
            CmbType.SelectedIndex = 0;

            _isInitialized = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки фильтров: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RefreshCatalog()
    {
        try
        {
            using var db = new BookingDbContext();
            _allAccommodations = db.Accommodations
                .Include(a => a.City)
                .Include(a => a.Type)
                .AsNoTracking()
                .ToList();

            ApplyFilters();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки данных каталога: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void FilterChanged(object sender, EventArgs e)
    {
        if (!_isInitialized) return;
        ApplyFilters();
    }

    // Шаг 8: Фильтрация, поиск и сортировка
    private void ApplyFilters()
    {
        if (_allAccommodations == null) return;

        IEnumerable<Accommodation> filtered = _allAccommodations;

        // Поиск по названию и адресу
        string query = TxtSearch.Text?.Trim().ToLower() ?? "";
        if (!string.IsNullOrEmpty(query))
        {
            filtered = filtered.Where(a =>
                a.Name.ToLower().Contains(query) ||
                a.Address.ToLower().Contains(query) ||
                (a.City != null && a.City.CityName.ToLower().Contains(query)));
        }

        // Фильтр по городу
        if (CmbCity.SelectedIndex > 0 && CmbCity.SelectedItem is string selectedCity)
        {
            filtered = filtered.Where(a => a.City != null && a.City.CityName == selectedCity);
        }

        // Фильтр по типу жилья
        if (CmbType.SelectedIndex > 0 && CmbType.SelectedItem is string selectedType)
        {
            filtered = filtered.Where(a => a.Type != null && a.Type.TypeName == selectedType);
        }

        // Сортировка
        if (CmbSort.SelectedItem is ComboBoxItem sortItem)
        {
            string sortMode = sortItem.Content.ToString() ?? "";
            filtered = sortMode switch
            {
                "Сначала недорогие" => filtered.OrderBy(a => a.PricePerNight),
                "Сначала дорогие" => filtered.OrderByDescending(a => a.PricePerNight),
                "По рейтингу (высокий)" => filtered.OrderByDescending(a => a.Rating ?? 0),
                _ => filtered.OrderBy(a => a.AccommodationId)
            };
        }

        var resultList = filtered.ToList();
        LvAccommodations.ItemsSource = resultList;
        TxtCount.Text = $"Найдено объектов размещения: {resultList.Count} из {_allAccommodations.Count}";
    }

    private void BtnReset_Click(object sender, RoutedEventArgs e)
    {
        _isInitialized = false;
        TxtSearch.Text = string.Empty;
        CmbCity.SelectedIndex = 0;
        CmbType.SelectedIndex = 0;
        CmbSort.SelectedIndex = 0;
        _isInitialized = true;
        ApplyFilters();
    }

    private void BtnAdminPanel_Click(object sender, RoutedEventArgs e)
    {
        NavigationService.Navigate(new AdminHubPage());
    }

    private void BtnBook_Click(object sender, RoutedEventArgs e)
    {
        if (!SessionContext.IsAuthenticated)
        {
            var res = MessageBox.Show(
                "Для бронирования номера необходимо войти в систему под своей учетной записью.\nПерейти на страницу авторизации?",
                "Требуется авторизация",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (res == MessageBoxResult.Yes)
            {
                NavigationService.Navigate(new AuthPage());
            }
            return;
        }

        if (sender is Button btn && btn.DataContext is Accommodation acc)
        {
            try
            {
                using var db = new BookingDbContext();

                // Ищем свободную или любую доступную комнату в данном отеле
                var room = db.Rooms.FirstOrDefault(r => r.AccommodationId == acc.AccommodationId && r.IsAvailable == true)
                           ?? db.Rooms.FirstOrDefault(r => r.AccommodationId == acc.AccommodationId);

                if (room == null)
                {
                    // Если комнат еще нет в фонде этого отеля, создаем базовую комнату
                    room = new Room
                    {
                        AccommodationId = acc.AccommodationId,
                        RoomNumber = "101",
                        Floor = 1,
                        RoomType = "Стандарт",
                        Capacity = 2,
                        PricePerNight = acc.PricePerNight,
                        IsAvailable = true
                    };
                    db.Rooms.Add(room);
                    db.SaveChanges();
                }

                // Статус "Ожидает оплаты" для подтверждения администратором
                var status = db.BookingStatuses.FirstOrDefault(s => s.StatusName == "Ожидает оплаты")
                             ?? db.BookingStatuses.FirstOrDefault();

                var today = DateOnly.FromDateTime(DateTime.Today);
                var checkIn = today.AddDays(1);
                var checkOut = today.AddDays(4);
                decimal totalPrice = acc.PricePerNight * 3;

                var newBooking = new Booking
                {
                    UserId = SessionContext.ActiveUser!.UserId,
                    RoomId = room.RoomId,
                    CheckIn = checkIn,
                    CheckOut = checkOut,
                    Guests = 2,
                    StatusId = status?.StatusId ?? 1,
                    CreatedAt = DateTime.Now,
                    Comment = $"Онлайн-бронирование через каталог: {acc.Name}"
                };

                db.Bookings.Add(newBooking);
                db.SaveChanges();

                // Фиксация платежа
                var payment = new Payment
                {
                    BookingId = newBooking.BookingId,
                    Amount = totalPrice,
                    PaymentDate = DateTime.Now,
                    PaymentMethod = "Банковская карта",
                    PaymentStatus = "Оплачено"
                };
                db.Payments.Add(payment);
                db.SaveChanges();

                MessageBox.Show(
                    $"Заявка на бронирование объекта «{acc.Name}» ({acc.City?.CityName}) успешно оформлена!\n\n" +
                    $"• Номер брони: №{newBooking.BookingId}\n" +
                    $"• Комната: №{room.RoomNumber} ({room.RoomType})\n" +
                    $"• Период: с {newBooking.CheckIn:dd.MM.yyyy} по {newBooking.CheckOut:dd.MM.yyyy} (3 суток)\n" +
                    $"• Сумма к оплате: {totalPrice:N0} ₽\n" +
                    $"• Текущий статус: {status?.StatusName}\n\n" +
                    $"Запись добавлена в базу данных и отображается в журнале бронирований.",
                    "Бронирование создано",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка создания бронирования: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
