// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global

using InventorySystem.Interfaces;

namespace InventorySystem.InventoryComponents;

public partial class Inventory
{
    #region Events

    public event EventHandler<InventorySizeChangedEventArgs>? InventorySizeChanged;
    public event EventHandler<ItemAddedEventArgs>? ItemInserted;
    public event EventHandler<ItemRemovedEventArgs>? ItemRemoved;
    public event EventHandler<FilterAppliedEventArgs>? FilterApplied;
    public event EventHandler? InventorySorted;
    public event EventHandler<ItemSwappedEventArgs>? ItemSwapped;

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
        public ItemAddedEventArgs(InsertionInfo insertion)
        {
            InsertionInfo = insertion;
        }

        public InsertionInfo InsertionInfo;
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
        public ItemSwappedEventArgs(bool isAnythingRemoved, IItem? removedItem, IItem insertedItemType)
        {
            IsAnythingRemoved = isAnythingRemoved;
            RemovedItem = removedItem;
            InsertedItemType = insertedItemType;
        }

        public bool IsAnythingRemoved { get; }
        public IItem? RemovedItem { get; }
        public IItem InsertedItemType { get; }
    }

    #endregion
}