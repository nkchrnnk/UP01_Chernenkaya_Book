using System;
using System.Security.Cryptography;
using System.Text;

namespace UP01_Chernenkaya_Book.Helpers;

public static class PasswordSecurity
{
    public static string ComputeSha1(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        byte[] bytes = Encoding.UTF8.GetBytes(input);
        byte[] hash = SHA1.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    public static bool VerifyPassword(string input, string storedHash)
    {
        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(storedHash))
            return false;

        string hashOfInput = ComputeSha1(input);
        return string.Equals(hashOfInput, storedHash, StringComparison.OrdinalIgnoreCase);
    }
}
