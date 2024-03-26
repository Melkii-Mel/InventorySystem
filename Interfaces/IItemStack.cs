namespace InventorySystem.Interfaces;

/// <summary>
///     Implement this interface to make a stackable item
///     Make sure do add [Serializable] attribute if you want to serialize your inventory
/// </summary>
public interface IItemStack : IItem
{
    /// <summary>
    ///     Type of the stack, containing it's Id and maximum stack size
    /// </summary>
    new IItemStackType Type { get; }

    /// <summary>
    ///     Current amount of items in the stack
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    ///     Stacks this item with another item of the same type, increasing amount of this stack and decreasing amount of stackFrom stack.
    /// </summary>
    /// <param name="stackFrom">The item to stack with this one</param>
    /// <exception cref="ArgumentException">Thrown when trying to stack different items</exception>
    /// <returns>The remaining item after stacking, if any.</returns>
    public void Stack(IItemStack stackFrom)
    {
        if (stackFrom.Type != Type) throw new ArgumentException("These are different items and so can't be stacked");
        var deltaAmount = Math.Min(Type.MaxStackSize - Amount, stackFrom.Amount);
        stackFrom.Amount -= deltaAmount;
        Amount += deltaAmount;
    }

    public IItemStack CreateEmptyClone();
}