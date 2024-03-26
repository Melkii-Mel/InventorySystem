namespace InventorySystem.Interfaces;

/// <summary>
///     An interface for an item that will be stored in inventory
///     In order to create an Item, you have to create a base struct for all types of items
///     Or you can simply use BaseItem struct
///     When creating an inventory, use this type as a type parameter for an Inventory
///     Make sure do add [Serializable] attribute for item implementation if you want to serialize your inventory
/// </summary>
public interface IItem
{
    /// <summary>
    ///     Type of the item instance
    /// </summary>
    IItemType Type { get; }
}

public sealed class ItemInfo
{
    public ItemInfo(IItemType type, int amount)
    {
        Type = type;
        Amount = amount;
    }

    public ItemInfo(IItemStack item, int amount)
    {
        Type = item.Type;
        Amount = amount;
    }

    public ItemInfo(IItem item)
    {
        Type = item.Type;
        Amount = 1;
    }

    /// <summary>
    /// Type of the item
    /// </summary>
    public IItemType Type { get; }
    
    /// <summary>
    /// Amount of items (if it's an IItemStack)
    /// </summary>
    public int Amount { get; internal set; }
}