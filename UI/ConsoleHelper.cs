using System; // For Console, ConsoleKeyInfo, StringComparison, etc.
using System.Text; // For StringBuilder
using System.Globalization; // For CultureInfo, NumberStyles

namespace Zielarnia.UI;

/// <summary>
/// Provides static utility methods for interacting with the console user interface.
/// Includes methods for displaying messages, reading various input types with validation.
/// </summary>
public static class ConsoleHelper
{
    // --- Display Methods ---

    /// <summary>
    /// Clears the console screen.
    /// </summary>
    public static void ClearScreen() => Console.Clear();

    /// <summary>
    /// Displays a formatted header text.
    /// </summary>
    /// <param name="title">The title text to display.</param>
    public static void DisplayHeader(string title)
    {
        Console.WriteLine(); // Add space before header
        Console.ForegroundColor = ConsoleColor.Cyan; // Example: Use color for headers
        Console.WriteLine($"--- {title.ToUpperInvariant()} ---"); // Make title stand out
        Console.ResetColor();
        Console.WriteLine(); // Add space after header
    }

    /// <summary>
    /// Writes a standard informational message to the console.
    /// </summary>
    /// <param name="message">The message to display.</param>
    public static void WriteMessage(string message)
    {
        Console.WriteLine(message);
    }

