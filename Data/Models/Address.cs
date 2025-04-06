using Zielarnia.Core.Models.Base; // Import Base class

namespace Zielarnia.Data.Models;

/// <summary>
/// Represents the 'adresy' table. Inherits from EntityBase.
/// </summary>
public class Address : EntityBase // Inherit from EntityBase
{
    // Id is inherited from EntityBase
    public string Country { get; set; } = string.Empty; // Kraj
    public string City { get; set; } = string.Empty;    // miasto
    public string Street { get; set; } = string.Empty;   // ulica
    public string BuildingNumber { get; set; } = string.Empty; // numer_budynku
    public string? ApartmentNumber { get; set; } // numer_mieszkania (nullable)
}