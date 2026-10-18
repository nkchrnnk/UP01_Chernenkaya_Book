using UP01_Chernenkaya_Book.Models;

namespace UP01_Chernenkaya_Book.Helpers;

/// <summary>
/// Глобальный контекст текущей пользовательской сессии в приложении
/// </summary>
public static class SessionContext
{
    /// <summary>
    /// Текущий авторизованный пользователь
    /// </summary>
    public static User? ActiveUser { get; set; }

    /// <summary>
    /// Проверка, выполнен ли вход в систему
    /// </summary>
    public static bool IsAuthenticated => ActiveUser != null;

    /// <summary>
    /// Проверка, обладает ли пользователь правами администратора
    /// </summary>
    public static bool IsAdministrator => ActiveUser != null && ActiveUser.Role.Equals("Admin", System.StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Выход из системы и очистка данных текущей сессии
    /// </summary>
    public static void SignOut()
    {
        ActiveUser = null;
    }
}
