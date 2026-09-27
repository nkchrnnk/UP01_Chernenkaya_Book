using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using UP01_Chernenkaya_Book.Helpers;
using UP01_Chernenkaya_Book.Pages;

namespace UP01_Chernenkaya_Book;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _clockTimer;

    public MainWindow()
    {
        InitializeComponent();

        // Шаг 2: Таймер даты и времени в правом нижнем углу
        _clockTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _clockTimer.Tick += ClockTimer_Tick;
        _clockTimer.Start();
        ClockTimer_Tick(null, EventArgs.Empty);

        // Переход на страницу авторизации (Шаг 4)
        MainFrame.Navigate(new AuthPage());
    }

    private void ClockTimer_Tick(object? sender, EventArgs e)
    {
        TxtClock.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
    }

    private void MainFrame_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
    {
        // Кнопка Назад скрыта на странице входа
        bool isAuthPage = e.Content is AuthPage;
        BtnBack.Visibility = (MainFrame.CanGoBack && !isAuthPage) ? Visibility.Visible : Visibility.Collapsed;

        // Отображение текущего пользователя
        if (SessionContext.ActiveUser != null)
        {
            TxtCurrentUser.Text = $"{SessionContext.ActiveUser.Fio} ({SessionContext.ActiveUser.Role})";
            BtnLogout.Visibility = Visibility.Visible;
        }
        else
        {
            TxtCurrentUser.Text = "Гость";
            BtnLogout.Visibility = Visibility.Collapsed;
        }
    }

    private void BtnBack_Click(object sender, RoutedEventArgs e)
    {
        if (MainFrame.CanGoBack)
        {
            MainFrame.GoBack();
        }
    }

    private void BtnLogout_Click(object sender, RoutedEventArgs e)
    {
        SessionContext.SignOut();
        MainFrame.Navigate(new AuthPage());
    }

    // Шаг 3: Переключение тем оформления
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

    // Шаг 2: Подтверждение выхода из приложения
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
