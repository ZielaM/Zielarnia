namespace Zielarnia.Core.Events;

/// <summary>
/// Provides data for user action events.
/// </summary>
public class UserActionEventArgs : EventArgs
{
    public string Login { get; }
    public DateTime Timestamp { get; }
    public bool Success { get; }
    public string? Message { get; } // Optional additional info/error

    public UserActionEventArgs(string login, bool success, string? message = null)
    {
        Login = login;
        Timestamp = DateTime.UtcNow; // Use UTC for consistency
        Success = success;
        Message = message;
    }
}