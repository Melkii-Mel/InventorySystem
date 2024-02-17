using InventorySystem.Interfaces;

namespace InventorySystem.InventoryComponents;

public partial class Inventory<TItem> where TItem : struct, IItem
{
    private TItem?[] _items;

    #region Helpers

    private IEnumerable<TItem> NotNullItems(bool backwards = false)
    {
        if (backwards)
            for (var index = Items.Length - 1; index >= 0; index--)
            {
                var item = Items[index];
                if (item is null) continue;
                yield return item.Value;
            }
        else
            foreach (var item in Items)
            {
                if (item is null) continue;
                yield return item.Value;
            }
    }

    private int FindFirstEmptySlot()
    {
        for (var i = 0; i < Items.Length; i++)
            if (Items[i] is null)
                return i;

        return -1;
    }

    private bool AreSameItems(TItem item0, TItem item1)
    {
        return item0.Type.Id == item1.Type.Id;
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

    private int FindItem(int index, bool backwards = false)
    {
        for (var i = backwards ? Items.Length - 1 : 0; backwards ? i > -1 : i < _items.Length; i += backwards ? -1 : 1)
        {
            var item = Items[i];
            if (item != null && item.Value.Type.Id == index) return i;
        }

        return -1;
    }

    private TItem AddIItemStackAmount(TItem item, int deltaValue)
    {
        var itemStack = (IItemStack) item;
        itemStack.Amount += deltaValue;
        return (TItem) itemStack;
    }

    private void SetIItemStackAmount(ref TItem item, int value)
    {
        var itemStack = (IItemStack) item;
        itemStack.Amount = value;
        item = (TItem) itemStack;
    }

    #endregion

    #region EventHandlers

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