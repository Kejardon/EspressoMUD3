using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections;

namespace KejUtils
{
    /// <summary>
    /// A list / queue of elements. Able to insert or remove quickly and efficiently from start or end of the list, and comparable
    /// performance to a normal list in the center.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CircularList<T> : IList<T>
    {
        private T[] items;
        private int start = 0;
        private int end = -1; //Important note: If this is -1, instead of an index, it means empty.
        //if end == start, it's full.

        public CircularList() : this(16)
        {
        }
        public CircularList(int capacity)
        {
            if (capacity <= 1)
                throw new ArgumentException("capacity must be at least 2");

            //this.Capacity = capacity;
            this.items = new T[capacity];
        }

        public int Capacity
        {
            get { return items.Length; }
        }

        public int Count
        {
            get
            {
                if (end == -1) return 0;
                int current = this.end - this.start;
                return current > 0 ? current : (current + Capacity);
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        //Convert an external index to an index for items
        private int fixIndex(int index)
        {
            index = index + start;
            return index < Capacity ? index : (index - Capacity);
        }
        //Convert an index for items to an external index
        private int unfixIndex(int index)
        {
            index = index - start;
            return index >= 0 ? index : (index + Capacity);
        }

        //public void AddRange(IEnumerable<T> items)
        //{
        //    if (items == null)
        //        return;

        //    foreach (var item in items)
        //    {
        //        AddWithLock(item);
        //    }
        //}

        private void EnsureCapacity(int size)
        {
            if (this.Capacity < size)
            {
                if (Capacity < int.MaxValue / 2)
                    size = Math.Max(Capacity * 2, size);
                else
                    size = int.MaxValue;
                T[] newItems = new T[size];
                int oldCount = Count;
                int firstCopy = Math.Min(oldCount, Capacity - start);
                Array.Copy(this.items, start, newItems, 0, firstCopy);
                if (firstCopy < oldCount)
                {
                    Array.Copy(this.items, 0, newItems, firstCopy, end);
                }

                this.items = newItems;
                this.start = 0;
                this.end = oldCount == 0 ? -1 : oldCount;
            }
        }


        public void Add(T item)
        {
            int oldCount = Count;
            EnsureCapacity(oldCount + 1);
            int newEnd = end;
            if (oldCount == 0) newEnd = start;
            items[newEnd] = item;
            newEnd++;
            if (newEnd == Capacity)
            {
                newEnd = 0;
            }
            end = newEnd;
        }

        public T this[int index]
        {
            get
            {
                if (index >= Count || index < 0) throw new IndexOutOfRangeException();
                return items[fixIndex(index)];
            }
            set
            {
                if (index == Count) { Insert(index, value); return; }

                if (index > Count || index < 0) throw new IndexOutOfRangeException();
                items[fixIndex(index)] = value;
            }
        }

        public void Insert(int index, T item)
        {
            int oldCount = Count;
            if (index > oldCount || index < 0) throw new ArgumentOutOfRangeException("index");
            if (index == oldCount) { Add(item); return; } //This also handles the Count == 0 case, so no need to check end for -1.
            EnsureCapacity(oldCount + 1); //TODO: Could make this faster by combining EnsureCapacity with below code.
            int currentCapacity = Capacity;
            if (index > oldCount / 2)
            {
                bool skipCopies = index == oldCount;
                index = fixIndex(index);
                if (!skipCopies)
                {
                    ShiftRange(true, index, end);
                }
                end++;
                if (end == currentCapacity)
                {
                    end = 0;
                }
            }
            else
            {
                bool skipCopies = index == 0;
                index = fixIndex(index);
                if (!skipCopies)
                {
                    ShiftRange(false, start, index);
                }
                index--;
                if (index < 0)
                {
                    index = currentCapacity - 1;
                }
                start--;
                if (start < 0)
                {
                    start = currentCapacity - 1;
                }
            }
            items[index] = item;
        }

        /// <summary>
        /// Moves a range of entries forwards/backwards by 1. Can loop around.
        /// </summary>
        /// <param name="forward"></param>
        /// <param name="startShift">First element to move, inclusive</param>
        /// <param name="endShift">Last element to move, exclusive</param>
        private void ShiftRange(bool forward, int startRange, int endRange)
        {
            if (startRange == endRange) return;
            endRange--; //Now inclusive, simplifies some edge cases.
            if (endRange < 0) endRange = Capacity - 1;
            int count = endRange - startRange + 1;
            if (forward)
            {
                if (startRange > endRange)
                {
                    if (endRange > 0)
                    {
                        Array.Copy(items, 0, items, 1, endRange);
                        items[0] = items[Capacity - 1];
                    }
                    count = Capacity - startRange - 1;
                }
                else
                {
                    if (endRange == Capacity - 1)
                    {
                        items[0] = items[Capacity - 1];
                        count--;
                    }
                }
                if (count > 0)
                {
                    Array.Copy(items, startRange, items, startRange + 1, count);
                }
            }
            else
            {
                int copyStart = startRange;
                if (startRange > endRange)
                {
                    Array.Copy(items, startRange, items, startRange - 1, Capacity - startRange);
                    items[Capacity - 1] = items[0];
                    copyStart = 1;
                }
                Array.Copy(items, copyStart, items, copyStart - 1, endRange - copyStart + 1);

            }
        }

        public T RemoveAt(int index)
        {
            int oldCount = Count;
            if (index >= oldCount || index < 0) throw new IndexOutOfRangeException();
            int internalIndex = fixIndex(index);
            T value = items[internalIndex];
            int currentCapacity = Capacity;
            if (index > oldCount / 2)
            {
                bool skipCopies = index == oldCount - 1;
                if (!skipCopies)
                {
                    ShiftRange(false, internalIndex+1, end);
                }
                end--;
                if (end < 0)
                {
                    end = currentCapacity - 1;
                }
                items[end] = default(T);
            }
            else
            {
                bool skipCopies = index == 0;
                if (!skipCopies)
                {
                    ShiftRange(true, start, internalIndex);
                }
                items[start] = default(T);
                start++;
                if (start == currentCapacity)
                {
                    start = 0;
                }
            }

            return value;
        }

        public int BinarySearch(T item, IComparer<T> comparer = null)
        {
            if (comparer == null) comparer = Comparer<T>.Default;

            if (end == -1) end = Count - 1;
            while (end >= start)
            {
                int mid = (end + start) / 2;
                int compare = comparer.Compare(item, this[mid]);
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

        public void Clear()
        {
            Array.Clear(items, 0, Capacity);
            start = 0;
            end = -1;
        }

        public int IndexOf(T item)
        {
            if (end == -1) return -1;
            int i = start;
            while(true)
            {
                T next = items[i];
                if (next.Equals(item)) return unfixIndex(i);
                i++;
                if (i == Capacity)
                {
                    i = 0;
                }
                if (i == end) break;
            }
            return -1;
        }

        void IList<T>.RemoveAt(int index)
        {
            RemoveAt(index);
        }

        public bool Contains(T item)
        {
            return (IndexOf(item) != -1);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (arrayIndex < 0) throw new ArgumentOutOfRangeException("Invalid arrayIndex");
            if (Count > array.Length - arrayIndex) throw new ArgumentException("Not enough space available in array.");

            int toCopy = Count;
            int lengthOne = Math.Min(Capacity - start, toCopy);
            if (lengthOne > 0)
            {
                Array.Copy(items, start, array, arrayIndex, lengthOne);
                toCopy -= lengthOne;
                if (toCopy > 0)
                {
                    Array.Copy(items, 0, array, arrayIndex + lengthOne, toCopy);
                }
            }
        }

        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index == -1) return false;
            RemoveAt(index);
            return true;
        }

        public IEnumerator<T> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
