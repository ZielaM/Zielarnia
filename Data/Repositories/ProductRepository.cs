using System;
using System.Collections.Generic;
using MySqlConnector;
using Zielarnia.Models;
using Zielarnia.Utils.Exceptions;

namespace Zielarnia.Data.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly DatabaseContext _dbContext;

        public ProductRepository(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IEnumerable<Product> GetAll()
        {
            var products = new List<Product>();
            
            try
            {
                using var cmd = _dbContext.CreateCommand(
                    "SELECT p.id, p.nazwa, p.opis, p.cena, k.nazwa as kategoria " +
                    "FROM Produkty p LEFT JOIN Kategorie k ON p.kategoria_id = k.id");
                
                using var reader = cmd.ExecuteReader();
                
                while (reader.Read())
                {
                    products.Add(new Product
                    {
                        Id = reader.GetInt32("id"),
                        Name = reader.GetString("nazwa"),
                        Description = reader.IsDBNull(reader.GetOrdinal("opis")) ? null : reader.GetString("opis"),
                        Price = reader.GetDecimal("cena"),
                        Category = reader.IsDBNull(reader.GetOrdinal("kategoria")) ? null : reader.GetString("kategoria")
                    });
                }
            }
            catch (MySqlException ex)
            {
                throw new DatabaseException("Błąd podczas pobierania produktów", ex);
            }
            
            return products;
        }

        public Product GetById(int id)
        {
            try
            {
                using var cmd = _dbContext.CreateCommand(
                    "SELECT p.id, p.nazwa, p.opis, p.cena, k.nazwa as kategoria " +
                    "FROM Produkty p LEFT JOIN Kategorie k ON p.kategoria_id = k.id " +
                    "WHERE p.id = @id");
                
                cmd.Parameters.AddWithValue("@id", id);
                
                using var reader = cmd.ExecuteReader();
                
                if (!reader.Read())
                    return null;
                
                return new Product
                {
                    Id = reader.GetInt32("id"),
                    Name = reader.GetString("nazwa"),
                    Description = reader.IsDBNull(reader.GetOrdinal("opis")) ? null : reader.GetString("opis"),
                    Price = reader.GetDecimal("cena"),
                    Category = reader.IsDBNull(reader.GetOrdinal("kategoria")) ? null : reader.GetString("kategoria")
                };
            }
            catch (MySqlException ex)
            {
                throw new DatabaseException("Błąd podczas pobierania produktu", ex);
            }
        }

        public void Add(Product product)
        {
            try
            {
                using var cmd = _dbContext.CreateCommand(
                    "INSERT INTO Produkty (nazwa, opis, cena, kategoria_id) " +
                    "VALUES (@name, @desc, @price, @categoryId)");
                
                cmd.Parameters.AddWithValue("@name", product.Name);
                cmd.Parameters.AddWithValue("@desc", product.Description ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@price", product.Price);
                cmd.Parameters.AddWithValue("@categoryId", product.CategoryId ?? (object)DBNull.Value);
                
                cmd.ExecuteNonQuery();
            }
            catch (MySqlException ex)
            {
                throw new DatabaseException("Błąd podczas dodawania produktu", ex);
            }
        }

        public void Update(Product product)
        {
            throw new NotImplementedException();
        }

        public void Delete(int id)
        {
            throw new NotImplementedException();
        }
    }
}