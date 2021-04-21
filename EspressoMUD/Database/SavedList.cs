using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD
{
    public struct SavedList<T>
    {
        public SavedList(List<T> backingList, object savedObject)
        {
            list = backingList;
            if (savedObject is ISaveable || savedObject is ISubobject)
                owner = savedObject;
            else throw new ArgumentException("savedObject must be an ISaveable or ISubobject");
        }

        private List<T> list;
        public object owner;

        private void Save()
        {
            ISaveable saveable = owner as ISaveable;
            if (saveable != null) saveable.Save();
            else (owner as ISubobject).Save();
        }

        public T this[int index]
        {
            get
            {
                return list[index];
            }
            set
            {
                list[index] = value;
                Save();
            }
        }
        public void Add(T newItem)
        {
            list.Add(newItem);
            Save();
        }
        public void RemoveAt(int index)
        {
            list.RemoveAt(index);
            Save();
        }
    }
}
