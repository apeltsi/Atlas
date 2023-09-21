using System.Collections;
using System.Collections.Concurrent;

namespace SolidCode.Atlas.Standard;

public class ManualConcurrentList<T> : ICollection<T> where T : IComparable<T>
{
    private readonly List<T> list = new();
    private readonly ConcurrentBag<T> toAdd = new();
    private readonly ConcurrentBag<T> toRemove = new();

    public int Count
    {
        get
        {
            lock (list)
            lock (toAdd)
            lock (toRemove)
            {
                return list.Count + toAdd.Count - toRemove.Count;
            }
        }
    }


    public bool IsReadOnly => false;

    public void Add(T item)
    {
        toAdd.Add(item);
    }

    public void Clear()
    {
        list.Clear();
        toAdd.Clear();
        toRemove.Clear();
    }

    public bool Contains(T item)
    {
        return (list.Contains(item) || toAdd.Contains(item)) && !toRemove.Contains(item);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        list.CopyTo(array, arrayIndex);
    }

    public bool Remove(T item)
    {
        toRemove.Add(item);
        return true;
    }

    public IEnumerator<T> GetEnumerator()
    {
        return list.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return list.GetEnumerator();
    }

    public void Update()
    {
        list.AddRange(toAdd);
        toAdd.Clear();
        var remove = toRemove.ToArray();
        toRemove.Clear();
        foreach (var item in remove) list.Remove(item);
    }

    public void AddSorted(T item)
    {
        lock (this)
        {
            Update();
            if (Count == 0)
            {
                list.Add(item);
                return;
            }

            if (list[^1].CompareTo(item) <= 0)
            {
                Add(item);
                return;
            }

            if (list[0].CompareTo(item) >= 0)
            {
                list.Insert(0, item);
                return;
            }

            var index = list.BinarySearch(item);
            if (index < 0)
                index = ~index;
            list.Insert(index, item);
        }
    }
}