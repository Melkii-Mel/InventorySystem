// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global

namespace InventorySystem;

/// <summary>
///     creates an inventory that can store items represented as an implementors of IItem
///     Supports methods for adding or removing elements, as well as non-stackable items and stackable items
///     Stackable items represented as IItemStack inheritors
/// </summary>
[Serializable]
public partial class Inventory<TItem> where TItem : struct, IItem
{
    /// <summary>
    ///     Initializes a new instance of the Inventory class with the specified size.
    /// </summary>
    /// <param name="size">The size of the inventory.</param>
    /// <param name="comparator">
    ///     A MultiComparator object used for sorting items in the inventory.
    ///     If sorting is not required, use MultiComparator.CreateBlank().
    /// </param>
    public Inventory(int size, MultiComparator<TItem> comparator)
    {
        Comparator = comparator;
        _items = new TItem?[size];
    }

    /// <summary>
    ///     All items containing in inventory
    ///     Manually adding or removing them is not recommended, use AddItem or TakeItem instead
    /// </summary>
    public TItem?[] Items
    {
        get => _items;
        set => _items = value;
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public MultiComparator<TItem> Comparator { get; set; }

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
    public List<TItem> ChangeSize(int deltaSize)
    {
        if (deltaSize + Items.Length < 0)
            throw new ArgumentException("Value is too low. deltaSize can't be less than -Items.Length");
        List<TItem> removedItems = new();
        for (var i = Items.Length - deltaSize; i < Items.Length; i++)
        {
            if (Items[i] is null) continue;
            removedItems.Add(Items[i]!.Value);
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
    /// <returns>An InsertionInfo object containing information about the insertion operation.</returns>
    public InsertionInfo<TItem> InsertItem(TItem addable)
    {
        var insertionInfo = new InsertionInfo<TItem>
        {
            FitInInventory = true,
            FitInOccupiedSlots = true,
            InsertedItem = addable
        };

        if (addable is IItemStack addableStack)
        {
            var totalAmount = addableStack.Amount;
            for (var i = 0; i < _items.Length; i++)
            {
                var item = _items[i];
                if (item is null) continue;
                if (item.Value.Type != addableStack.Type) continue;
                var itemStack = (IItemStack) item.Value;
                addableStack = itemStack.Stack(addableStack);
                _items[i] = (TItem?) itemStack;
                if (addableStack.Amount == 0) break;
            }

            if (addableStack.Amount == 0)
            {
                insertionInfo.FitInOccupiedSlots = true;
                OnItemInserted(new ItemAddedEventArgs(addable, null, !insertionInfo.FitInOccupiedSlots, true));
                return insertionInfo;
            }

            insertionInfo.FitInOccupiedSlots = false;

            if (IsFull)
            {
                insertionInfo.FitInInventory = false;
                var insertedItemsStack = (IItemStack) addable;
                insertedItemsStack.Amount = totalAmount - addableStack.Amount;
                OnItemInserted(new ItemAddedEventArgs((TItem) insertedItemsStack, (TItem) addableStack,
                    !insertionInfo.FitInOccupiedSlots,
                    true));
                return insertionInfo;
            }

            while (addableStack.Amount > 0)
            {
                var newAddable = (TItem) addableStack;
                var deltaAmount = Math.Min(addableStack.Type.MaxStackSize, addableStack.Amount);
                SetIItemStackAmount(ref newAddable, deltaAmount);
                addableStack.Amount -= deltaAmount;
                Items[FindFirstEmptySlot()] = newAddable;
                if (IsFull) break;
            }

            var itemsInserted = (TItem) addableStack;
            SetIItemStackAmount(ref itemsInserted, totalAmount - addableStack.Amount);
            OnItemInserted(new ItemAddedEventArgs(itemsInserted, (TItem) addableStack,
                !insertionInfo.FitInOccupiedSlots,
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
    /// <returns>
    ///     An InsertionInfo object containing information about the insertion operation
    /// </returns>
    public InsertionInfo<TItem> InsertItem(TItem addable, int index)
    {
        var insertionInfo = new InsertionInfo<TItem>
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

        if (!AreSameItems(addable, item.Value))
        {
            insertionInfo.FitInOccupiedSlots = false;
            insertionInfo.FitInInventory = false;
            OnItemInserted(new ItemAddedEventArgs(null, addable, false, false));
            return insertionInfo;
        }

        var itemStack = (IItemStack) item.Value;
        var initialAmount = addableStack.Amount;
        addableStack = itemStack.Stack(addableStack);
        _items[index] = (TItem) itemStack;
        var insertedStack = addableStack;
        insertedStack.Amount = initialAmount - addableStack.Amount;
        OnItemInserted(new ItemAddedEventArgs((TItem) insertedStack, (TItem) addableStack, false,
            addableStack.Amount == 0));
        return new InsertionInfo<TItem>
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
    public TItem? SwapItems(TItem insertable, int slot)
    {
        var removable = Items[slot];
        Items[slot] = insertable;
        OnItemSwapped(new ItemSwappedEventArgs(removable is null, removable, insertable));
        return removable;
    }

    /// <summary>
    ///     Tries to remove an item from the inventory. Items won't be removed if there are not enough of them
    /// </summary>
    /// <param name="takeables">Items to be removed from the inventory</param>
    /// <returns>True if the item was successfully removed; otherwise, false.</returns>
    public bool TryTakeItems(params TItem[] takeables)
    {
        if (!IsEnoughItems(takeables)) return false;

        IItem?[] takeablesSingles = takeables.Where(item => item is not IItemStack).Select(item => item as IItem).ToArray();
        var takeableStacks = takeables.OfType<IItemStack>().ToArray();
        for (var i = _items.Length - 1; i > -1; i--)
        {
            var item = _items[i];
            if (item is null) continue;
            if (item is IItemStack itemStack)
                foreach (var takeableStack in takeableStacks)
                {
                    if (!AreSameItems((TItem) takeableStack, (TItem) item)) continue;
                    var deltaAmount = Math.Min(takeableStack.Amount, itemStack.Amount);
                    takeableStack.Amount -= deltaAmount;
                    itemStack.Amount -= deltaAmount;
                    if (itemStack.Amount == 0)
                    {
                        _items[i] = null;
                        break;
                    }

                    _items[i] = (TItem) itemStack;
                }
            else
                for (var index = 0; index < takeablesSingles.Length; index++)
                {
                    if (!AreSameItems((TItem) takeableStacks[index], (TItem) item)) continue;
                    takeablesSingles[index] = null;
                    _items[i] = null;
                    break;
                }
        }

        return true;
    }

    /// <summary>
    ///     returns all items with the specified Item Type
    /// </summary>
    /// <param name="type">Type of receivable items</param>
    public TItem[] GetItems(IItemType type)
    {
        return Items.Where(item => item.HasValue && item.Value.Type == type).Select(item => item!.Value).ToArray();
    }

    /// <summary>
    ///     returns amount of items in the inventory
    ///     With IItemStack, returns amount of separated stack
    ///     If you want to find the exact amount of items in stacks, use GetStackableItemsAmount
    /// </summary>
    /// <param name="type">Type of items you want to count</param>
    public int GetItemsAmount(IItemType type)
    {
        return GetItems(type).Length;
    }

    public bool IsEnoughItems(TItem[] initialTakeableItems)
    {
        var notNullItems = NotNullItems().ToArray();
        var takeableItems = (TItem[])initialTakeableItems.Clone();
        var takeablesAmounts = new int[takeableItems.Length];
        for (var i = 0; i < takeableItems.Length; i++)
        {
            var takeable = takeableItems[i];
            takeablesAmounts[i] = takeable is IItemStack takeableStack ? takeableStack.Amount : 1;
        }

        for (var notNullItemIndex = 0; notNullItemIndex < notNullItems.Length; notNullItemIndex++)
        {
            var notNullItem = notNullItems[notNullItemIndex];
            for (var takeableAmountIndex = 0; takeableAmountIndex < takeablesAmounts.Length; takeableAmountIndex++)
            {
                var takeable = takeableItems[takeableAmountIndex];
                if (takeable is IItemStack takeableStack)
                {
                    if (!AreSameItems(notNullItem, takeableItems[takeableAmountIndex])) continue;
                    var itemStack = (notNullItem as IItemStack)!;
                    var deltaAmount = Math.Min(itemStack.Amount, takeableStack.Amount);
                    takeablesAmounts[takeableAmountIndex] -= deltaAmount;
                    takeableStack.Amount -= deltaAmount;
                    takeableItems[takeableAmountIndex] = (TItem) takeableStack;
                    (notNullItem as IItemStack)!.Amount -= deltaAmount;
                    notNullItems[notNullItemIndex] = notNullItem;
                    if ((notNullItem as IItemStack)!.Amount == 0) break;
                }
            }
        }

        return takeablesAmounts.Sum() == 0;
    }

    /// <summary>
    ///     Returns amount of items of the specified type in all stacks
    /// </summary>
    /// <param name="type">Type of items you want to count</param>
    public int GetStackableItemsAmount(IItemStackType type)
    {
        return GetItems(type).Select(item => ((IItemStack) item).Amount).Sum();
    }

    /// <summary>
    ///     Removes an item from the inventory if there is any, then returns it
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
        Array.Sort(_items, Comparator);
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
    public TItem[] FilterItems(Func<TItem, bool> predicate)
    {
        var result = Items.Where(item => item != null && predicate(item.Value)).Select(item => item!.Value).ToArray();
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