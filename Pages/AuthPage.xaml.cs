using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UP01_Chernenkaya_Book.Helpers;
using UP01_Chernenkaya_Book.Models;

namespace UP01_Chernenkaya_Book.Pages;

/// <summary>
/// Страница авторизации пользователей: ввод логина и пароля, проверка капчи и разделение ролей
/// </summary>
public partial class AuthPage : Page
{
    private int _failedAttempts = 0;
    private string _currentCaptcha = string.Empty;
    private readonly Random _random = new();

    /// <summary>
    /// Инициализация формы авторизации и установка перехватчика для блокировки копирования капчи
    /// </summary>
    public AuthPage()
    {
        InitializeComponent();
        CommandManager.AddPreviewExecutedHandler(TxtCaptchaInput, OnCaptchaExecuted);
    }

    /// <summary>
    /// Блокировка системных команд копирования, вырезания и вставки в поле ввода капчи
    /// </summary>
    /// <param name="sender">Источник события</param>
    /// <param name="e">Параметры выполнения команды</param>
    private void OnCaptchaExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (e.Command == ApplicationCommands.Copy ||
            e.Command == ApplicationCommands.Cut ||
            e.Command == ApplicationCommands.Paste)
        {
            e.Handled = true;
        }
    }

    /// <summary>
    /// Генерация случайной 4-значной последовательности символов для проверки капчи
    /// </summary>
    private void GenerateCaptcha()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        _currentCaptcha = new string(Enumerable.Repeat(chars, 4).Select(s => s[_random.Next(s.Length)]).ToArray());
        TxtCaptchaCode.Text = _currentCaptcha;
        TxtCaptchaInput.Text = string.Empty;
    }

    /// <summary>
    /// Аутентификация пользователя, проверка хеша пароля и перенаправление в зависимости от роли
    /// </summary>
    /// <param name="sender">Кнопка «Войти»</param>
    /// <param name="e">Параметры события</param>
    private void BtnSignIn_Click(object sender, RoutedEventArgs e)
    {
        string login = TxtLogin.Text.Trim();
        string password = PbPassword.Password;

        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
        {
            MessageBox.Show("Пожалуйста, заполните логин и пароль.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

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

    /// <summary>
    /// Вход в систему под гостевым профилем без авторизации
    /// </summary>
    /// <param name="sender">Кнопка «Войти как гость»</param>
    /// <param name="e">Параметры события</param>
    private void BtnGuest_Click(object sender, RoutedEventArgs e)
    {
        SessionContext.SignOut();
        NavigationService.Navigate(new CatalogPage());
    }

    /// <summary>
    /// Переход на страницу регистрации нового пользователя
    /// </summary>
    /// <param name="sender">Кнопка «Регистрация»</param>
    /// <param name="e">Параметры события</param>
    private void BtnRegister_Click(object sender, RoutedEventArgs e)
    {
        NavigationService.Navigate(new RegisterPage());
    }

    /// <summary>
    /// Переход на страницу изменения пароля
    /// </summary>
    /// <param name="sender">Кнопка смены пароля</param>
    /// <param name="e">Параметры события</param>
    private void BtnChangePassword_Click(object sender, RoutedEventArgs e)
    {
        NavigationService.Navigate(new ChangePasswordPage());
    }
}
