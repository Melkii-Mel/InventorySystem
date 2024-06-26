﻿// ReSharper disable MemberCanBePrivate.Global

using InventorySystem.Interfaces;

namespace InventorySystem;

public class MultiComparator : IComparer<IItem?>
{
    private string _currentComparator;

    public MultiComparator(Dictionary<string, Func<IItem, IItem, int>> comparators, string initialComparator)
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
    public Dictionary<string, Func<IItem, IItem, int>> Comparators { get; }

    public int Compare(IItem? x, IItem? y)
    {
        if (x == null && y == null) return 0;
        if (x == null) return -1;
        return y == null ? 1 : Comparators[CurrentComparator](x, y);
    }

    public static MultiComparator CreateBlank()
    {
        return new MultiComparator(new(), string.Empty);
    }
}