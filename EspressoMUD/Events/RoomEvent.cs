using KejUtils.SharedLocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD
{
    public enum EventType
    {
        //TryEvents are mostly internal to a MOB, planning how to do an action
        TryGo, //Try to go to a specific place. Figure out how to get there, continue with an actual movement event.

        Walk, //Normal legged motion from one place to another across a (mostly) contiguous surface
        Look //Active action to look at current surroundings or a particular object
    }

    public abstract class RoomEvent : ILockHolder, IDisposable
    {
        public static bool IsMovement(EventType eventType)
        {
            switch (eventType)
            {
                case EventType.Walk:
                    return true;
            }
            return false;
        }

        public int LockPriority
        {
            get
            {
                return 1;
            }
        }

        public virtual void InterruptOtherEvent(ILockHolder otherHolder, bool ownThread)
        {
        }

        public virtual void RespondToInterrupt(ILockHolder otherHolder, bool ownThread)
        {
        }

        public void Dispose()
        {
            if (lockToDispose != null)
            {
                lockToDispose.Dispose();
                lockToDispose = null;
            }
            ThreadManager.DisposeEvent(this);
        }


        /// <summary>
        /// This should only be used by ThreadManager to set up the lock to dispose of when this object is being disposed.
        /// </summary>
        public LockableLock lockToDispose { private get; set; }
    }
}
