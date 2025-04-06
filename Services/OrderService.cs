using System;
using System.Linq;
using Zielarnia.Data.Repositories;
using Zielarnia.Models;
using Zielarnia.Utils.Exceptions;

namespace Zielarnia.Services
{
    public class OrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IProductRepository _productRepository;

        public OrderService(IOrderRepository orderRepository, IProductRepository productRepository)
        {
            _orderRepository = orderRepository;
            _productRepository = productRepository;
        }

        public void ViewCustomerOrders(int customerId)
        {
            try
            {
                var orders = _orderRepository.GetByCustomerId(customerId).ToList();
                
                Console.WriteLine("\n=== TWOJE ZAMÓWIENIA ===");
                foreach (var order in orders)
                {
                    Console.WriteLine($"{order.Id}. Data: {order.OrderDate}, Status: {order.Status}, Suma: {order.TotalPrice:C}");
                }
            }
            catch (DatabaseException ex)
            {
                Console.WriteLine($"Błąd podczas pobierania zamówień: {ex.Message}");
                throw;
            }
        }

        public void CreateOrder(int customerId, int productId, int quantity)
        {
            try
            {
                var product = _productRepository.GetById(productId);
                if (product == null)
                    throw new Exception("Produkt nie istnieje");
                
                _orderRepository.Create(customerId, productId, quantity);
                Console.WriteLine("Zamówienie zostało złożone pomyślnie");
            }
            catch (DatabaseException ex)
            {
                Console.WriteLine($"Błąd podczas składania zamówienia: {ex.Message}");
                throw;
            }
        }

        public void ManageOrders()
        {
            throw new NotImplementedException();
        }
    }
}