namespace Zielarnia.Data.Models;

/// <summary>
/// Represents the 'SzczegolyZamowienia' join table.
/// Does not inherit from EntityBase due to composite primary key.
/// </summary>
public class OrderDetail
{
    public int OrderId { get; set; } // zamowienie_id (PK, FK)
    public int ProductId { get; set; } // produkt_id (PK, FK)
    public int Quantity { get; set; } // ilosc
}