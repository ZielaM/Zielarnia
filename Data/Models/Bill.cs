using Zielarnia.Core.Models.Base;

namespace Zielarnia.Data.Models;

/// <summary>
/// Represents the 'rachunki' table. Inherits from EntityBase.
/// </summary>
public class Bill : EntityBase
{
    // Id is inherited
    public DateOnly BillDate { get; set; } // data (DATE maps well to DateOnly in .NET 6+)
    public int BillTypeId { get; set; } // typ (FK - refers to typy_rachunkow.id)
    public decimal Amount { get; set; } // kwota

    // Navigation property (optional, requires loading logic)
    // public virtual BillType BillType { get; set; }
}