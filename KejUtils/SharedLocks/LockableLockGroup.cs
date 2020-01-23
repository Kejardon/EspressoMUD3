
using System;
using System.Collections.Generic;
using System.Threading;

namespace KejUtils.SharedLocks
{
    /// <summary>
    /// Lock that helps detect and work around deadlocks.
    /// Ideally this should be used by threads. This can be used by tasks, but it is important that the tasks do NOT share threads.
    /// If a thread is currently trying to acquire more locks, then it should have first cleaned up everything currently in this lock group to a safe
    /// state, so that other threads may also access the data currently managed by the thread. It MUST clean everything up before letting another
    /// thread execute with resources locked in this group.
    /// </summary>
    public class LockableLockGroup
    {
        /// <summary>
        /// Current LockableLockGroup being used by a thread. Represents that this thread owns this lock.
        /// This ensures that child events will use the same lock group.
        /// </summary>
        [ThreadStatic]
        private static LockableLockGroup CurrentLockGroup;

        /// <summary>
        /// When two threads manage to have identical priority, they can get a priority from this int with an atomic inc-and-read action.
        /// (should be extraordinarily rare, but nothing technically stops it from happening)
        /// </summary>
        private static int PriorityChooser = 0;

        /// <summary>
        /// Final priority authority if everything else fails, assigned from static incremented variable.
        /// -1 if not set yet, otherwise will only be set once and never modified again later.
        /// Lower numbers = higher priority.
        /// </summary>
        private volatile int subSubPriority = -1;


        private LockableLockGroup()
        {
        }

        #region Status variables
        /// <summary>
        /// Monitored object for changes in status of this lock group. This lock is not held while the event is doing things, only for
        /// a brief bit of time when the event starts or stops, or other threads want to check the status.
        /// </summary>
        private readonly object statusMutex = new object();
        /// <summary>
        /// List of events that have this as their primary lock group.
        /// Because these will all be from the same thread, this list is essentially a queue; event 0 is the root / parent event, and each later
        /// event is a child event.
        /// This is populated as soon as the lock group is created. If it is 0, it is a sign that the lock group has been discarded.
        /// </summary>
        private readonly List<LockableLock> eventQueue = new List<LockableLock>();
        /// <summary>
        /// Currently LockableLockGroup that has a resource this group is waiting on. Used to detect lock loops/deadlocks.
        /// </summary>
        private LockableLockGroup waitingOn;
        /// <summary>
        /// Flag for blocking, similar to a ResetEvent. Helps prevent race conditions from sleeping/waking up threads.
        /// </summary>
        private bool willWait = false;
        #endregion

        /// <summary>
        /// Checks of this lock contains the other lock. Should only be called by this own thread while it's active, assumes
        /// there's no race conditions.
        /// </summary>
        /// <param name="other"></param>
        /// <param name="recursive">True if subgroups of subgroups should be searched</param>
        /// <returns></returns>
        private bool contains(LockableLockGroup other, bool recursive)
        {
            if (other == this)
            {
                return true;
            }
            foreach (LockableLock nextEvent in this.eventQueue)
            {
                if (nextEvent.ownedSubgroups == null)
                {
                    continue;
                }
                foreach (LockableLockGroup nextGroup in nextEvent.ownedSubgroups)
                {
                    if (recursive)
                    {
                        if (nextGroup.contains(other, recursive)) return true;
                    }
                    else if (nextGroup == other) return true;
                }
            }
            return false;
        }

        private HashSet<LockableLockGroup> containedGroups(bool recursive, HashSet<LockableLockGroup> foundSoFar = null)
        {
            if (foundSoFar == null)
            {
                foundSoFar = new HashSet<LockableLockGroup>();
            }
            foreach (LockableLock entry in eventQueue)
            {
                if (entry.ownedSubgroups == null) continue;
                foreach (LockableLockGroup nextGroup in entry.ownedSubgroups)
                {
                    if (foundSoFar.Add(nextGroup) && recursive)
                    {
                        nextGroup.containedGroups(recursive, foundSoFar);
                    }
                }
            }
            return foundSoFar;
        }