    /// <summary>
    /// Writes a success message, typically highlighted in green.
    /// </summary>
    /// <param name="message">The success message.</param>
    public static void WriteSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[OK] {message}");
        Console.ResetColor();
    }

     /// <summary>
    /// Writes a warning message, typically highlighted in yellow.
    /// </summary>
    /// <param name="message">The warning message.</param>
    public static void WriteWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[UWAGA] {message}"); // Polish prefix
        Console.ResetColor();
    }

    /// <summary>
    /// Writes an error message, typically highlighted in red.
    /// </summary>
    /// <param name="message">The error message.</param>
    public static void WriteError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[BŁĄD] {message}"); // Polish prefix
        Console.ResetColor();
    }

    // --- Input Reading Methods ---

    /// <summary>
    /// Prompts the user and reads a string input. Loops until valid input is provided.
    /// </summary>
    /// <param name="prompt">The message to display to the user before input.</param>
    /// <param name="allowEmpty">If true, allows the user to enter an empty string.</param>
    /// <returns>The non-null string entered by the user.</returns>
    public static string ReadString(string prompt, bool allowEmpty = false)
    {
        string? input;
        do
        {
            Console.Write($"{prompt} ");
            input = Console.ReadLine();
            if (!allowEmpty && string.IsNullOrWhiteSpace(input))
            {
                WriteWarning("Wartość nie może być pusta. Spróbuj ponownie.");
            }
            // Loop continues if input is required (!allowEmpty) and is whitespace (or null)
        } while (!allowEmpty && string.IsNullOrWhiteSpace(input));
        return input ?? string.Empty; // Return empty string if input was null (shouldn't happen with ReadLine but good practice)
    }

     /// <summary>
    /// Prompts the user and reads a password without displaying characters on the console.
    /// Displays '*' for each character entered.
    /// </summary>
    /// <param name="prompt">The message to display to the user.</param>
    /// <returns>The password string entered by the user.</returns>
    public static string ReadPassword(string prompt)
    {
        Console.Write($"{prompt} ");
        var password = new StringBuilder();
        while (true)
        {
            ConsoleKeyInfo keyInfo = Console.ReadKey(true); // Read key without displaying it

            if (keyInfo.Key == ConsoleKey.Enter)
            {
                Console.WriteLine(); // Move to next line after Enter is pressed
                break; // Exit loop on Enter
            }

            if (keyInfo.Key == ConsoleKey.Backspace && password.Length > 0)
            {
                // Handle Backspace: remove last char from StringBuilder and visually from console
                password.Remove(password.Length - 1, 1);
                Console.Write("\b \b"); // Move cursor back, write space to overwrite '*', move back again
            }
            // Ignore control keys, function keys, navigation keys etc.
            else if (!char.IsControl(keyInfo.KeyChar))
            {
                // Append character to StringBuilder and display '*'
                password.Append(keyInfo.KeyChar);
                Console.Write("*");
            }
            // Ignore other keys (like arrows, function keys, etc.)
        }
        return password.ToString();
    }


    /// <summary>
    /// Prompts the user and reads an integer input. Loops until valid input within optional bounds is provided.
    /// </summary>
    /// <param name="prompt">The message to display to the user.</param>
    /// <param name="minValue">Optional minimum allowed value (inclusive).</param>
    /// <param name="maxValue">Optional maximum allowed value (inclusive).</param>
    /// <returns>The valid integer entered by the user.</returns>
    public static int ReadInt(string prompt, int? minValue = null, int? maxValue = null)
    {
        int value;
        bool isValid;
        string? input;
        do
        {
            Console.Write($"{prompt} ");
            input = Console.ReadLine();
            isValid = int.TryParse(input, out value);

            if (!isValid)
            {
                WriteWarning("Nieprawidłowa liczba całkowita. Spróbuj ponownie.");
            }
            else if (minValue.HasValue && value < minValue.Value)
            {
                WriteWarning($"Wartość musi być większa lub równa {minValue.Value}. Spróbuj ponownie.");
                isValid = false;
            }
            else if (maxValue.HasValue && value > maxValue.Value)
            {
                WriteWarning($"Wartość musi być mniejsza lub równa {maxValue.Value}. Spróbuj ponownie.");
                isValid = false;
            }
            // Loop continues if parsing failed or value out of bounds
        } while (!isValid);
        return value;
    }

     /// <summary>
    /// Prompts the user and reads a decimal input. Loops until valid input above an optional minimum is provided.
    /// Handles both '.' and ',' as decimal separators.
    /// </summary>
    /// <param name="prompt">The message to display to the user.</param>
    /// <param name="minValue">Optional minimum allowed value (inclusive).</param>
    /// <returns>The valid decimal entered by the user.</returns>
    public static decimal ReadDecimal(string prompt, decimal? minValue = null)
    {
        decimal value;
        bool isValid;
        string? input;
        do
        {
            Console.Write($"{prompt} ");
            input = Console.ReadLine()?.Replace(',', '.'); // Standardize decimal separator to '.'

            // Use InvariantCulture for parsing to ensure '.' is the separator, regardless of system locale
            isValid = decimal.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out value);

            if (!isValid)
            {
                 WriteWarning("Nieprawidłowa liczba dziesiętna. Użyj '.' lub ',' jako separatora. Spróbuj ponownie.");
            }
            else if (minValue.HasValue && value < minValue.Value)
            {
                // Format the minimum value to show decimals if appropriate
                WriteWarning($"Wartość musi być większa lub równa {minValue.Value:F2}. Spróbuj ponownie.");
                isValid = false;
            }
            // Loop continues if parsing failed or value below minimum
        } while (!isValid);
        return value;
    }

     /// <summary>
    /// Prompts the user for a Yes/No confirmation (T/N). Loops until valid input is received.
    /// </summary>
    /// <param name="prompt">The confirmation question to ask the user.</param>
    /// <returns>True if the user confirms (enters T or t), False otherwise (enters N or n).</returns>
    public static bool Confirm(string prompt)
    {
        string? input;
        do
        {
            Console.Write($"{prompt} (T/N): ");
            input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input) ||
               (!input.Trim().Equals("T", StringComparison.OrdinalIgnoreCase) &&
                !input.Trim().Equals("N", StringComparison.OrdinalIgnoreCase)))
            {
                WriteWarning("Wpisz 'T' (Tak) lub 'N' (Nie).");
                input = null; // Force loop to continue
            }
            // Loop continues if input is invalid
        } while (input == null);

        // Return true only if input is 'T' (case-insensitive)
        return input.Trim().Equals("T", StringComparison.OrdinalIgnoreCase);
    }
}