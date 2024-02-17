namespace InventorySystem;

/// <summary>
///     An interface for an item that will be stored in inventory
///     In order to create an Item, you have to create a base struct for all types of items
///     Or you can simply use BaseItem struct
///     Then you can inherit it to create different types of items
///     When creating an inventory, use this type as a type parameter in order to be able to store different types of items
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