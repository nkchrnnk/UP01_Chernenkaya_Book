using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using UP01_Chernenkaya_Book.Models;

namespace UP01_Chernenkaya_Book.Pages;

public partial class AddEditRoomPage : Page
{
    private readonly Room? _currentRoom;

    public AddEditRoomPage(Room? room)
    {
        InitializeComponent();
        _currentRoom = room;

        LoadHotels();

        if (_currentRoom != null)
        {
            TxtTitle.Text = "РЕДАКТИРОВАНИЕ НОМЕРА";
            CmbHotel.SelectedValue = _currentRoom.AccommodationId;
            TxtRoomNumber.Text = _currentRoom.RoomNumber;
            TxtFloor.Text = _currentRoom.Floor.ToString();
            TxtRoomType.Text = _currentRoom.RoomType;
            TxtCapacity.Text = _currentRoom.Capacity.ToString();
            TxtPrice.Text = _currentRoom.PricePerNight.ToString("F0");
            ChkIsAvailable.IsChecked = _currentRoom.IsAvailable;
        }
        else
        {
            TxtTitle.Text = "ДОБАВЛЕНИЕ НОВОГО НОМЕРА";
            TxtFloor.Text = "2";
            TxtRoomType.Text = "Стандарт Double";
            TxtCapacity.Text = "2";
            TxtPrice.Text = "4500";
            ChkIsAvailable.IsChecked = true;
        }
    }

    private void LoadHotels()
    {
        try
        {
            using var db = new BookingDbContext();
            CmbHotel.ItemsSource = db.Accommodations.OrderBy(a => a.Name).ToList();
            if (CmbHotel.Items.Count > 0) CmbHotel.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки отелей: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (CmbHotel.SelectedValue == null)
        {
            MessageBox.Show("Пожалуйста, выберите объект размещения.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string roomNumber = TxtRoomNumber.Text.Trim();
        string roomType = TxtRoomType.Text.Trim();

        if (string.IsNullOrWhiteSpace(roomNumber) || string.IsNullOrWhiteSpace(roomType))
        {
            MessageBox.Show("Заполните номер комнаты и категорию номера.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(TxtFloor.Text.Trim(), out int floor) || floor <= 0)
        {
            MessageBox.Show("Укажите корректный этаж (положительное целое число).", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(TxtCapacity.Text.Trim(), out int capacity) || capacity <= 0)
        {
            MessageBox.Show("Укажите корректную вместимость номера.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(TxtPrice.Text.Trim().Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal price) || price <= 0)
        {
            MessageBox.Show("Укажите корректную стоимость за сутки в рублях.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        int hotelId = (int)CmbHotel.SelectedValue;
        bool isAvailable = ChkIsAvailable.IsChecked ?? true;

        try
        {
            using var db = new BookingDbContext();

            if (_currentRoom == null)
            {
                var newRoom = new Room
                {
                    AccommodationId = hotelId,
                    RoomNumber = roomNumber,
                    Floor = floor,
                    RoomType = roomType,
                    Capacity = capacity,
                    PricePerNight = price,
                    IsAvailable = isAvailable
                };

                db.Rooms.Add(newRoom);
                db.SaveChanges();
                MessageBox.Show("Номер успешно добавлен в номерной фонд!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                var existing = db.Rooms.Find(_currentRoom.RoomId);
                if (existing != null)
                {
                    existing.AccommodationId = hotelId;
                    existing.RoomNumber = roomNumber;
                    existing.Floor = floor;
                    existing.RoomType = roomType;
                    existing.Capacity = capacity;
                    existing.PricePerNight = price;
                    existing.IsAvailable = isAvailable;

                    db.SaveChanges();
                    MessageBox.Show("Данные номера успешно обновлены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }

            NavigationService.Navigate(new RoomsTabPage());
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка сохранения номера: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        NavigationService.Navigate(new RoomsTabPage());
    }
}
