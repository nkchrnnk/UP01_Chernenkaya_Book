using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using UP01_Chernenkaya_Book.Models;

namespace UP01_Chernenkaya_Book.Pages;

/// <summary>
/// Окно оплаты бронирования с выбором платежного метода
/// </summary>
public partial class PaymentWindow : Window
{
    private readonly int _bookingId;
    private Booking? _booking;
    private decimal _totalAmount = 0;

    /// <summary>
    /// Конструктор окна оплаты бронирования, загрузка данных счета
    /// </summary>
    /// <param name="bookingId">Идентификатор бронирования</param>
    public PaymentWindow(int bookingId)
    {
        InitializeComponent();
        _bookingId = bookingId;
        LoadBookingBill();
    }

    /// <summary>
    /// Загрузка информации о бронировании, номерах, услугах и расчет общей суммы к оплате
    /// </summary>
    private void LoadBookingBill()
    {
        try
        {
            using var db = new BookingDbContext();
            _booking = db.Bookings
                .Include(b => b.Room).ThenInclude(r => r.Accommodation)
                .Include(b => b.BookingServices).ThenInclude(bs => bs.Service)
                .FirstOrDefault(b => b.BookingId == _bookingId);

            if (_booking == null)
            {
                MessageBox.Show("Бронирование не найдено в базе данных.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            TxtTitle.Text = $"Оплата бронирования №{_booking.BookingId}";
            TxtAccName.Text = $"Объект размещения: {_booking.Room?.Accommodation?.Name ?? "Отель"}";
            TxtRoomDetails.Text = $"Категория номера: {_booking.Room?.RoomType} (№{_booking.Room?.RoomNumber}, этаж {_booking.Room?.Floor})";
            TxtDates.Text = $"Даты проживания: с {_booking.CheckIn:dd.MM.yyyy} по {_booking.CheckOut:dd.MM.yyyy}";
            TxtGuests.Text = $"Количество гостей: {_booking.Guests} чел.";

            int nights = _booking.CheckOut.DayNumber - _booking.CheckIn.DayNumber;
            if (nights <= 0) nights = 1;

            decimal roomPrice = _booking.Room?.PricePerNight ?? 0;
            decimal roomCost = roomPrice * nights;

            decimal servicesCost = _booking.BookingServices.Sum(bs => bs.PriceAtBooking * bs.Quantity);
            _totalAmount = roomCost + servicesCost;

            TxtCostBreakdown.Text = $"Проживание: {nights} сут. × {roomPrice:N0} ₽ = {roomCost:N0} ₽" +
                                   (servicesCost > 0 ? $" | Услуги: {servicesCost:N0} ₽" : "");

            TxtTotalAmount.Text = $"К ОПЛАТЕ: {_totalAmount:N0} ₽";
            BtnPay.Content = $"Оплатить {_totalAmount:N0} ₽";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка расчета счета: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Переключение видимости полей ввода в зависимости от выбранного способа оплаты
    /// </summary>
    /// <param name="sender">Выпадающий список методов оплаты</param>
    /// <param name="e">Параметры выбора</param>
    private void CmbMethod_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PanelCard == null || PanelSbp == null || PanelCash == null) return;

        PanelCard.Visibility = Visibility.Collapsed;
        PanelSbp.Visibility = Visibility.Collapsed;
        PanelCash.Visibility = Visibility.Collapsed;

        if (CmbMethod.SelectedIndex == 0)
        {
            PanelCard.Visibility = Visibility.Visible;
        }
        else if (CmbMethod.SelectedIndex == 1)
        {
            PanelSbp.Visibility = Visibility.Visible;
        }
        else
        {
            PanelCash.Visibility = Visibility.Visible;
        }
    }

    /// <summary>
    /// Проверка платежных реквизитов, регистрация платежа в базе и обновление статуса брони
    /// </summary>
    /// <param name="sender">Кнопка «Оплатить»</param>
    /// <param name="e">Параметры события</param>
    private void BtnPay_Click(object sender, RoutedEventArgs e)
    {
        if (_booking == null) return;

        string methodName = "Банковская карта";
        if (CmbMethod.SelectedItem is ComboBoxItem item)
        {
            string content = item.Content.ToString() ?? "";
            if (content.Contains("СБП")) methodName = "СБП";
            else if (content.Contains("Наличные")) methodName = "Наличные при заезде";
        }

        if (methodName == "Банковская карта")
        {
            if (string.IsNullOrWhiteSpace(TxtCardNumber.Text) || TxtCardNumber.Text.Length < 16)
            {
                MessageBox.Show("Пожалуйста, введите корректный 16-значный номер карты.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }
        else if (methodName == "СБП")
        {
            if (string.IsNullOrWhiteSpace(TxtSbpPhone.Text) || TxtSbpPhone.Text.Length < 11)
            {
                MessageBox.Show("Пожалуйста, укажите контактный телефон для оплаты через СБП.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        try
        {
            using var db = new BookingDbContext();

            var payment = new Payment
            {
                BookingId = _bookingId,
                PaymentDate = DateTime.Now,
                Amount = _totalAmount,
                PaymentMethod = methodName,
                PaymentStatus = "Оплачено"
            };

            db.Payments.Add(payment);

            var entity = db.Bookings.Find(_bookingId);
            if (entity != null)
            {
                var confirmedStatus = db.BookingStatuses.FirstOrDefault(s => s.StatusName == "Подтверждено");
                if (confirmedStatus != null)
                {
                    entity.StatusId = confirmedStatus.StatusId;
                }
            }

            db.SaveChanges();

            MessageBox.Show(
                $"Платеж на сумму {_totalAmount:N0} ₽ успешно проведен через «{methodName}»!\n\n" +
                $"• Номер платежа: №{payment.PaymentId}\n" +
                $"• Номер брони: №{_bookingId}\n" +
                $"• Статус бронирования: Подтверждено\n\n" +
                $"Электронная квитанция сохранена в системе.",
                "Оплата успешно завершена",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка проведения платежа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Закрытие окна без проведения платежа
    /// </summary>
    /// <param name="sender">Кнопка «Отмена»</param>
    /// <param name="e">Параметры события</param>
    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
