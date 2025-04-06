using System;
using System.Collections.Generic;
using MySqlConnector;
using Zielarnia.Models;
using Zielarnia.Utils.Exceptions;

namespace Zielarnia.Data.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly DatabaseContext _dbContext;

        public OrderRepository(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IEnumerable<Order> GetByCustomerId(int customerId)
        {
            var orders = new List<Order>();
            
            try
            {
                using var cmd = _dbContext.CreateCommand(
                    "SELECT z.id, z.data_zamowienia, z.status, " +
                    "SUM(sz.ilosc * p.cena) as total " +
                    "FROM Zamowienia z " +
                    "JOIN SzczegolyZamowienia sz ON z.id = sz.zamowienie_id " +
                    "JOIN Produkty p ON sz.produkt_id = p.id " +
                    "WHERE z.klient_id = @customerId " +
                    "GROUP BY z.id");
                
                cmd.Parameters.AddWithValue("@customerId", customerId);
                
                using var reader = cmd.ExecuteReader();
                
                while (reader.Read())
                {
                    orders.Add(new Order
                    {
                        Id = reader.GetInt32("id"),
                        OrderDate = reader.GetDateTime("data_zamowienia"),
                        Status = reader.GetString("status"),
                        TotalPrice = reader.GetDecimal("total")
                    });
                }
            }
            catch (MySqlException ex)
            {
                throw new DatabaseException("Błąd podczas pobierania zamówień", ex);
            }
            
            return orders;
        }

        public void Create(int customerId, int productId, int quantity)
        {
            try
            {
                using var transaction = _dbContext.BeginTransaction();

                try
                {
                    var cmdOrder = _dbContext.CreateCommand(
                        "INSERT INTO Zamowienia (klient_id, status) VALUES (@customerId, 'Nowe')");
                    cmdOrder.Parameters.AddWithValue("@customerId", customerId);
                    cmdOrder.ExecuteNonQuery();

                    var cmdLastId = _dbContext.CreateCommand("SELECT LAST_INSERT_ID()");
                    var orderId = Convert.ToInt32(cmdLastId.ExecuteScalar());

                    var cmdDetails = _dbContext.CreateCommand(
                        "INSERT INTO SzczegolyZamowienia (zamowienie_id, produkt_id, ilosc) " +
                        "VALUES (@orderId, @productId, @quantity)");
                    cmdDetails.Parameters.AddWithValue("@orderId", orderId);
                    cmdDetails.Parameters.AddWithValue("@productId", productId);
                    cmdDetails.Parameters.AddWithValue("@quantity", quantity);
                    cmdDetails.ExecuteNonQuery();

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (MySqlException ex)
            {
                throw new DatabaseException("Błąd podczas tworzenia zamówienia", ex);
            }
        }

        public void UpdateStatus(int orderId, string status)
        {
            try
            {
                using var cmd = _dbContext.CreateCommand(
                    "UPDATE Zamowienia SET status = @status WHERE id = @orderId");
                
                cmd.Parameters.AddWithValue("@status", status);
                cmd.Parameters.AddWithValue("@orderId", orderId);
                
                cmd.ExecuteNonQuery();
            }
            catch (MySqlException ex)
            {
                throw new DatabaseException("Błąd podczas aktualizacji zamówienia", ex);
            }
        }
    }
}