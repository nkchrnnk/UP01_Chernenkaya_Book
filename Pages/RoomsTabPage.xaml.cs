using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using UP01_Chernenkaya_Book.Models;

namespace UP01_Chernenkaya_Book.Pages;

public partial class RoomsTabPage : Page
{
    private List<Room> _allRooms = new();
    private bool _isInitialized = false;

    public RoomsTabPage()
    {
        InitializeComponent();
        LoadFilterData();
        LoadRooms();
    }

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

    private void FilterChanged(object sender, EventArgs e)
    {
        if (!_isInitialized) return;
        ApplyFilters();
    }

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

    private void BtnAddRoom_Click(object sender, RoutedEventArgs e)
    {
        NavigationService.Navigate(new AddEditRoomPage(null));
    }

    private void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is Room room)
        {
            NavigationService.Navigate(new AddEditRoomPage(room));
        }
    }

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

    private void BtnBackToHub_Click(object sender, RoutedEventArgs e)
    {
        NavigationService.Navigate(new AdminHubPage());
    }
}
