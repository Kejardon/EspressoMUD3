using KejUtils.SharedLocks;
using KejUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace EspressoMUD
{
    /// <summary>
    /// Manages locks and threads
    /// </summary>
    public static class ThreadManager
    {
        public static bool MUDIsPaused { get { return PauseRequests == 0; } }
        /// <summary>
        /// Default amount of time to wait to try to get a lock, in milliseconds. -1 would be to wait forever.
        /// </summary>
        public const int DefaultTimeout = 1000;

        private static ConditionalWeakTable<object, ReaderWriterLockSlim> CustomLocks = new ConditionalWeakTable<object, ReaderWriterLockSlim>();
        //private static Dictionary<Thread, List<MUDLock>> Locks = new Dictionary<Thread, List<MUDLock>>();
        /// <summary>
        /// Collection of threads that may be modifying MUD state. 
        /// This field should never be used except with a lock on WaitUntilNoThreads.
        /// </summary>
        private static HashSet<Thread> Locks = new HashSet<Thread>();
        /// <summary>
        /// Pause threads here that are trying to get a MUDLock. When the MUD is no longer paused, the threads will be released.
        /// Lock this object directly before using MUDIsPaused
        /// </summary>
        private static ManualResetEvent WaitUntilUnpaused = new ManualResetEvent(true);
        /// <summary>
        /// Pause threads here that are waiting for all MUDLocks to be returned. When Locks is empty, the threads will be released.
        /// Lock this object directly before using Locks
        /// </summary>
        private static ManualResetEvent WaitUntilNoThreads = new ManualResetEvent(true);
        private static int PauseRequests = 0;
        //private static object PauseLock = new object();


        /// <summary>
        /// Pauses the MUD. Caller can pause with using(), or manually unpause the MUD when they are done.
        /// </summary>
        /// <param name="waitUntilThreadsDone">If true, this thread blocks until the MUD is actually paused.</param>
        /// <param name="timeout">If waitUntilThreadsDone is true, MUD will block for this long before giving up and returning. WaitUntilNoThreads could maybe be checked if it's important to know?</param>
        /// <returns>Disposable to unpause the MUD when a specific block of code is done.</returns>
        public static IDisposable PauseMUD(bool waitUntilThreadsDone, int timeout = -1)
        {
            lock (WaitUntilUnpaused)
            {
                WaitUntilUnpaused.Reset();
                PauseRequests++;
            }
            if (waitUntilThreadsDone)
            {
                WaitUntilNoThreads.WaitOne(timeout);
            }
            return new MUDPause();
        }


        public static void UnpauseMUD()
        {
            lock (WaitUntilUnpaused)
            {
                PauseRequests--;
                if (PauseRequests == 0)
                    WaitUntilUnpaused.Set();
            }
        }
        //private static MUDLock CreateMUDLock(object forObject)
        //{
        //    MUDLock newLock = new MUDLock(forObject);
        //    lock (WaitUntilNoThreads)
        //    {
        //        List<MUDLock> threadLocks;
        //        if(!Locks.TryGetValue(Thread.CurrentThread, out threadLocks))
        //        {
        //            Locks[Thread.CurrentThread] = threadLocks = new List<MUDLock>();
        //        }
        //        threadLocks.Add(newLock);
        //        WaitUntilNoThreads.Set();
        //    }
        //    return newLock;
        //}
        /// <summary>
        /// Provides a unique ReaderWriterLockSlim for any object, guaranteed.
        /// </summary>
        /// <param name="forObject">object to get an associated lock object for.</param>
        /// <returns></returns>
        //private static ReaderWriterLockSlim GetRWLock(object forObject)
        //{
        //    //if (forObject == null) return null;
        //    ILockable asLockable = forObject as ILockable;
        //    ReaderWriterLockSlim rwLock;
        //    if (asLockable != null)
        //    {
        //        rwLock = asLockable.Lock;
        //        if (rwLock == null) { asLockable.Lock = rwLock = new ReaderWriterLockSlim(); }
        //        return rwLock;
        //    }
        //    //TODO: There should be a warning logged here. Ideally no object should need custom lock mutexes. Relying on this causes performance degredation.
        //    lock (CustomLocks)
        //    {
        //        if (!CustomLocks.TryGetValue(forObject, out rwLock))
        //        {
        //            rwLock = new ReaderWriterLockSlim();
        //            CustomLocks.Add(forObject, rwLock);
        //        }
        //    }
        //    return rwLock;
        //}

        //private static MUDLock GetMUDLock(object forObject, int timeout, bool ignorePause)
        //{
        //    ReaderWriterLockSlim lockObject = GetRWLock(forObject);
        //    if (ignorePause)
        //    {
        //        if (!lockObject.TryEnterWriteLock(timeout))
        //            return default(MUDLock);
        //        return CreateMUDLock(forObject);
        //    }
        //    else
        //    {
        //        DateTime start = DateTime.Now;
        //        int nextTimeout = timeout;
        //        while (true)
        //        {
        //            if (!lockObject.TryEnterWriteLock(nextTimeout))
        //                return default(MUDLock);
        //            lock (WaitUntilUnpaused)
        //            {
        //                if (MUDIsPaused) goto tryPause;
        //                return CreateMUDLock(forObject);
        //            }
        //        tryPause:
        //            //leave timeout to -1 if it was that before, or subtract the timespan used but not below 0.
        //            nextTimeout = Math.Max(Math.Min(timeout, 0), timeout - (int)TimeSpan.FromTicks(DateTime.Now.Ticks - start.Ticks).TotalMilliseconds);
        //            if (!WaitUntilUnpaused.WaitOne(nextTimeout))
        //                return default(MUDLock);
        //            nextTimeout = Math.Max(Math.Min(timeout, 0), timeout - (int)TimeSpan.FromTicks(DateTime.Now.Ticks - start.Ticks).TotalMilliseconds);
        //        }
        //    }
        //}

        /// <summary>
        /// Get a lock for a specific event and resources related to that event. Returns a RoomEvent if successful, or null if
        /// it failed to get the lock.
        /// </summary>
        /// <param name="forRoom"></param>
        /// <param name="baseEvent"></param>
        /// <param name="timeout">-1 to wait forever. 0 to not wait at all. Otherwise milliseconds to wait.</param>
        /// <param name="ignorePause">Allow this to get a lock even if the MUD is paused. This probably should never be used.</param>
        /// <returns></returns>
        public static RoomEvent StartEvent(Room forRoom, RoomEvent baseEvent = null, int timeout = ThreadManager.DefaultTimeout, bool ignorePause = false)
        {
            DateTime startTime = DateTime.UtcNow;

            if (baseEvent == null)
                baseEvent = new SimpleRoomEvent();

            //Skip MUDIsPaused check if this thread is already modifying MUD state.
            ignorePause |= LockableLockGroup.HasALock();
            using (IDisposable MUDLock = ignorePause ? GetMUDLockIgnorePause() : GetMUDLock(startTime.RemainingTimeout(timeout)))
            {
                if (MUDLock != null)
                {

                    LockableLock foundLock = baseEvent.lockToDispose = forRoom.EnterLock(baseEvent, startTime.RemainingTimeout(timeout));
                    try
                    {
                        //TODO: set up locks and stuff.
                        if (foundLock == null) //Failed to get a lock.
                        {
                            return null;
                        }

                        return baseEvent;
                    }
                    catch (Exception e)
                    {
                        try
                        {
                            baseEvent.Dispose();
                        }
                        catch (Exception) { } //Log? Probably not, really just want to log and fix the first exception.
                        throw e;
                    }
                }
            }
            return null;
        }
        public static void DisposeEvent(RoomEvent finishedEvent)
        {
            if (!LockableLockGroup.HasALock())
            {
                lock (WaitUntilNoThreads)
                {
                    Locks.Remove(Thread.CurrentThread);
                    if (Locks.Count == 0)
                    {
                        WaitUntilNoThreads.Set();
                    }
                }
            }
        }

        /// <summary>
        /// Get the lock for a specific object. Should always be wrapped in a using block. If the MUDLock
        /// returned is default(MUDLock), a lock was not obtained and the related object should not be edited.
        /// </summary>
        /// <param name="forObject"></param>
        /// <param name="timeout"></param>
        /// <param name="ignorePause">If this should ignore </param>
        /// <returns></returns>
        //public static MUDLock GetLock(object forObject, int timeout, bool ignorePause)
        //{
        //    //TODO: Convert forObject to a room-container if possible?
        //    return GetMUDLock(forObject, timeout, ignorePause);
        //}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="forObject"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        //public static MUDLock GetLock(object forObject, int timeout = ThreadManager.DefaultTimeout)
        //{
        //    //TODO: Convert forObject to a room-container if possible?
        //    return GetMUDLock(forObject, timeout, Locks.ContainsKey(Thread.CurrentThread));
        //}
        //public static void ReturnLock(MUDLock oldLock)
        //{
        //    //object forObject = oldLock.LockObject;
        //    ReaderWriterLockSlim lockObject = GetRWLock(oldLock.LockObject);
        //    lockObject.ExitWriteLock();
        //    lock (WaitUntilNoThreads)
        //    {
        //        List<MUDLock> threadLocks = Locks[oldLock.OwningThread];
        //        threadLocks.Remove(oldLock);
        //        if(threadLocks.Count == 0)
        //        {
        //            Locks.Remove(oldLock.OwningThread);
        //            if(Locks.Count == 0)
        //            {
        //                WaitUntilNoThreads.Reset();
        //            }
        //        }
        //    }
        //}

        private static ThreadLockDisposable reusableDisposable = new ThreadLockDisposable();
        [ThreadStatic]
        private static int LockCount = 0;
        /// <summary>
        /// Get a lock to notify the MUD that this thread may modify things on the MUD.
        /// Note that this ignores some checks; it should NOT be used when this thread will modify MUD state and doesn't
        /// already have a lock. Otherwise it may allow the MUD to modify objects as the database is trying to save them
        /// or other similar issues.
        /// </summary>
        /// <returns></returns>
        public static IDisposable GetMUDLockIgnorePause()
        {
            lock (WaitUntilNoThreads)
            {
                if (LockCount == 0)
                {
                    Locks.Add(Thread.CurrentThread);
                }
                LockCount++;
                WaitUntilNoThreads.Reset();
            }
            return reusableDisposable;
        }
        /// <summary>
        /// Attempts to get a lock to allow this thread to modify things on the MUD. Prevents some global things like finishing
        /// a write to the database.
        /// Caller should dispose of the recieved object when it is no longer working directly with the MUD state.
        /// </summary>
        /// <param name="timeout">How long to wait if the MUD is paused before giving up.
        /// -1 will wait forever. 0 will not wait and fail immediately if the MUD is paused.</param>
        /// <returns>A disposable object if a lock was successfully gotten. Null if no lock was gotten and the caller should abort.</returns>
        public static IDisposable GetMUDLock(int timeout = -1)
        {
            DateTime startTime = DateTime.Now;
            while(true)
            {
                lock (WaitUntilUnpaused)
                {
                    if (!MUDIsPaused)
                    {
                        lock (WaitUntilNoThreads)
                        {
                            if (LockCount == 0)
                            {
                                Locks.Add(Thread.CurrentThread);
                            }
                            LockCount++;
                            WaitUntilNoThreads.Reset();
                        }
                        return reusableDisposable;
                    }
                }
                if (!WaitUntilUnpaused.WaitOne(startTime.RemainingTimeout(timeout)))
                {
                    //Timed out while MUD was paused.
                    return null;
                }
            }
        }
        private class ThreadLockDisposable : IDisposable
        {
            public void Dispose()
            {
                lock (WaitUntilNoThreads)
                {
                    if (!Locks.Contains(Thread.CurrentThread))
                    {
                        throw new InvalidOperationException("Current thread does not have a MUD lock.");
                    }
                    LockCount--;
                    if (LockCount == 0)
                    {
                        Locks.Remove(Thread.CurrentThread);
                        if (Locks.Count == 0)
                        {
                            WaitUntilNoThreads.Set();
                        }
                    }
                }
            }
        }

        private class SimpleRoomEvent : RoomEvent
        {
            //TODO: What does this need?
        }
    }
    //public struct MUDLock : IDisposable
    //{
    //    public object LockObject;
    //    public Thread OwningThread;
    //    public MUDLock(object forObject)
    //    {
    //        LockObject = forObject;
    //        OwningThread = Thread.CurrentThread;
    //    }

    //    public void Dispose()
    //    {
    //        if (LockObject != null)
    //        {
    //            ILockable isLockable = LockObject as ILockable;
    //            if (isLockable != null)
    //            {
    //                ThreadManager.ReturnLock(this);
    //            }
    //        }
    //    }
    //}
    public class MUDPause : IDisposable
    {
        bool finished = false;
        public void Dispose()
        {
            if (!finished)
            {
                ThreadManager.UnpauseMUD();
                finished = true;
            }
        }
    }
}
