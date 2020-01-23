using KejUtils.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD.Geometry
{
    /// <summary>
    /// The position of an object by the world's origin point instead of the room's origin point.
    /// -Z in this is always pointing 'down', in the direction gravity is applied. This is not intended to be saved - it is used
    /// as a cached value while the MUD is running calculated from container and self.
    /// </summary>
    public struct WorldRelativePosition
    {
        public WorldRelativePosition(Room room, Point position)
        {
            OriginRoom = room;
            x = position.x;
            y = position.y;
            z = position.z;
        }

        public static explicit operator Point(WorldRelativePosition p)
        {
            Point newPosition;
            newPosition.x = p.x;
            newPosition.y = p.y;
            newPosition.z = p.z;
            return newPosition;
        }

        public Room OriginRoom;
        public int x;
        public int y;
        public int z;

        public bool EqualTo(WorldRelativePosition p)
        {
            if (OriginRoom != p.OriginRoom) return false;
            if (x != p.x || y != p.y || z != p.z) return false;
            return true;
        }
    }
    public struct WorldRelativeOrientation
    {
        public static explicit operator Point(WorldRelativeOrientation p)
        {
            Point newPosition;
            newPosition.x = p.x;
            newPosition.y = p.y;
            newPosition.z = p.z;
            return newPosition;
        }
        public static explicit operator WorldRelativePosition(WorldRelativeOrientation p)
        {
            WorldRelativePosition newPosition;
            newPosition.OriginRoom = p.OriginRoom;
            newPosition.x = p.x;
            newPosition.y = p.y;
            newPosition.z = p.z;
            return newPosition;
        }
        public static explicit operator Rotation(WorldRelativeOrientation p)
        {
            Rotation newRotation;
            newRotation.Direction = p.Direction;
            newRotation.Roll = p.Roll;
            newRotation.Tilt = p.Tilt;
            return newRotation;
        }
        public WorldRelativeOrientation(WorldRelativePosition p)
        {
            OriginRoom = p.OriginRoom;
            x = p.x;
            y = p.y;
            z = p.z;
            Direction = 0;
            Tilt = 0;
            Roll = 0;
        }

        public Room OriginRoom;
        public int x;
        public int y;
        public int z;
        public float Direction;
        public float Tilt;
        public float Roll;
    }
    //public struct WorldRelativeRotation? Not really needed I don't think
    public struct Orientation
    {
        public static explicit operator Point(Orientation p)
        {
            return new Point() { x = p.x, y = p.y, z = p.z };
        }
        public static explicit operator Rotation(Orientation p)
        {
            return new Rotation() { Direction = p.Direction, Tilt = p.Tilt, Roll = p.Roll };
        }
        public static explicit operator WorldRelativeOrientation(Orientation p)
        {
            return new WorldRelativeOrientation()
            {
                Direction = p.Direction, Tilt = p.Tilt, Roll = p.Roll,
                x = p.x, y = p.y, z = p.z
            };
        }
        public Orientation(int x, int y, int z, float dir, float tilt, float roll)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            Direction = dir;
            Tilt = tilt;
            Roll = roll;
        }
        public int x;
        public int y;
        public int z;
        public float Direction;
        public float Tilt;
        public float Roll;
    }
    public struct Rotation
    {
        public Rotation(float dir, float tilt, float roll)
        {
            Direction = dir;
            Tilt = tilt;
            Roll = roll;
        }

        /// <summary>
        /// Cardinal direction of the face. Rotations starting with north, should always be [0,1). Applied before Tilt
        /// 0: Facing north.
        /// 0.25: Facing east.
        /// 0.5: Facing south.
        /// 0.75: Facing west.
        /// </summary>
        public float Direction;
        /// <summary>
        /// Tilt of the face. Quarter-rotations from standing upright, should always be [-1,1]. Applied after Direction.
        /// 0: Normal (Facing forward).
        /// 1: On back (Facing up).
        /// -1: On front (Facing down).
        /// </summary>
        public float Tilt;
        /// <summary>
        /// Rotation around the axis of the face. Half-rotations starting with 'feet down', should always be (-1,1]. Applied after Tilt.
        /// 0: Normal
        /// 0.5: On right side
        /// 1: Upside down
        /// -0.5: On left side
        /// </summary>
        public float Roll;

        /// <summary>
        /// Check how the hitbox is oriented.
        /// </summary>
        /// <returns>True if the hitbox is mostly upright or upside down. False if it's more on its side or back.
        /// Diagonal defaults to upright.</returns>
        public bool IsVertical()
        {
            float tiltRatio = Math.Abs(Tilt);
            if (tiltRatio > 0.5) return false; //On its front or back
            float remainder = 1 - tiltRatio;
            if (remainder * Math.Abs(Math.Sin(Roll * Math.PI)) < 0.5) return false; //On its side (or some mix of the two)
            return true;
        }
    }
}
