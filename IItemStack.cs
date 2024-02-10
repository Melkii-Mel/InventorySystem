namespace InventorySystem;

/// <summary>
///     Implement this interface to make a stackable item
///     Make sure do add [Serializable] attribute if you want to serialize your inventory
/// </summary>
public interface IItemStack : IItem
{
    public int MaxStackSize { get; }
    public int Amount { get; set; }
}