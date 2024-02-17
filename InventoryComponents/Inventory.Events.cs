// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global

using InventorySystem.Interfaces;

namespace InventorySystem.InventoryComponents;

public partial class Inventory<TItem> where TItem : struct, IItem
{
    #region Events

    public event EventHandler? InventorySizeChanged;
    public event EventHandler? ItemInserted;
    public event EventHandler? ItemRemoved;
    public event EventHandler? FilterApplied;
    public event EventHandler? InventorySorted;
    public event EventHandler? ItemSwapped;

    #endregion

    #region EventHandlers

    public class InventorySizeChangedEventArgs : EventArgs
    {
        public InventorySizeChangedEventArgs(int deltaSize, int newSize, int prevSize, List<TItem> removedItems)
        {
            DeltaSize = deltaSize;
            NewSize = newSize;
            PrevSize = prevSize;
            RemovedItems = removedItems;
        }

        public int PrevSize { get; }
        public int NewSize { get; }
        public int DeltaSize { get; }
        public List<TItem> RemovedItems { get; }
    }

    public class ItemAddedEventArgs : EventArgs
    {
        public ItemAddedEventArgs(TItem? addedItems, TItem? rejectedItems, bool occupiedNewSlots, bool fitInInventory)
        {
            AddedItems = addedItems;
            RejectedItems = rejectedItems;
            OccupiedNewSlots = occupiedNewSlots;
            FitInInventory = fitInInventory;
        }

        public TItem? AddedItems { get; }
        public TItem? RejectedItems { get; }
        public bool OccupiedNewSlots { get; }
        public bool FitInInventory { get; }
    }

    public class ItemRemovedEventArgs : EventArgs
    {
        public ItemRemovedEventArgs(bool removed, TItem? removable)
        {
            Removed = removed;
            Removable = removable;
        }

        public TItem? Removable { get; }
        public bool Removed { get; }
    }

    public class FilterAppliedEventArgs : EventArgs
    {
        public FilterAppliedEventArgs(Func<TItem, bool> filter, TItem[] result)
        {
            Filter = filter;
            Result = result;
        }

        public Func<TItem, bool> Filter { get; }
        public TItem[] Result { get; }
    }

    public class ItemSwappedEventArgs : EventArgs
    {
        public ItemSwappedEventArgs(bool isAnythingRemoved, TItem? removedItem, TItem insertedItemType)
        {
            IsAnythingRemoved = isAnythingRemoved;
            RemovedItem = removedItem;
            InsertedItemType = insertedItemType;
        }

        public bool IsAnythingRemoved { get; }
        public TItem? RemovedItem { get; }
        public TItem InsertedItemType { get; }
    }

    #endregion
}