// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global

using InventorySystem.Interfaces;

namespace InventorySystem.InventoryComponents;

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
        _items = new IItem?[size];
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
            Items[i] = null;
        }

        Array.Resize(ref _items, Items.Length + deltaSize);
        OnInventorySizeChanged(
            new InventorySizeChangedEventArgs(deltaSize, Items.Length, Items.Length - deltaSize, removedItems));
        return removedItems;
    }

    /// <summary>
    ///     Inserts an item into an inventory.
    /// </summary>
    /// <param name="addables">Items to be added to the inventory.</param>
    /// <returns>An InsertionInfo object containing information about the insertion operation.</returns>
    public InsertionInfo InsertItems(params IItem[] addables)
    {
        var insertionInfo = new InsertionInfo
        {
            FitInInventory = true,
            FitInOccupiedSlots = true,
            InsertedItems = new()
        };

        int addableStacksCounter = 0;
        foreach (IItemStack addableStack in addables.Cast<IItemStack>())
        {
            addableStacksCounter++;
            InsertionInfo info = AddStack(addableStack);
            insertionInfo.Merge(info);
        }

        if (addableStacksCounter == addables.Length)
        {
            OnItemInserted(insertionInfo);
            return insertionInfo;
        }

        insertionInfo.FitInOccupiedSlots = false;

        foreach (IItem addable in addables)
        {
            if (addable is IItemStack) continue;
            if (IsFull)
            {
                insertionInfo.RejectedItems.Add(new ItemInfo(addable));
                continue;
            }
            Items[FindFirstEmptySlot()] = addable;
            insertionInfo.InsertedItems.Add(new ItemInfo(addable));
        }

        OnItemInserted(insertionInfo);
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
    public InsertionInfo InsertItem(IItem addable, int index = -1)
    {
        if (index == -1) return InsertItems(addable);

        var insertionInfo = new InsertionInfo();

        var item = Items[index];
        if (item is null)
        {
            insertionInfo.FitInInventory = true;
            insertionInfo.FitInOccupiedSlots = false;
            Items[index] = addable;
            insertionInfo.InsertedItems = new() { new ItemInfo(addable) };
            OnItemInserted(insertionInfo);
            return insertionInfo;
        }

        if (addable is not IItemStack addableStack || !AreSameItems(addable, item))
        {
            insertionInfo.FitInOccupiedSlots = false;
            insertionInfo.FitInInventory = false;
            insertionInfo.RejectedItems = new() { new ItemInfo(addable) };
            OnItemInserted(insertionInfo);
            return insertionInfo;
        }

        var itemStack = (IItemStack) item;
        var initialAmount = addableStack.Amount;
        itemStack.Stack(addableStack);
        if (itemStack.Amount < initialAmount)
        {
            insertionInfo.InsertedItems.Add(new ItemInfo(addableStack.Type, initialAmount - itemStack.Amount));
        }
        if (itemStack.Amount > 0)
        { 
            insertionInfo.RejectedItems.Add(new ItemInfo(addableStack.Type, itemStack.Amount));
        }
        insertionInfo.FitInInventory = addableStack.Amount == 0;
        insertionInfo.FitInOccupiedSlots = addableStack.Amount == 0;
        OnItemInserted(insertionInfo);
        return insertionInfo;
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
    ///     Tries to remove an item from the inventory. Items won't be removed if there are not enough of them
    /// </summary>
    /// <param name="takeables">Items to be removed from the inventory</param>
    /// <returns>True if the item was successfully removed; otherwise, false.</returns>
    public bool TryTakeItems(params IItem[] takeables)
    {
        if (!IsEnoughItems(takeables)) return false;

        IItem?[] takeablesSingles =
            takeables.Where(item => item is not IItemStack && item is not null).ToArray();
        var takeableStacks = takeables.OfType<IItemStack>().ToArray();
        for (int i = _items.Length - 1; i > -1; i--)
        {
            var currentInventoryItem = _items[i];
            if (currentInventoryItem is null) continue;
            if (currentInventoryItem is IItemStack itemStack)
                foreach (var takeableStack in takeableStacks)
                {
                    if (!AreSameItems(takeableStack, currentInventoryItem)) continue;
                    var deltaAmount = Math.Min(takeableStack.Amount, itemStack.Amount);
                    takeableStack.Amount -= deltaAmount;
                    itemStack.Amount -= deltaAmount;
                    if (itemStack.Amount == 0)
                    {
                        _items[i] = null;
                        break;
                    }

                    _items[i] = itemStack;
                }
            else
                for (int index = 0; index < takeablesSingles.Length; index++)
                {
                    if (!AreSameItems(takeableStacks[index], currentInventoryItem)) continue;
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
    public IItem[] GetItems(IItemType type)
    {
        return Items.Where(item => item != null && item.Type == type).ToArray()!;
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

    public bool IsEnoughItems(IItem[] takeableItems)
    {
        var takeablesAmounts = new int[takeableItems.Length];
        
        for (var i = 0; i < takeableItems.Length; i++)
        {
            var takeable = takeableItems[i];
            takeablesAmounts[i] = takeable is IItemStack takeableStack ? takeableStack.Amount : 1;
        }

        foreach (IItem item in NotNullItems())
        {
            if (item is IItemStack itemStack)
            {
                int itemStackAmount = itemStack.Amount;
                for (int i = 0; i < takeableItems.Length; i++)
                {
                    if (takeableItems[i] is not IItemStack takeableItemStack) continue;
                    if (!AreSameItems(takeableItemStack, itemStack)) continue;
                    int deltaAmount = Math.Min(takeablesAmounts[i], itemStackAmount);
                    takeablesAmounts[i] -= deltaAmount;
                    itemStackAmount -= deltaAmount;

                    if (itemStackAmount == 0) break;
                }
            }

            else
            {
                for (int i = 0; i < takeableItems.Length; i++)
                {
                    if (takeableItems[i] is IItemStack) continue;
                    IItem takeableItem = takeableItems[i];
                    if (!AreSameItems(takeableItem, item)) continue;
                    takeablesAmounts[i] = 0;
                    break;
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
    public IItem[] FilterItems(Func<IItem, bool> predicate)
    {
        IItem[] result = Items.Where(item => item != null && predicate(item)).ToArray()!;
        OnFilterApplied(new FilterAppliedEventArgs(predicate, result));
        return result;
    }

    /// <summary>
    ///     Being generated by AddItem method, provides data about operation result
    /// </summary>
    public struct InsertionInfo
    {
        /// <summary>
        ///     Item that has been inserted
        /// </summary>
        public List<ItemInfo> InsertedItems { get; internal set; } = new();

        /// <summary>
        ///     Item that has not been inserted due to lack of space
        /// </summary>
        public List<ItemInfo> RejectedItems { get; internal set; } = new();

        /// <summary>
        ///     True if item was successfully inserted in the inventory
        /// </summary>
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public bool FitInInventory { get; internal set; } = false;

        /// <summary>
        ///     True if item didn't occupy any additional slot
        /// </summary>
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public bool FitInOccupiedSlots { get; internal set; } = false;

        public InsertionInfo()
        {
            
        }

        public void Merge(InsertionInfo other)
        {
            static void MergeItems(List<ItemInfo> from, List<ItemInfo> to)
            {
                foreach (var fromItem in from)
                {
                    bool inserted = false;
                    foreach (var item in to)
                    {
                        if (item.Type == fromItem.Type)
                        {
                            item.Amount += fromItem.Amount;
                            inserted = true;
                            break;
                        }
                    }
                    if (inserted) continue;
                    to.Add(fromItem);
                }
            }
            MergeItems(other.InsertedItems, InsertedItems);
            MergeItems(other.RejectedItems, RejectedItems);
            FitInInventory = FitInInventory && other.FitInInventory;
            FitInOccupiedSlots = FitInOccupiedSlots && other.FitInOccupiedSlots;
        }
    }
}