using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KejUtils
{
    /// <summary>
    /// A lazy list that uses a minimum of space/objects. Ideal for lists that are often 0 or 1 in length. This version
    /// is designed to be used as a private field in a class, instead of an object that's publicly accessible.
    /// This can be used for structs but it is better suited for reference objects.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct InternalListStruct<T> where T : class
    {
        private object data;
        private int count;

        public int Count
        {
            get
            {
                return count;
            }
        }
        public T this[int i]
        {
            get
            {
                T[] array = data as T[];
                if (array != null) return array[i];
                if (count - 1 < i || i < 0) throw new IndexOutOfRangeException();
                return data as T;
            }
            //set
            //{
            //    if (count - 1 < i || i < 0) throw new IndexOutOfRangeException();
            //    T[] array = asArray;
            //    if (array == null) data = value;
            //    else array[i] = value;
            //}
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
        public static void Set(ref InternalListStruct<T> list, int i, T newValue)
        {
            if (list.count - 1 < i || i < 0) throw new IndexOutOfRangeException();
            T[] array = list.data as T[];
            if (array == null) list.data = newValue;
            else array[i] = newValue;
        }
        public static void Add(ref InternalListStruct<T> list, T newValue)
        {
            list.count++;
            T[] array = list.data as T[];
            if (list.count > 1)
            {
                if (array == null) list.data = new T[16];
                else if (array.Length < list.count)
                {
                    T[] newArray = new T[array.Length * 2];
                    Array.Copy(array, newArray, array.Length);
                    list.data = newArray;
                }
                (list.data as T[])[list.count - 1] = newValue;
            }
            else
            {
                if (array != null) array[0] = newValue;
                else list.data = newValue;
            }
        }
        public static void RemoveAt(ref InternalListStruct<T> list, int i)
        {
            if (list.count - 1 < i || i < 0) throw new IndexOutOfRangeException();
            list.count--;
            T[] array = list.data as T[];
            if (array != null)
            {
                Array.Copy(array, i + 1, array, i, list.count - i);
                array[list.count] = null;
            }
            else
            {
                list.data = null;
            }
        }
        public static void Clear(ref InternalListStruct<T> list)
        {
            T[] asArray = list.data as T[];
            if (asArray == null)
            {
                list.data = null;
            }
            else
            {
                Array.Clear(asArray, 0, list.count);
            }
            list.count = 0;
        }
    }
}
