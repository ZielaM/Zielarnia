namespace Zielarnia.Models.User
{
    public class Customer : User
    {
        public Customer()
        {
            Role = "customer";
        }

        public override string GetRole() => Role;
    }
}