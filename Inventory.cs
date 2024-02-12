// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global

namespace InventorySystem;

/// <summary>
///     creates an inventory that can store items represented as an implementors of IItem
///     Supports methods for adding or removing elements, as well as non-stackable items and stackable items
///     Stackable items represented as IItemStack inheritors
/// </summary>
[Serializable]
public partial class Inventory
{
    /// <summary>
    ///     Initializes a new instance of the Inventory class with the specified size.
    /// </summary>
    /// <param name="size">The size of the inventory.</param>
    /// <param name="comparator">
    ///     A MultiComparator object used for sorting items in the inventory.
    ///     If sorting is not required, use MultiComparator.CreateBlank().
    /// </param>
    public Inventory(int size, MultiComparator comparator)
    {
        Comparator = comparator;
        _items = new IItem[size];
    }

    /// <summary>
    ///     All items containing in inventory
    ///     Manually adding or removing them is not recommended, use AddItem or TakeItem instead
    /// </summary>
    public IItem?[] Items
    {
        get => _items;
        set => _items = value;
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public MultiComparator Comparator { get; set; }

    /// <summary>
    ///     Gets a value indicating whether the inventory is full.
    /// </summary>
    /// <remarks>
    ///     Returns true if all slots in the inventory are occupied; otherwise, false.
    /// </remarks>
    public bool IsFull
    {
        get
        {
            foreach (var item in Items)
                if (item is null)
                    return false;

            return true;
        }
    }

    /// <summary>
    ///     Gets the number of empty slots in the inventory.
    /// </summary>
    public int EmptySlotsAmount
    {
        get
        {
            var result = 0;
            foreach (var item in Items)
                if (item is null)
                    result++;

            return result;
        }
    }

    /// <summary>
    ///     Gets a list of indexes representing empty slots in the inventory.
    /// </summary>
    public List<int> EmptySlots
    {
        get
        {
            List<int> result = new();
            for (var i = 0; i < Items.Length; i++)
                if (Items[i] is null)
                    result.Add(i);
            return result;
        }
    }

    /// <summary>
    ///     Changes the size of the inventory by the specified deltaSize.
    /// </summary>
    /// <param name="deltaSize">The change in size, which can be positive or negative.</param>
    /// <returns>A list of items that were removed due to slot resizing.</returns>
    public List<IItem> ChangeSize(int deltaSize)
    {
        if (deltaSize + Items.Length < 0)
            throw new ArgumentException("Value is too low. deltaSize can't be less than -Items.Length");
        List<IItem> removedItems = new();
        for (var i = Items.Length - deltaSize; i < Items.Length; i++)
        {
            if (Items[i] is null) continue;
            removedItems.Add(Items[i]!);
        }

        Array.Resize(ref _items, Items.Length + deltaSize);
        OnInventorySizeChanged(
            new InventorySizeChangedEventArgs(deltaSize, Items.Length, Items.Length - deltaSize, removedItems));
        return removedItems;
    }

    /// <summary>
    ///     Inserts an item into the inventory.
    /// </summary>
    /// <param name="addable">The item to be added to the inventory.</param>
    /// <typeparam name="T">Specific type of implementor of IItem</typeparam>
    /// <returns>An InsertionInfo object containing information about the insertion operation.</returns>
    public InsertionInfo<T> InsertItem<T>(T addable) where T : IItem
    {
        var insertionInfo = new InsertionInfo<T>
        {
            FitInInventory = true,
            FitInOccupiedSlots = true,
            InsertedItem = addable
        };

        if (addable is IItemStack addableStack)
        {
            var totalAmount = addableStack.Amount;
            foreach (var item in NotNullItems())
            {
                if (item.Type != addableStack.Type) continue;
                var itemStack = (IItemStack) item;
                addableStack = itemStack.Stack(addableStack);
                if (addableStack.Amount == 0) break;
            }

            if (addableStack.Amount == 0)
            {
                insertionInfo.FitInOccupiedSlots = true;
                OnItemInserted(new ItemAddedEventArgs(addableStack, null, !insertionInfo.FitInOccupiedSlots, true));
                return insertionInfo;
            }

            insertionInfo.FitInOccupiedSlots = false;

            if (IsFull)
            {
                insertionInfo.FitInInventory = false;
                var insertedItems = addableStack;
                insertedItems.Amount = totalAmount - addableStack.Amount;
                OnItemInserted(new ItemAddedEventArgs(insertedItems, addableStack, !insertionInfo.FitInOccupiedSlots,
                    true));
                return insertionInfo;
            }

            while (addableStack.Amount > 0)
            {
                var newStack = addableStack;
                var deltaAmount = Math.Min(addableStack.Type.MaxStackSize, addableStack.Amount);
                newStack.Amount = deltaAmount;
                addableStack.Amount -= deltaAmount;
                Items[FindFirstEmptySlot()] = newStack;
                if (IsFull) break;
            }

            var itemsInserted = addableStack;
            itemsInserted.Amount = totalAmount - addableStack.Amount;
            OnItemInserted(new ItemAddedEventArgs(itemsInserted, addableStack, !insertionInfo.FitInOccupiedSlots,
                insertionInfo.FitInInventory));
            return insertionInfo;
        }

        insertionInfo.FitInOccupiedSlots = false;
        if (IsFull)
        {
            insertionInfo.FitInInventory = false;
            OnItemInserted(new ItemAddedEventArgs(null, addable, false, false));
            return insertionInfo;
        }

        Items[FindFirstEmptySlot()] = addable;
        OnItemInserted(new ItemAddedEventArgs(addable, null, false, true));
        return insertionInfo;
    }

    /// <summary>
    ///     Tries to insert item into specified slot
    /// </summary>
    /// <param name="addable">Item to insert</param>
    /// <param name="index">Index of slot in which an item will be inserted</param>
    /// <typeparam name="T">Specific type of implementor of IItem</typeparam>
    /// <returns>
    ///     An InsertionInfo object containing information about the insertion operation
    /// </returns>
    public InsertionInfo<T> InsertItem<T>(T addable, int index) where T : IItem
    {
        var insertionInfo = new InsertionInfo<T>
        {
            FitInOccupiedSlots = false,
            FitInInventory = false,
            InsertedItem = addable
        };
        var item = Items[index];
        if (item is null)
        {
            Items[index] = addable;
            insertionInfo.FitInOccupiedSlots = false;
            insertionInfo.FitInInventory = true;
            OnItemInserted(new ItemAddedEventArgs(addable, null, true, true));
            return insertionInfo;
        }

        if (addable is not IItemStack addableStack)
        {
            insertionInfo.FitInOccupiedSlots = false;
            insertionInfo.FitInInventory = false;
            OnItemInserted(new ItemAddedEventArgs(null, addable, false, false));
            return insertionInfo;
        }

        if (!AreSameItems(addableStack, item))
        {
            insertionInfo.FitInOccupiedSlots = false;
            insertionInfo.FitInInventory = false;
            OnItemInserted(new ItemAddedEventArgs(null, addable, false, false));
            return insertionInfo;
        }

        var itemStack = (IItemStack) item;
        var initialAmount = addableStack.Amount;
        addableStack = itemStack.Stack(addableStack);
        var insertedStack = addableStack;
        insertedStack.Amount = initialAmount - addableStack.Amount;
        OnItemInserted(new ItemAddedEventArgs(insertedStack, addableStack, false, addableStack.Amount == 0));
        return new InsertionInfo<T>
        {
            InsertedItem = addable,
            FitInInventory = addableStack.Amount == 0,
            FitInOccupiedSlots = addableStack.Amount == 0
        };
    }

    /// <summary>
    ///     Inserts an item in the provided slot and returns removed item it it had any
    /// </summary>
    /// <param name="insertable">Item that will be inserted</param>
    /// <param name="slot">Slot in which an item will be inserted</param>
    /// <returns>Removed item?</returns>
    public IItem? SwapItems(IItem insertable, int slot)
    {
        var removable = Items[slot];
        Items[slot] = insertable;
        OnItemSwapped(new ItemSwappedEventArgs(removable is null, removable, insertable));
        return removable;
    }

    /// <summary>
    ///     Tries to remove an item from the inventory.
    /// </summary>
    /// <param name="takeables">Items to be removed from the inventory</param>
    /// <typeparam name="T">The type of the item to be removed.</typeparam>
    /// <returns>True if the item was successfully removed; otherwise, false.</returns>
    public bool TryTakeItems<T>(params T[] takeables) where T : IItem
    {
        int[] takeablesAmounts = new int[takeables.Length];
        for (var i = 0; i < takeables.Length; i++)
        {
            var takeable = takeables[i];
            takeablesAmounts[i] = takeable is IItemStack takeableStack ? takeableStack.Amount : 1;
        }

        foreach(var item in NotNullItems(true))
        {
            if (item is IItemStack itemStack)
            {
                for (var i = 0; i < takeables.Length; i++)
                {
                    var takeable = takeables[i];
                    if (!AreSameItems(takeable, item)) continue;
                    var deltaAmount = Math.Min(itemStack.Amount, takeablesAmounts[i]);
                    takeablesAmounts[i] -= deltaAmount;
                    itemStack.Amount -= deltaAmount;
                    if (itemStack.Amount == 0) break;
                }
            }
            else
            {
                for (var i = 0; i < takeables.Length; i++)
                {
                    if (!AreSameItems(takeables[i], item)) continue;
                    takeablesAmounts[i] = 0;
                    break;
                }
            }
        }
        if (takeablesAmounts.Count(a => a != 0) != 0) return false;

        for (int i = _items.Length - 1; i > -1; i++)
        {
            var item = _items[i];
            if (item is null) continue;
            foreach (var takeable in takeables)
            {
                if (!AreSameItems(takeable, item)) continue;
                if (takeable is IItemStack takeableStack && item is IItemStack itemStack)
                {
                    var deltaAmount = Math.Min(takeableStack.Amount, itemStack.Amount);
                    ((IItemStack) _items[i]!).Amount -= deltaAmount;
                    if (((IItemStack) _items[i]!).Amount == 0) break;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// returns all items with the specified Item Type
    /// </summary>
    /// <param name="type">Type of receivable items</param>
    public IItem[] GetItems(IItemType type)
    {
        return Items.Where(item => item is not null && item.Type == type).ToArray()!;
    }

    /// <summary>
    /// returns amount of items in the inventory
    /// With IItemStack, returns amount of separated stack
    /// If you want to find the exact amount of items in stacks, use GetStackableItemsAmount
    /// </summary>
    /// <param name="type">Type of items you want to count</param>
    public int GetItemsAmount(IItemType type)
    {
        return GetItems(type).Length;
    }

    /// <summary>
    /// Returns amount of items of the specified type in all stacks
    /// </summary>
    /// <param name="type">Type of items you want to count</param>
    public int GetStackableItemsAmount(IItemStackType type)
    {
        return ((IItemStack[])GetItems(type)).Select(item => item.Amount).Sum();
    }

    /// <summary>
    /// Removes an item from the inventory if there is any, then returns it
    /// </summary>
    /// <param name="index">Index of slot from which an item will be removed</param>
    /// <returns>Removed item?</returns>
    public IItem? TryTakeItem(int index)
    {
        var result = Items[index];
        Items[index] = null;
        OnItemRemoved(new ItemRemovedEventArgs(result is null, result));
        return result;
    }

    /// <summary>
    ///     Sorts the items in the inventory using the current comparator.
    /// </summary>
    public void SortItems()
    {
        Array.Sort(Items, Comparator);
        OnInventorySorted();
    }

    /// <summary>
    ///     Sorts the items in the inventory using the specified comparator.
    /// </summary>
    /// <param name="comparatorName">The name of the comparator to be used for sorting.</param>
    public void SortItems(string comparatorName)
    {
        var currentName = Comparator.CurrentComparator;
        Comparator.CurrentComparator = comparatorName;
        Array.Sort(Items);
        Comparator.CurrentComparator = currentName;
    }

    /// <summary>
    ///     Filters the items in the inventory using the specified predicate.
    /// </summary>
    /// <param name="predicate">The predicate used to filter the items.</param>
    /// <returns>An array of items that satisfy the predicate condition.</returns>
    public IItem[] FilterItems(Func<IItem, bool> predicate)
    {
        IItem[] result = Items.Where(item => item != null && predicate(item)).ToArray()!;
        OnFilterApplied(new FilterAppliedEventArgs(predicate, result));
        return result;
    }

    /// <summary>
    ///     Being generated by AddItem method, provides data about operation result
    /// </summary>
    public struct InsertionInfo<T> where T : IItem
    {
        /// <summary>
        ///     Item that has been inserted
        /// </summary>
        public T? InsertedItem { get; internal set; }

        /// <summary>
        ///     Item that has not been inserted due to lack of space
        /// </summary>
        public T? RejectedItem { get; internal set; }

        /// <summary>
        ///     True if item was successfully inserted in the inventory
        /// </summary>
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public bool FitInInventory { get; internal set; }

        /// <summary>
        ///     True if item didn't occupy any additional slot
        /// </summary>
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public bool FitInOccupiedSlots { get; internal set; }
    }
}