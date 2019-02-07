using KejUtils.SharedLocks;
using System;
using System.Collections.Generic;

namespace KejUtils.SharedLocks
{
    public class LockableLock : IDisposable
    {
        public LockableLock(ILockHolder holder, LockableLockGroup parentGroup)
        {
            this.holder = holder;
            this.subPriority = DateTime.UtcNow;
            this.group = parentGroup;
        }
        /// <summary>
        /// Event this class represents
        /// </summary>
        internal ILockHolder holder;
        /// <summary>
        /// LockGroup for this event
        /// </summary>
        internal LockableLockGroup group;
        /// <summary>
        /// Second priority if events own priority match up. Earlier events get higher priority.
        /// </summary>
        private DateTime subPriority;
        /// <summary>
        /// List of subgroups that this event has taken the lock for.
        /// </summary>
        internal List<LockableLockGroup> ownedSubgroups;

        /// <summary>
        /// </summary>
        /// <param name="otherEvent"></param>
        /// <returns>Positive if this event has priority. Negative if other event has priority. 0 if the events have the same priority.</returns>
        internal int comparePriority(LockableLock otherEvent)
        {
            if (otherEvent == this) return 0;
            int priority = holder.LockPriority;
            int otherPriority = otherEvent.holder.LockPriority;
            if (priority != otherPriority)
            {
                return priority - otherPriority;
            }
            long ticks = (otherEvent.subPriority - subPriority).Ticks;
            if (ticks != 0)
            {
                return (ticks > 0 ? 1 : -1);
            }
            return 0;
        }

        /// <summary>
        /// Dispose of this lock.
        /// Locks should ideally be in a 'using' statement and call this automatically.
        /// </summary>
        public void Dispose()
        {
            group.DisposeOf(this);
        }

        /// <summary>
        /// Get the lock for the requested resource. This may block if the resource is locked by another thread.
        /// This will avoid deadlocks in case of multiple threads locking the same resources; one thread will allow
        /// another thread to take its resources and interrupt it depending on event priority.
        /// </summary>
        /// <param name="resource"></param>
        public void AddResource(ILockable resource)
        {
            group.AddResource(resource, this);
        }

    }
}

namespace KejUtils
{
    public static partial class Extensions
    {
        /// <summary>
        /// Call this with a using() statement, for the first resource/Lockable an event needs.
        /// Get and set up the current DeadlockHandler for this Lockable. Caller is expected to continue collecting Lockables
        /// with AddResource.
        /// </summary>
        /// <param name="objectToLock"></param>
        /// <param name="forEvent"></param>
        /// <returns></returns>
        public static LockableLock EnterLock(this ILockable objectToLock, ILockHolder forEvent, int timeout = System.Threading.Timeout.Infinite)
        {
            return LockableLockGroup.EnterLock(objectToLock, forEvent, timeout);
        }
    }
}
