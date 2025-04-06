using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySqlConnector;

namespace Zielarnia
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var connectionString = "Server=localhost;Database=zielonazielarnia;User=root;Password=admin;";

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            var command = new MySqlCommand("SELECT * FROM klienci;", connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                Console.WriteLine(reader.GetInt32(0)); // przykład odczytu pierwszej kolumny
            }
        }
    }
}
