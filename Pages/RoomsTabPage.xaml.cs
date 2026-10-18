using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using UP01_Chernenkaya_Book.Models;

namespace UP01_Chernenkaya_Book.Pages;

/// <summary>
/// Страница управления номерным фондом объектов размещения
/// </summary>
public partial class RoomsTabPage : Page
{
    private List<Room> _allRooms = new();
    private bool _isInitialized = false;

    /// <summary>
    /// Конструктор страницы управления номерным фондом
    /// </summary>
    public RoomsTabPage()
    {
        InitializeComponent();
        LoadFilterData();
        LoadRooms();
    }

    /// <summary>
    /// Загрузка перечня отелей для выпадающего списка фильтрации
    /// </summary>
    private void LoadFilterData()
    {
        try
        {
            using var db = new BookingDbContext();
            var hotels = db.Accommodations.OrderBy(a => a.Name).ToList();

            var list = new List<string> { "Все объекты" };
            list.AddRange(hotels.Select(h => h.Name));

            CmbHotelFilter.ItemsSource = list;
            CmbHotelFilter.SelectedIndex = 0;
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки фильтра отелей: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Загрузка всех комнат с включением данных об отеле
    /// </summary>
    private void LoadRooms()
    {
        try
        {
            using var db = new BookingDbContext();
            _allRooms = db.Rooms
                .Include(r => r.Accommodation)
                .OrderBy(r => r.Accommodation.Name)
                .ThenBy(r => r.RoomNumber)
                .ToList();

            ApplyFilters();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при загрузке номерного фонда: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Обработчик изменения строки поиска или выбранного отеля
    /// </summary>
    /// <param name="sender">Источник события</param>
    /// <param name="e">Параметры события</param>
    private void FilterChanged(object sender, EventArgs e)
    {
        if (!_isInitialized) return;
        ApplyFilters();
    }

    /// <summary>
    /// Применение критериев поиска по номеру комнаты, категории и названию отеля
    /// </summary>
    private void ApplyFilters()
    {
        if (_allRooms == null) return;

        IEnumerable<Room> filtered = _allRooms;

        string query = TxtSearch.Text?.Trim().ToLower() ?? "";
        if (!string.IsNullOrEmpty(query))
        {
            filtered = filtered.Where(r =>
                r.RoomNumber.ToLower().Contains(query) ||
                r.RoomType.ToLower().Contains(query) ||
                (r.Accommodation != null && r.Accommodation.Name.ToLower().Contains(query)));
        }

        if (CmbHotelFilter.SelectedIndex > 0 && CmbHotelFilter.SelectedItem is string hotelName)
        {
            filtered = filtered.Where(r => r.Accommodation != null && r.Accommodation.Name == hotelName);
        }

        var result = filtered.ToList();
        DgRooms.ItemsSource = result;
        TxtStatus.Text = $"Отображено номеров: {result.Count} из {_allRooms.Count}";
    }

    /// <summary>
    /// Переход к форме создания нового номера
    /// </summary>
    /// <param name="sender">Кнопка «Добавить номер»</param>
    /// <param name="e">Параметры события</param>
    private void BtnAddRoom_Click(object sender, RoutedEventArgs e)
    {
        NavigationService.Navigate(new AddEditRoomPage(null));
    }

    /// <summary>
    /// Переход к форме редактирования выбранного номера
    /// </summary>
    /// <param name="sender">Кнопка «Изменить»</param>
    /// <param name="e">Параметры события</param>
    private void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is Room room)
        {
            NavigationService.Navigate(new AddEditRoomPage(room));
        }
    }

    /// <summary>
    /// Удаление номера из базы данных с подтверждением
    /// </summary>
    /// <param name="sender">Кнопка «Удалить»</param>
    /// <param name="e">Параметры события</param>
    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is Room room)
        {
            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить номер №{room.RoomNumber} ({room.Accommodation?.Name})?\nВсе связанные бронирования данного номера также будут удалены!",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using var db = new BookingDbContext();
                    var entity = db.Rooms.Find(room.RoomId);
                    if (entity != null)
                    {
                        db.Rooms.Remove(entity);
                        db.SaveChanges();
                        MessageBox.Show("Номер успешно удален.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadRooms();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении номера: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    /// <summary>
    /// Возврат в панель администратора
    /// </summary>
    /// <param name="sender">Кнопка «Назад»</param>
    /// <param name="e">Параметры события</param>
    private void BtnBackToHub_Click(object sender, RoutedEventArgs e)
    {
        NavigationService.Navigate(new AdminHubPage());
    }
}
