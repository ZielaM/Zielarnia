using Zielarnia.Models;

namespace Zielarnia.Models.User
{
    public abstract class User : ILoggable
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Role { get; set; } 

        public abstract string GetRole();

        public virtual void LogAction(string action)
        {
            Console.WriteLine($"[{DateTime.Now}] User {Username} ({Role}) performed: {action}");
        }
    }
}