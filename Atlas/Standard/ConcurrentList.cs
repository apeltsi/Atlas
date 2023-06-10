using System.Collections;
using System.Collections.Concurrent;

namespace SolidCode.Atlas.Standard
{
    public class ManualConcurrentList<T> : ICollection<T> where T : IComparable<T>
    {
        private List<T> list = new List<T>();
        private ConcurrentBag<T> toRemove = new ConcurrentBag<T>();
        private ConcurrentBag<T> toAdd = new ConcurrentBag<T>();

        public int Count
        {
            get
            {
                lock(list) lock(toAdd) lock (toRemove)
                    return list.Count + toAdd.Count - toRemove.Count;
            }
        }


        public bool IsReadOnly => false;

        public void Update()
        {
            list.AddRange(toAdd);
            toAdd.Clear();
            T[] remove = toRemove.ToArray();
            toRemove.Clear();
            foreach (T item in remove)
            {
                list.Remove(item);
            }
        }

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
        
        public void AddSorted(T item)
        {
            lock (this)
            {
                Update();
                if (this.Count == 0)
                {
                    this.list.Add(item);
                    return;
                }
                if (this.list[^1].CompareTo(item) <= 0)
                {
                    this.Add(item);
                    return;
                }
                if (this.list[0].CompareTo(item) >= 0)
                {
                    this.list.Insert(0, item);
                    return;
                }
                int index = this.list.BinarySearch(item);
                if (index < 0)
                    index = ~index;
                this.list.Insert(index, item);
            }
        }

    }
}