using Microsoft.Extensions.Configuration;
using Zielarnia.Core.Models;
using Zielarnia.Core.Services;
using Zielarnia.Data.Services;
using Zielarnia.UI;
using Zielarnia.Core.Interfaces; // Logger interface
using Zielarnia.Core.Events; // EventArgs
using System; // For Exception, Directory, Path
using System.IO; // For IOException etc.
using System.Threading.Tasks; // For Task

namespace Zielarnia;

internal class Program
{
    // Logger instance available to static event handlers
    private static ILoggerService? _logger;

    static async Task Main(string[] args)
    {
        // --- Konfiguracja ---
        IConfiguration configuration;
        try
        {
             configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                // Make appsettings optional initially to provide better error message if missing
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

             // Check if critical configuration exists
             if (string.IsNullOrEmpty(configuration.GetConnectionString("DefaultConnection")))
             {
                 throw new InvalidOperationException("Connection string 'DefaultConnection' not found in appsettings.json.");
             }
              if (string.IsNullOrEmpty(configuration["UserSettings:UsersFilePath"]))
             {
                 throw new InvalidOperationException("User settings 'UsersFilePath' not found in appsettings.json.");
             }

        }
        catch (Exception ex) // Catches issues loading config file or missing critical keys
        {
             ConsoleHelper.WriteError($"Krytyczny błąd konfiguracji: {ex.Message}");
             ConsoleHelper.WriteError("Sprawdź, czy plik 'appsettings.json' istnieje i zawiera wymagane klucze ('ConnectionStrings:DefaultConnection', 'UserSettings:UsersFilePath').");
             Console.WriteLine("\nNaciśnij dowolny klawisz aby zakończyć...");
             Console.ReadKey();
             return; // Exit application if configuration is broken
        }


        // --- Tworzenie serwisów ---
        try
        {
            // Logger first, as other services might need it
            _logger = new FileLoggerService(configuration); // Create logger instance

            // Subscribe logger to its own event (example)
            _logger.LogSaved += Logger_LogSaved; // Use += to subscribe

            // Other services
            var dbService = new DatabaseService(configuration);
            // Test DB connection early
            await dbService.TestConnectionAsync();

            var authService = new AuthenticationService(configuration, _logger); // Inject logger
            var menuService = new MenuService(dbService, _logger); // Inject logger


            // --- Subskrypcja zdarzeń logowania --- [source: 5]
            authService.LoginAttempt += AuthService_LoginAttempt; // Use += to subscribe
            authService.UserLoggedIn += AuthService_UserLoggedIn; // Use += to subscribe


            // --- Główna logika aplikacji ---
            await _logger.LogInfoAsync("Application started successfully.");
            Console.WriteLine("Witaj w aplikacji Zielarnia!");

            User? currentUser = null;
            while (currentUser == null) // Login loop
            {
                Console.WriteLine("\n--- Logowanie ---");
                string login = ConsoleHelper.ReadString("Podaj login:");
                // Basic check - allow trying empty login which should fail validation/lookup
                // if (string.IsNullOrWhiteSpace(login)) continue;

                string password = ConsoleHelper.ReadPassword("Podaj hasło:");

                try
                {
                    currentUser = await authService.LoginAsync(login, password);

                    if (currentUser == null)
                    {
                        // Generic message shown to user. Specific reason logged by AuthService.
                        ConsoleHelper.WriteError("Nieprawidłowy login lub hasło. Spróbuj ponownie.");
                        await Task.Delay(1500); // Pause before clearing screen
                        Console.Clear();
                    }
                }
                catch (Exception ex) // Catch unexpected errors during login process itself
                {
                    // Logged by lower layers if it's file read or verification error
                    // This catches issues in the LoginAsync flow itself
                    ConsoleHelper.WriteError($"Wystąpił nieoczekiwany błąd podczas próby logowania. Sprawdź logi.");
                    // Log here with context if not already logged adequately
                    await _logger.LogErrorAsync($"Unexpected error during LoginAsync call for user '{login}' in Program.cs", ex);
                    await Task.Delay(2000);
                    Console.Clear();
                    // Decide if you want to exit or retry after unexpected error
                }
            } // End login loop

            // Login successful
            Console.Clear();
            ConsoleHelper.WriteSuccess($"Zalogowano pomyślnie jako: {currentUser.Login} (Rola: {currentUser.Role})");
            Console.WriteLine("\nWciśnij dowolny klawisz, aby przejść do menu głównego...");
            Console.ReadKey();


            // --- Główna pętla menu ---
            await menuService.ShowMainMenu(currentUser);

        }
        catch (InvalidOperationException dbEx) when (dbEx.Message.Contains("Failed to connect"))
        {
             // Specific handling for initial DB connection failure
             ConsoleHelper.WriteError($"Nie można połączyć się z bazą danych. Sprawdź konfigurację i status serwera.");
             await (_logger?.LogErrorAsync("Application startup failed due to database connection error.", dbEx) ?? Task.CompletedTask);
        }
        catch (Exception ex) // Catch unexpected errors during service creation or main menu loop
        {
             ConsoleHelper.WriteError($"Wystąpił krytyczny błąd aplikacji: {ex.Message}. Sprawdź logi.");
             // Ensure logger exists before logging critical startup/runtime error
             await (_logger?.LogErrorAsync("Critical application error occurred outside login/menu loop.", ex) ?? Task.CompletedTask);
        }
        finally
        {
             // Log application shutdown regardless of success/failure
             await (_logger?.LogInfoAsync("Application shutting down.") ?? Task.CompletedTask);
             Console.WriteLine("\nNaciśnij dowolny klawisz aby zamknąć okno...");
             Console.ReadKey();
        }
    } // End Main

    // --- Event Handlers ---

    // These handlers MUST be static because Main is static, or the logger/authservice
    // instances need to be accessible in a non-static context where handlers are defined.
    // Using a static logger field makes static handlers feasible here.

    /// <summary>
    /// Handles the LoginAttempt event from AuthenticationService.
    /// </summary>
    private static Task AuthService_LoginAttempt(object sender, UserActionEventArgs e)
    {
        // Log every login attempt - Can be verbose, consider logging level or conditional logging
        // Use Task.CompletedTask if no async work needed, but keep async signature for delegate compatibility
        return _logger?.LogInfoAsync($"Login attempt recorded for user: '{e.Login}'. Timestamp: {e.Timestamp:O}") ?? Task.CompletedTask;
    }

    /// <summary>
    /// Handles the UserLoggedIn event from AuthenticationService.
    /// </summary>
    private static Task AuthService_UserLoggedIn(object sender, UserActionEventArgs e)
    {
        // Log successful login details
        if (e.Success && _logger != null)
        {
             // AuthenticationService already logs basic success, add more context here if needed
             return _logger.LogInfoAsync($"Event Handler: User '{e.Login}' login confirmed. Role assigned. Details: {e.Message}");
        }
        return Task.CompletedTask;
    }

     /// <summary>
    /// Handles the LogSaved event from the FileLoggerService (example).
    /// </summary>
    private static Task Logger_LogSaved(string logEntry)
    {
        // Optional: Perform action when a log is saved. Avoid complex/blocking operations here.
        // Example: Write to console for real-time debug view (can be noisy)
        // Console.ForegroundColor = ConsoleColor.DarkGray;
        // Console.WriteLine($"[DEBUG LOG] {logEntry.Trim()}");
        // Console.ResetColor();
        return Task.CompletedTask; // Return completed task as required by Func<string, Task>
    }
}