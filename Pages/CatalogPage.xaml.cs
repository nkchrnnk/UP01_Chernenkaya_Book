using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using UP01_Chernenkaya_Book.Helpers;
using UP01_Chernenkaya_Book.Models;

namespace UP01_Chernenkaya_Book.Pages;

/// <summary>
/// Страница каталога объектов размещения с поиском, фильтрацией и возможностью бронирования
/// </summary>
public partial class CatalogPage : Page
{
    private List<Accommodation> _allAccommodations = new();
    private bool _isInitialized = false;

    /// <summary>
    /// Конструктор страницы каталога, загрузка фильтров и списка объектов
    /// </summary>
    public CatalogPage()
    {
        InitializeComponent();
        LoadFiltersData();
        RefreshCatalog();

        BtnAdminPanel.Visibility = SessionContext.IsAdministrator ? Visibility.Visible : Visibility.Collapsed;
        BtnCabinet.Visibility = (SessionContext.IsAuthenticated && !SessionContext.IsAdministrator) ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    /// Загрузка списков городов, типов жилья и категорий номеров для выпадающих списков
    /// </summary>
    private void LoadFiltersData()
    {
        try
        {
            using var db = new BookingDbContext();

            var cities = db.Cities.OrderBy(c => c.CityName).ToList();
            var cityList = new List<string> { "Все города" };
            cityList.AddRange(cities.Select(c => c.CityName));
            CmbCity.ItemsSource = cityList;
            CmbCity.SelectedIndex = 0;

            var types = db.AccommodationTypes.OrderBy(t => t.TypeName).ToList();
            var typeList = new List<string> { "Все типы жилья" };
            typeList.AddRange(types.Select(t => t.TypeName));
            CmbType.ItemsSource = typeList;
            CmbType.SelectedIndex = 0;

            var categories = new List<string> { "Все категории номеров", "Люкс", "Делюкс", "Стандарт", "Студия", "Апартаменты", "Коттедж" };
            CmbRoomCategory.ItemsSource = categories;
            CmbRoomCategory.SelectedIndex = 0;

            _isInitialized = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки фильтров: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Обновление полного перечня объектов размещения из базы данных
    /// </summary>
    private void RefreshCatalog()
    {
        try
        {
            using var db = new BookingDbContext();
            _allAccommodations = db.Accommodations
                .Include(a => a.City)
                .Include(a => a.Type)
                .Include(a => a.RoomsNavigation)
                .AsNoTracking()
                .ToList();

            ApplyFilters();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки данных каталога: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Обработчик события изменения любого критерия поиска или фильтрации
    /// </summary>
    /// <param name="sender">Источник события</param>
    /// <param name="e">Параметры события</param>
    private void FilterChanged(object sender, EventArgs e)
    {
        if (!_isInitialized) return;
        ApplyFilters();
    }

    /// <summary>
    /// Применение выбранных фильтров, текстового поиска и сортировки к списку объектов
    /// </summary>
    private void ApplyFilters()
    {
        if (_allAccommodations == null) return;

        IEnumerable<Accommodation> filtered = _allAccommodations;

        string query = TxtSearch.Text?.Trim().ToLower() ?? "";
        if (!string.IsNullOrEmpty(query))
        {
            filtered = filtered.Where(a =>
                a.Name.ToLower().Contains(query) ||
                a.Address.ToLower().Contains(query) ||
                (a.City != null && a.City.CityName.ToLower().Contains(query)));
        }

        if (CmbCity.SelectedIndex > 0 && CmbCity.SelectedItem is string selectedCity)
        {
            filtered = filtered.Where(a => a.City != null && a.City.CityName == selectedCity);
        }

        if (CmbType.SelectedIndex > 0 && CmbType.SelectedItem is string selectedType)
        {
            filtered = filtered.Where(a => a.Type != null && a.Type.TypeName == selectedType);
        }

        if (CmbRoomCategory.SelectedIndex > 0 && CmbRoomCategory.SelectedItem is string selectedCategory)
        {
            filtered = filtered.Where(a => a.RoomsNavigation.Any(r => r.RoomType.ToLower().Contains(selectedCategory.ToLower())));
        }

        if (CmbGuests.SelectedIndex > 0)
        {
            int minGuests = CmbGuests.SelectedIndex;
            filtered = filtered.Where(a => a.Capacity >= minGuests || a.RoomsNavigation.Any(r => r.Capacity >= minGuests));
        }

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

    /// <summary>
    /// Сброс всех установленных фильтров к значениям по умолчанию
    /// </summary>
    /// <param name="sender">Кнопка сброса фильтров</param>
    /// <param name="e">Параметры события</param>
    private void BtnReset_Click(object sender, RoutedEventArgs e)
    {
        _isInitialized = false;
        TxtSearch.Text = string.Empty;
        CmbCity.SelectedIndex = 0;
        CmbType.SelectedIndex = 0;
        CmbRoomCategory.SelectedIndex = 0;
        CmbGuests.SelectedIndex = 0;
        CmbSort.SelectedIndex = 0;
        _isInitialized = true;
        ApplyFilters();
    }

    /// <summary>
    /// Переход в личный кабинет текущего пользователя
    /// </summary>
    /// <param name="sender">Кнопка «Личный кабинет»</param>
    /// <param name="e">Параметры события</param>
    private void BtnCabinet_Click(object sender, RoutedEventArgs e)
    {
        if (!SessionContext.IsAuthenticated || SessionContext.IsAdministrator)
        {
            return;
        }

        NavigationService.Navigate(new UserCabinetPage());
    }

    /// <summary>
    /// Переход в панель управления администратора
    /// </summary>
    /// <param name="sender">Кнопка «Панель администратора»</param>
    /// <param name="e">Параметры события</param>
    private void BtnAdminPanel_Click(object sender, RoutedEventArgs e)
    {
        NavigationService.Navigate(new AdminHubPage());
    }

    /// <summary>
    /// Открытие окна бронирования выбранного объекта с выбором номеров и дат
    /// </summary>
    /// <param name="sender">Кнопка «Забронировать»</param>
    /// <param name="e">Параметры события</param>
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
            var bookingWindow = new BookingWindow(acc);
            if (bookingWindow.ShowDialog() == true)
            {
                var askCabinet = MessageBox.Show(
                    "Заявка на бронирование успешно сформирована!\nПерейти в Личный кабинет для просмотра статуса и счета?",
                    "Переход в личный кабинет",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (askCabinet == MessageBoxResult.Yes)
                {
                    NavigationService.Navigate(new UserCabinetPage());
                }
            }
        }
    }
}
