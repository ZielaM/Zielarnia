using System;
using Zielarnia.Data.Repositories;
using Zielarnia.Events;
using Zielarnia.Models.User;
using Zielarnia.Utils.Exceptions;

namespace Zielarnia.Services
{
    public class AuthService
    {
        public delegate void AuthEventHandler(object sender, AuthEventArgs e);
        public event AuthEventHandler UserLoggedIn;
        public event AuthEventHandler UserLoggedOut;
        
        private readonly IUserRepository _userRepository;
        
        public AuthService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }
        
        public User Login(string username, string password)
        {
            try
            {
                var user = _userRepository.Authenticate(username, password);
                OnUserLoggedIn(user);
                return user;
            }
            catch (Exception ex)
            {
                throw new AuthException("Login failed", ex);
            }
        }
        
        public void Logout()
        {
            if (UserLoggedOut != null)
            {
                OnUserLoggedOut(_currentUser);
            }
            _currentUser = null;
        }
        
        protected virtual void OnUserLoggedIn(User user)
        {
            UserLoggedIn?.Invoke(this, new AuthEventArgs(user));
        }
        
        protected virtual void OnUserLoggedOut(User user)
        {
            UserLoggedOut?.Invoke(this, new AuthEventArgs(user));
        }
        
        private static User _currentUser;
    }
}