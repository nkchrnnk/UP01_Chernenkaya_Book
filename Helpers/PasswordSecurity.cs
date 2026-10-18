using System;
using System.Security.Cryptography;
using System.Text;

namespace UP01_Chernenkaya_Book.Helpers;

/// <summary>
/// Сервисный класс для хеширования и безопасной проверки паролей
/// </summary>
public static class PasswordSecurity
{
    /// <summary>
    /// Вычисление хеша пароля по алгоритму SHA-1 в шестнадцатеричном виде
    /// </summary>
    /// <param name="input">Исходный строковый пароль</param>
    /// <returns>Строка шестнадцатеричного хеша SHA-1</returns>
    public static string ComputeSha1(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        byte[] bytes = Encoding.UTF8.GetBytes(input);
        byte[] hash = SHA1.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    /// <summary>
    /// Сверка введенного пользователем пароля с сохраненным хешем в базе данных
    /// </summary>
    /// <param name="input">Введенный пользователем пароль</param>
    /// <param name="storedHash">Хеш из базы данных</param>
    /// <returns>True, если хеши совпадают; иначе False</returns>
    public static bool VerifyPassword(string input, string storedHash)
    {
        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(storedHash))
            return false;

        string hashOfInput = ComputeSha1(input);
        return string.Equals(hashOfInput, storedHash, StringComparison.OrdinalIgnoreCase);
    }
}
