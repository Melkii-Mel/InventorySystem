namespace InventorySystem;

[Serializable]
public struct BaseItem : IItem
{
    public IItemType Type { get; }

    public BaseItem(IItemType type)
    {
        Type = type;
    }
}