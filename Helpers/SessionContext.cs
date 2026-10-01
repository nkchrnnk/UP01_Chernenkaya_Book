using UP01_Chernenkaya_Book.Models;

namespace UP01_Chernenkaya_Book.Helpers;

public static class SessionContext
{
    public static User? ActiveUser { get; set; }

    public static bool IsAuthenticated => ActiveUser != null;

    public static bool IsAdministrator => ActiveUser != null && ActiveUser.Role.Equals("Admin", System.StringComparison.OrdinalIgnoreCase);

    public static void SignOut()
    {
        ActiveUser = null;
    }
}
