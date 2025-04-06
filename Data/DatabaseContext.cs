using MySqlConnector;

namespace Zielarnia.Data
{
    public class DatabaseContext : IDisposable
    {
        private readonly MySqlConnection _connection;
        
        public DatabaseContext(string connectionString)
        {
            _connection = new MySqlConnection(connectionString);
            _connection.Open();
        }
        
        public MySqlCommand CreateCommand(string query)
        {
            return new MySqlCommand(query, _connection);
        }
        
        public void Dispose()
        {
            _connection?.Close();
            _connection?.Dispose();
        }
    }
}