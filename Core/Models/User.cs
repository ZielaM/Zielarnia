using Zielarnia.Core.Models.Enums;

namespace Zielarnia.Core.Models;

/// <summary>
/// Represents a logged-in user session.
/// Does not inherit from EntityBase as it's not a direct DB entity in this design.
/// </summary>
public class User
{
    public string Login { get; private set; }
    public UserRole Role { get; private set; }

    public User(string login, UserRole role)
    {
        Login = login;
        Role = role;
    }
}