        /// <summary>
        /// </summary>
        /// <param name="newSubgroup">Subgroup being used by a new event</param>
        /// <param name="forEvent">Event interrupting things by taking the subgroup</param>
        private void interruptAllWithGroup(LockableLockGroup newSubgroup, ILockHolder forEvent)
        {
            foreach (LockableLock myEvent in eventQueue)
            {
                if (myEvent.ownedSubgroups != null && myEvent.ownedSubgroups.Contains(newSubgroup))
                {
                    forEvent.InterruptOtherEvent(myEvent.holder, false);
                    myEvent.holder.RespondToInterrupt(forEvent, false);
                }
            }
        }

        /// <summary>
        /// Make sure an event contains a subgroup. Does nothing if it already does.
        /// Handles interrupts for events in that subgroup, and also interrupts for other groups using that subgroup.
        /// </summary>
        /// <param name="currentEvent">Event to add the subgroup to. Must be part of this lock group.</param>
        /// <param name="newSubgroup">Subgroup to try to add to the event.</param>
        private void addToLockableLock(LockableLock currentEvent, LockableLockGroup newSubgroup)
        {
            lock (statusMutex)
            {
                if (currentEvent.ownedSubgroups == null)
                {
                    currentEvent.ownedSubgroups = new List<LockableLockGroup>();
                }
                if (currentEvent.ownedSubgroups.Contains(newSubgroup)) return;
                //Don't have the lock on other lock groups, but that's okay, because they already said they are waiting
                //and this will be the thread that wakes them up, so they won't modify themselves right now.
                currentEvent.ownedSubgroups.Add(newSubgroup);
            }
            foreach (LockableLock otherEvent in newSubgroup.eventQueue)
            {
                currentEvent.holder.InterruptOtherEvent(otherEvent.holder, false);
                otherEvent.holder.RespondToInterrupt(currentEvent.holder, false);
            }
            foreach (LockableLockGroup interruptedGroup in containedGroups(true))
            {
                interruptedGroup.interruptAllWithGroup(newSubgroup, currentEvent.holder);
            }
        }

        /// <summary>
        /// Get the highest priority lock in this lock group.
        /// </summary>
        /// <returns></returns>
        private LockableLock highestPriority()
        {
            LockableLock highestPriority = this.eventQueue[0];
            for (int i = 0; i < eventQueue.Count; i++)
            {
                LockableLock otherEvent = eventQueue[i];
                if (highestPriority.comparePriority(otherEvent) < 0)
                    highestPriority = otherEvent;
            }
            return highestPriority;
        }

        /// <summary>
        /// Compare this event's priority to the other group's priority. Finds and returns the highest priority event.
        /// </summary>
        /// <param name="previousHighest"></param>
        /// <param name="otherGroup">MUST HAVE THE LOCK ON THIS GROUP'S MUTEX</param>
        /// <returns></returns>
        private LockableLock chooseHighestPriority(LockableLock previousHighest, LockableLockGroup otherGroup)
        {
            LockableLock otherHighest = otherGroup.highestPriority();
            int priority = previousHighest.comparePriority(otherHighest);
            if (priority == 0)
            {
                if (otherGroup.subSubPriority == -1)
                {
                    otherGroup.subSubPriority = Interlocked.Increment(ref LockableLockGroup.PriorityChooser);
                }
                if (previousHighest.group.subSubPriority == -1)
                {
                    //Previous event doesn't have a priority yet and will get a new value later, which must be higher than otherGroup's value.
                    //So the other event has priority.
                    priority = -1;
                }
                else
                {
                    priority = otherHighest.group.subSubPriority - previousHighest.group.subSubPriority;
                }
            }
            if (priority < 0)
                return otherHighest;
            return previousHighest;
        }

