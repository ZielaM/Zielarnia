using Zielarnia.Core.Models.Base;
using Zielarnia.Core.Models.Enums; // For OrderStatus

namespace Zielarnia.Data.Models;

/// <summary>
/// Represents the 'Zamowienia' table. Inherits from EntityBase.
/// </summary>
public class Order : EntityBase
{
    // Id is inherited
    public int? ClientId { get; set; } // klient_id (FK, nullable?) Check schema constraints
    public DateTime OrderDate { get; set; } = DateTime.UtcNow; // data_zamowienia (DEFAULT CURRENT_TIMESTAMP)
    public OrderStatus Status { get; set; } // status (ENUM) - Use the enum

    // Navigation property (optional, requires loading logic)
    // public virtual Client? Client { get; set; }
}