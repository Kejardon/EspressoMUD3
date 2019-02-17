using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD
{
    public class RoomLink : IRoomLinkContainer
    {
        public RoomLink RoomLinkObject { get { return this; } }

        //ISaveable template
        public SaveValues SaveValues { get; set; }
        [SaveID("ID")]
        protected int RoomLinkID = -1; //Only supports IRoom ObjectType, so assume IRoom
        public int GetSaveID(ObjectType databaseGroup) { return RoomLinkID; }
        public void SetSaveID(ObjectType databaseGroup, int id) { RoomLinkID = id; }
    }
}
