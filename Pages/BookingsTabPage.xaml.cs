using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using UP01_Chernenkaya_Book.Models;

namespace UP01_Chernenkaya_Book.Pages;

public partial class BookingsTabPage : Page
{
    private List<Booking> _allBookings = new();
    private bool _isInitialized = false;

    public BookingsTabPage()
    {
        InitializeComponent();
        LoadFilterData();
        LoadBookings();
    }

    private void LoadFilterData()
    {
        try
        {
            using var db = new BookingDbContext();
            var statuses = db.BookingStatuses.OrderBy(s => s.StatusName).ToList();

            var list = new List<string> { "Все статусы" };
            list.AddRange(statuses.Select(s => s.StatusName));

            CmbStatusFilter.ItemsSource = list;
            CmbStatusFilter.SelectedIndex = 0;
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки статусов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadBookings()
    {
        try
        {
            using var db = new BookingDbContext();
            _allBookings = db.Bookings
                .Include(b => b.User)
                .Include(b => b.Room).ThenInclude(r => r.Accommodation)
                .Include(b => b.Status)
                .OrderByDescending(b => b.CreatedAt)
                .ToList();

            ApplyFilters();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при загрузке бронирований: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void FilterChanged(object sender, EventArgs e)
    {
        if (!_isInitialized) return;
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        if (_allBookings == null) return;

        IEnumerable<Booking> filtered = _allBookings;

        string query = TxtSearch.Text?.Trim().ToLower() ?? "";
        if (!string.IsNullOrEmpty(query))
        {
            filtered = filtered.Where(b =>
                (b.User != null && b.User.Fio.ToLower().Contains(query)) ||
                (b.Room?.Accommodation != null && b.Room.Accommodation.Name.ToLower().Contains(query)) ||
                b.BookingId.ToString().Contains(query));
        }

        if (CmbStatusFilter.SelectedIndex > 0 && CmbStatusFilter.SelectedItem is string status)
        {
            filtered = filtered.Where(b => b.Status != null && b.Status.StatusName == status);
        }

        var result = filtered.ToList();
        DgBookings.ItemsSource = null;
        DgBookings.ItemsSource = result;
        TxtStatus.Text = $"Отображено бронирований: {result.Count} из {_allBookings.Count}";
    }

    private void BtnConfirm_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is Booking booking)
        {
            try
            {
                using var db = new BookingDbContext();
                var entity = db.Bookings.Find(booking.BookingId);
                var confirmStatus = db.BookingStatuses.FirstOrDefault(s => s.StatusName == "Подтверждено");

                if (entity != null && confirmStatus != null)
                {
                    entity.StatusId = confirmStatus.StatusId;
                    db.SaveChanges();
                    MessageBox.Show($"Бронирование №{booking.BookingId} успешно подтверждено!", "Статус обновлен", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadBookings();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления статуса: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void BtnCancelBooking_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is Booking booking)
        {
            var res = MessageBox.Show(
                $"Вы уверены, что хотите перевести бронирование №{booking.BookingId} в статус «Отменено»?",
                "Подтверждение отмены",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (res == MessageBoxResult.Yes)
            {
                try
                {
                    using var db = new BookingDbContext();
                    var entity = db.Bookings.Find(booking.BookingId);
                    var cancelStatus = db.BookingStatuses.FirstOrDefault(s => s.StatusName == "Отменено");

                    if (entity != null && cancelStatus != null)
                    {
                        entity.StatusId = cancelStatus.StatusId;
                        db.SaveChanges();
                        MessageBox.Show($"Бронирование №{booking.BookingId} отменено.", "Статус обновлен", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadBookings();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка обновления статуса: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private void BtnDeleteBooking_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is Booking booking)
        {
            var res = MessageBox.Show(
                $"Вы уверены, что хотите полностью удалить бронирование №{booking.BookingId} из базы данных?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (res == MessageBoxResult.Yes)
            {
                try
                {
                    using var db = new BookingDbContext();
                    var entity = db.Bookings.Include(b => b.Payments).Include(b => b.BookingServices).FirstOrDefault(b => b.BookingId == booking.BookingId);
                    if (entity != null)
                    {
                        db.Payments.RemoveRange(entity.Payments);
                        db.BookingServices.RemoveRange(entity.BookingServices);
                        db.Bookings.Remove(entity);
                        db.SaveChanges();

                        MessageBox.Show($"Бронирование №{booking.BookingId} успешно удалено!", "Удаление", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadBookings();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления бронирования: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private void BtnBackToHub_Click(object sender, RoutedEventArgs e)
    {
        NavigationService.Navigate(new AdminHubPage());
    }
}
