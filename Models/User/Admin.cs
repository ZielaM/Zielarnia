namespace Zielarnia.Models.User
{
    public class Admin : User
    {
        public Admin()
        {
            Role = "admin";
        }

        public override string GetRole() => Role;
    }
}