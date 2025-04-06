using Zielarnia.Core.Models.Base;

namespace Zielarnia.Data.Models;

/// <summary>
/// Represents the 'Klienci' table. Inherits from EntityBase.
/// Fulfills inheritance requirement [source: 3] (derived class).
/// </summary>
public class Client : EntityBase
{
    // Id is inherited
    public string FirstName { get; set; } = string.Empty; // imie
    public string LastName { get; set; } = string.Empty;  // nazwisko
    public string Email { get; set; } = string.Empty;     // email
    public string PhoneNumber { get; set; } = string.Empty; // telefon
    public int AddressId { get; set; } // adres_id (FK)
    public string? ParcelLockerId { get; set; } // id_paczkomatu (nullable)

    // Navigation property (optional, requires loading logic)
    // public virtual Address Address { get; set; }
}