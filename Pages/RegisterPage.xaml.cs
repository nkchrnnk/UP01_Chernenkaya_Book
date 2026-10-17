using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using UP01_Chernenkaya_Book.Helpers;
using UP01_Chernenkaya_Book.Models;

namespace UP01_Chernenkaya_Book.Pages;

public partial class RegisterPage : Page
{
    private bool _isFormattingPhone = false;

    // Инициализация компонентов формы регистрации
    public RegisterPage()
    {
        InitializeComponent();
    }

    // Форматирование телефонного номера по маске +7 (999) 999-99-99 в реальном времени
    private void TxtPhone_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isFormattingPhone) return;

        _isFormattingPhone = true;
        try
        {
            string digits = new string(TxtPhone.Text.Where(char.IsDigit).ToArray());

            // Убираем первую 7 или 8 при вводе полного номера
            if (digits.StartsWith("7") || digits.StartsWith("8"))
            {
                digits = digits.Substring(1);
            }

            // Не более 10 цифр после кода страны +7
            if (digits.Length > 10)
            {
                digits = digits.Substring(0, 10);
            }

            if (string.IsNullOrEmpty(digits))
            {
                TxtPhone.Text = string.Empty;
                return;
            }

            // Наложение маски +7 (999) 999-99-99
            var formatted = new StringBuilder("+7 (");
            for (int i = 0; i < digits.Length; i++)
            {
                if (i == 3) formatted.Append(") ");
                else if (i == 6) formatted.Append("-");
                else if (i == 8) formatted.Append("-");
                formatted.Append(digits[i]);
            }

            TxtPhone.Text = formatted.ToString();
            TxtPhone.CaretIndex = TxtPhone.Text.Length;
        }
        finally
        {
            _isFormattingPhone = false;
        }
    }

    // Проверка заполнения полей, валидация пароля и добавление новой учетной записи гостя
    private void BtnSubmitRegister_Click(object sender, RoutedEventArgs e)
    {
        string fio = TxtFio.Text.Trim();
        string login = TxtLogin.Text.Trim();
        string email = TxtEmail.Text.Trim();
        string phone = TxtPhone.Text.Trim();
        string password = PbPassword.Password;
        string confirmPassword = PbPasswordConfirm.Password;

        // Проверяем обязательность заполнения всех полей анкеты
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

        // Проверяем совпадение введенного пароля и его подтверждения
        if (password != confirmPassword)
        {
            MessageBox.Show("Введенные пароли не совпадают!", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Проверка надежности пароля: длина от 6 символов, наличие цифр и букв
        if (password.Length < 6 || !password.Any(char.IsDigit) || !password.Any(char.IsLetter))
        {
            MessageBox.Show("Пароль должен содержать не менее 6 символов, включая цифры и латинские буквы.", "Слабый пароль", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Проверка корректности формата введенного email
        if (!email.Contains('@') || !email.Contains('.'))
        {
            MessageBox.Show("Укажите корректный адрес электронной почты (например, name@mail.ru).", "Неверный формат email", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Проверка полноты номера телефона по маске
        string digitsOnly = new string(phone.Where(char.IsDigit).ToArray());
        if (digitsOnly.Length < 11)
        {
            MessageBox.Show("Пожалуйста, введите полный номер телефона в формате +7 (999) 999-99-99.", "Некорректный телефон", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            using var db = new BookingDbContext();

            // Проверка логина на уникальность
            if (db.Users.Any(u => u.Login.ToLower() == login.ToLower()))
            {
                MessageBox.Show($"Пользователь с логином «{login}» уже зарегистрирован. Пожалуйста, выберите другой логин.", "Логин занят", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Хеширование пароля алгоритмом SHA-1 перед сохранением
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

    // Возврат на экран авторизации
    private void BtnBackToLogin_Click(object sender, RoutedEventArgs e)
    {
        NavigationService.Navigate(new AuthPage());
    }
}
