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
/// Модель представления для удобного отображения строк бронирований в личном кабинете гостя
/// </summary>
public class UserBookingItem
{
    public Booking Booking { get; set; } = null!;
    public int BookingId { get; set; }
    public Room? Room { get; set; }
    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }
    public int Guests { get; set; }
    public BookingStatus? Status { get; set; }
    public decimal TotalAmount { get; set; }
    public bool IsPaid { get; set; }

    public string LocationText { get; set; } = "";
    public string PeriodText { get; set; } = "";

    public bool CanPay => (Booking?.Status?.StatusName == "Ожидает оплаты") && !IsPaid;
    public bool CanCancel => Booking?.Status?.StatusName != "Отменено" && Booking?.Status?.StatusName != "Завершено";

    public string PaymentStatusText => IsPaid 
        ? "✓ Оплачено" 
        : (Booking?.Status?.StatusName == "Ожидает оплаты" 
            ? "Ожидает оплаты" 
            : (Booking?.Status?.StatusName == "На рассмотрении" ? "На рассмотрении" : "Счет формируется"));
    public string TotalAmountText => $"{TotalAmount:N0} ₽";
}

/// <summary>
/// Страница личного кабинета пользователя для просмотра бронирований и оплаты
/// </summary>
public partial class UserCabinetPage : Page
{
    private List<UserBookingItem> _items = new();

    /// <summary>
    /// Инициализация компонентов личного кабинета пользователя
    /// </summary>
    public UserCabinetPage()
    {
        InitializeComponent();
        DisplayUserProfile();
        LoadUserBookings();
    }

    /// <summary>
    /// Отображение личных данных активного пользователя
    /// </summary>
    private void DisplayUserProfile()
    {
        if (SessionContext.ActiveUser != null)
        {
            var u = SessionContext.ActiveUser;
            TxtUserProfile.Text = $"ФИО: {u.Fio} | Телефон: {u.Phone ?? "-"} | Email: {u.Email ?? "-"}";
        }
        else
        {
            TxtUserProfile.Text = "Гостевой сеанс (авторизация не выполнена)";
        }
    }

    /// <summary>
    /// Загрузка всех бронирований текущего пользователя из базы данных и расчет сумм
    /// </summary>
    private void LoadUserBookings()
    {
        if (!SessionContext.IsAuthenticated || SessionContext.ActiveUser == null)
        {
            TxtStatus.Text = "Для просмотра истории заявок необходимо авторизоваться.";
            DgUserBookings.ItemsSource = null;
            return;
        }

        if (SessionContext.ActiveUser.Role == "Admin")
        {
            TxtStatus.Text = "Личный кабинет предназначен для гостей. Управление всеми бронированиями осуществляется в Журнале бронирований.";
            DgUserBookings.ItemsSource = null;
            return;
        }

        try
        {
            using var db = new BookingDbContext();
            int currentUserId = SessionContext.ActiveUser.UserId;

            var bookings = db.Bookings
                .Where(b => b.UserId == currentUserId)
                .Include(b => b.Room).ThenInclude(r => r.Accommodation).ThenInclude(a => a.City)
                .Include(b => b.Status)
                .Include(b => b.Payments)
                .Include(b => b.BookingServices).ThenInclude(bs => bs.Service)
                .OrderByDescending(b => b.CreatedAt)
                .ToList();

            _items = bookings.Select(b =>
            {
                int nights = b.CheckOut.DayNumber - b.CheckIn.DayNumber;
                if (nights <= 0) nights = 1;

                decimal roomCost = (b.Room?.PricePerNight ?? 0) * nights;
                decimal servicesCost = b.BookingServices.Sum(bs => bs.PriceAtBooking * bs.Quantity);
                decimal total = roomCost + servicesCost;

                bool isPaid = b.Payments.Any(p => p.PaymentStatus == "Оплачено");

                return new UserBookingItem
                {
                    Booking = b,
                    BookingId = b.BookingId,
                    Room = b.Room,
                    CheckIn = b.CheckIn,
                    CheckOut = b.CheckOut,
                    Guests = b.Guests,
                    Status = b.Status,
                    TotalAmount = total,
                    IsPaid = isPaid,
                    LocationText = $"{b.Room?.Accommodation?.City?.CityName ?? "Город не указан"}, {b.Room?.Accommodation?.Address ?? "Адрес не указан"}",
                    PeriodText = $"{b.CheckIn:dd.MM.yyyy} — {b.CheckOut:dd.MM.yyyy}"
                };
            }).ToList();

            DgUserBookings.ItemsSource = null;
            DgUserBookings.ItemsSource = _items;

            TxtTotalBookings.Text = _items.Count.ToString();
            TxtPendingPayment.Text = _items.Count(i => i.Booking?.Status?.StatusName == "Ожидает оплаты" && !i.IsPaid).ToString();
            TxtConfirmedBookings.Text = _items.Count(i => i.IsPaid || i.Booking?.Status?.StatusName == "Подтверждено").ToString();

            TxtStatus.Text = $"Всего ваших бронирований: {_items.Count}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки бронирований: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Обработчик оплаты счета по выбранному бронированию
    /// </summary>
    /// <param name="sender">Кнопка оплаты</param>
    /// <param name="e">Параметры события</param>
    private void BtnPayBooking_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is UserBookingItem item)
        {
            var paymentWnd = new PaymentWindow(item.BookingId);
            if (paymentWnd.ShowDialog() == true)
            {
                LoadUserBookings();
            }
        }
    }

