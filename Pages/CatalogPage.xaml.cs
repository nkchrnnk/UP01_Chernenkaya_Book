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
            MessageBox.Show(
                $"Заявка на бронирование объекта «{acc.Name}» ({acc.City?.CityName}) успешно отправлена!\n" +
                $"С вами свяжется администратор отеля по телефону {SessionContext.ActiveUser?.Phone}.",
                "Бронирование подтверждено",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}
