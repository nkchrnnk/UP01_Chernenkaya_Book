using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using UP01_Chernenkaya_Book.Models;

namespace UP01_Chernenkaya_Book.Pages;

public partial class AccommodationsTabPage : Page
{
    private List<Accommodation> _accommodations = new();

    public AccommodationsTabPage()
    {
        InitializeComponent();
        LoadAccommodations();
    }

    private void LoadAccommodations()
    {
        try
        {
            using var db = new BookingDbContext();
            _accommodations = db.Accommodations
                .Include(a => a.City)
                .Include(a => a.Type)
                .OrderBy(a => a.Name)
                .ToList();

            ApplySearchFilter();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при загрузке объектов размещения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplySearchFilter()
    {
        string query = TxtSearch.Text?.Trim().ToLower() ?? "";
        var filtered = string.IsNullOrEmpty(query)
            ? _accommodations
            : _accommodations.Where(a =>
                a.Name.ToLower().Contains(query) ||
                a.Address.ToLower().Contains(query) ||
                (a.City != null && a.City.CityName.ToLower().Contains(query))).ToList();

        DgAccommodations.ItemsSource = filtered;
        TxtStatus.Text = $"Отображено объектов: {filtered.Count} из {_accommodations.Count}";
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplySearchFilter();
    }

    private void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        NavigationService.Navigate(new AddEditAccommodationPage(null));
    }

    private void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is Accommodation acc)
        {
            NavigationService.Navigate(new AddEditAccommodationPage(acc));
        }
    }

    // Шаг 10: Каскадное удаление с подтверждением
    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is Accommodation acc)
        {
            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить объект размещения «{acc.Name}»?\n\nВнимание: связанные с ним номера и бронирования также будут удалены из базы данных!",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using var db = new BookingDbContext();
                    var entity = db.Accommodations.Find(acc.AccommodationId);
                    if (entity != null)
                    {
                        db.Accommodations.Remove(entity);
                        db.SaveChanges();
                        MessageBox.Show("Объект размещения успешно удален.", "Удаление завершено", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadAccommodations();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении объекта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private void BtnBackToHub_Click(object sender, RoutedEventArgs e)
    {
        NavigationService.Navigate(new AdminHubPage());
    }
}
