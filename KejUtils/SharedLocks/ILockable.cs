using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KejUtils.SharedLocks
{
    /// <summary>
    /// A thing that needs to have a lock, but may run into deadlock situations.
    /// </summary>
    public interface ILockable
    {
        /// <summary>
        /// Object to lock on when modifying CurrentLock. Must return the same object every time, and never be null.
        /// Must be implemented by parent class.
        /// </summary>
        object LockMutex { get; }
        /* Suggested implementation options:
        public override object LockMutex { get; private set; } = new object();
        public override object LockMutex { get {return this;} }
         */
        /// <summary>
        /// Current group of Locks that contains this Lockable object.
        /// </summary>
        LockableLockGroup CurrentLock { get; set; }
    }
}
