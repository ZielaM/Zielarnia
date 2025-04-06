using Zielarnia.Core.Models.Base;

namespace Zielarnia.Data.Models;

/// <summary>
/// Represents the 'Dostawcy' table. Inherits from EntityBase.
/// </summary>
public class Supplier : EntityBase
{
    // Id is inherited
    public string Name { get; set; } = string.Empty; // nazwa
    public string? ContactInfo { get; set; } // kontakt (nullable)
    public int? AddressId { get; set; } // adres_id (FK, nullable based on schema)

    // Navigation property (optional, requires loading logic)
    // public virtual Address? Address { get; set; }
}