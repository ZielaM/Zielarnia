using Zielarnia.Models.User;

namespace Zielarnia.Events
{
    public class AuthEventArgs : EventArgs
    {
        public User User { get; }
        
        public AuthEventArgs(User user)
        {
            User = user;
        }
    }
}