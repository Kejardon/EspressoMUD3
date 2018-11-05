using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KejUtils
{
    /// <summary>
    /// A generic builder for any list type of data.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Builder<T> : List<T>
    {
        public Builder<T> Append(T value)
        {
            this.Add(value);
            return this;
        }
        public Builder<T> Append(T[] value)
        {
            this.AddRange(value);
            return this;
        }
        public Builder<T> Append(T[] value, int start, int end)
        {
            SubEnumeration subEnum = new SubEnumeration(value, start, end);
            this.AddRange(subEnum);
            return this;
        }

        public Builder<T> InsertAt(int offset, T value)
        {
            ((List<T>)this).Insert(offset, value);
            return this;
        }
        public Builder<T> InsertAt(int offset, T[] value)
        {
            this.InsertRange(offset, value);
            return this;
        }
        public Builder<T> InsertAt(int offset, T[] value, int start, int end)
        {
            SubEnumeration subEnum = new SubEnumeration(value, start, end);
            this.InsertRange(offset, subEnum);
            return this;
        }
        /// <summary>
        /// Removes entries from the end of this to match the requested length.
        /// </summary>
        /// <param name="i"></param>
        // Exceptions:
        //   T:System.ArgumentOutOfRangeException:
        //     index is less than 0.-or-count is less than 0.
        //
        //   T:System.ArgumentException:
        //     index and count do not denote a valid range of elements in the System.Collections.Generic.List`1.
        public void TrimToLength(int i)
        {
            this.RemoveRange(i, this.Count - i);
        }

        public T[] ToArray(int start, int amount)
        {
            T[] array = new T[amount];
            this.CopyTo(start, array, 0, amount);
            return array;
        }

        /// <summary>
        /// Helper class to handle lists.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private class SubEnumeration : IEnumerable<T>, IEnumerator<T>
        {
            T[] source;
            int start;
            int end;
            public SubEnumeration(T[] source, int start, int end)
            {
                this.source = source;
                this.start = start;
                this.end = end;
            }

            public T Current
            {
                get
                {
                    return source[start];
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return source[start];
                }
            }

            public void Dispose()
            {
            }

            public IEnumerator<T> GetEnumerator()
            {
                return this;
            }

            public bool MoveNext()
            {
                if (start == end) return false;
                start++;
                return true;
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this;
            }
        }
    }
}
