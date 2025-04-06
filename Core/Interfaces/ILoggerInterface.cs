namespace Zielarnia.Core.Interfaces;

/// <summary>
/// Interface for logging service. Fulfills interface requirement.
/// </summary>
public interface ILoggerService
{
    /// <summary>
    /// Logs an informational message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    Task LogInfoAsync(string message);

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    Task LogWarningAsync(string message);

    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="exception">Optional exception details.</param>
    Task LogErrorAsync(string message, Exception? exception = null);

    /// <summary>
    /// Event raised when a log entry has been successfully saved. [source: 5]
    /// </summary>
    event Func<string, Task>? LogSaved; // Example of an event using Func delegate
}