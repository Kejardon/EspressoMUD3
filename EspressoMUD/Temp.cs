using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD
{
    /// <summary>
    /// Fake class for stuff I don't know where I want to put yet.
    /// </summary>
    class Temp
    {
    }

    /// <summary>
    /// List of saveable objects. Objects will be delayed-loaded by default. Owning object should be saved as usual after
    /// modifications. Not thread-safe, it is assumed the MUD will enforce single-thread writers 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class ListSaveables<T> where T : ISaveable
    {
        List<Tuple<int,T>> data = new List<Tuple<int, T>>();
        protected virtual ObjectType Type { get; }

        public ListSaveables()
        {
        }
        public T Get(int i)
        {
            Tuple<int, T> datum = data[i];
            T saveable = datum.Item2;
            if (saveable != null) return saveable;
            int saveId = datum.Item1;
            saveable = (T)Type.Get(saveId, true, true); //TODO: Handle unexpected issues here?
            data[i] = new Tuple<int, T>(saveId, saveable);
            return saveable;
        }
        /// <summary>
        /// Add an object to this list. Gives the object a Save ID if it didn't already have one.
        /// The object owning this list should be saved after calling this.
        /// </summary>
        /// <param name="next"></param>
        public void Add(T next)
        {
            int saveId = next.GetSetSaveID();
            lock(this)
            {
                data.Add(new Tuple<int, T>(saveId, next));
            }
        }
        public bool Remove(T old)
        {
            int saveId = old.GetSaveID();
            if (saveId == -1) return false;
            lock (this)
            {
                for (int i = 0; i < data.Count; i++)
                {
                    if (data[i].Item1 == saveId)
                    {
                        data.RemoveAt(i);
                        return true;
                    }
                }
            }
            return false;
        }
        //public void RemoveAt(int i)
        //{
        //    try
        //    {
        //        lock (this)
        //        {
        //            data.RemoveAt(i);
        //        }
        //    }
        //    catch (ArgumentOutOfRangeException) { }
        //}
        public int Count { get { return data.Count; } }
        public T[] GetAll()
        {
            T[] all;
            lock (this)
            {
                all = new T[data.Count];
                for(int i = 0; i < data.Count; i++)
                {
                    all[i] = Get(i);
                }
            }
            return all;
        }

        /// <summary>
        /// Only used by the database. Gets a list of IDs to save to the database.
        /// </summary>
        /// <returns></returns>
        public int[] GetIDs()
        {
            int[] ids = new int[data.Count];
            //This isn't actually threadsafe for the database. But if this is modified so it could throw an error, then this
            //read doesn't matter anyways, a later read while the MUD is paused will load accurate data.
            try
            {
                for (int i = 0; i < data.Count; i++)
                {
                    ids[i] = data[i].Item1;
                }
            }
            catch (Exception) { }
            return ids;
        }
        /// <summary>
        /// Only used by the database.
        /// </summary>
        /// <param name="ids"></param>
        public void SetIDs(int[] ids)
        {
            foreach (int id in ids)
            {
                data.Add(new Tuple<int, T>(id, default(T)));
            }
        }
        public bool Contains(int i)
        {
            if (i < 0) return false; //Unsaved objects are not supported, objects need a save ID to be in this list.
            foreach (Tuple<int, T> datum in data)
            {
                if (datum.Item1 == i) return true;
            }
            return false;
        }
        public bool Contains(ISaveable obj)
        {
            return Contains(obj.GetSaveID());
        }
    }
    public class ListMOBs : ListSaveables<MOB>
    {
        protected override ObjectType Type
        {
            get
            {
                return ObjectType.TypeByClass[typeof(MOB)];
            }
        }
    }
    public class ListItems : ListSaveables<Item>
    {
        protected override ObjectType Type
        {
            get
            {
                return ObjectType.TypeByClass[typeof(Item)];
            }
        }
    }
    public class ListRooms : ListSaveables<Room>
    {
        protected override ObjectType Type
        {
            get
            {
                return ObjectType.TypeByClass[typeof(Room)];
            }
        }
    }
    public class ListRoomLinks : ListSaveables<RoomLink>
    {
        protected override ObjectType Type
        {
            get
            {
                return ObjectType.TypeByClass[typeof(RoomLink)];
            }
        }
    }


    public interface IDelayedObject<T> where T : ISaveable
    {
        Type baseType { get; }
        int Id { get; set; }
        T Value { get; set; }
    }

    public struct DelayedAccount : IDelayedObject<Account>
    {
        public int Id { get; set; }
        public Account Value { get; set; }
        public Type baseType { get { return typeof(Account); } }

        public DelayedAccount(int id, Account acc = null)
        {
            this.Id = id;
            this.Value = acc;
        }

        public static implicit operator Account(DelayedAccount obj)
        {
            return obj.LoadFromDatabase(false);
        }
    }
    public struct DelayedMOB : IDelayedObject<MOB>
    {
        public int Id { get; set; }
        public MOB Value { get; set; }
        public Type baseType { get { return typeof(MOB); } }
        private Account Account;

        public DelayedMOB(int id, MOB mob = null, Account account = null)
        {
            this.Id = id;
            this.Value = mob;
            this.Account = account;
        }

        public static implicit operator MOB(DelayedMOB obj)
        {
            MOB mob = obj.LoadFromDatabase(false);
            if (mob == null)
            {
                Log.LogText("MOB reference not found on the database.");
                return null;
            }
            mob.OwningAccount = obj.Account;
            return mob;
        }
        public override bool Equals(object obj)
        {
            int thisId = Id == -1 ? Value.GetSaveID() : Id;
            if (obj is DelayedMOB)
            {
                DelayedMOB otherDelayed = (DelayedMOB)obj;
                int otherId = otherDelayed.Id == -1 ? otherDelayed.Value.GetSaveID() : otherDelayed.Id;
                return otherId == thisId;
            }
            MOB mob = obj as MOB;
            if (mob == null) return false;
            return mob.GetSaveID() == thisId;
        }
        public override int GetHashCode()
        {
            if (Id != -1) return Id;
            return Value.GetSaveID();
        }
    }

    public static partial class Extensions
    {

        public static T LoadFromDatabase<T>(this IDelayedObject<T> delayedObject, bool waitFor) where T : ISaveable
        {
            if (delayedObject.Value != null)
            {
                return delayedObject.Value;
            }
            ObjectType loader = ObjectType.TypeByClass[delayedObject.baseType];
            return (T)loader.Get(delayedObject.Id, true, waitFor);
        }
    }

}
