namespace InventorySystem;

/// <summary>
///     Implement this interface to make an non-stackable item
///     Make sure do add [Serializable] attribute if you want to serialize your inventory
/// </summary>
public interface IItem : ICloneable, IComparable<IItem>
{
    public int Id { get; }
}