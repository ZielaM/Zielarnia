using System;
using MySqlConnector;
using Zielarnia.Data;
using Zielarnia.Models;
using Zielarnia.Models.User;
using Zielarnia.Utils.Exceptions;

namespace Zielarnia.Services
{
    public class LoggingService
    {
        private readonly DatabaseContext _dbContext;

        public LoggingService(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        public void LogAction(User user, string action)
        {
            try
            {
                using var cmd = _dbContext.CreateCommand(
                    "INSERT INTO logs (user_id, action, timestamp) VALUES (@userId, @action, @timestamp)");
                
                cmd.Parameters.AddWithValue("@userId", user.Id);
                cmd.Parameters.AddWithValue("@action", $"{user.Role}: {action}");
                cmd.Parameters.AddWithValue("@timestamp", DateTime.Now);
                
                cmd.ExecuteNonQuery();
            }
            catch (MySqlException ex)
            {
                throw new DatabaseException("Błąd podczas zapisywania logu", ex);
            }
        }

        public void LogError(Exception ex)
        {
            try
            {
                using var cmd = _dbContext.CreateCommand(
                    "INSERT INTO error_logs (error_message, stack_trace, timestamp) VALUES (@msg, @trace, @timestamp)");
                
                cmd.Parameters.AddWithValue("@msg", ex.Message);
                cmd.Parameters.AddWithValue("@trace", ex.StackTrace);
                cmd.Parameters.AddWithValue("@timestamp", DateTime.Now);
                
                cmd.ExecuteNonQuery();
            }
            catch (MySqlException dbEx)
            {
                Console.WriteLine($"KRYTYCZNY BŁĄD: Nie udało się zapisać błędu do logu: {dbEx.Message}");
            }
        }

        public void ShowSystemLogs()
        {
            try
            {
                Console.WriteLine("\n=== LOGI SYSTEMOWE ===");
                
                using var cmd = _dbContext.CreateCommand(
                    "SELECT l.timestamp, k.imie, k.nazwisko, l.action " +
                    "FROM logs l JOIN Klienci k ON l.user_id = k.id " +
                    "ORDER BY l.timestamp DESC LIMIT 50");
                
                using var reader = cmd.ExecuteReader();
                
                while (reader.Read())
                {
                    Console.WriteLine($"[{reader.GetDateTime("timestamp")}] {reader.GetString("imie")} {reader.GetString("nazwisko")}: {reader.GetString("action")}");
                }
            }
            catch (MySqlException ex)
            {
                throw new DatabaseException("Błąd podczas odczytu logów", ex);
            }
        }
    }
}