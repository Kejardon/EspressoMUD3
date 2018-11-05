using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace KejUtils
{
    /// <summary>
    /// A queue of elements with a maximum size. Expected to be used as LIFO, and when the maximum size is reached the
    /// oldest entries are removed.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CircularConcurrentQueue<T>
    {
        private T[] items;
        private int start = 0;
        private int end = 0;
        private ReaderWriterLockSlim queueLock = new ReaderWriterLockSlim();

        public CircularConcurrentQueue(int capacity)
        {
            if (capacity <= 1)
                throw new ArgumentException("capacity must be at least 2");

            this.Capacity = capacity;
            this.items = new T[capacity];
        }

        public int Capacity { get; private set; }

        public int Count
        {
            get
            {
                queueLock.EnterReadLock();
                try
                {
                    int current = this.end - this.start;
                    return current >= 0 ? current : (current + Capacity);
                }
                finally { queueLock.ExitReadLock(); }
            }
        }

        public void AddRange(IEnumerable<T> items)
        {
            if (items == null)
                return;

            queueLock.EnterWriteLock();
            try
            {
                foreach (var item in items)
                {
                    AddWithLock(item);
                }
            }
            finally { queueLock.ExitWriteLock(); }
        }

        private void AddWithLock(T item)
        {
            items[end++] = item;
            if (end == Capacity)
            {
                end = 0;
            }
            if (end == start)
            {
                start++;
                if (start == Capacity)
                {
                    start = 0;
                }
            }
        }

        public void Add(T item)
        {
            queueLock.EnterWriteLock();
            try
            {
                AddWithLock(item);
            }
            finally { queueLock.ExitWriteLock(); }
        }

        public void Clear()
        {
            queueLock.EnterWriteLock();
            try
            {
                Array.Clear(items, 0, Capacity);
                start = 0;
                end = 0;
            }
            finally { queueLock.ExitWriteLock(); }
        }

        /// <summary>
        /// Tries to get the nth item in this queue (0 is the most recently added item).
        /// </summary>
        /// <param name="reverseIndex"></param>
        /// <param name="value"></param>
        /// <returns>True if the item was successfully retrieved, false if index was invalid at the time of reading.</returns>
        public bool TryGet(int reverseIndex, out T value)
        {
            value = default(T);
            queueLock.EnterReadLock();
            try
            {
                if (reverseIndex > Count) return false;

                if ((reverseIndex = end - reverseIndex - 1) < 0) reverseIndex += Capacity;
                value = items[reverseIndex];
                return true;
            }
            finally { queueLock.ExitReadLock(); }
        }

        /// <summary>
        /// Returns as many items as possible/requested in this queue. 0 is the oldest item available, .Length is the newest item.
        /// </summary>
        /// <returns>Array of items from this queue. May be of size 0, never null.</returns>
        public T[] ToArray() { return ToArray(Capacity); }
        public T[] ToArray(int maxAmount)
        {
            queueLock.EnterReadLock();
            try
            {
                //TODO: Test this logic.
                int currentCount = Count;
                maxAmount = Math.Min(maxAmount, currentCount);
                T[] foundItems = new T[maxAmount];

                int copyStart = start + (currentCount - maxAmount);
                if (copyStart >= Capacity) copyStart -= Capacity;
                if (end == 0 || copyStart < end)
                {
                    Array.Copy(items, copyStart, foundItems, 0, maxAmount);
                }
                else
                {
                    int tillStart = Capacity - copyStart;
                    Array.Copy(items, copyStart, foundItems, 0, tillStart);
                    Array.Copy(items, 0, foundItems, tillStart, end);
                }
                return foundItems;
            }
            finally { queueLock.ExitReadLock(); }
        }

        // Not sure how removal should be implemented. For now it's not needed at least.
    }
}
