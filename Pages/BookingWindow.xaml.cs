using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using UP01_Chernenkaya_Book.Helpers;
using UP01_Chernenkaya_Book.Models;

namespace UP01_Chernenkaya_Book.Pages;

/// <summary>
/// Окно оформления заявки на бронирование объекта размещения
/// </summary>
public partial class BookingWindow : Window
{
    private readonly Accommodation _accommodation;
    private List<Room> _rooms = new();
    private List<Service> _services = new();
    private readonly List<CheckBox> _serviceBoxes = new();

    /// <summary>
    /// Конструктор окна бронирования, привязка объекта размещения и загрузка данных
    /// </summary>
    /// <param name="accommodation">Выбранный объект размещения</param>
    public BookingWindow(Accommodation accommodation)
    {
        InitializeComponent();
        _accommodation = accommodation;

        TxtHotelName.Text = _accommodation.Name;
        TxtHotelAddress.Text = $"📍 {_accommodation.City?.CityName ?? "Город не указан"}, {_accommodation.Address}";

        DpCheckIn.SelectedDate = DateTime.Today.AddDays(1);
        DpCheckOut.SelectedDate = DateTime.Today.AddDays(4);

        LoadRoomsAndServices();
        CalculateTotal();
    }

    /// <summary>
    /// Загрузка доступных номеров отеля и списка дополнительных услуг из базы данных
    /// </summary>
    private void LoadRoomsAndServices()
    {
        try
        {
            using var db = new BookingDbContext();

            _rooms = db.Rooms
                .Where(r => r.AccommodationId == _accommodation.AccommodationId && r.IsAvailable == true)
                .OrderBy(r => r.PricePerNight)
                .ToList();

            if (_rooms.Count == 0)
            {
                var fallbackRoom = new Room
                {
                    AccommodationId = _accommodation.AccommodationId,
                    RoomNumber = "101",
                    Floor = 1,
                    RoomType = "Стандарт",
                    Capacity = 2,
                    PricePerNight = _accommodation.PricePerNight,
                    IsAvailable = true
                };
                db.Rooms.Add(fallbackRoom);
                db.SaveChanges();
                _rooms.Add(fallbackRoom);
            }

            CmbRooms.ItemsSource = _rooms.Select(r =>
                $"{r.RoomType} (№{r.RoomNumber}, этаж {r.Floor}, до {r.Capacity} гостей) — {r.PricePerNight:N0} ₽/сутки").ToList();
            CmbRooms.SelectedIndex = 0;

            _services = db.Services.OrderBy(s => s.Price).ToList();
            PanelServices.Children.Clear();
            _serviceBoxes.Clear();

            foreach (var s in _services)
            {
                var cb = new CheckBox
                {
                    Content = $"{s.ServiceName} (+{s.Price:N0} ₽)",
                    Tag = s,
                    Margin = new Thickness(0, 3, 0, 3)
                };
                cb.Checked += (_, _) => CalculateTotal();
                cb.Unchecked += (_, _) => CalculateTotal();
                _serviceBoxes.Add(cb);
                PanelServices.Children.Add(cb);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки номеров и услуг: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Пересчет итоговой стоимости проживания и выбранных дополнительных услуг
    /// </summary>
    private void CalculateTotal()
    {
        if (CmbRooms.SelectedIndex < 0 || CmbRooms.SelectedIndex >= _rooms.Count) return;

        var selectedRoom = _rooms[CmbRooms.SelectedIndex];
        var checkIn = DpCheckIn.SelectedDate ?? DateTime.Today.AddDays(1);
        var checkOut = DpCheckOut.SelectedDate ?? checkIn.AddDays(1);

        int nights = (checkOut - checkIn).Days;
        if (nights <= 0) nights = 1;

        TxtNightsCount.Text = $"Продолжительность: {nights} суток";

        decimal roomCost = selectedRoom.PricePerNight * nights;
        decimal servicesCost = 0;

        foreach (var cb in _serviceBoxes)
        {
            if (cb.IsChecked == true && cb.Tag is Service s)
            {
                servicesCost += s.Price;
            }
        }

        decimal total = roomCost + servicesCost;

        TxtCostRoom.Text = $"Проживание ({nights} сут. × {selectedRoom.PricePerNight:N0} ₽): {roomCost:N0} ₽";
        TxtCostServices.Text = $"Дополнительные услуги: {servicesCost:N0} ₽";
        TxtTotalCost.Text = $"ИТОГО К ОПЛАТЕ: {total:N0} ₽";
    }

    /// <summary>
    /// Обработчик изменения дат проживания в календарях
    /// </summary>
    /// <param name="sender">Календарь выбора даты</param>
    /// <param name="e">Параметры выбора даты</param>
    private void Dates_SelectedDateChanged(object? sender, SelectionChangedEventArgs e)
    {
        CalculateTotal();
    }

    /// <summary>
    /// Обработчик выбора категории и номера
    /// </summary>
    /// <param name="sender">Выпадающий список номеров</param>
    /// <param name="e">Параметры выбора</param>
    private void CmbRooms_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        CalculateTotal();
    }

    /// <summary>
    /// Проверка условий бронирования и отправка новой заявки в базу данных
    /// </summary>
    /// <param name="sender">Кнопка «Забронировать»</param>
    /// <param name="e">Параметры события</param>
    private void BtnSubmit_Click(object sender, RoutedEventArgs e)
    {
        if (!SessionContext.IsAuthenticated)
        {
            MessageBox.Show("Необходимо авторизоваться для совершения бронирования.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (CmbRooms.SelectedIndex < 0 || CmbRooms.SelectedIndex >= _rooms.Count)
        {
            MessageBox.Show("Пожалуйста, выберите подходящую категорию номера.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var checkIn = DpCheckIn.SelectedDate ?? DateTime.Today;
        var checkOut = DpCheckOut.SelectedDate ?? DateTime.Today;

        if (checkIn < DateTime.Today)
        {
            MessageBox.Show("Дата заезда не может быть в прошлом.", "Ошибка дат", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (checkOut <= checkIn)
        {
            MessageBox.Show("Дата выезда должна быть строго позже даты заезда.", "Ошибка дат", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var selectedRoom = _rooms[CmbRooms.SelectedIndex];
        int guestsCount = CmbGuests.SelectedIndex + 1;

        if (guestsCount > selectedRoom.Capacity)
        {
            var res = MessageBox.Show(
                $"Вы выбрали {guestsCount} гостей, тогда как вместимость данного номера составляет до {selectedRoom.Capacity} человек.\nВсе равно продолжить?",
                "Внимание к вместимости",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (res != MessageBoxResult.Yes) return;
        }

        try
        {
            using var db = new BookingDbContext();

            var status = db.BookingStatuses.FirstOrDefault(s => s.StatusName == "На рассмотрении")
                         ?? db.BookingStatuses.FirstOrDefault(s => s.StatusName == "Ожидает оплаты")
                         ?? db.BookingStatuses.FirstOrDefault();

            if (status == null)
            {
                status = new BookingStatus { StatusName = "На рассмотрении" };
                db.BookingStatuses.Add(status);
                db.SaveChanges();
            }

            var newBooking = new Booking
            {
                UserId = SessionContext.ActiveUser!.UserId,
                RoomId = selectedRoom.RoomId,
                CheckIn = DateOnly.FromDateTime(checkIn),
                CheckOut = DateOnly.FromDateTime(checkOut),
                Guests = guestsCount,
                StatusId = status.StatusId,
                CreatedAt = DateTime.Now,
                Comment = string.IsNullOrWhiteSpace(TxtComment.Text) ? null : TxtComment.Text.Trim()
            };

            db.Bookings.Add(newBooking);
            db.SaveChanges();

            foreach (var cb in _serviceBoxes)
            {
                if (cb.IsChecked == true && cb.Tag is Service s)
                {
                    db.BookingServices.Add(new BookingService
                    {
                        BookingId = newBooking.BookingId,
                        ServiceId = s.ServiceId,
                        Quantity = 1,
                        PriceAtBooking = s.Price
                    });
                }
            }
            db.SaveChanges();

            MessageBox.Show(
                $"Ваша заявка №{newBooking.BookingId} на бронирование объекта «{_accommodation.Name}» успешно отправлена!\n\n" +
                $"• Номер: {selectedRoom.RoomType} (№{selectedRoom.RoomNumber})\n" +
                $"• Заезд: {newBooking.CheckIn:dd.MM.yyyy}, Выезд: {newBooking.CheckOut:dd.MM.yyyy}\n" +
                $"• Статус: «{status.StatusName}»\n\n" +
                $"Администратор проверит бронь и подтвердит её. После подтверждения оплатить счет можно в Личном кабинете.",
                "Заявка принята",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка создания заявки на бронирование: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Закрытие окна без сохранения заявки
    /// </summary>
    /// <param name="sender">Кнопка «Отмена»</param>
    /// <param name="e">Параметры события</param>
    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
