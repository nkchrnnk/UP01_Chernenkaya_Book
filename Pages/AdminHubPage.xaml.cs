using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using UP01_Chernenkaya_Book.Helpers;
using UP01_Chernenkaya_Book.Models;

namespace UP01_Chernenkaya_Book.Pages;

/// <summary>
/// Главный хаб администратора системы бронирования отелей
/// </summary>
public partial class AdminHubPage : Page
{
    /// <summary>
    /// Конструктор главного хаба администратора, инициализация и загрузка сводки
    /// </summary>
    public AdminHubPage()
    {
        InitializeComponent();
        LoadDashboardMetrics();
    }

    /// <summary>
    /// Загрузка ключевых показателей дашборда: количество отелей, номеров, броней и сумма оплат
    /// </summary>
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

    /// <summary>
    /// Переход в публичный каталог объектов
    /// </summary>
    /// <param name="sender">Кнопка перехода</param>
    /// <param name="e">Параметры события</param>
    private void BtnGoToCatalog_Click(object sender, RoutedEventArgs e)
    {
        NavigationService.Navigate(new CatalogPage());
    }

    /// <summary>
    /// Переход к модулю управления объектами размещения
    /// </summary>
    /// <param name="sender">Кнопка перехода</param>
    /// <param name="e">Параметры события</param>
    private void BtnNavAccommodations_Click(object sender, RoutedEventArgs e)
    {
        NavigationService.Navigate(new AccommodationsTabPage());
    }

    /// <summary>
    /// Переход к модулю управления номерным фондом
    /// </summary>
    /// <param name="sender">Кнопка перехода</param>
    /// <param name="e">Параметры события</param>
    private void BtnNavRooms_Click(object sender, RoutedEventArgs e)
    {
        NavigationService.Navigate(new RoomsTabPage());
    }

    /// <summary>
    /// Переход к модулю журнала бронирований
    /// </summary>
    /// <param name="sender">Кнопка перехода</param>
    /// <param name="e">Параметры события</param>
    private void BtnNavBookings_Click(object sender, RoutedEventArgs e)
    {
        NavigationService.Navigate(new BookingsTabPage());
    }

    /// <summary>
    /// Переход к модулю аналитики, диаграмм и экспорта отчетов
    /// </summary>
    /// <param name="sender">Кнопка перехода</param>
    /// <param name="e">Параметры события</param>
    private void BtnNavAnalytics_Click(object sender, RoutedEventArgs e)
    {
        NavigationService.Navigate(new AnalyticsPage());
    }
}
