using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD
{
    public abstract class RoomLink : IRoomLinkContainer, ISaveable
    {
        public RoomLink RoomLinkObject { get { return this; } }

        //[SaveField("ExitRoom")]
        //private Room exitRoom;
        //public Room ExitRoom { get { return exitRoom; } set { exitRoom = value; this.Save(); } }

        /// <summary>
        /// Check where something will end up if it goes through the specified point.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="exitRoom"></param>
        /// <param name="exitPosition"></param>
        /// <returns>True if this is a valid passage. False if not.</returns>
        public abstract bool RoomThrough(IRoomPosition position, out Room exitRoom, out IRoomPosition exitPosition);

        /// <summary>
        /// Places the item can go to to enter this exit.
        /// TODO later: This needs more options for if a person is squeezing / crawling / something similar to fit through.
        /// </summary>
        /// <param name="item">Thing trying to find and enter this.</param>
        /// <returns>What 'options' there are to pass through this exit. If null, cannot enter.</returns>
        public abstract List<PositionAndShape> EnterThrough(Item item);
        //TODO: out MoreDetails for reason they can't enter? (can't see, can't fit, can't reach, blocked...)

        
        [SaveSubobject("Position")]
        private IRoomPosition position;
        public IRoomPosition Position
        {
            get { return position; }
            set { position = value; this.Save(); }
        }

        //ISaveable template
        public SaveValues SaveValues { get; set; }
        [SaveID("ID")]
        protected int RoomLinkID = -1; //Only supports IRoom ObjectType, so assume IRoom
        public int GetSaveID() { return RoomLinkID; }
        public void SetSaveID(int id) { RoomLinkID = id; }
    }

    public struct PositionAndShape
    {
        public Shape shape;
        public IRoomPosition position;
        public PositionAndShape(IRoomPosition position, Shape shape)
        {
            this.shape = shape;
            this.position = position;
        }
    }
}
