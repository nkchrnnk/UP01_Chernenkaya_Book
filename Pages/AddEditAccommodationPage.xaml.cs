using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using UP01_Chernenkaya_Book.Models;

namespace UP01_Chernenkaya_Book.Pages;

public partial class AddEditAccommodationPage : Page
{
    private readonly Accommodation? _currentAccommodation;

    public AddEditAccommodationPage(Accommodation? accommodation)
    {
        InitializeComponent();
        _currentAccommodation = accommodation;

        LoadReferenceData();

        if (_currentAccommodation != null)
        {
            TxtTitle.Text = "РЕДАКТИРОВАНИЕ ОБЪЕКТА РАЗМЕЩЕНИЯ";
            TxtName.Text = _currentAccommodation.Name;
            CmbType.SelectedValue = _currentAccommodation.TypeId;
            CmbCity.SelectedValue = _currentAccommodation.CityId;
            TxtAddress.Text = _currentAccommodation.Address;
            TxtRooms.Text = _currentAccommodation.Rooms.ToString();
            TxtCapacity.Text = _currentAccommodation.Capacity.ToString();
            TxtPrice.Text = _currentAccommodation.PricePerNight.ToString("F0");
            TxtRating.Text = _currentAccommodation.Rating?.ToString(CultureInfo.InvariantCulture) ?? "4.5";
            TxtDescription.Text = _currentAccommodation.Description ?? "";
            TxtPhoto.Text = _currentAccommodation.Photo ?? "/res/default_hotel.png";
        }
        else
        {
            TxtTitle.Text = "ДОБАВЛЕНИЕ НОВОГО ОБЪЕКТА РАЗМЕЩЕНИЯ";
            TxtPhoto.Text = "/res/hotel1.png";
            TxtRating.Text = "4.8";
            TxtRooms.Text = "20";
            TxtCapacity.Text = "45";
            TxtPrice.Text = "5000";
        }
    }

    private void LoadReferenceData()
    {
        try
        {
            using var db = new BookingDbContext();
            CmbType.ItemsSource = db.AccommodationTypes.OrderBy(t => t.TypeName).ToList();
            if (CmbType.Items.Count > 0) CmbType.SelectedIndex = 0;

            CmbCity.ItemsSource = db.Cities.OrderBy(c => c.CityName).ToList();
            if (CmbCity.Items.Count > 0) CmbCity.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки справочников: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        string name = TxtName.Text.Trim();
        string address = TxtAddress.Text.Trim();
        string description = TxtDescription.Text.Trim();
        string photo = TxtPhoto.Text.Trim();

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(address))
        {
            MessageBox.Show("Пожалуйста, заполните название и адрес объекта.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (CmbType.SelectedValue == null || CmbCity.SelectedValue == null)
        {
            MessageBox.Show("Пожалуйста, выберите тип жилья и город расположения.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(TxtRooms.Text.Trim(), out int rooms) || rooms <= 0)
        {
            MessageBox.Show("Количество номеров должно быть положительным целым числом.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(TxtCapacity.Text.Trim(), out int capacity) || capacity <= 0)
        {
            MessageBox.Show("Вместимость должна быть положительным целым числом.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(TxtPrice.Text.Trim().Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal price) || price <= 0)
        {
            MessageBox.Show("Укажите корректную базовую стоимость за ночь в рублях.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        decimal? rating = null;
        if (decimal.TryParse(TxtRating.Text.Trim().Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal r))
        {
            rating = Math.Clamp(r, 1.0m, 5.0m);
        }

        try
        {
            using var db = new BookingDbContext();

            if (_currentAccommodation == null)
            {
                // Добавление
                var newAcc = new Accommodation
                {
                    Name = name,
                    TypeId = (int)CmbType.SelectedValue,
                    CityId = (int)CmbCity.SelectedValue,
                    Address = address,
                    Rooms = rooms,
                    Capacity = capacity,
                    PricePerNight = price,
                    Rating = rating,
                    Description = description,
                    Photo = string.IsNullOrWhiteSpace(photo) ? "/res/default_hotel.png" : photo
                };

                db.Accommodations.Add(newAcc);
                db.SaveChanges();
                MessageBox.Show("Объект размещения успешно добавлен в систему!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                // Редактирование
                var existing = db.Accommodations.Find(_currentAccommodation.AccommodationId);
                if (existing != null)
                {
                    existing.Name = name;
                    existing.TypeId = (int)CmbType.SelectedValue;
                    existing.CityId = (int)CmbCity.SelectedValue;
                    existing.Address = address;
                    existing.Rooms = rooms;
                    existing.Capacity = capacity;
                    existing.PricePerNight = price;
                    existing.Rating = rating;
                    existing.Description = description;
                    existing.Photo = string.IsNullOrWhiteSpace(photo) ? "/res/default_hotel.png" : photo;

                    db.SaveChanges();
                    MessageBox.Show("Данные объекта размещения успешно обновлены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }

            NavigationService.Navigate(new AccommodationsTabPage());
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка сохранения данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        NavigationService.Navigate(new AccommodationsTabPage());
    }
}
