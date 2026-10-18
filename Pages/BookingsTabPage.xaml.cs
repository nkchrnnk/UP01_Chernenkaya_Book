using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using UP01_Chernenkaya_Book.Models;

namespace UP01_Chernenkaya_Book.Pages;

/// <summary>
/// Страница управления журналом бронирований для администратора
/// </summary>
public partial class BookingsTabPage : Page
{
    private List<Booking> _allBookings = new();
    private bool _isInitialized = false;

    /// <summary>
    /// Конструктор страницы управления бронированиями в панели администратора
    /// </summary>
    public BookingsTabPage()
    {
        InitializeComponent();
        LoadFilterData();
        LoadBookings();
    }

    /// <summary>
    /// Загрузка доступных статусов для фильтрации списка заявок
    /// </summary>
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

    /// <summary>
    /// Загрузка всех бронирований с подгрузкой связанных сущностей отелей, комнат и клиентов
    /// </summary>
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

    /// <summary>
    /// Обработчик изменения строки поиска или выбранного статуса
    /// </summary>
    /// <param name="sender">Источник события</param>
    /// <param name="e">Параметры события</param>
    private void FilterChanged(object sender, EventArgs e)
    {
        if (!_isInitialized) return;
        ApplyFilters();
    }

    /// <summary>
    /// Применение текстовой фильтрации и фильтра по статусу к таблице бронирований
    /// </summary>
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

    /// <summary>
    /// Одобрение заявки администратором: перевод в статус «Ожидает оплаты» для выставления счета гостю
    /// </summary>
    /// <param name="sender">Кнопка «Одобрить заявку»</param>
    /// <param name="e">Параметры события</param>
    private void BtnApprove_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is Booking booking)
        {
            try
            {
                using var db = new BookingDbContext();
                var entity = db.Bookings.Find(booking.BookingId);
                var paymentStatus = db.BookingStatuses.FirstOrDefault(s => s.StatusName == "Ожидает оплаты");

                if (entity != null && paymentStatus != null)
                {
                    entity.StatusId = paymentStatus.StatusId;
                    db.SaveChanges();
                    MessageBox.Show($"Заявка №{booking.BookingId} одобрена! Статус изменен на «Ожидает оплаты». Счет доступен гостю в Личном кабинете.", "Статус обновлен", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadBookings();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка изменения статуса: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    /// <summary>
    /// Перевод бронирования в статус «Подтверждено»
    /// </summary>
    /// <param name="sender">Кнопка «Подтвердить»</param>
    /// <param name="e">Параметры события</param>
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

    /// <summary>
    /// Перевод бронирования в статус «Отменено»
    /// </summary>
    /// <param name="sender">Кнопка «Отменить»</param>
    /// <param name="e">Параметры события</param>
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

    /// <summary>
    /// Полное каскадное удаление бронирования и связанных услуг и платежей из базы данных
    /// </summary>
    /// <param name="sender">Кнопка «Удалить»</param>
    /// <param name="e">Параметры события</param>
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

    /// <summary>
    /// Возврат в панель управления администратора
    /// </summary>
    /// <param name="sender">Кнопка «Назад»</param>
    /// <param name="e">Параметры события</param>
    private void BtnBackToHub_Click(object sender, RoutedEventArgs e)
    {
        NavigationService.Navigate(new AdminHubPage());
    }
}