        /// <summary>
        /// Wait until there are no events using this resource and this deadlock group is discarded, so another thread
        /// can start a new lock group on this resource.
        /// </summary>
        private void WaitToEnter(DateTime startTime, int timeout)
        {
            int remaining = startTime.RemainingTimeout(timeout);
            while (remaining != 0)
            {
                if (Monitor.TryEnter(statusMutex, remaining)) try
                    {
                        if (eventQueue.Count == 0) return; //Can try to get the lock.
                        Monitor.Wait(statusMutex, startTime.RemainingTimeout(timeout));
                    }
                    finally
                    {
                        Monitor.Exit(statusMutex);
                    }
                else
                {
                    //Timer expired. Expect to return without a lock.
                    return;
                }
            }
        }
        /// <summary>
        /// Get the lock for the requested resource. This may block if the resource is locked by another thread.
        /// This will avoid deadlocks in case of multiple threads locking the same resources; one thread will allow
        /// another thread to take its resources and interrupt it depending on event priority.
        /// </summary>
        /// <param name="resource">Resource to get the lock on</param>
        /// <param name="newLock">Returned lock that represents the event's lock on this resource.</param>
        internal void AddResource(ILockable resource, LockableLock newLock)
        {
            LockableLockGroup otherGroup;
        //LockableLock currentEvent = eventQueue[eventQueue.Count - 1];
        restartAddResource:
            LockableLock highestPriority = null;
            bool wakeHighestPriority = false;

            //Check if we can trivially get the lock
            lock (resource.LockMutex)
            {
                otherGroup = resource.CurrentLock;
                if (otherGroup == null || otherGroup.eventQueue.Count == 0)
                {
                    resource.CurrentLock = this;
                    return;
                }
            }
            //Check if we already have the lock
            if (this.contains(otherGroup, false))
            {
                //Already have the lock somewhere. Make sure *this* event also has it marked as being used.
                addToLockableLock(newLock, otherGroup);
                return;
            }

            //Check if we're in a deadlock.
            List<LockableLockGroup> otherGroupQueue;
            lock (otherGroup.statusMutex)
            {
                if (otherGroup.eventQueue.Count == 0)
                {
                    goto restartAddResource;
                }
                otherGroupQueue = new List<LockableLockGroup>();
                otherGroupQueue.Add(otherGroup);
            }

            lock (this.statusMutex)
            {
                this.waitingOn = otherGroupQueue[0];
                this.willWait = true;
            }
            try
            {
                lock (otherGroup.statusMutex)
                {
                    while (otherGroup.waitingOn == null)
                    {
                        if (otherGroup.eventQueue.Count == 0)
                        {
                            goto restartAddResource;
                        }
                        //Other group is active. Have to wait for that group first.
                        Monitor.Wait(otherGroup.statusMutex);
                    }
                    if (otherGroup.eventQueue.Count == 0)
                    {
                        goto restartAddResource;
                    }
                    highestPriority = this.highestPriority();
                    otherGroupQueue.Add(otherGroup.waitingOn);
                    highestPriority = chooseHighestPriority(highestPriority, otherGroup);
                }
                //Waiting on a lock that is not active. Check to see what we should do.
                while (true)
                {
                    LockableLockGroup nextGroup = otherGroupQueue[otherGroupQueue.Count - 1];
                    lock (nextGroup.statusMutex)
                    {
                        //Make sure lock is up to date.
                        if (nextGroup.eventQueue.Count == 0)
                        {
                            //This event is out of date, meaning there is an active thread. We can wait until the group we're waiting
                            //on finishes, or another thread tells us we're the highest priority thread.
                            break;
                        }
                        //Lock isn't out of date. Check which group this is.
                        else if (this.contains(nextGroup, true))
                        {
                            //There is a loop/deadlock, eventually otherGroup is waiting on this group.
                            if (this.contains(highestPriority.group, true))
                            {
                                //We are the highest priority thread found. We get to take a new subgroup and continue getting locks.
                                goto setupSubgroup;
                            }
                            //else wake up the highestPriority thread.
                            wakeHighestPriority = true;
                            break;
                        }
                        LockableLockGroup nextNextGroup = nextGroup.waitingOn;
                        if (nextNextGroup == null)
                        {
                            //Not currently in a deadlock. Continue and wait on otherGroup.
                            break;
                        }
                        foreach (LockableLockGroup otherWaitingGroup in otherGroupQueue)
                        {
                            if (otherWaitingGroup == nextNextGroup)
                            {
                                //There's a circular loop, but this thread's not in it. Consider it the same as an active thread
                                break;
                            }
                        }
                        //This group is also waiting on a thread. Check the next event/group it is waiting on.
                        highestPriority = chooseHighestPriority(highestPriority, nextGroup);
                        otherGroupQueue.Add(nextGroup.waitingOn);
                        continue;
                    }
                }

                //Need to wait for another thread.
                if (wakeHighestPriority)
                {
                    //We're in a deadlock and not the highest priority thread. Make sure the highest priority thread is woken up.
                    object mutex;
                    lock (highestPriority.group.statusMutex)
                    {
                        //Tell that thread to wake up
                        highestPriority.group.willWait = false;
                        mutex = highestPriority.group.waitingOn?.statusMutex;
                    }

                    // If this is out of date, the highest priority thread already did stuff and we didn't need to wake it up anyways.
                    if (mutex != null) lock (mutex)
                        {
                            Monitor.PulseAll(mutex);
                        }
                    wakeHighestPriority = false;
                }
                lock (otherGroup.statusMutex)
                {
                    //This isn't the lock for willWait, but the thread that would wake this thread up would clear willWait, then try to get this same lock, then wake it up, so it works out.
                    if (willWait && otherGroup.eventQueue.Count > 0)
                        Monitor.Wait(otherGroup.statusMutex);
                }
                goto restartAddResource;
            }
            finally
            {
                lock (this.statusMutex)
                {
                    this.waitingOn = null;
                    this.willWait = false;
                }
            }
        setupSubgroup:
            //This is the highest priority thread. Setup subgroups.
            addToLockableLock(newLock, otherGroup);
            return;

        }

