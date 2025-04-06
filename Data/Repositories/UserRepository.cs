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
                
                return reader.GetString("role") switch
                {
                    "admin" => new Admin
                    {
                        Id = reader.GetInt32("id"),
                        Username = reader.GetString("email"),
                        FirstName = reader.GetString("imie"),
                        LastName = reader.GetString("nazwisko"),
                        Role = "admin"
                    },
                    _ => new Customer
                    {
                        Id = reader.GetInt32("id"),
                        Username = reader.GetString("email"),
                        FirstName = reader.GetString("imie"),
                        LastName = reader.GetString("nazwisko"),
                        Role = "customer"
                    }
                };
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