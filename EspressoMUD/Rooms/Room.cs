using KejUtils.SharedLocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD
{
    public abstract class Room : IRoom, ISaveable, ILockable
    {
        [SaveField("Items")]
        private ListItems contents = new ListItems();

        protected virtual void AddItem(IItem item)
        {
            contents.Add(item);
            this.Save();
        }
        protected virtual bool RemoveItem(IItem item)
        {
            if (contents.Remove(item))
            {
                this.Save();
                return true;
            }
            return false;
        }

        [SaveField("Exits")]
        private ListRoomLinks exits = new ListRoomLinks();

        protected virtual void AddExit(IRoomLink exit)
        {
            exits.Add(exit);
            this.Save();
        }
        protected virtual bool RemoveExit(IRoomLink exit)
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
}
