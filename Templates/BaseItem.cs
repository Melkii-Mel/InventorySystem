using InventorySystem.Interfaces;

namespace InventorySystem.Templates;

[Serializable]
public struct BaseItem : IItem
{
    public IItemType Type { get; }

    private List<IProperty> _properties = new();

    public T? GetProperty<T>() where T : IProperty
    {
        foreach (var property in _properties)
            if (property is T tProperty)
                return tProperty;

        return default;
    }

    public void AddProperty(IProperty property)
    {
        _properties.Add(property);
    }

    public bool HasProperty<T>() where T : IProperty
    {
        return GetProperty<T>() is not null;
    }

    public void RemoveProperty(IProperty property)
    {
        _properties.Remove(property);
    }

    public BaseItem(IItemType type)
    {
        Type = type;
    }
}