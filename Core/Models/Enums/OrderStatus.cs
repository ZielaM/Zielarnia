namespace Zielarnia.Core.Models.Enums;

// Odpowiada ENUM('Nowe', 'W trakcie', 'Zrealizowane', 'Anulowane') w tabeli Zamowienia
public enum OrderStatus
{
    Nowe,        // Polish 'Nowe'
    W_trakcie,   // Polish 'W trakcie' - underscore to make it a valid identifier
    Zrealizowane, // Polish 'Zrealizowane'
    Anulowane    // Polish 'Anulowane'
}

// Helper do konwersji z/do stringa bazodanowego
public static class OrderStatusConverter
{
    public static string ToDbString(OrderStatus status) => status switch
    {
        OrderStatus.Nowe => "Nowe",
        OrderStatus.W_trakcie => "W trakcie",
        OrderStatus.Zrealizowane => "Zrealizowane",
        OrderStatus.Anulowane => "Anulowane",
        _ => throw new ArgumentOutOfRangeException(nameof(status), $"Not expected status value: {status}"),
    };

    public static OrderStatus FromDbString(string status) => status switch
    {
        "Nowe" => OrderStatus.Nowe,
        "W trakcie" => OrderStatus.W_trakcie,
        "Zrealizowane" => OrderStatus.Zrealizowane,
        "Anulowane" => OrderStatus.Anulowane,
        _ => throw new ArgumentException($"Invalid status string from database: {status}", nameof(status)),
    };
}