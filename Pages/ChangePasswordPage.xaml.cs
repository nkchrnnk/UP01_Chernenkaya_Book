using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using UP01_Chernenkaya_Book.Helpers;
using UP01_Chernenkaya_Book.Models;

namespace UP01_Chernenkaya_Book.Pages;

public partial class ChangePasswordPage : Page
{
    public ChangePasswordPage()
    {
        InitializeComponent();

        if (SessionContext.ActiveUser != null)
        {
            TxtLogin.Text = SessionContext.ActiveUser.Login;
            TxtLogin.IsEnabled = false;
        }
    }

    private void BtnSubmitChangePassword_Click(object sender, RoutedEventArgs e)
    {
        string login = TxtLogin.Text.Trim();
        string oldPassword = PbOldPassword.Password;
        string newPassword = PbNewPassword.Password;
        string confirmPassword = PbNewPasswordConfirm.Password;

        if (string.IsNullOrWhiteSpace(login) ||
            string.IsNullOrWhiteSpace(oldPassword) ||
            string.IsNullOrWhiteSpace(newPassword) ||
            string.IsNullOrWhiteSpace(confirmPassword))
        {
            MessageBox.Show("Пожалуйста, заполните все поля формы смены пароля.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (newPassword != confirmPassword)
        {
            MessageBox.Show("Новый пароль и его подтверждение не совпадают!", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (oldPassword == newPassword)
        {
            MessageBox.Show("Новый пароль не должен совпадать с текущим старым паролем.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (newPassword.Length < 6 || !newPassword.Any(char.IsDigit) || !newPassword.Any(char.IsLetter))
        {
            MessageBox.Show("Новый пароль должен содержать минимум 6 символов, цифры и латинские буквы.", "Слабый пароль", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            using var db = new BookingDbContext();
            var user = db.Users.FirstOrDefault(u => u.Login.ToLower() == login.ToLower());

            if (user == null || !PasswordSecurity.VerifyPassword(oldPassword, user.Password))
            {
                MessageBox.Show("Неверный логин или текущий старый пароль!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Шаг 11: Обновление хеша пароля
            user.Password = PasswordSecurity.ComputeSha1(newPassword);
            db.SaveChanges();

            MessageBox.Show("Пароль успешно изменен! Используйте новый пароль при следующем входе.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
            else
            {
                NavigationService.Navigate(new AuthPage());
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при обновлении пароля: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnBack_Click(object sender, RoutedEventArgs e)
    {
        if (NavigationService.CanGoBack)
        {
            NavigationService.GoBack();
        }
        else
        {
            NavigationService.Navigate(new AuthPage());
        }
    }
}
