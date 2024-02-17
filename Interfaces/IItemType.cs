namespace InventorySystem.Interfaces;

/// <summary>
///     Implement this interface to create a type that contains all data that is shared between items of the same type
///     (examples of item types: "Healing potion", "Longsword")
/// </summary>
public interface IItemType
{
    public int Id { get; }
}

public interface IItemStackType : IItemType
{
    public int MaxStackSize { get; }
}