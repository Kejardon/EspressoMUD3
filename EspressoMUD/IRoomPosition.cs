using EspressoMUD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD
{
    /// <summary>
    /// Generic interface for how a room manages the position of an object inside of itself.
    /// </summary>
    public interface IRoomPosition : ISubobject
    {
        /// <summary>
        /// The room that this position goes to.
        /// Positions should never be able to change rooms - if an item moves to a new room the old position object
        /// must be discarded and a new position must be created for the new room.
        /// </summary>
        Room ForRoom { get; }
        /// <summary>
        /// The 'world' that this position goes to. All rooms with the same origin room can be resolved to comparable points
        /// to calculate distances and paths and such. If rooms have different origin rooms, it means that they MUST go through
        /// a portal or some similar thing to be compared, otherwise the two points are completely unreachable from eachother.
        /// </summary>
        Room OriginRoom { get; }

        int X { get; set; }
        int Y { get; set; }
        int Z { get; set; }
        /// <summary>
        /// The coordinate position inside the room this position represents. This is not necessarily how it is stored internally
        /// nor is 'get' guaranteed to return a value assigned with 'set'.
        /// </summary>
        KejUtils.Geometry.Point PositionValue { get; set; }
        /// <summary>
        /// Cardinal direction of the face. Rotations starting with north, should always be [0,1). Applied last, after Rotation
        /// 0: Facing north.
        /// 0.25: Facing east.
        /// 0.5: Facing south.
        /// 0.75: Facing west.
        /// </summary>
        float Direction { get; set; }
        /// <summary>
        /// Tilt of the face. Quarter-rotations from standing upright, should always be [-1,1]. Applied first, before Rotation.
        /// 0: Normal (Facing forward).
        /// 1: On back (Facing up).
        /// -1: On front (Facing down).
        /// </summary>
        float Tilt { get; set; }
        /// <summary>
        /// Rotation around the axis of the face. Half-rotations starting with 'feet down', should always be (-1,1]. Applied second, after Tilt.
        /// 0: Normal
        /// 0.5: On right side
        /// 1: Upside down
        /// -0.5: On left side
        /// </summary>
        float Roll { get; set; }
        Rotation Rotation { get; set; }

        /// <summary>
        /// Get the position relative to the world's origin. Generally this requires calculations.
        /// </summary>
        /// <returns></returns>
        WorldRelativePosition WorldPosition();
        /// <summary>
        /// Get the position and orientation relative to the world's origin. Generally this requires calculations.
        /// </summary>
        /// <returns></returns>
        WorldRelativeOrientation WorldOrientation();
        /// <summary>
        /// Apply a direction to this position. If the new position is in the same room, set newPosition and return true.
        /// If the new position is outside of the room and there's a viable exit in that direction to the passed in mob,
        /// set the exit and how far left there is to go after reaching the exit, then return true.
        /// If neither works, return false.
        /// TODO: Add a parameter for known obstacles the MOB cannot get through. Return a Path instead of newPosition.
        /// Include MovementTypes in returned Path instead of modifying moveType parameter.
        /// </summary>
        /// <param name="mob">The person trying to decide on a path - they must be able to sense the exit to choose it,
        /// and measurements are done in their perspective. If null, no checks are done.</param>
        /// <param name="vehicle">The object moving on the path. Usually mob's Body.</param>
        /// <param name="direction">How far to move and in what direction.</param>
        /// <param name="moveType">How the MOB will attempt to move. If this Unspecified, then this function should set
        /// it to indicate a valid way for the MOB to try to move.</param>
        /// <param name="newPosition">Return value, the new position in the room. Null if 'direction' can't be reached
        /// in the same room.</param>
        /// <param name="leftoverDirection">Return value, the MovementDirection to travel once the exit is reached.
        /// Null if can't / shouldn't leave the room to travel 'direction'.</param>
        /// <param name="exit">Return value, the exit to travel to first. Null if can't / shouldn't leave the room to travel 'direction'.</param>
        /// <returns>True if there's somewhere new to go to based on this position and direction. False if there's nowhere to go.</returns>
        //bool FindNewPositionOrExit(MOB mob, Body vehicle, MovementDirection direction, ref MovementEvent.MovementTypes moveType, out IRoomPosition newPosition, out MovementDirection leftoverDirection, out RoomLink exit);

        /// <summary>
        /// Apply a direction to this position. If the new position is in the same room, set newPosition and return true.
        /// If the new position is outside of the room and there's a viable exit in that direction to the passed in mob,
        /// set the exit and how far left there is to go after reaching the exit, then return true.
        /// If neither works, return false.
        /// </summary>
        /// <param name="mob">The person trying to decide on a path - they must be able to sense the exit to choose it,
        /// and measurements are done in their perspective. If null, no checks are done.</param>
        /// <param name="direction">How far to move and in what direction.</param>
        /// <param name="newPosition">Return value, the new position in the room. Null if 'direction' can't be reached
        /// in the same room.</param>
        /// <param name="leftoverDirection">Return value, the PreciseMovementDirection to travel once the exit is reached.
        /// Null if can't / shouldn't leave the room to travel 'direction'.</param>
        /// <param name="exit">Return value, the exit to travel to first. Null if can't / shouldn't leave the room to travel 'direction'.</param>
        /// <returns>True if there's somewhere new to go to based on this position and direction. False if there's nowhere to go.</returns>
        //bool FindNewPositionOrExit(MOB mob, PreciseMovementDirection direction, out IRoomPosition newPosition, out PreciseMovementDirection leftoverDirection, out RoomLink exit);

        //Distance DistanceTo(IRoomPosition otherPosition);

        /// <summary>
        /// If this object is loosely 'attached' to another object.
        /// </summary>
        Item RestingOn { get; set; }
        /// <summary>
        /// Helper data. When this object is loosely 'attached' to another object, which side of the object it is attached to.
        /// N/A if no object.
        /// </summary>
        //For now just a surface index is enough. Long term maybe this will change to something fancier.
        int RestingOnId { get; set; }
        /// <summary>
        /// What pose the object is in, wether it's on a surface or in the air.
        /// </summary>
        PoseMechanism RestingPose { get; set; }
        //ObstacleSurface RestingOn { get; set; }



        /// <summary>
        /// Return a duplicate of this position that is guaranteed to not be modified by something else.
        /// If this IPosition implementation is immutable it may return itself.
        /// </summary>
        /// <returns></returns>
        //IRoomPosition Clone();
    }
}
