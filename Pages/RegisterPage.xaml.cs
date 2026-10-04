using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using UP01_Chernenkaya_Book.Helpers;
using UP01_Chernenkaya_Book.Models;

namespace UP01_Chernenkaya_Book.Pages;

public partial class RegisterPage : Page
{
    public RegisterPage()
    {
        InitializeComponent();
    }

    private void BtnSubmitRegister_Click(object sender, RoutedEventArgs e)
    {
        string fio = TxtFio.Text.Trim();
        string login = TxtLogin.Text.Trim();
        string email = TxtEmail.Text.Trim();
        string phone = TxtPhone.Text.Trim();
        string password = PbPassword.Password;
        string confirmPassword = PbPasswordConfirm.Password;

        // Валидация полей
        if (string.IsNullOrWhiteSpace(fio) ||
            string.IsNullOrWhiteSpace(login) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(phone) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(confirmPassword))
        {
            MessageBox.Show("Пожалуйста, заполните все обязательные поля формы регистрации.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Проверка совпадения паролей
        if (password != confirmPassword)
        {
            MessageBox.Show("Введенные пароли не совпадают!", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Проверка сложности пароля (от 6 символов, содержит цифры и буквы)
        if (password.Length < 6 || !password.Any(char.IsDigit) || !password.Any(char.IsLetter))
        {
            MessageBox.Show("Пароль должен содержать не менее 6 символов, включая цифры и латинские буквы.", "Слабый пароль", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Валидация email
        if (!email.Contains('@') || !email.Contains('.'))
        {
            MessageBox.Show("Укажите корректный адрес электронной почты (например, name@mail.ru).", "Неверный формат email", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            using var db = new BookingDbContext();

            // Проверка уникальности логина
            if (db.Users.Any(u => u.Login.ToLower() == login.ToLower()))
            {
                MessageBox.Show($"Пользователь с логином «{login}» уже зарегистрирован. Пожалуйста, выберите другой логин.", "Логин занят", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Шаг 7: Хеширование пароля алгоритмом SHA-1
            string passwordHash = PasswordSecurity.ComputeSha1(password);

            var newUser = new User
            {
                Fio = fio,
                Login = login,
                Email = email,
                Phone = phone,
                Password = passwordHash,
                Role = "User"
            };

            db.Users.Add(newUser);
            db.SaveChanges();

            MessageBox.Show("Регистрация успешно завершена! Теперь вы можете войти в систему со своим логином и паролем.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            NavigationService.Navigate(new AuthPage());
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при сохранении пользователя: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnBackToLogin_Click(object sender, RoutedEventArgs e)
    {
        NavigationService.Navigate(new AuthPage());
    }
}
