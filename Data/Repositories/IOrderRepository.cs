using System.Collections.Generic;
using Zielarnia.Models;

namespace Zielarnia.Data.Repositories
{
    public interface IOrderRepository
    {
        IEnumerable<Order> GetByCustomerId(int customerId);
        void Create(int customerId, int productId, int quantity);
        void UpdateStatus(int orderId, string status);
    }
}