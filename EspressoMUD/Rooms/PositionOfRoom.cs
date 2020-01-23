using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EspressoMUD.Geometry;
using KejUtils.Geometry;

namespace EspressoMUD
{
    /// <summary>
    /// As an IRoomPosition, this is designed to be a subobject. Specifically this is the position for a room.
    /// Rooms should save themselves after modifying any of the Saveable values here.
    /// </summary>
    // TODO: How will ItemRooms work with this? Ideally I guess it should be some kind of pass-through to the Item's position.
    public class PositionOfRoom : IRoomPosition
    {
        /// <summary>
        /// Room whose position is being stored.
        /// </summary>
        private Room parent;
        public object Parent
        {
            get { return parent; }
            set { parent = value as Room; }
        }

        /// <summary>
        /// Room that contains this room/position.
        /// If null, this is the outermost room of a world.
        /// </summary>
        [SaveField("Room")]
        private Room room;
        public Room ForRoom
        {
            get { return room; }
            set { room = value; this.Save(); }
        }

        public Room OriginRoom
        {
            get
            {
                if (room != null) return room.Position.OriginRoom;
                return parent;
            }
        }

        [SaveField("PositionX")]
        private int x;
        public int X { get { return x; } set { x = value;  this.Save(); } }
        [SaveField("PositionY")]
        private int y;
        public int Y { get { return y; } set { y = value;  this.Save(); } }
        [SaveField("PositionZ")]
        private int z;
        public int Z { get { return z; } set { z = value;  this.Save(); } }
        
        public Point PositionValue
        {
            get { return new Point() {x=x,y=y,z=z }; }
            set { x = value.x; y = value.y; z = value.z;
                this.Save(); }
        }


        [SaveField("Direction")]
        private float direction;
        public float Direction
        {
            get { return direction; }
            set { direction = value; this.Save(); }
        }
        [SaveField("Tilt")]
        private float tilt;
        public float Tilt
        {
            get { return tilt; }
            set { tilt = value; this.Save(); }
        }
        [SaveField("Roll")]
        private float roll;
        public float Roll
        {
            get { return roll; }
            set { roll = value; this.Save(); }
        }

        public Rotation Rotation
        {
            get { return new Rotation(direction, tilt, roll); }
            set
            {
                direction = value.Direction; tilt = value.Tilt; roll = value.Roll;
                this.Save();
            }
        }

        /// <summary>
        /// Rooms will probably not be fixed to a single surface, instead a room will 'resize' as any of the surfaces around it move.
        /// </summary>
        public Item RestingOn
        {
            get { return null; }
            set {  }
        }
        public int RestingOnId
        {
            get { return -1; }
            set { }
        }
        public PoseMechanism RestingPose
        {
            get { return null; }
            set {  }
        }
        
        //IMPORTANT: When the room moves, this needs to be cleared. Also needs to notify all things it contains to clear their
        //cache, if they have a similar one.
        private WorldRelativeOrientation cachedOrientation;
        
        public WorldRelativePosition WorldPosition()
        {
            return (WorldRelativePosition)WorldOrientation();
        }

        public WorldRelativeOrientation WorldOrientation()
        {
            if (cachedOrientation.OriginRoom == null)
            {
                if (room != null)
                {
                    WorldRelativeOrientation p = room.Position.WorldOrientation();
                    Orientation innerOrientation = new Orientation(x, y, z, direction, roll, tilt);
                    cachedOrientation = (WorldRelativeOrientation)MUDGeometry.ApplyRotationToOrientation((Rotation)p, innerOrientation);
                    cachedOrientation.OriginRoom = p.OriginRoom;
                }
                else
                {
                    cachedOrientation.OriginRoom = parent;
                    cachedOrientation.Direction = 0;
                    cachedOrientation.Roll = 0;
                    cachedOrientation.Tilt = 0;
                    cachedOrientation.x = 0;
                    cachedOrientation.y = 0;
                    cachedOrientation.z = 0;
                }
            }
            return cachedOrientation;
        }
    }
}
