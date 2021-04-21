using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EspressoMUD.Geometry;
using KejUtils.Geometry;

namespace EspressoMUD.Rooms
{
    /// <summary>
    /// A very standard framed room with a rectangular shape.
    /// </summary>
    public class RectangleFramedRoom : Room
    {
        /// <summary>
        /// Handle items that are anchored in another room, but are partially in this room also.
        /// </summary>
        /// <param name="item">Item to add</param>
        /// <returns>True if the item was added. Some rooms may not support overlapping items.</returns>
        protected override bool AddOverlappingItem(Item item)
        {
            //TODO: create virtual item that points to the overlapping item. Add that to the contents of the room.
            return false;
        }
        /// <summary>
        /// Handle items that are anchored in another room, but are partially in this room also.
        /// </summary>
        /// <param name="item"></param>
        /// <returns>True if the item was found and removed.</returns>
        protected override bool RemoveOverlappingItem(Item item)
        {
            //TODO: find matching virtual item that points to the overlapping item and remove it.
            return false;
        }

        [SaveField("Width")]
        private int width;
        public int Width { get { return width; } set { width = value; this.Save(); } }

        [SaveField("Length")]
        private int length;
        public int Length { get { return length; } set { length = value; this.Save(); } }

        [SaveField("Height")]
        private int height;
        public int Height { get { return height; } set { height = value; this.Save(); } }

        [SaveField("Material")]
        private Materials.Material material;
        public Materials.Material Material { get { return material; } set { material = value; this.Save(); } }

        [SaveSubobjectList("RoomWalls")]
        private List<RoomFullWall> roomWalls;
        public SavedList<RoomFullWall> RoomWalls { get { return new SavedList<RoomFullWall>(roomWalls, this); } }


        public class RoomFullWall : ISubobject
        {
            private RectangleFramedRoom parent;
            public object Parent { get { return parent; } set { parent = (RectangleFramedRoom)value; } }

            //In order from 0 to 5: Bottom, top, north, east, south, west. 
            [SaveField("Direction")]
            private int direction;
            public int Direction { get { return direction; } set { direction = value; this.Save(); } }

            [SaveField("Thickness")]
            private int thickness;
            public int Thickness { get { return thickness; } set { thickness = value; this.Save(); } }

            [SaveField("Material")]
            private Materials.Material material;
            public Materials.Material Material { get { return material; } set { material = value; this.Save(); } }

            //Parts of the wall that aren't the same as the main portion of it. Typically doors, windows, holes etc.
            [SaveSubobjectList("Holes")]
            private List<WallHole> holes;
            public List<WallHole> Holes { get { return holes; } set { holes = value; this.Save(); } }

        }

        public class WallHole : ISubobject
        {
            private RoomFullWall parent;
            public object Parent { get { return parent; } set { parent = (RoomFullWall)value; } }

            /// <summary>
            /// The position along the first dimension (X, or if no x, Y)
            /// </summary>
            [SaveField("PositionX")]
            private int x;
            public int PositionX { get { return x; } set { x = value; this.Save(); } }

            /// <summary>
            /// The position along the second dimension (Z, or if no z, Y)
            /// </summary>
            [SaveField("PositionY")]
            private int y;
            public int PositionY { get { return y; } set { y = value; this.Save(); } }

            /// <summary>
            /// The size along the first dimension
            /// </summary>
            [SaveField("Width")]
            private int width;
            public int Width { get { return width; } set { width = value; this.Save(); } }

            /// <summary>
            /// The size along the second dimension
            /// </summary>
            [SaveField("Length")]
            private int length;
            public int Length { get { return length; } set { length = value; this.Save(); } }

            /// <summary>
            /// What is filling the space of this hole. If null, it has the same filler material as the room.
            /// </summary>
            [SaveField("Door")]
            private ItemDoor door;
            public ItemDoor Door { get { return door; } set { door = value; this.Save(); } }
        }

        public class StandardPosition : IRoomPosition
        {
            public StandardPosition(Room startingRoom, object parent)
            {
                forRoom = startingRoom;
                Parent = parent;
            }

            [SaveField("Direction")]
            private float direction;
            public float Direction { get { return direction; } set { direction = value; this.Save(); } }
            [SaveField("Roll")]
            private float roll;
            public float Roll { get { return roll; } set { roll = value; this.Save(); } }
            [SaveField("Tilt")]
            private float tilt;
            public float Tilt { get { return tilt; } set { tilt = value; this.Save(); } }
            public Rotation Rotation
            {
                get { return new Rotation(direction, tilt, roll); }
                set
                {
                    direction = value.Direction; tilt = value.Tilt; roll = value.Roll;
                    this.Save();
                }
            }

            [SaveField("PositionX")]
            private int x;
            public int X { get { return x; } set { x = value; this.Save(); } }
            [SaveField("PositionY")]
            private int y;
            public int Y { get { return y; } set { y = value; this.Save(); } }
            [SaveField("PositionZ")]
            private int z;
            public int Z { get { return z; } set { z = value; this.Save(); } }

            [SaveField("ForRoom")]
            private Room forRoom;
            public Room ForRoom { get { return forRoom; } }
            //Should be read-only instead.
            //set { forRoom = value; this.Save(); }
            

            public Room OriginRoom
            {
                get
                {
                    return forRoom.Position.OriginRoom;
                }
            }

            private object parent;
            public object Parent { get { return parent; } set { parent = value; } }

            public Point PositionValue
            {
                get { return new Point() { x = x, y = y, z = z }; }
                set
                {
                    x = value.x; y = value.y; z = value.z;
                    this.Save();
                }
            }


            [SaveField("RestingOn")]
            private Item restingOn;
            public Item RestingOn { get { return restingOn; } set { restingOn = value; this.Save(); } }

            [SaveField("RestingOnId")]
            private int restingOnId;
            public int RestingOnId { get { return restingOnId; } set { restingOnId = value; this.Save(); } }

            //TODO IMPORTANT. Mechanisms currently aren't saveables, they're expected to be a more dynamicly generated thing.
            //Need to figure out how to save a reference to them. Probably need to figure out how they are generated first.
            public PoseMechanism RestingPose
            {
                get
                {
                    throw new NotImplementedException();
                }

                set
                {
                    throw new NotImplementedException();
                }
            }
            

            private WorldRelativeOrientation cachedOrientation;
            public WorldRelativeOrientation WorldOrientation()
            {
                if (cachedOrientation.OriginRoom == null)
                {
                    WorldRelativeOrientation p = forRoom.Position.WorldOrientation();
                    Orientation innerOrientation = new Orientation(x, y, z, direction, roll, tilt);
                    cachedOrientation = (WorldRelativeOrientation)MUDGeometry.ApplyRotationToOrientation((Rotation)p, innerOrientation);
                    cachedOrientation.OriginRoom = p.OriginRoom;
                }
                return cachedOrientation;
            }

            public WorldRelativePosition WorldPosition()
            {
                return (WorldRelativePosition)WorldOrientation();
            }
        }
    }
}
