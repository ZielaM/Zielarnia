using Zielarnia.Models.User;

namespace Zielarnia.Data.Repositories
{
    public interface IUserRepository
    {
        User Authenticate(string username, string password);
        User GetById(int id);
    }
}