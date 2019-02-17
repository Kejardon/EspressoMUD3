using KejUtils.SharedLocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD
{
    public abstract class Room : IRoomContainer, ISaveable, ILockable
    {
        public Room RoomObject { get { return this; } }

        [SaveField("Items")]
        private ListItems contents = new ListItems();

        protected virtual void AddItem(Item item)
        {
            contents.Add(item);
            this.Save();
        }
        protected virtual bool RemoveItem(Item item)
        {
            if (contents.Remove(item))
            {
                this.Save();
                return true;
            }
            return false;
        }
        
        public Item[] GetItems()
        {
            return contents.GetAll();
        }

        [SaveField("Exits")]
        private ListRoomLinks exits = new ListRoomLinks();

        public RoomLink[] GetExits()
        {
            return exits.GetAll();
        }

        protected virtual void AddExit(RoomLink exit)
        {
            exits.Add(exit);
            this.Save();
        }
        protected virtual bool RemoveExit(RoomLink exit)
        {
            if(exits.Remove(exit))
            {
                this.Save();
                return true;
            }
            return false;
        }

        //ISaveable template
        public SaveValues SaveValues { get; set; }

        //ILockable template
        public object LockMutex { get { return this; } }
        public LockableLockGroup CurrentLock { get; set; }

        [SaveID("ID")]
        protected int RoomID = -1; //Only supports IRoom ObjectType, so assume IRoom
        public int GetSaveID(ObjectType databaseGroup) { return RoomID; }
        public void SetSaveID(ObjectType databaseGroup, int id) { RoomID = id; }
    }

    public static partial class Extensions
    {
        public static Room GetRoom(MOB fromMob)
        {
            return fromMob.Body?.Position?.forRoom;
        }
    }

}
