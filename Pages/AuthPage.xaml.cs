using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UP01_Chernenkaya_Book.Helpers;
using UP01_Chernenkaya_Book.Models;

namespace UP01_Chernenkaya_Book.Pages;

public partial class AuthPage : Page
{
    private int _failedAttempts = 0;
    private string _currentCaptcha = string.Empty;
    private readonly Random _random = new();

    public AuthPage()
    {
        InitializeComponent();

        // Шаг 5: Запрет копирования, вырезания и вставки капчи
        CommandManager.AddPreviewExecutedHandler(TxtCaptchaInput, OnCaptchaExecuted);
    }

    private void OnCaptchaExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (e.Command == ApplicationCommands.Copy ||
            e.Command == ApplicationCommands.Cut ||
            e.Command == ApplicationCommands.Paste)
        {
            e.Handled = true;
        }
    }

    private void GenerateCaptcha()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        _currentCaptcha = new string(Enumerable.Repeat(chars, 4).Select(s => s[_random.Next(s.Length)]).ToArray());
        TxtCaptchaCode.Text = _currentCaptcha;
        TxtCaptchaInput.Text = string.Empty;
    }

    private void BtnSignIn_Click(object sender, RoutedEventArgs e)
    {
        string login = TxtLogin.Text.Trim();
        string password = PbPassword.Password;

        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
        {
            MessageBox.Show("Пожалуйста, заполните логин и пароль.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Шаг 5: Проверка капчи при 3 и более неудачных попытках
        if (_failedAttempts >= 3)
        {
            if (string.IsNullOrWhiteSpace(TxtCaptchaInput.Text) ||
                !string.Equals(TxtCaptchaInput.Text.Trim(), _currentCaptcha, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Защитный код (капча) введен неверно!", "Ошибка капчи", MessageBoxButton.OK, MessageBoxImage.Warning);
                GenerateCaptcha();
                return;
            }
        }

        try
        {
            using var db = new BookingDbContext();
            var user = db.Users.FirstOrDefault(u => u.Login == login);

            if (user != null && PasswordSecurity.VerifyPassword(password, user.Password))
            {
                // Успешный вход
                SessionContext.ActiveUser = user;
                _failedAttempts = 0;

                MessageBox.Show($"Добро пожаловать, {user.Fio}!", "Успешный вход", MessageBoxButton.OK, MessageBoxImage.Information);

                if (SessionContext.IsAdministrator)
                {
                    NavigationService.Navigate(new AdminHubPage());
                }
                else
                {
                    NavigationService.Navigate(new CatalogPage());
                }
            }
            else
            {
                _failedAttempts++;
                if (_failedAttempts >= 3)
                {
                    CaptchaPanel.Visibility = Visibility.Visible;
                    GenerateCaptcha();
                    MessageBox.Show("Неверный логин или пароль! Активирована защита от подбора паролей (введите капчу).", "Ошибка авторизации", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    MessageBox.Show($"Неверный логин или пароль! Осталось попыток до появления капчи: {3 - _failedAttempts}", "Ошибка авторизации", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка подключения к базе данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnGuest_Click(object sender, RoutedEventArgs e)
    {
        SessionContext.SignOut();
        NavigationService.Navigate(new CatalogPage());
    }

    private void BtnRegister_Click(object sender, RoutedEventArgs e)
    {
        NavigationService.Navigate(new RegisterPage());
    }

    private void BtnChangePassword_Click(object sender, RoutedEventArgs e)
    {
        NavigationService.Navigate(new ChangePasswordPage());
    }
}
