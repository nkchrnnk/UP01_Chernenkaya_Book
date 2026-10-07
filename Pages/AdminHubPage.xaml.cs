using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using UP01_Chernenkaya_Book.Helpers;
using UP01_Chernenkaya_Book.Models;

namespace UP01_Chernenkaya_Book.Pages;

public partial class AdminHubPage : Page
{
    public AdminHubPage()
    {
        InitializeComponent();
        LoadDashboardMetrics();
    }

    private void LoadDashboardMetrics()
    {
        try
        {
            if (SessionContext.ActiveUser != null)
            {
                TxtAdminSubtitle.Text = $"Администратор: {SessionContext.ActiveUser.Fio} | Полный доступ ко всем модулям системы";
            }

            using var db = new BookingDbContext();
            int hotelsCount = db.Accommodations.Count();
            int roomsCount = db.Rooms.Count();
            int bookingsCount = db.Bookings.Count();
            decimal totalRevenue = db.Payments.Where(p => p.PaymentStatus == "Оплачено").Sum(p => (decimal?)p.Amount) ?? 0m;

            TxtStatHotels.Text = hotelsCount.ToString();
            TxtStatRooms.Text = roomsCount.ToString();
            TxtStatBookings.Text = bookingsCount.ToString();
            TxtStatRevenue.Text = $"{totalRevenue:N0} ₽";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки статистики: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnGoToCatalog_Click(object sender, RoutedEventArgs e)
    {
        NavigationService.Navigate(new CatalogPage());
    }

    private void BtnNavAccommodations_Click(object sender, RoutedEventArgs e)
    {
        NavigationService.Navigate(new AccommodationsTabPage());
    }

    private void BtnNavRooms_Click(object sender, RoutedEventArgs e)
    {
        NavigationService.Navigate(new RoomsTabPage());
    }

    private void BtnNavBookings_Click(object sender, RoutedEventArgs e)
    {
        NavigationService.Navigate(new BookingsTabPage());
    }

    private void BtnNavAnalytics_Click(object sender, RoutedEventArgs e)
    {
        NavigationService.Navigate(new AnalyticsPage());
    }
}
