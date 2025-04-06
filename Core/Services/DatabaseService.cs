using MySqlConnector;
using Microsoft.Extensions.Configuration;
using Zielarnia.Data.Models; // Import your models
using Zielarnia.Core.Models.Enums; // For OrderStatus
using Zielarnia.UI; // For potential error logging to console
using System.Data.Common; // For DbException
using System; // For Func, List, Exception etc.
using System.Collections.Generic; // For List<T>
using System.Threading.Tasks; // For Task
using System.Linq; // For FirstOrDefault

namespace Zielarnia.Data.Services;

/// <summary>
/// Provides methods for interacting with the MariaDB database.
/// Encapsulates connection management and SQL execution.
/// Propagates exceptions upwards for handling by services/UI layer.
/// </summary>
public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found in configuration.");

        // Optional: Test connection on startup
        // TestConnectionAsync().GetAwaiter().GetResult(); // Synchronous wait - use with caution
    }

    /// <summary>
    /// Gets a new database connection instance.
    /// </summary>
    private MySqlConnection GetConnection() => new MySqlConnection(_connectionString);

    /// <summary>
    /// Tests the database connection. Throws exception on failure.
    /// </summary>
    public async Task TestConnectionAsync()
    {
        using var connection = GetConnection();
        try
        {
            await connection.OpenAsync();
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"Database connection failed: {ex.Message}");
            throw new InvalidOperationException("Failed to connect to the database.", ex);
        }
    }


    // --- Generic Helper Methods with Error Handling ---

    /// <summary>
    /// Executes a command that does not return any data (INSERT, UPDATE, DELETE).
    /// Throws DbException or MySqlException on database errors.
    /// </summary>
    private async Task ExecuteNonQueryAsync(string sql, params MySqlParameter[] parameters)
    {
        using var connection = GetConnection();
        try
        {
            await connection.OpenAsync();
            using var command = new MySqlCommand(sql, connection);
            if (parameters != null && parameters.Length > 0) command.Parameters.AddRange(parameters);
            await command.ExecuteNonQueryAsync();
        }
        catch (MySqlException ex) { throw; }
        catch (DbException ex) { throw; }
    }

    /// <summary>
    /// Executes a query and returns the first column of the first row.
    /// Returns null if no rows are returned or if the value is DBNull.
    /// Throws DbException or MySqlException on database errors.
    /// </summary>
    private async Task<object?> ExecuteScalarAsync(string sql, params MySqlParameter[] parameters)
    {
        using var connection = GetConnection();
        try
        {
            await connection.OpenAsync();
            using var command = new MySqlCommand(sql, connection);
            if (parameters != null && parameters.Length > 0) command.Parameters.AddRange(parameters);
            var result = await command.ExecuteScalarAsync();
            return (result == DBNull.Value) ? null : result;
        }
        catch (MySqlException ex) { throw; }
        catch (DbException ex) { throw; }
    }


    /// <summary>
    /// Executes a query and maps the results to a list of objects using a provided mapping function.
    /// Handles database errors and potential mapping errors.
    /// </summary>
    private async Task<List<T>> ReadDataAsync<T>(string sql, Func<MySqlDataReader, T> mapFunction, params MySqlParameter[] parameters)
    {
        var results = new List<T>();
        using var connection = GetConnection();
        MySqlDataReader? reader = null; // Declare reader outside try block for finally
        try
        {
            await connection.OpenAsync();
            using var command = new MySqlCommand(sql, connection);
            if (parameters != null && parameters.Length > 0) command.Parameters.AddRange(parameters);

            reader = await command.ExecuteReaderAsync(); // Assign reader here
            while (await reader.ReadAsync())
            {
                try
                {
                    results.Add(mapFunction(reader));
                }
                catch (Exception ex) when (ex is InvalidCastException || ex is IndexOutOfRangeException || ex is ArgumentException) // Catch common mapping errors
                {
                    // Ensure reader is closed before throwing to avoid resource leaks if caller doesn't handle well
                    if (reader != null && !reader.IsClosed) await reader.CloseAsync();
                    throw new ApplicationException($"Błąd podczas mapowania danych z bazy. SQL: {sql}", ex);
                }
            }
            // Close reader explicitly after loop finishes successfully
            if (reader != null && !reader.IsClosed) await reader.CloseAsync();
            return results;
        }
        catch (MySqlException ex)
        {
            // Ensure reader is closed on DB error
            if (reader != null && !reader.IsClosed) await reader.CloseAsync();
            throw; // Propagate DB errors
        }
        catch (DbException ex)
        {
            // Ensure reader is closed on DB error
            if (reader != null && !reader.IsClosed) await reader.CloseAsync();
            throw;   // Propagate DB errors
        }
        // No need for finally block if reader is closed in catch blocks and after successful loop
    }


    // --- Specific Data Access Methods (IMPLEMENT AS NEEDED) ---

    // -- Products --
    public async Task<List<Product>> GetAllProductsAsync()
    {
        string sql = "SELECT id, nazwa, opis, cena, kategoria_id FROM Produkty;";
        return await ReadDataAsync(sql, reader =>
        {
            // Get ordinals (indexes) for nullable columns first
            int opisOrdinal = reader.GetOrdinal("opis");
            int kategoriaIdOrdinal = reader.GetOrdinal("kategoria_id");

            return new Product
            {
                Id = reader.GetInt32("id"),
                Name = reader.GetString("nazwa"),
                // Use ordinal index with IsDBNull
                Description = reader.IsDBNull(opisOrdinal) ? null : reader.GetString(opisOrdinal),
                Price = reader.GetDecimal("cena"),
                // Use ordinal index with IsDBNull
                CategoryId = reader.IsDBNull(kategoriaIdOrdinal) ? (int?)null : reader.GetInt32(kategoriaIdOrdinal)
            };
        });
    }

    public async Task<Product?> GetProductByIdAsync(int productId)
    {
        string sql = "SELECT id, nazwa, opis, cena, kategoria_id FROM Produkty WHERE id = @Id;";
        var products = await ReadDataAsync(sql, reader =>
        {
            int opisOrdinal = reader.GetOrdinal("opis");
            int kategoriaIdOrdinal = reader.GetOrdinal("kategoria_id");
            return new Product
            {
                Id = reader.GetInt32("id"),
                Name = reader.GetString("nazwa"),
                Description = reader.IsDBNull(opisOrdinal) ? null : reader.GetString(opisOrdinal),
                Price = reader.GetDecimal("cena"),
                CategoryId = reader.IsDBNull(kategoriaIdOrdinal) ? (int?)null : reader.GetInt32(kategoriaIdOrdinal)
            };
        }, new MySqlParameter("@Id", productId));
        return products.FirstOrDefault(); // Returns null if not found
    }


    public async Task AddProductAsync(Product product)
    {
        string sql = "INSERT INTO Produkty (nazwa, opis, cena, kategoria_id) VALUES (@Name, @Desc, @Price, @CatId);";
        var parameters = new MySqlParameter[] {
            new MySqlParameter("@Name", product.Name),
            new MySqlParameter("@Desc", product.Description ?? (object)DBNull.Value),
            new MySqlParameter("@Price", product.Price),
            new MySqlParameter("@CatId", product.CategoryId ?? (object)DBNull.Value)
         };
        await ExecuteNonQueryAsync(sql, parameters);
    }

    public async Task UpdateProductAsync(Product product)
    {
        string sql = "UPDATE Produkty SET nazwa = @Name, opis = @Desc, cena = @Price, kategoria_id = @CatId WHERE id = @Id;";
        var parameters = new MySqlParameter[] {
            new MySqlParameter("@Name", product.Name),
            new MySqlParameter("@Desc", product.Description ?? (object)DBNull.Value),
            new MySqlParameter("@Price", product.Price),
            new MySqlParameter("@CatId", product.CategoryId ?? (object)DBNull.Value),
            new MySqlParameter("@Id", product.Id)
         };
        await ExecuteNonQueryAsync(sql, parameters);
    }

    public async Task DeleteProductAsync(int productId)
    {
        string sql = "DELETE FROM Produkty WHERE id = @Id;";
        await ExecuteNonQueryAsync(sql, new MySqlParameter("@Id", productId));
    }


    // -- Clients --
    public async Task<List<Client>> GetAllClientsAsync()
    {
        string sql = "SELECT id, imie, nazwisko, email, telefon, adres_id, id_paczkomatu FROM Klienci;";
        return await ReadDataAsync(sql, reader =>
        {
            // Get ordinal for nullable column
            int paczkomatOrdinal = reader.GetOrdinal("id_paczkomatu");
            return new Client
            {
                Id = reader.GetInt32("id"),
                FirstName = reader.GetString("imie"),
                LastName = reader.GetString("nazwisko"),
                Email = reader.GetString("email"),
                PhoneNumber = reader.GetString("telefon"),
                AddressId = reader.GetInt32("adres_id"),
                // Use ordinal index with IsDBNull
                ParcelLockerId = reader.IsDBNull(paczkomatOrdinal) ? null : reader.GetString(paczkomatOrdinal)
            };
        });
    }

    public async Task<Client?> GetClientByIdAsync(int clientId)
    {
        string sql = "SELECT id, imie, nazwisko, email, telefon, adres_id, id_paczkomatu FROM Klienci WHERE id = @Id;";
        var results = await ReadDataAsync(sql, reader =>
        {
            int paczkomatOrdinal = reader.GetOrdinal("id_paczkomatu");
            return new Client
            {
                Id = reader.GetInt32("id"),
                FirstName = reader.GetString("imie"),
                LastName = reader.GetString("nazwisko"),
                Email = reader.GetString("email"),
                PhoneNumber = reader.GetString("telefon"),
                AddressId = reader.GetInt32("adres_id"),
                ParcelLockerId = reader.IsDBNull(paczkomatOrdinal) ? null : reader.GetString(paczkomatOrdinal)
            };
        }, new MySqlParameter("@Id", clientId));
        return results.FirstOrDefault();
    }

    public async Task<Client?> GetClientByEmailAsync(string email)
    {
        string sql = "SELECT id, imie, nazwisko, email, telefon, adres_id, id_paczkomatu FROM Klienci WHERE email = @Email;";
        var results = await ReadDataAsync(sql, reader =>
        {
            int paczkomatOrdinal = reader.GetOrdinal("id_paczkomatu");
            return new Client
            {
                Id = reader.GetInt32("id"),
                FirstName = reader.GetString("imie"),
                LastName = reader.GetString("nazwisko"),
                Email = reader.GetString("email"),
                PhoneNumber = reader.GetString("telefon"),
                AddressId = reader.GetInt32("adres_id"),
                ParcelLockerId = reader.IsDBNull(paczkomatOrdinal) ? null : reader.GetString(paczkomatOrdinal)
            };
        }, new MySqlParameter("@Email", email));
        return results.FirstOrDefault();
    }

    // TODO: AddClientAsync, UpdateClientAsync, DeleteClientAsync...


    // -- Orders --
    public async Task<List<Order>> GetOrdersByClientIdAsync(int clientId)
    {
        string sql = "SELECT id, klient_id, data_zamowienia, status FROM Zamowienia WHERE klient_id = @ClientId ORDER BY data_zamowienia DESC;";
        return await ReadDataAsync(sql, reader =>
        {
            // Get ordinal for nullable column
            int klientIdOrdinal = reader.GetOrdinal("klient_id");
            return new Order
            {
                Id = reader.GetInt32("id"),
                // Use ordinal index with IsDBNull
                ClientId = reader.IsDBNull(klientIdOrdinal) ? null : reader.GetInt32(klientIdOrdinal),
                OrderDate = reader.GetDateTime("data_zamowienia"),
                Status = OrderStatusConverter.FromDbString(reader.GetString("status"))
            };
        }, new MySqlParameter("@ClientId", clientId));
    }

    public async Task<List<OrderDetail>> GetOrderDetailsAsync(int orderId)
    {
        string sql = "SELECT zamowienie_id, produkt_id, ilosc FROM SzczegolyZamowienia WHERE zamowienie_id = @OrderId;";
        return await ReadDataAsync(sql, reader => new OrderDetail
        {
            OrderId = reader.GetInt32("zamowienie_id"),
            ProductId = reader.GetInt32("produkt_id"),
            Quantity = reader.GetInt32("ilosc")
        }, new MySqlParameter("@OrderId", orderId));
    }


    public async Task<int> CreateOrderAsync(Order order)
    {
        string sql = "INSERT INTO Zamowienia (klient_id, data_zamowienia, status) VALUES (@ClientId, @OrderDate, @Status); SELECT LAST_INSERT_ID();";
        var parameters = new MySqlParameter[] {
             new MySqlParameter("@ClientId", order.ClientId ?? (object)DBNull.Value),
             new MySqlParameter("@OrderDate", order.OrderDate),
             new MySqlParameter("@Status", OrderStatusConverter.ToDbString(order.Status))
         };
        var result = await ExecuteScalarAsync(sql, parameters);
        if (result == null) throw new ApplicationException("Failed to create order or retrieve last insert ID.");
        return Convert.ToInt32(result);
    }

    public async Task AddOrderDetailAsync(OrderDetail detail)
    {
        string sql = "INSERT INTO SzczegolyZamowienia (zamowienie_id, produkt_id, ilosc) VALUES (@OrderId, @ProductId, @Quantity);";
        var parameters = new MySqlParameter[] {
             new MySqlParameter("@OrderId", detail.OrderId),
             new MySqlParameter("@ProductId", detail.ProductId),
             new MySqlParameter("@Quantity", detail.Quantity)
         };
        await ExecuteNonQueryAsync(sql, parameters);
    }


    public async Task UpdateOrderStatusAsync(int orderId, OrderStatus newStatus)
    {
        string sql = "UPDATE Zamowienia SET status = @Status WHERE id = @Id;";
        var parameters = new MySqlParameter[] {
             new MySqlParameter("@Status", OrderStatusConverter.ToDbString(newStatus)),
             new MySqlParameter("@Id", orderId)
         };
        await ExecuteNonQueryAsync(sql, parameters);
    }

    // --- Transaction Example ---
    public async Task<int> CreateOrderWithDetailsAsync(Order order, List<OrderDetail> details)
    {
        using var connection = GetConnection();
        await connection.OpenAsync();
        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // 1. Insert Order header
            string orderSql = "INSERT INTO Zamowienia (klient_id, data_zamowienia, status) VALUES (@ClientId, @OrderDate, @Status); SELECT LAST_INSERT_ID();";
            var orderParams = new MySqlParameter[] {
                 new MySqlParameter("@ClientId", order.ClientId ?? (object)DBNull.Value),
                 new MySqlParameter("@OrderDate", order.OrderDate),
                 new MySqlParameter("@Status", OrderStatusConverter.ToDbString(order.Status))
             };
            int orderId;
            using (var orderCommand = new MySqlCommand(orderSql, connection, transaction))
            {
                orderCommand.Parameters.AddRange(orderParams);
                var result = await orderCommand.ExecuteScalarAsync();
                if (result == null || result == DBNull.Value) throw new ApplicationException("Failed to create order header or retrieve ID.");
                orderId = Convert.ToInt32(result);
                order.Id = orderId;
            }


            // 2. Insert OrderDetails
            string detailSql = "INSERT INTO SzczegolyZamowienia (zamowienie_id, produkt_id, ilosc) VALUES (@OrderId, @ProductId, @Quantity);";
            foreach (var detail in details)
            {
                if (detail.Quantity <= 0) continue;

                detail.OrderId = orderId;
                var detailParams = new MySqlParameter[] {
                     new MySqlParameter("@OrderId", detail.OrderId),
                     new MySqlParameter("@ProductId", detail.ProductId),
                     new MySqlParameter("@Quantity", detail.Quantity)
                 };
                using (var detailCommand = new MySqlCommand(detailSql, connection, transaction))
                {
                    detailCommand.Parameters.AddRange(detailParams);
                    await detailCommand.ExecuteNonQueryAsync();
                }
            }

            await transaction.CommitAsync();
            return orderId;
        }
        catch (Exception)
        {
            try { await transaction.RollbackAsync(); }
            catch (Exception rollbackEx)
            {
                ConsoleHelper.WriteError($"KRYTYCZNY BŁĄD: Nie można wycofać transakcji! {rollbackEx.Message}");
            }
            throw;
        }
    }

    // --- TODO: Add methods for other entities ---

    // -- Addresses --
    public async Task<int> AddAddressAsync(Address address)
    {
        string sql = "INSERT INTO adresy (Kraj, miasto, ulica, numer_budynku, numer_mieszkania) VALUES (@Country, @City, @Street, @Building, @Apt); SELECT LAST_INSERT_ID();";
        var parameters = new MySqlParameter[] {
             new MySqlParameter("@Country", address.Country),
             new MySqlParameter("@City", address.City),
             new MySqlParameter("@Street", address.Street),
             new MySqlParameter("@Building", address.BuildingNumber),
             // Use DBNull.Value for nullable string
             new MySqlParameter("@Apt", (object?)address.ApartmentNumber ?? DBNull.Value)
         };
        var result = await ExecuteScalarAsync(sql, parameters);
        if (result == null) throw new ApplicationException("Failed to create address or retrieve last insert ID.");
        return Convert.ToInt32(result);
    }

    // Add methods for Category, Supplier, Delivery, Bill, BillType...
    // Remember to handle nullable columns correctly using reader.GetOrdinal and reader.IsDBNull
    // Example for Supplier:
    public async Task<Supplier?> GetSupplierByIdAsync(int supplierId)
    {
        string sql = "SELECT id, nazwa, kontakt, adres_id FROM Dostawcy WHERE id = @Id;";
        var results = await ReadDataAsync(sql, reader => {
            int kontaktOrdinal = reader.GetOrdinal("kontakt");
            int adresIdOrdinal = reader.GetOrdinal("adres_id");
            return new Supplier
            {
                Id = reader.GetInt32("id"),
                Name = reader.GetString("nazwa"),
                ContactInfo = reader.IsDBNull(kontaktOrdinal) ? null : reader.GetString(kontaktOrdinal),
                AddressId = reader.IsDBNull(adresIdOrdinal) ? null : reader.GetInt32(adresIdOrdinal)
            };
        }, new MySqlParameter("@Id", supplierId));
        return results.FirstOrDefault();
    }
}