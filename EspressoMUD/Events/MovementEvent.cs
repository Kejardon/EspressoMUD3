using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD
{

    /// <summary>
    /// A room event where the vehicle (usually a person's body) is attempting to move deliberately to a goal.
    /// The SubType of this event specifies what type of motion is being used.
    /// For unintentional motion (falling / thrown / rolling), the Momentum effect (TODO) is used instead.
    /// </summary>
    public class MovementEvent : RoomEvent
    {
        public enum MovementTypes
        {
            Unspecified, //Only used for TryGo. Hasn't decided on a movement type yet.
            Spawn, //Appearing from nowhere. Used for created / logging in.

            Walk, //Normal legged motion from one place to another across a (mostly) contiguous surface
            //Run, //Fast legged motion from one place to another. Maybe not, this is literally just walk.
            //Crawl, //Alternate legged motion from one place to another. Changes hitbox.
            //Squeeze, //Legged motion from one place to another. Changes hitbox, collides with nearby obstacles.
            //Jump, //psuedo-flying legged motion from one place to another. Only requires surfaces on start and end.

            Fly, //Moving in the air, without directly touching solid surfaces.
            Teleport, //Movement goes from a start position to an end position without actually traveling across space in between.
        }
        public const int MovementTypeCount = 3; //Walk, Fly, Teleport


        public MovementEvent()
        {
        }

        public override EventType Type { get { return EventType.Movement; } }


        public MovementTypes SubType;

        /// <summary>
        /// Physical object that is the destination of this movement action. If null, vehicle is just moving
        /// in a direction without a specific destination. Longest term goal.
        /// Only used as a hint of where they might be going for messages.
        /// </summary>
        public Item target;

        /// <summary>
        /// Exit that the vehicle is trying to travel through. If null, vehicle is not trying to travel to another room.
        /// Takes priority over target. Usually an intermediate goal, but may be the last goal.
        /// Only used as a hint of where they might be going for messages.
        /// </summary>
        public RoomLink targetExit;

        //TODO later: Replace these IPositions with a Path of some kind.
        /// <summary>
        /// Position that the vehicle is trying to travel to immediately. Actual thing being used by movement calculations.
        /// </summary>
        public IRoomPosition targetPosition;
        public IRoomPosition originalPosition;

        /// <summary>
        /// Which direction and/or how far to try to move, after the current room. If null, the vehicle isn't going to an
        /// arbitrary point outside the room (going to a specific item or a point inside the current room instead). Longest term goal.
        /// </summary>
        //public MovementDirection direction;

        /// <summary>
        /// What the action is trying to do; move 'onto' or 'into' or just 'near' the target.
        /// Only used as a hint of where they might be going for messages.
        /// </summary>
        public MovementPreposition relation;

        /// <summary>
        /// How far the vehicle moves per tick (10000 = 1 meter per tick).
        /// This may be more than the vehicle may move in the whole action or move.
        /// </summary>
        public double speed;



        protected Body eventSource;
        protected MOB movementSource;
        public override Item EventSource()
        {
            return eventSource;
        }
        public MOB MoveSource() { return movementSource; }
        public void SetMoveSource(MOB mob, Body vehicle)
        {
            movementSource = mob;
            eventSource = vehicle ?? mob.Body;
            originalPosition = eventSource.Position;
        }

        protected double tickDuration = -1;
        public override double TickDuration()
        {
            if (tickDuration == -1)
            {
                if (eventSource.Position.ForRoom == targetPosition.ForRoom)
                {
                    //TODO: Path calculation here when Path replaces targetPosition
                    //Distance distance = eventSource.Position.DistanceTo(targetPosition);
                    //tickDuration = Math.Min(1, distance.total / speed);
                }
                else
                {
                    tickDuration = 0;
                }
            }
            return tickDuration;
        }

        public override IDisposable StarttEventLocks()
        {
            IDisposable disposable, returnValue;
            using (disposable = ThreadManager.StartEvent(originalPosition.ForRoom, this))
            {
                if (originalPosition.ForRoom != eventSource.Position.ForRoom) AddItemLock(eventSource);
                if (originalPosition.ForRoom != movementSource.Body.Position.ForRoom) AddMOBLock(movementSource);
                if (originalPosition.ForRoom != targetPosition.ForRoom) AddRoomLock(targetPosition.ForRoom);
                
                returnValue = disposable;
                disposable = null;
                return returnValue;
            }
        }
    }

    //TODO: Implement this.
    // Not sure if there will be several Path implementations. There will probably be a variety of PathSegment options that
    // Paths can have a list of.
    //  PathSegments probably always have a StartPosition and EndPosition. Not sure if more is actually needed.
    //  PathSegment probably needs to specify the MoveType. May have different types of PathSegment for each MoveType.
    //    Walking (and similar things like crawling or running) will probably specify surfaces to some extent.
    //    and simple 2D paths on the surface. Paths simple lines or arcs?
    //    Flying will alwyas be straight lines?
    //public class Path
    //{

    //}
}
