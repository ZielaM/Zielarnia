using Zielarnia.Core.Models.Base;

namespace Zielarnia.Data.Models;

/// <summary>
/// Represents the 'typy_rachunkow' table. Inherits from EntityBase.
/// </summary>
public class BillType : EntityBase
{
    // Id is inherited
    public string TypeName { get; set; } = string.Empty; // typ
    public string? Description { get; set; } // opis (nullable TEXT)
}