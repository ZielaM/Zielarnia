namespace Zielarnia.Models.User
{
    public class Customer : User
    {
        public Customer()
        {
            Role = "customer";  // Teraz mo¿na ustawiæ Role, poniewa¿ setter jest publiczny
        }

        public override string GetRole() => Role;
    }
}