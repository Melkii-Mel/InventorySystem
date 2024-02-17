// ReSharper disable MemberCanBePrivate.Global

using InventorySystem.Interfaces;

namespace InventorySystem;

public class MultiComparator<TItem> : IComparer<TItem?> where TItem : struct, IItem
{
    private string _currentComparator;

    public MultiComparator(Dictionary<string, Func<TItem, TItem, int>> comparators, string initialComparator)
    {
        Comparators = comparators;
        _currentComparator = initialComparator;
    }

    public string CurrentComparator
    {
        get => _currentComparator;
        set
        {
            if (!Comparators.Keys.ToArray()[0].Contains(_currentComparator))
                throw new ArgumentException($"Comparators don't contain a comparator of name {value}");
            _currentComparator = value;
        }
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public Dictionary<string, Func<TItem, TItem, int>> Comparators { get; }

    public int Compare(TItem? x, TItem? y)
    {
        if (x == null && y == null) return 0;
        if (x == null) return -1;
        return y == null ? 1 : Comparators[CurrentComparator](x.Value, y.Value);
    }

    public static MultiComparator<TItem> CreateBlank()
    {
        return new MultiComparator<TItem>(new Dictionary<string, Func<TItem, TItem, int>>(), string.Empty);
    }
}