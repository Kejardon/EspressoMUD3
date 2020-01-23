using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KejUtils
{
    /// <summary>
    /// A lazy list of structs that uses a minimum of space/objects. Ideal for lists that are often 0 in length. This version
    /// is designed to be used as a private field in a class, instead of an object that's publicly accessible.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct StructListStruct<T>
    {
        private T[] data;
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
                if (count - 1 < i || i < 0) throw new IndexOutOfRangeException();
                return data[i];
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
            for (int i = 0; i < count; i++)
            {
                if (value.Equals(data[i])) return true;
            }
            return false;
        }
        public static void Set(ref StructListStruct<T> list, int i, T newValue)
        {
            if (list.count - 1 < i || i < 0) throw new IndexOutOfRangeException();
            list.data[i] = newValue;
        }
        public static void Add(ref StructListStruct<T> list, T newValue)
        {
            list.count++;
            T[] array = list.data;
            if (array == null) list.data = new T[16];
            else if (array.Length < list.count)
            {
                T[] newArray = new T[array.Length * 2];
                Array.Copy(array, newArray, array.Length);
                list.data = newArray;
            }
            list.data[list.count - 1] = newValue;
        }
        public static void RemoveAt(ref StructListStruct<T> list, int i)
        {
            if (list.count - 1 < i || i < 0) throw new IndexOutOfRangeException();
            list.count--;
            T[] array = list.data;
            Array.Copy(array, i + 1, array, i, list.count - i);
            array[list.count] = default(T);
        }
        public static void Clear(ref StructListStruct<T> list)
        {
            T[] array = list.data;
            if (array != null)
            {
                Array.Clear(array, 0, list.count);
                list.count = 0;
            }
        }
    }
}

}
