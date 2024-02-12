// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace InventorySystem;

public partial class Inventory
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