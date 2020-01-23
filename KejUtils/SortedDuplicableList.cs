using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KejUtils
{
    /// <summary>
    /// C#'s sorted lists don't allow duplicates. This is stupid. This class is a sorted list that allows duplicates (and
    /// some other small differences).
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SortedDuplicableList<T> : IEnumerable<T>
    {
        private List<T> backingList = new List<T>();
        private readonly IComparer<T> comparer;

        public enum AddAlignment
        {
            Start,
            End,
            Anywhere,
        }

        public SortedDuplicableList(IComparer<T> comparer, AddAlignment defaultAlignment=AddAlignment.Anywhere)
        {
            if (comparer == null) throw new ArgumentNullException("comparer");
            this.comparer = comparer;
            DefaultAlignment = defaultAlignment;
        }


        public AddAlignment DefaultAlignment { get; set; }

        public static int BinarySearch(SortedDuplicableList<T> list, T value, int start = 0, int end = -1)
        {
            return BinarySearch(list.backingList, value, list.comparer, start, end);
        }

        private static int BinarySearch(List<T> list, T value, IComparer<T> comparer, int start=0, int end=-1)
        {
            if (end == -1) end = list.Count - 1;
            while (end >= start)
            {
                int mid = (end + start) / 2;
                int compare = comparer.Compare(value, list[mid]);
                if (compare == 0)
                {
                    return mid;
                }
                if (compare < 0)
                {
                    end = mid - 1;
                }
                else
                {
                    start = mid + 1;
                }
            }
            return -start - 1;
        }

        public void Add(T item)
        {
            Add(item, DefaultAlignment);
        }

        public void Add(T item, AddAlignment align)
        {
            IComparer<T> thisComparer = comparer;
            int index = BinarySearch(backingList, item, thisComparer);
            if (index < 0)
            {
                backingList.Insert(-index - 1, item);
                return;
            }
            switch(align)
            {
                case AddAlignment.Anywhere:
                    break;
                case AddAlignment.Start:
                    while (index > 0  && thisComparer.Compare(item, backingList[index - 1]) == 0) index--;
                    break;
                case AddAlignment.End:
                    int count = backingList.Count;
                    index++;
                    while (index < count && thisComparer.Compare(item, backingList[index]) == 0) index++;
                    break;
            }
            backingList.Insert(index, item);
            return;
        }
        public void RemoveAt(int i)
        {
            backingList.RemoveAt(i);
        }
        public bool Remove(T oldElement)
        {
            return backingList.Remove(oldElement);
        }
        public T this[int index]
        {
            get
            {
                return backingList[index];
            }
        }
        public int Count
        {
            get
            {
                return backingList.Count;
            }
        }
        public void Clear()
        {
            backingList.Clear();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return backingList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return backingList.GetEnumerator();
        }
    }
}
