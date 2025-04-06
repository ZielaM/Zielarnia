using System;
using MySqlConnector;
using Zielarnia.Models.User;
using Zielarnia.Utils.Exceptions;

namespace Zielarnia.Data.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly DatabaseContext _dbContext;

        public UserRepository(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        public User Authenticate(string username, string password)
        {
            try
            {
                using var cmd = _dbContext.CreateCommand(
                    "SELECT id, imie, nazwisko, email, 'admin' as role FROM Klienci WHERE email = @username AND telefon = @password");

                cmd.Parameters.AddWithValue("@username", username);
                cmd.Parameters.AddWithValue("@password", password);

                using var reader = cmd.ExecuteReader();

                if (!reader.Read())
                    throw new AuthException("Nieprawidłowy login lub hasło");

                User user = reader.GetString("role") switch
                {
                    "admin" => new Admin(),
                    _ => new Customer()
                };


                user.Id = reader.GetInt32("id");
                user.Username = reader.GetString("email");
                user.FirstName = reader.GetString("imie");
                user.LastName = reader.GetString("nazwisko");
                // Role jest już ustawiona w konstruktorze Admin/Customer

                return user;
            }
            catch (MySqlException ex)
            {
                throw new DatabaseException("Błąd podczas autentykacji użytkownika", ex);
            }
        }

        public User GetById(int id)
        {
            throw new NotImplementedException();
        }
    }
}