// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global

namespace InventorySystem;

/// <summary>
///     creates an inventory that can store items represented as an implementors of IItem
///     Supports methods for adding or removing elements, as well as non-stackable items and stackable items
///     Stackable items represented as IItemStack inheritors
/// </summary>
[Serializable]
public class Inventory
{
    private IItem?[] _items;

    /// <summary>
    ///     Initializes a new Inventory with the set size
    /// </summary>
    /// <param name="size">Size of the inventory</param>
    /// <param name="comparator">
    ///     MultiComparator for providing different ways of sorting.
    ///     If sorting is not implemented, use Comparator.CreateBlank()
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

    // ReSharper disable once MemberCanBePrivate.Global
    /// <summary>
    ///     Checks if inventory has empty slots in it
    /// </summary>
    public bool IsFull
    {
        get
        {
            foreach (var item in Items)
                if (item is null)
                    return true;

            return false;
        }
    }

    /// <summary>
    ///     Returns the amount of empty slots in an inventory
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
    ///     Changes size based on deltaSize
    ///     Can be positive or negative
    /// </summary>
    /// <param name="deltaSize"></param>
    /// <returns>List of items that was removed by removing slots</returns>
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
    ///     Adds an item to the inventory
    /// </summary>
    /// <param name="addable">Item you want to add to an inventory</param>
    /// <typeparam name="T">IItem type</typeparam>
    /// <returns>
    ///     Insertion info containing operation result as well as Inserted Item
    ///     If inserted item is an itemStack, it's amount represents how much items did NOT fit in the inventory
    ///     Inserted item will be provided even if insertion has failed!
    /// </returns>
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
                if (item.Id != addableStack.Id) continue;
                var itemStack = (IItemStack) item;
                var spaceInSlot = itemStack.MaxStackSize - itemStack.Amount;
                var deltaAmount = Math.Min(spaceInSlot, addableStack.Amount);
                itemStack.Amount += deltaAmount;
                addableStack.Amount -= deltaAmount;
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
                var insertedItems = (IItemStack) addableStack.Clone();
                insertedItems.Amount = totalAmount - addableStack.Amount;
                OnItemInserted(new ItemAddedEventArgs(insertedItems, addableStack, !insertionInfo.FitInOccupiedSlots,
                    true));
                return insertionInfo;
            }

            while (addableStack.Amount > 0)
            {
                var newStack = (IItemStack) addable.Clone();
                var deltaAmount = Math.Min(addableStack.MaxStackSize, addableStack.Amount);
                newStack.Amount = deltaAmount;
                addableStack.Amount -= deltaAmount;
                Items[FindFirstEmptySlot()] = newStack;
                if (IsFull) break;
            }

            var itemsInserted = (IItemStack) addableStack.Clone();
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
    /// <typeparam name="T">Type of addable</typeparam>
    /// <returns>
    ///     Insertion info containing operation results as well as addable item.
    ///     If inserted item is an itemStack, it's amount represents how much items did NOT fit in the inventory
    ///     Inserted item will be provided even if insertion has failed!
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
        var spaceLeft = itemStack.MaxStackSize - itemStack.Amount;
        var deltaAmount = Math.Min(spaceLeft, addableStack.Amount);
        itemStack.Amount += deltaAmount;
        addableStack.Amount -= deltaAmount;
        var insertedStack = (IItemStack) addableStack.Clone();
        insertedStack.Amount = deltaAmount;
        OnItemInserted(new ItemAddedEventArgs(insertedStack, addable, false, false));
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
    ///     Tries to remove a takeable from the inventory
    /// </summary>
    /// <param name="takeable">Item to remove</param>
    /// <typeparam name="T">Type of IItem</typeparam>
    /// <returns>true if items were removed successfully, false otherwise (if inventory didn't have enough items in it)</returns>
    public bool RemoveItem<T>(T takeable) where T : IItem, new()
    {
        if (takeable is IItemStack takeableStack)
        {
            var targetAmount = takeableStack.Amount;
            var havingAmount = 0;
            foreach (var item in NotNullItems())
            {
                if (!AreSameItems(takeable, item)) continue;
                var itemStack = (IItemStack) item;
                havingAmount += itemStack.Amount;
            }

            if (havingAmount < targetAmount)
            {
                OnItemRemoved(new ItemRemovedEventArgs(false, takeable));
                return false;
            }

            foreach (var item in NotNullItems())
            {
                if (!AreSameItems(takeable, item)) continue;
                var itemStack = (IItemStack) item;
                var deltaAmount = Math.Min(itemStack.Amount, targetAmount);
                itemStack.Amount -= deltaAmount;
                takeableStack.Amount -= deltaAmount;
                if (itemStack.Amount == 0) RemoveItemFromArray(itemStack);
                OnItemRemoved(new ItemRemovedEventArgs(true, takeable));
                if (takeableStack.Amount <= 0) return true;
            }
        }
        else
        {
            var itemIndex = FindItem(takeable.Id);
            if (itemIndex == -1)
            {
                OnItemRemoved(new ItemRemovedEventArgs(false, takeable));
                return false;
            }

            Items[itemIndex] = null;
            OnItemRemoved(new ItemRemovedEventArgs(true, takeable));
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Tries to remove an item from a provided slot
    /// </summary>
    /// <param name="index">Index of slot from which an item will be removed</param>
    /// <returns>Removed item?</returns>
    public IItem? RemoveItem(int index)
    {
        var result = Items[index];
        Items[index] = null;
        OnItemRemoved(new ItemRemovedEventArgs(result is null, result));
        return result;
    }

    /// <summary>
    ///     Sorts items by the comparator with the Comparator.CurrentComparator name
    /// </summary>
    public void SortItems()
    {
        Array.Sort(Items, Comparator);
        OnInventorySorted();
    }

    /// <summary>
    ///     Sorts items by the comparator with the comparatorName name
    /// </summary>
    /// <param name="comparatorName">name of the comparator that will be used</param>
    public void SortItems(string comparatorName)
    {
        var currentName = Comparator.CurrentComparator;
        Comparator.CurrentComparator = comparatorName;
        Array.Sort(Items);
        Comparator.CurrentComparator = currentName;
    }

    /// <summary>
    ///     Filter items using a predicate parameter
    /// </summary>
    /// <param name="predicate">Expression that works as a filter</param>
    /// <returns>Filtered array of items</returns>
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
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        /// <summary>
        ///     Item that has been inserted
        /// </summary>
        public T InsertedItem { get; internal set; }

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

    #region Events

    public event EventHandler? InventorySizeChanged;
    public event EventHandler? ItemInserted;
    public event EventHandler? ItemRemoved;
    public event EventHandler? FilterApplied;
    public event EventHandler? InventorySorted;
    public event EventHandler? ItemSwapped;

    #endregion

    #region Helpers

    private IEnumerable<IItem> NotNullItems()
    {
        foreach (var item in Items)
        {
            if (item is null) continue;
            yield return item;
        }
    }

    private int FindFirstEmptySlot()
    {
        for (var i = 0; i < Items.Length; i++)
            if (Items[i] is null)
                return i;

        return -1;
    }

    private bool AreSameItems(IItem item0, IItem item1)
    {
        return item0.Id == item1.Id;
    }

    private void RemoveItemFromArray(IItem removable)
    {
        for (var i = 0; i < Items.Length; i++)
        {
            var item = Items[i];
            if (item == null || !item.Equals(removable)) continue;
            Items[i] = null;
            return;
        }
    }

    private int FindItem(int index)
    {
        for (var i = 0; i < Items.Length; i++)
        {
            var item = Items[i];
            if (item != null && item.Id == index) return i;
        }

        return -1;
    }

    #endregion

    #region EventHandlers

    public class InventorySizeChangedEventArgs : EventArgs
    {
        public InventorySizeChangedEventArgs(int deltaSize, int newSize, int prevSize, List<IItem> removedItems)
        {
            DeltaSize = deltaSize;
            NewSize = newSize;
            PrevSize = prevSize;
            RemovedItems = removedItems;
        }

        public int PrevSize { get; }
        public int NewSize { get; }
        public int DeltaSize { get; }
        public List<IItem> RemovedItems { get; }
    }

    public class ItemAddedEventArgs : EventArgs
    {
        public ItemAddedEventArgs(IItem? addedItems, IItem? rejectedItems, bool occupiedNewSlots, bool fitInInventory)
        {
            AddedItems = addedItems;
            RejectedItems = rejectedItems;
            OccupiedNewSlots = occupiedNewSlots;
            FitInInventory = fitInInventory;
        }

        public IItem? AddedItems { get; }
        public IItem? RejectedItems { get; }
        public bool OccupiedNewSlots { get; }
        public bool FitInInventory { get; }
    }

    public class ItemRemovedEventArgs : EventArgs
    {
        public ItemRemovedEventArgs(bool removed, IItem? removable)
        {
            Removed = removed;
            Removable = removable;
        }

        public IItem? Removable { get; }
        public bool Removed { get; }
    }

    public class FilterAppliedEventArgs : EventArgs
    {
        public FilterAppliedEventArgs(Func<IItem, bool> filter, IItem[] result)
        {
            Filter = filter;
            Result = result;
        }

        public Func<IItem, bool> Filter { get; }
        public IItem[] Result { get; }
    }

    public class ItemSwappedEventArgs : EventArgs
    {
        public ItemSwappedEventArgs(bool isAnythingRemoved, IItem? removedItem, IItem insertedItem)
        {
            IsAnythingRemoved = isAnythingRemoved;
            RemovedItem = removedItem;
            InsertedItem = insertedItem;
        }

        public bool IsAnythingRemoved { get; }
        public IItem? RemovedItem { get; }
        public IItem InsertedItem { get; }
    }

    protected virtual void OnInventorySizeChanged(InventorySizeChangedEventArgs args)
    {
        InventorySizeChanged?.Invoke(this, args);
    }

    protected virtual void OnItemInserted(ItemAddedEventArgs args)
    {
        ItemInserted?.Invoke(this, args);
    }

    protected virtual void OnItemRemoved(ItemRemovedEventArgs args)
    {
        ItemRemoved?.Invoke(this, args);
    }

    protected virtual void OnFilterApplied(FilterAppliedEventArgs args)
    {
        FilterApplied?.Invoke(this, args);
    }

    protected virtual void OnInventorySorted()
    {
        InventorySorted?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void OnItemSwapped(ItemSwappedEventArgs args)
    {
        ItemSwapped?.Invoke(this, args);
    }

    #endregion
}