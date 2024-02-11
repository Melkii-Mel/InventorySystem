namespace InventorySystem;

/// <summary>
///     Implement this interface to create a type that contains all data that is shared between items of the same type
/// </summary>
public interface IItemType
{
    public int Id { get; }
}

public interface IItemStackType : IItemType
{
    public int MaxStackSize { get; }
}