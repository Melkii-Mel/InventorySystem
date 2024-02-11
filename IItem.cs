namespace InventorySystem;

/// <summary>
///     An interface for an item that will be stored in inventory
///     It's recommended to implement this interface in a struct rather than in a class
///     Here you can define which properties different items with the same type will have
///     Make sure do add [Serializable] attribute if you want to serialize your inventory
/// </summary>
public interface IItem
{
    /// <summary>
    ///     Type of the item instance
    /// </summary>
    IItemType Type { get; }
}