        /// <summary>
        /// Add an event to the event queue.
        /// </summary>
        /// <param name="newEvent"></param>
        private LockableLock addEvent(ILockHolder newEvent)
        {
            LockableLock newLock = new LockableLock(newEvent, this);
            eventQueue.Add(newLock);
            return newLock;
        }

        /// <summary>
        /// Check if the current thread already has a lock.
        /// </summary>
        /// <returns></returns>
        public static bool HasALock()
        {
            return CurrentLockGroup != null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="objectToLock"></param>
        /// <param name="forEvent"></param>
        /// <returns></returns>
        internal static LockableLock EnterLock(ILockable objectToLock, ILockHolder forEvent, int timeout = Timeout.Infinite)
        {
            LockableLockGroup currentLock;
            DateTime startTime = DateTime.UtcNow;

            //Check if this is a subsequent event in a single thread
            if (LockableLockGroup.CurrentLockGroup != null)
            {
                currentLock = LockableLockGroup.CurrentLockGroup;
                foreach (LockableLock ancestorEvent in currentLock.eventQueue)
                {
                    forEvent.InterruptOtherEvent(ancestorEvent.holder, true);
                    ancestorEvent.holder.RespondToInterrupt(forEvent, true);
                }
                LockableLock newLock;
                lock (currentLock.statusMutex)
                {
                    newLock = currentLock.addEvent(forEvent);
                }
                currentLock.AddResource(objectToLock, newLock);
                return newLock;
            }

            while (true)
            {
                if (Monitor.TryEnter(objectToLock.LockMutex, startTime.RemainingTimeout(timeout))) try
                    {
                        currentLock = objectToLock.CurrentLock;
                        if (currentLock == null || currentLock.eventQueue.Count == 0)
                        {
                            currentLock = new LockableLockGroup();

                            objectToLock.CurrentLock = currentLock;
                            LockableLockGroup.CurrentLockGroup = currentLock;
                            return currentLock.addEvent(forEvent);
                        }
                    }
                    finally
                    {
                        Monitor.Exit(objectToLock.LockMutex);
                    }
                else
                {
                    //Failed to get the lock in a reasonable amount of time.
                    return null;
                }
                //A different thread is using this resource. Wait until that thread is done and try again.
                currentLock.WaitToEnter(startTime, timeout);
            }
        }

        /// <summary>
        /// Dispose of this lock.
        /// </summary>
        internal void DisposeOf(LockableLock oldLock)
        {
            lock (statusMutex)
            {
                if (LockableLockGroup.CurrentLockGroup != this)
                    throw new SynchronizationLockException("This thread is not currently using this lock group.");

                if (!eventQueue.Remove(oldLock))
                    throw new SynchronizationLockException("This thread is not currently using this lock group.");

                if (eventQueue.Count == 0)
                {
                    LockableLockGroup.CurrentLockGroup = null;
                    Monitor.PulseAll(statusMutex);
                }

            }
        }
    }


}