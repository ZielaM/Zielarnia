using Zielarnia.Core.Models.Base;

namespace Zielarnia.Data.Models;

/// <summary>
/// Represents the 'Kategorie' table. Inherits from EntityBase.
/// </summary>
public class Category : EntityBase
{
    // Id is inherited
    public string Name { get; set; } = string.Empty; // nazwa
}