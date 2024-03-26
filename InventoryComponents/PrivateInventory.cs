using InventorySystem.Interfaces;

namespace InventorySystem.InventoryComponents;

public partial class Inventory
{
    private IItem?[] _items;

    #region Helpers

    private IEnumerable<IItem> NotNullItems(bool backwards = false)
    {
        if (backwards)
            for (var index = Items.Length - 1; index >= 0; index--)
            {
                var item = Items[index];
                if (item is null) continue;
                yield return item;
            }
        else
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

    private bool AreSameItems(IItem? item0, IItem? item1)
    {
        return item0 != null && item1 != null && item0.Type.Id == item1.Type.Id;
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
            if (item != null && item.Type.Id == index) return i;
        }

        return -1;
    }

    private IItem AddIItemStackAmount(IItem item, int deltaValue)
    {
        var itemStack = (IItemStack) item;
        itemStack.Amount += deltaValue;
        return (IItem) itemStack;
    }

    private InsertionInfo AddStack(IItemStack addableStack)
    {

        InsertionInfo insertion = new()
        {
            FitInOccupiedSlots = true
        };
        var totalAmount = addableStack.Amount;

        void SetInsertionInfo()
        {
            insertion.FitInInventory = addableStack.Amount == 0;
            if (totalAmount - addableStack.Amount != 0) 
                insertion.InsertedItems = new() { new(addableStack.Type, totalAmount - addableStack.Amount) };
            if (addableStack.Amount != 0)
                insertion.RejectedItems = new() { new(addableStack.Type, addableStack.Amount) };
        }

        for (var i = 0; i < _items.Length; i++)
        {
            var item = _items[i];
            if (!AreSameItems(item, addableStack)) continue;
            IItemStack itemStack = (IItemStack)item!;
            itemStack.Stack(addableStack);
            if (addableStack.Amount == 0)
            {
                SetInsertionInfo();
                return insertion;
            }
        }

        insertion.FitInOccupiedSlots = false;

        if (IsFull)
        {
            SetInsertionInfo();
            return insertion;
        }

        while (addableStack.Amount > 0 && !IsFull)
        {
            IItemStack newStack = addableStack.CreateEmptyClone();
            var deltaAmount = Math.Min(addableStack.Type.MaxStackSize, addableStack.Amount);
            newStack.Amount = deltaAmount;
            addableStack.Amount -= deltaAmount;
            Items[FindFirstEmptySlot()] = newStack;
        }

        SetInsertionInfo();
        return insertion;
    }

    #endregion

    #region EventHandlers

    protected virtual void OnInventorySizeChanged(InventorySizeChangedEventArgs args)
    {
        InventorySizeChanged?.Invoke(this, args);
    }
    
    protected virtual void OnItemInserted(InsertionInfo info)
    {
        ItemInserted?.Invoke(this, new ItemAddedEventArgs(info));
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