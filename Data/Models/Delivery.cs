namespace Zielarnia.Data.Models;

/// <summary>
/// Represents the 'Dostawy' join table (linking Dostawcy and Produkty).
/// Does not inherit from EntityBase due to composite primary key.
/// </summary>
public class Delivery
{
    public int SupplierId { get; set; } // dostawca_id (PK, FK)
    public int ProductId { get; set; } // produkt_id (PK, FK)

    // Add other columns if they exist in the 'Dostawy' table, e.g., Quantity, DeliveryDate
    // public int Quantity { get; set; }
    // public DateTime DeliveryDate { get; set; }
}