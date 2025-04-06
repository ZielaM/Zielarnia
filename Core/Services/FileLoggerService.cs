using Microsoft.Extensions.Configuration;
using Zielarnia.Core.Interfaces;
using System.Text; // For StringBuilder
using System.Threading; // For Monitor
using System.IO; // For Path, File, Directory etc.

namespace Zielarnia.Core.Services;

/// <summary>
/// Implements ILoggerService to log messages to a text file. [source: 13]
/// </summary>
public class FileLoggerService : ILoggerService
{
    private readonly string _logFilePath;
    private static readonly object _lock = new object(); // Simple lock for file access

    public event Func<string, Task>? LogSaved; // Implement event from interface [source: 5]

    public FileLoggerService(IConfiguration configuration)
    {
        _logFilePath = configuration["UserSettings:LogsFilePath"]
            ?? Path.Combine(Directory.GetCurrentDirectory(), "logs.txt"); // Default path

        // Ensure directory exists
        try
        {
            var logDirectory = Path.GetDirectoryName(_logFilePath);
            if (!string.IsNullOrEmpty(logDirectory) && !Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine($"FATAL: Could not create log directory '{Path.GetDirectoryName(_logFilePath)}': {ex.Message}");
            Console.ResetColor();
            // Application might need to terminate if logging directory cannot be created.
            // Consider throwing an exception here or handling it more gracefully.
        }
    }

    public async Task LogInfoAsync(string message)
    {
        await WriteLogAsync("INFO", message);
    }

    public async Task LogWarningAsync(string message)
    {
        await WriteLogAsync("WARN", message);
    }

    public async Task LogErrorAsync(string message, Exception? exception = null)
    {
        var fullMessage = new StringBuilder();
        fullMessage.Append(message);
        if (exception != null)
        {
            // Include inner exceptions for more details
            var currentEx = exception;
            int level = 0;
            while (currentEx != null && level < 5) // Limit depth
            {
                 fullMessage.AppendLine($" | L{level} Exception: {currentEx.GetType().Name}: {currentEx.Message}");
                 fullMessage.Append($" | StackTrace: {currentEx.StackTrace}");
                 currentEx = currentEx.InnerException;
                 level++;
                 if (currentEx != null) fullMessage.AppendLine(" ---> Inner Exception:");
            }
        }
        await WriteLogAsync("ERROR", fullMessage.ToString());
    }

    private async Task WriteLogAsync(string level, string message)
    {
        string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}{Environment.NewLine}";

        // Using lock for basic thread safety on file access
        bool lockTaken = false;
        try
        {
            Monitor.Enter(_lock, ref lockTaken); // Enter the lock
            // Use using statement for FileStream to ensure disposal
            // Specify FileMode.Append and FileAccess.Write
            using (var stream = new FileStream(_logFilePath, FileMode.Append, FileAccess.Write, FileShare.Read)) // Allow reading while appending
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
            {
                await writer.WriteAsync(logEntry);
            }

            // Raise the event after successfully writing [source: 5]
            if (LogSaved != null)
            {
                // We don't wait for subscribers here to avoid blocking the logging call
                _ = Task.Run(() => LogSaved.Invoke(logEntry));
            }
        }
        catch (Exception ex)
        {
            // Log writing error to console (can't log to file if file writing failed)
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine($"FATAL: Could not write to log file '{_logFilePath}': {ex.Message}");
            Console.ResetColor();
        }
        finally
        {
            if (lockTaken)
            {
                Monitor.Exit(_lock); // Ensure the lock is released
            }
        }
    }
}