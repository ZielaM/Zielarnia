using Zielarnia.Core.Models.Base;

namespace Zielarnia.Data.Models;

/// <summary>
/// Represents the 'Produkty' table. Inherits from EntityBase.
/// Fulfills inheritance requirement [source: 3] (derived class).
/// </summary>
public class Product : EntityBase
{
    // Id is inherited
    public string Name { get; set; } = string.Empty; // nazwa
    public string? Description { get; set; } // opis (nullable)
    public decimal Price { get; set; } // cena (DECIMAL maps well to decimal)
    public int? CategoryId { get; set; } // kategoria_id (FK, nullable due to ON DELETE SET NULL)

    // Navigation property (optional, requires loading logic)
    // public virtual Category? Category { get; set; }
}