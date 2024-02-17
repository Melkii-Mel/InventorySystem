using System.Numerics;
using InventorySystem.Interfaces;

namespace InventorySystem.InventoryComponents;

/// <summary>
///     Two-dimensional wrapper for an inventory that allows you to access cells by their row and column
/// </summary>
public class Inventory2D<TItem> where TItem : struct, IItem
{
    private readonly Inventory<TItem> _inventory;
    private int _sizeX;
    private int _sizeY;

    public Inventory2D(int sizeX, int sizeY, MultiComparator<TItem>? multiComparator = default)
    {
        _sizeX = sizeX;
        _sizeY = sizeY;
        _inventory = new Inventory<TItem>(sizeX * sizeY, multiComparator ?? MultiComparator<TItem>.CreateBlank());
        _inventory.FilterApplied += OnFilterApplied;
        _inventory.InventorySorted += OnInventorySorted;
        _inventory.InventorySizeChanged += OnInventorySizeChanged;
        _inventory.ItemSwapped += OnItemSwapped;
        _inventory.ItemRemoved += OnItemRemoved;
        _inventory.ItemInserted += OnItemInserted;
    }

    public TItem?[][] Items
    {
        set => _inventory.Items = ConvertTo1D(value);
        get => ConvertTo2D(_inventory.Items, _sizeX);
    }

    public MultiComparator<TItem> Comparator
    {
        get => _inventory.Comparator;
        set => _inventory.Comparator = value;
    }

    public bool IsFull => _inventory.IsFull;
    public int EmptySlotsAmount => _inventory.EmptySlotsAmount;

    public List<Vector2> EmptySlots
    {
        get
        {
            var emptyCells = new List<Vector2>();
            var index = 0;
            var emptySlots = _inventory.EmptySlots;
            for (var i = 0; i < _sizeY; i++)
            for (var j = 0; j < _sizeX; j++)
            {
                if (emptySlots.Contains(index)) emptyCells.Add(new Vector2(i, j));
                index++;
            }

            return emptyCells;
        }
    }

    public List<TItem> ChangeSize(int x, int y)
    {
        var result = new List<TItem>();
        result.AddRange(_inventory.ChangeSize(x * _sizeY));
        _sizeY = y;
        result.AddRange(_inventory.ChangeSize(y * _sizeX));
        _sizeX = x;
        return result;
    }

    private Vector2 ConvertTo2D(int index)
    {
        return ConvertTo2D(index, _sizeX);
    }

    private T[][] ConvertTo2D<T>(T[] objs, int sizeX)
    {
        var index = 0;
        var lastVector = Vector2.Zero;
        var result = new T[objs.Length / sizeX + (objs.Length % sizeX == 0 ? 0 : 1)][];
        foreach (var obj in objs)
        {
            var vec2 = ConvertTo2D(index, sizeX);
            if ((int) lastVector.Y != (int) vec2.Y) result[(int) vec2.X] = new T[sizeX];
            result[(int) vec2.Y][(int) vec2.X] = obj;
            index++;
            lastVector = vec2;
        }

        return result;
    }

    private T[] ConvertTo1D<T>(T[][] objss)
    {
        var result = new List<T>();
        foreach (var objs in objss)
        foreach (var obj in objs)
            result.Add(obj);

        return result.ToArray();
    }

    private Vector2 ConvertTo2D(int index, int sizeX)
    {
        var x = index % sizeX;
        var y = index / sizeX;
        return new Vector2(x, y);
    }

    private int ConvertToIndex(Vector2 coordinates)
    {
        return ConvertToIndex(coordinates, _sizeX);
    }

    private int ConvertToIndex(Vector2 coordinates, int sizeX)
    {
        return (int) (coordinates.Y * sizeX + coordinates.X);
    }

    public Inventory<TItem>.InsertionInfo<TItem> InsertItem(TItem item)
    {
        return _inventory.InsertItem(item);
    }

    public Inventory<TItem>.InsertionInfo<TItem> InsertItem(TItem item, Vector2 cell)
    {
        return _inventory.InsertItem(item, ConvertToIndex(cell));
    }

    public TItem? SwapItems(TItem insertable, Vector2 slot)
    {
        return _inventory.SwapItems(insertable, ConvertToIndex(slot));
    }

    public bool TryTakeItems(params TItem[] removable)
    {
        return _inventory.TryTakeItems(removable);
    }

    public IItem? TryTakeItem(Vector2 position)
    {
        return _inventory.TryTakeItem(ConvertToIndex(position));
    }

    public void SortItems()
    {
        _inventory.SortItems();
    }

    public void SortItems(string comparatorName)
    {
        _inventory.SortItems(comparatorName);
    }

    public TItem[] FilterItems(Func<TItem, bool> predicate)
    {
        return _inventory.FilterItems(predicate);
    }

    public TItem[] GetItems(IItemType type)
    {
        return _inventory.GetItems(type);
    }

    #region Events

    public event EventHandler? InventorySizeChanged;
    public event EventHandler? ItemInserted;
    public event EventHandler? ItemRemoved;
    public event EventHandler? FilterApplied;
    public event EventHandler? InventorySorted;
    public event EventHandler? ItemSwapped;

    protected virtual void OnInventorySizeChanged(object sender, EventArgs args)
    {
        InventorySizeChanged?.Invoke(this, args);
    }

    protected virtual void OnItemInserted(object sender, EventArgs args)
    {
        ItemInserted?.Invoke(this, args);
    }

    protected virtual void OnItemRemoved(object sender, EventArgs args)
    {
        ItemRemoved?.Invoke(this, args);
    }

    protected virtual void OnFilterApplied(object sender, EventArgs args)
    {
        FilterApplied?.Invoke(this, args);
    }

    protected virtual void OnInventorySorted(object sender, EventArgs args)
    {
        InventorySorted?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void OnItemSwapped(object sender, EventArgs args)
    {
        ItemSwapped?.Invoke(this, args);
    }

    #endregion
}