namespace InventorySystem;

/// <summary>
///     Represents a collection of item types with methods for retrieval, addition, and removal.
///     Create a single instance of this class in the global scope.
/// </summary>
public class ItemTypesHolder
{
    private readonly List<IItemType> _itemTypes = new();

    /// <summary>
    ///     Retrieves the item type with the specified ID.
    /// </summary>
    /// <param name="id">The ID of the item type to retrieve.</param>
    /// <returns>The item type with the specified ID.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no item type with the specified ID exists in the collection.</exception>
    public IItemType GetType(int id)
    {
        var result = _itemTypes.FirstOrDefault(type => type.Id == id);
        if (result is null) throw new InvalidOperationException($"No item type with ID={id} exists in the collection.");
        return result;
    }

    /// <summary>
    ///     Retrieves an array of item types that satisfy the specified condition.
    /// </summary>
    /// <param name="predicate">The condition that item types must satisfy.</param>
    /// <returns>An array of item types that satisfy the specified condition.</returns>
    public IItemType[] GetType(Func<IItemType, bool> predicate)
    {
        return _itemTypes.Where(predicate).ToArray();
    }

    /// <summary>
    ///     Adds the specified item types to the collection.
    /// </summary>
    /// <param name="itemTypes">The item types to add to the collection.</param>
    /// <exception cref="ArgumentException">Thrown if an item type with the same ID already exists in the collection.</exception>
    public void AddTypes(params IItemType[] itemTypes)
    {
        foreach (var type in itemTypes)
            if (_itemTypes.Any(t => t.Id == type.Id))
                throw new ArgumentException(
                    $"An item type with ID={type.Id} already exists in the collection." +
                    "\nA collection of item types should not contain multiple types with the same ID.");
        _itemTypes.AddRange(itemTypes);
    }

    /// <summary>
    ///     Removes the specified item types from the collection.
    /// </summary>
    /// <param name="itemTypes">The item types to remove from the collection.</param>
    /// <returns>An array of item types that were not removed from the collection.</returns>
    public IItemType[] RemoveTypes(params IItemType[] itemTypes)
    {
        var notRemovedTypes = new List<IItemType>();

        foreach (var type in itemTypes)
            if (!_itemTypes.Remove(type))
                notRemovedTypes.Add(type);

        return notRemovedTypes.ToArray();
    }
}