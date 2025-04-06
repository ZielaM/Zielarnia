using BCryptNet = BCrypt.Net.BCrypt;
using Microsoft.Extensions.Configuration;
using Zielarnia.Core.Models;
using Zielarnia.Core.Models.Enums;
using Zielarnia.UI;
using Zielarnia.Core.Interfaces; // Use ILoggerService
using Zielarnia.Core.Events; // Use EventArgs
using System.IO; // For File operations, IOException
using System; // For Exception, Enum, StringComparison etc.
using System.Threading.Tasks; // For Task

namespace Zielarnia.Core.Services;

/// <summary>
/// Delegate for handling user action events. [source: 4]
/// </summary>
public delegate Task UserActionEventHandler(object sender, UserActionEventArgs e);

/// <summary>
/// Handles user authentication based on a flat file.
/// </summary>
public class AuthenticationService
{
    private readonly string _usersFilePath;
    private readonly ILoggerService _logger; // Inject logger

    /// <summary>
    /// Raised when a user successfully logs in. [source: 5]
    /// </summary>
    public event UserActionEventHandler? UserLoggedIn;

    /// <summary>
    /// Raised during a login attempt (before success/failure decision). [source: 5]
    /// </summary>
    public event UserActionEventHandler? LoginAttempt;


    public AuthenticationService(IConfiguration configuration, ILoggerService logger) // Inject ILoggerService
    {
        _logger = logger;
        _usersFilePath = configuration["UserSettings:UsersFilePath"]
            ?? throw new InvalidOperationException("User settings 'UsersFilePath' not found in configuration.");
    }

    /// <summary>
    /// Attempts to log in a user based on credentials in the users file.
    /// </summary>
    /// <param name="login">Provided login.</param>
    /// <param name="password">Provided password (plain text).</param>
    /// <returns>User object if login is successful, otherwise null.</returns>
    public async Task<User?> LoginAsync(string login, string password)
    {
        string? failureReason = null; // Store reason for failure logging

        // Raise LoginAttempt event [source: 5]
        // Use Task.Run to ensure event handlers don't block the main flow unnecessarily,
        // but wait for it if logging the attempt itself is critical before proceeding.
        await OnLoginAttempt(new UserActionEventArgs(login, false)); // Initially assume failure

        if (!File.Exists(_usersFilePath))
        {
            failureReason = $"Users file not found at '{_usersFilePath}'";
            ConsoleHelper.WriteError($"Błąd: {failureReason}");
            // Log critical error - application might not be usable
            await _logger.LogErrorAsync($"Login failed for user '{login}'. Reason: {failureReason}");
            return null; // Handle file read error [source: 10]
        }

        try
        {
            string[] lines = await File.ReadAllLinesAsync(_usersFilePath);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#")) continue;

                var parts = line.Split(',');
                if (parts.Length != 3)
                {
                     // Log and warn, but continue searching in case other lines are valid
                     ConsoleHelper.WriteWarning($"Ostrzeżenie: Pomijanie nieprawidłowej linii w pliku użytkowników: {line}");
                     await _logger.LogWarningAsync($"Invalid line format in users file, skipping line: {line}");
                     continue; // Handle incorrect data format [source: 10]
                }

                var storedLogin = parts[0].Trim();
                var storedHash = parts[1].Trim();
                var roleString = parts[2].Trim();

                // Compare logins (case-insensitive)
                if (storedLogin.Equals(login, StringComparison.OrdinalIgnoreCase))
                {
                    bool passwordVerified = false;
                    try
                    {
                        // Verify the password against the stored hash
                        passwordVerified = BCryptNet.Verify(password, storedHash);
                    }
                    catch (BCrypt.Net.SaltParseException ex) // Specific BCrypt error
                    {
                         failureReason = "Invalid hash format in users file.";
                         ConsoleHelper.WriteError($"Błąd: {failureReason} dla użytkownika '{login}'");
                         // Log as error - this user cannot log in until fixed
                         await _logger.LogErrorAsync($"Login failed for user '{login}'. Reason: {failureReason}", ex);
                         return null; // Handle incorrect data format [source: 10]
                    }
                    catch (Exception ex) // Catch unexpected errors during verification
                    {
                         failureReason = "Unexpected error during password verification.";
                         ConsoleHelper.WriteError($"Błąd: {failureReason}");
                         await _logger.LogErrorAsync($"Login failed for user '{login}'. Reason: {failureReason}", ex);
                         return null; // Handle unexpected exception [source: 10]
                    }


                    if (passwordVerified)
                    {
                        // Password matches, now check role
                        if (Enum.TryParse<UserRole>(roleString, true, out var role))
                        {
                            var user = new User(storedLogin, role);
                            // Raise UserLoggedIn event [source: 5]
                            await OnUserLoggedIn(new UserActionEventArgs(login, true, $"Logged in as {role}"));
                            // Info level log is sufficient here as event handler might do more
                            await _logger.LogInfoAsync($"User '{login}' logged in successfully as {role}.");
                            return user; // Login successful
                        }
                        else
                        {
                            // Valid user, valid password, but invalid role string
                            failureReason = $"Invalid role '{roleString}' for user '{login}' in users file.";
                            ConsoleHelper.WriteWarning($"Ostrzeżenie: {failureReason}");
                            // Log as warning - user exists but cannot log in due to config error
                            await _logger.LogWarningAsync($"Login attempt failed for '{login}'. Reason: {failureReason}");
                            return null; // Handle incorrect data format [source: 10]
                        }
                    }
                    else
                    {
                        // Password mismatch
                        failureReason = "Incorrect password.";
                        // Log failure before returning null (if user exists but password wrong)
                        await _logger.LogWarningAsync($"Login attempt failed for '{login}'. Reason: {failureReason}");
                        // Don't write to console here, let the main loop handle generic "invalid login" message
                        return null;
                    }
                }
            }
        }
        catch (IOException ex) // File system errors
        {
             failureReason = "Error reading users file.";
             ConsoleHelper.WriteError($"Błąd: {failureReason}");
             await _logger.LogErrorAsync($"Login failed for user '{login}'. Reason: {failureReason}", ex);
             return null; // Handle file read error [source: 10]
        }
        catch (Exception ex) // Catch other unexpected errors during the process
        {
             failureReason = "An unexpected error occurred during login process.";
             ConsoleHelper.WriteError($"Błąd: {failureReason}");
             await _logger.LogErrorAsync($"Login failed for user '{login}'. Reason: {failureReason}", ex);
             return null; // Handle unexpected exception [source: 10]
        }

