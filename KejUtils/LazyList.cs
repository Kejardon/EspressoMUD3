using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KejUtils
{
    /// <summary>
    /// A lazy List that uses a minimum of size and objects to contain its data.
    /// Ideal for Lists that often never have more than 0 or 1 items in them.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LazyList<T> where T : class
    {
        private object data;
        private int count;

        private T[] asArray { get { return data as T[]; } }

        public int Count { get
            {
                return count;
            } }
        public T this[int i]
        {
            get
            {
                T[] array = asArray;
                if (array != null) return array[i];
                if (count - 1 < i || i < 0) throw new IndexOutOfRangeException();
                return data as T;
            }
            set
            {
                if (count - 1 < i || i < 0) throw new IndexOutOfRangeException();
                T[] array = asArray;
                if (array == null) data = value;
                else array[i] = value;
            }
        }
        public bool Contains(T value)
        {
            T[] array = data as T[];
            if (array == null)
            {
                return count == 1 && value.Equals(data);
            }
            for (int i = 0; i < count; i++)
            {
                if (value.Equals(array[i])) return true;
            }
            return false;
        }
        public void Add(T newValue)
        {
            count++;
            T[] array = asArray;
            if (count > 1)
            {
                if (array == null) data = new T[16];
                else if (array.Length < count)
                {
                    T[] newArray = new T[array.Length * 2];
                    Array.Copy(array, newArray, array.Length);
                    data = newArray;
                }
                asArray[count-1] = newValue;
            }
            else
            {
                if (array != null) array[0] = newValue;
                else data = newValue;
            }
        }
        public void RemoveAt(int i)
        {
            if (count - 1 < i || i < 0) throw new IndexOutOfRangeException();
            count--;
            T[] array = asArray;
            if (array != null)
            {
                Array.Copy(array, i + 1, array, i, count - i);
                array[count] = null;
            }
            else
            {
                data = null;
            }
        }

    }


}
