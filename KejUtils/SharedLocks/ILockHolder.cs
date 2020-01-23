using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KejUtils.SharedLocks
{
    /// <summary>
    /// 
    /// </summary>
    public interface ILockHolder
    {
        /// <summary>
        /// Priority for a lock. Only used when deadlock is hit, otherwise active threads keep priority.
        /// Highest priority (higher integer) LockHolder will run first.
        /// In case of ties in priority, the earliest lock obtained will run first.
        /// </summary>
        int LockPriority { get; }

        /// <summary>
        /// Optional action when this event is interrupting another event.
        /// This is called before RespondToInterrupt on the other event. This may be called multiple times for the same
        /// LockHolder, if that LockHolder has multiple independant locks.
        /// </summary>
        /// <param name="otherHolder">The event being interrupted.</param>
        /// <param name="ownThread">True if otherHolder is on the same thread / is a parent event.</param>
        void InterruptOtherEvent(ILockHolder otherHolder, bool ownThread);

        /// <summary>
        /// Optional response to this event being interrupted by another event.
        /// This is called after InterruptOtherEvent from the other event. This may be called multiple times for the same
        /// LockHolder, if that LockHolder has multiple independant locks.
        /// </summary>
        /// <param name="otherHolder">The event interrupting this event.</param>
        /// <param name="ownThread">True if otherHolder is on the same thread / is a child event.</param>
        void RespondToInterrupt(ILockHolder otherHolder, bool ownThread);
    }
}
