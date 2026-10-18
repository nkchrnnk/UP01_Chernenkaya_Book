using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using UP01_Chernenkaya_Book.Helpers;
using UP01_Chernenkaya_Book.Pages;

namespace UP01_Chernenkaya_Book;

/// <summary>
/// Главное окно приложения: контейнер навигации, таймер времени, управление сессией и темами
/// </summary>
public partial class MainWindow : Window
{
    private readonly DispatcherTimer _clockTimer;

    /// <summary>
    /// Конструктор главного окна приложения, запуск часов и переход на страницу входа
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        _clockTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _clockTimer.Tick += ClockTimer_Tick;
        _clockTimer.Start();
        ClockTimer_Tick(null, EventArgs.Empty);

        MainFrame.Navigate(new AuthPage());
    }

    /// <summary>
    /// Обработчик тика таймера для отображения актуального времени в подвале
    /// </summary>
    /// <param name="sender">Источник события таймера</param>
    /// <param name="e">Параметры события</param>
    private void ClockTimer_Tick(object? sender, EventArgs e)
    {
        TxtClock.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
    }

    /// <summary>
    /// Обновление состояния элементов интерфейса шапки при смене страниц
    /// </summary>
    /// <param name="sender">Источник события навигации</param>
    /// <param name="e">Параметры навигации</param>
    private void MainFrame_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
    {
        bool isAuthPage = e.Content is AuthPage;
        BtnBack.Visibility = (MainFrame.CanGoBack && !isAuthPage) ? Visibility.Visible : Visibility.Collapsed;

        if (SessionContext.ActiveUser != null)
        {
            TxtCurrentUser.Text = $"{SessionContext.ActiveUser.Fio} ({SessionContext.ActiveUser.Role})";
            BtnLogout.Visibility = Visibility.Visible;
            BtnCabinet.Visibility = Visibility.Visible;
        }
        else
        {
            TxtCurrentUser.Text = "Гость";
            BtnLogout.Visibility = Visibility.Collapsed;
            BtnCabinet.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>
    /// Возврат на предыдущую посещенную страницу
    /// </summary>
    /// <param name="sender">Кнопка «Назад»</param>
    /// <param name="e">Параметры события</param>
    private void BtnBack_Click(object sender, RoutedEventArgs e)
    {
        if (MainFrame.CanGoBack)
        {
            MainFrame.GoBack();
        }
    }

    /// <summary>
    /// Быстрый переход в личный кабинет авторизованного пользователя
    /// </summary>
    /// <param name="sender">Кнопка «Личный кабинет»</param>
    /// <param name="e">Параметры события</param>
    private void BtnCabinet_Click(object sender, RoutedEventArgs e)
    {
        if (SessionContext.IsAuthenticated)
        {
            MainFrame.Navigate(new UserCabinetPage());
        }
    }

    /// <summary>
    /// Завершение текущего пользовательского сеанса и возврат на форму авторизации
    /// </summary>
    /// <param name="sender">Кнопка «Выйти»</param>
    /// <param name="e">Параметры события</param>
    private void BtnLogout_Click(object sender, RoutedEventArgs e)
    {
        SessionContext.SignOut();
        MainFrame.Navigate(new AuthPage());
    }

    /// <summary>
    /// Динамическая смена темы оформления (фирменная или альтернативная темная)
    /// </summary>
    /// <param name="sender">Выпадающий список тем</param>
    /// <param name="e">Параметры выбора</param>
    private void CmbTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbTheme.SelectedItem is ComboBoxItem selectedItem)
        {
            string themeName = selectedItem.Content.ToString() ?? "";
            string themeUri = themeName.Contains("Тёмная") ? "CustomTheme.xaml" : "Dictionary.xaml";

            try
            {
                var newDict = new ResourceDictionary
                {
                    Source = new Uri(themeUri, UriKind.Relative)
                };

                Application.Current.Resources.MergedDictionaries.Clear();
                Application.Current.Resources.MergedDictionaries.Add(newDict);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка переключения темы: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    /// <summary>
    /// Подтверждение намерения пользователя закрыть программу
    /// </summary>
    /// <param name="sender">Окно приложения</param>
    /// <param name="e">Параметры события закрытия</param>
    private void Window_Closing(object sender, CancelEventArgs e)
    {
        var result = MessageBox.Show(
            "Вы уверены, что хотите завершить работу с сервисом бронирования?",
            "Подтверждение выхода",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.No)
        {
            e.Cancel = true;
        }
    }
}
