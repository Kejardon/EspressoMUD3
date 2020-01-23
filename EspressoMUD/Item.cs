using KejUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD
{
    public class Item : IItemContainer, ISaveable, IEventListener
    {
        public Item ItemObject { get { return this; } }

        [SaveField("Name")]
        private string name;
        public string Name
        {
            get { return name; }
            set { name = value; this.Save(); }
        }

        [SaveField("Desc")]
        private string description;
        public string Description
        {
            get { return description; }
            set { description = value;  this.Save(); }
        }

        [SaveSubobject("Position")]
        private IRoomPosition position;
        public IRoomPosition Position
        {
            get { return position; }
            set { position = value;  this.Save(); }
        }

        [SaveSubobject("Size")]
        private Hitbox size;
        public Hitbox Size
        {
            get { return size; }
            set { size = value;  this.Save(); }
        }
        
        public virtual bool IsObstacle() { return false; }
        public virtual Obstacle AsObstacle() { return null; }

        #region Item Event Listener management
        private List<IEventListener> allListeners = new List<IEventListener>();
        public void AddEventListeners(RoomEvent forEvent)
        {
            this.AddEventListener(forEvent);
            //EventType[] eventTypes = forEvent.Types();
            foreach (IEventListener listener in allListeners)
            {
                //This probably is not a useful optimization. Removing it and just calling AddEventListener for everything.
                //if (eventTypes.Any(listener.ListensToType))
                //{
                    listener.AddEventListener(forEvent);
                //}
            }
        }
        public void AddEventListener(IEventListener listener)
        {
            if (!allListeners.Contains(listener))
                allListeners.Add(listener);
        }
        public bool RemoveEventListener(IEventListener listener)
        {
            return allListeners.Remove(listener);
        }
        #endregion

        #region IEventListener template
        public virtual void AddEventListener(RoomEvent forEvent)
        {
            //TODO: 
        }
        #endregion

        #region ISaveable template
        public SaveValues SaveValues { get; set; }
        
        [SaveID("ID")]
        protected int ItemID = -1; //Only supports IItem ObjectType, so assume IItem
        public int GetSaveID() { return ItemID; }
        public void SetSaveID(int id) { ItemID = id; }
        #endregion
    }
}
