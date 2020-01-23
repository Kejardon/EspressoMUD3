using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EspressoMUD.Geometry;
using KejUtils.Geometry;

namespace EspressoMUD.Rooms
{
    //On second thought, I don't really want this.
    //A 'PositionlessRoom' will be useful in the future. For things like a bag where everything is just thrown in and unsorted.
    //No reason not to just use normal rooms most of the time though.
    /*
    public class DiscreteRoom : Room
    {
        [SaveField("Width", Default = 3)]
        private int width = 3;

        [SaveField("Height", Default = 3)]
        private int height = 3;

        [SaveField("Length", Default = 3)]
        private int length = 3;

        [SaveField("UnitSize", Default = 100)]
        private int unitSize = 100;



        public class DiscreteRoomPosition : IRoomPosition
        {
            public DiscreteRoomPosition(DiscreteRoom creatingRoom, object parent)
            {
                this.creatingRoom = creatingRoom;
                Parent = parent;
            }
            public float Direction
            {
                get
                {
                    return -2;
                }

                set
                {
                }
            }

            private DiscreteRoom creatingRoom;
            public Room forRoom
            {
                get
                {
                    return creatingRoom;
                }
            }

            public Room OriginRoom
            {
                get
                {
                    return creatingRoom.Position.OriginRoom;
                }
            }

            public object Parent
            {
                get;

                set;
            }

            public Point PositionValue
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

            public Item RestingOn
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

            public int RestingOnId
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

            public float Roll
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

            public float Tilt
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

            public Distance DistanceTo(IRoomPosition otherPosition)
            {
                throw new NotImplementedException();
            }

            public bool FindNewPositionOrExit(MOB mob, PreciseMovementDirection direction, out IRoomPosition newPosition, out PreciseMovementDirection leftoverDirection, out RoomLink exit)
            {
                throw new NotImplementedException();
            }

            public bool FindNewPositionOrExit(MOB mob, Body vehicle, MovementDirection direction, ref MovementEvent.MovementTypes moveType, out IRoomPosition newPosition, out MovementDirection leftoverDirection, out RoomLink exit)
            {
                throw new NotImplementedException();
            }

            public WorldRelativeOrientation WorldOrientation()
            {
                throw new NotImplementedException();
            }

            public WorldRelativePosition WorldPosition()
            {
                throw new NotImplementedException();
            }
        }

    }
    */
}
