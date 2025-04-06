namespace Zielarnia.Core.Models.Base;

/// <summary>
/// Base class for data models that have a primary key Id.
/// Fulfills the inheritance requirement [source: 3].
/// </summary>
public abstract class EntityBase
{
    /// <summary>
    /// Primary key identifier for the entity.
    /// </summary>
    public int Id { get; set; }
}