    /// <summary>
    /// Просмотр детализированного счета и квитанции по бронированию
    /// </summary>
    /// <param name="sender">Кнопка просмотра счета</param>
    /// <param name="e">Параметры события</param>
    private void BtnViewBill_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is UserBookingItem item)
        {
            var b = item.Booking;
            int nights = b.CheckOut.DayNumber - b.CheckIn.DayNumber;
            if (nights <= 0) nights = 1;

            string servicesText = "";
            if (b.BookingServices.Count > 0)
            {
                servicesText = "\n\nДополнительные услуги:\n" +
                    string.Join("\n", b.BookingServices.Select(bs => $"• {bs.Service?.ServiceName}: {bs.PriceAtBooking:N0} ₽"));
            }

            string paymentInfo = item.IsPaid
                ? "\n\nСтатус оплаты: ОПЛАЧЕНО ПОЛНОСТЬЮ"
                : $"\n\nСтатус оплаты: {item.PaymentStatusText}\nДля оплаты нажмите кнопку «💳 Оплатить» в таблице.";

            MessageBox.Show(
                $"СЧЕТ НА ОПЛАТУ №{b.BookingId}\n" +
                $"Объект: {b.Room?.Accommodation?.Name} ({b.Room?.Accommodation?.City?.CityName})\n" +
                $"Адрес: {b.Room?.Accommodation?.Address}\n" +
                $"Номер: {b.Room?.RoomType} (№{b.Room?.RoomNumber}, этаж {b.Room?.Floor})\n" +
                $"Период: с {b.CheckIn:dd.MM.yyyy} по {b.CheckOut:dd.MM.yyyy} ({nights} суток)\n" +
                $"Количество гостей: {b.Guests} чел.\n\n" +
                $"Проживание: {nights} сут. × {b.Room?.PricePerNight:N0} ₽ = {(nights * (b.Room?.PricePerNight ?? 0)):N0} ₽" +
                servicesText +
                $"\n\nИТОГО К ОПЛАТЕ: {item.TotalAmount:N0} ₽" +
                $"\nТекущий статус брони: {b.Status?.StatusName}" +
                paymentInfo,
                $"Счет бронирования №{b.BookingId}",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    /// <summary>
    /// Отмена заявки на бронирование
    /// </summary>
    /// <param name="sender">Кнопка «Отменить»</param>
    /// <param name="e">Параметры события</param>
    private void BtnCancelBooking_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is UserBookingItem item)
        {
            var res = MessageBox.Show(
                $"Вы действительно хотите отменить заявку на бронирование №{item.BookingId}?",
                "Подтверждение отмены",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (res == MessageBoxResult.Yes)
            {
                try
                {
                    using var db = new BookingDbContext();
                    var entity = db.Bookings.Find(item.BookingId);
                    var cancelStatus = db.BookingStatuses.FirstOrDefault(s => s.StatusName == "Отменено");

                    if (entity != null && cancelStatus != null)
                    {
                        entity.StatusId = cancelStatus.StatusId;
                        db.SaveChanges();
                        MessageBox.Show($"Бронирование №{item.BookingId} успешно отменено.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadUserBookings();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка отмены заявки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    /// <summary>
    /// Переход обратно на страницу каталога объектов
    /// </summary>
    /// <param name="sender">Кнопка «В каталог»</param>
    /// <param name="e">Параметры события</param>
    private void BtnCatalog_Click(object sender, RoutedEventArgs e)
    {
        NavigationService.Navigate(new CatalogPage());
    }

    /// <summary>
    /// Переход на страницу изменения пароля
    /// </summary>
    /// <param name="sender">Кнопка «Сменить пароль»</param>
    /// <param name="e">Параметры события</param>
    private void BtnChangePassword_Click(object sender, RoutedEventArgs e)
    {
        NavigationService.Navigate(new ChangePasswordPage());
    }
}