        // If loop finishes without finding the user login
        failureReason = "User not found.";
        // Log attempt for non-existent user
        await _logger.LogWarningAsync($"Login attempt failed for '{login}'. Reason: {failureReason}");
        // Handle non-existent user [source: 10] - is handled by returning null
        return null;
    }

    // Methods to raise events safely
    protected virtual async Task OnUserLoggedIn(UserActionEventArgs e)
    {
        // Use task to avoid blocking and handle potential async subscribers
        if (UserLoggedIn != null)
        {
             // Invoke each subscriber individually to prevent one failing subscriber stopping others
             var handlers = UserLoggedIn.GetInvocationList()
                                .Cast<UserActionEventHandler>()
                                .Select(handler => handler(this, e))
                                .ToArray();
             try
             {
                await Task.WhenAll(handlers); // Wait for all handlers to complete if needed
             }
             catch(Exception ex)
             {
                // Log if any handler threw an exception (Task.WhenAll aggregates exceptions)
                 await _logger.LogErrorAsync($"Error occurred in one or more UserLoggedIn event subscribers: {ex.Message}", ex);
             }
        }
    }

    protected virtual async Task OnLoginAttempt(UserActionEventArgs e)
    {
        if (LoginAttempt != null)
        {
            var handlers = LoginAttempt.GetInvocationList()
                                .Cast<UserActionEventHandler>()
                                .Select(handler => handler(this, e))
                                .ToArray();
             try
             {
                await Task.WhenAll(handlers);
             }
             catch(Exception ex)
             {
                 await _logger.LogErrorAsync($"Error occurred in one or more LoginAttempt event subscribers: {ex.Message}", ex);
             }
        }
    }
}