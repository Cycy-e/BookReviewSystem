using BookReviewSystem.Models;

namespace BookReviewSystem.Controllers;

/// <summary>Holds the currently logged-in user. A visitor has no CurrentUser.</summary>
public static class Session
{
    public static User CurrentUser { get; private set; }

    public static bool IsLoggedIn => CurrentUser != null;
    public static bool IsAdmin => CurrentUser != null && CurrentUser.IsAdmin;
    public static bool IsReader => CurrentUser != null && !CurrentUser.IsAdmin;

    public static void SignIn(User user) => CurrentUser = user;
    public static void Clear() => CurrentUser = null;
}
