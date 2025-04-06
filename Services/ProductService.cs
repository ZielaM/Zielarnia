using System;
using System.Collections.Generic;
using System.Linq;
using Zielarnia.Data.Repositories;
using Zielarnia.Models;
using Zielarnia.Utils.Exceptions;

namespace Zielarnia.Services
{
    public class ProductService
    {
        private readonly IProductRepository _productRepository;

        public ProductService(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public void BrowseProducts()
        {
            try
            {
                var products = _productRepository.GetAll().ToList();
                
                Console.WriteLine("\n=== LISTA PRODUKTÓW ===");
                foreach (var product in products)
                {
                    Console.WriteLine($"{product.Id}. {product.Name} - {product.Price:C}");
                    if (!string.IsNullOrEmpty(product.Category))
                        Console.WriteLine($"   Kategoria: {product.Category}");
                    if (!string.IsNullOrEmpty(product.Description))
                        Console.WriteLine($"   Opis: {product.Description}");
                    Console.WriteLine();
                }
            }
            catch (DatabaseException ex)
            {
                Console.WriteLine($"Błąd podczas pobierania produktów: {ex.Message}");
                throw;
            }
        }

        public void AddProduct(string name, string description, decimal price, int? categoryId)
        {
            try
            {
                var product = new Product
                {
                    Name = name,
                    Description = description,
                    Price = price,
                    CategoryId = categoryId
                };
                
                _productRepository.Add(product);
                Console.WriteLine($"Dodano nowy produkt: {name}");
            }
            catch (DatabaseException ex)
            {
                Console.WriteLine($"Błąd podczas dodawania produktu: {ex.Message}");
                throw;
            }
        }

        public Product GetProduct(int id)
        {
            try
            {
                return _productRepository.GetById(id);
            }
            catch (DatabaseException ex)
            {
                Console.WriteLine($"Błąd podczas pobierania produktu: {ex.Message}");
                throw;
            }
        }
    }
}