using KejUtils;
using KejUtils.SharedLocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD
{
    /// <summary>
    /// List of event types that may be used for RoomEvents.
    /// </summary>
    public enum EventType
    {
        //TryEvents are mostly internal to a MOB, planning how to do an action.
        //Not ENTIRELY sure I want these but they kinda make sense, probably will use them later.
        TryGo, //Try to go to a specific place. Figure out how to get there, continue with an actual movement event.

        //Intentional motion from a self-powered object
        Movement, //Unspecified type of motion. Used when logging on to announce a player 'entering'.

        Look //Active action to look at current surroundings or a particular object
    }
    public delegate bool InterestingCheck(Item item);

    public abstract class RoomEvent : ILockHolder, IDisposable
    {
        /// <summary>
        /// Generally this should always be 1. If there are ever important events that need to go first they can
        /// have a higher priority.
        /// </summary>
        public int LockPriority
        {
            get
            {
                return 1;
            }
        }

        /// <summary>
        /// What portion of a tick this action takes. Can go from 0 (instantaneous, literally impossible to interrupt, only
        /// reacted to) to 1 (spending the entire tick performing this action, may have continued before/after this tick).
        /// </summary>
        /// <returns></returns>
        //public virtual double TickDuration() { return 1; }
        public abstract double TickDuration();

        /// <summary>
        /// Current calculated time, as a portion of the tick this action is happening in. A value between 0 and TickDuration;
        /// 0 meaning the exact time the action is starting and TickDuration meaning the exact time the action is finished.
        /// Classes extending RoomEvent should provide functions telling when interesting moments in the action happen.
        /// </summary>
        public double CurrentTickFraction { get; private set; }
        /// <summary>
        /// Current calculated time. Used for estimating how much time things have to accomplish actions. Note that things may
        /// happen out-of-order according to calculated times, do not rely on this for knowing what happened in what order.
        /// </summary>
        public DateTime CurrentCalculatedTime { get {
                return CalculatedStartTime() + new TimeSpan((long)
                    (ThreadManager.RealMillisecondsPerTick *
                     TimeSpan.TicksPerMillisecond *
                     CurrentTickFraction));
            } }

        /// <summary>
        /// The 'object' 'causing' this event. Often a MOB's body. Might also be an autonomous object or an enchanted object,
        /// or something that the effect is being channeled through.
        /// </summary>
        /// <returns></returns>
        public abstract Item EventSource();

        //TODO: Need some of these, maybe with virtual implementations. For now, just implementing Types, probably don't need
        //anything more.
        public abstract EventType Type { get; }
        //public abstract EventType Type();
        //public abstract bool IsMultipleTypes();

        /// <summary>
        /// Called when another event was happening but is now paused, and this event is happening at the same time.
        /// RespondToInterrupt will be called for the other event also.
        /// </summary>
        /// <param name="otherHolder"></param>
        /// <param name="ownThread"></param>
        public virtual void InterruptOtherEvent(ILockHolder otherHolder, bool ownThread)
        {
        }
        /// <summary>
        /// Called when this event was happening but is now paused, and another event is happening at the same time.
        /// InterruptOtherEvent has already been called for the other event also.
        /// </summary>
        /// <param name="otherHolder"></param>
        /// <param name="ownThread"></param>
        public virtual void RespondToInterrupt(ILockHolder otherHolder, bool ownThread)
        {
        }

        /// <summary>
        /// Start of an Event. Gets the locks needed to make sure parsing and event processing can work safely.
        /// Can also do setup or parsing or similar things. If it fails, should return null and notify the MOB why it failed.
        /// </summary>
        /// <returns></returns>
        public abstract IDisposable StarttEventLocks();

        /// <summary>
        /// This event is ready to fire.
        /// Do any last preparation for this event and confirm this event should still fire.
        /// This event may submit a different event instead during FinishEventSetup. If it does so, it should return false here.
        /// </summary>
        /// <returns>False if this event should be canceled at the last second.</returns>
        public virtual bool FinishEventSetup(out RoomEvent replacementEvent)
        {
            replacementEvent = null;
            return true;
        }

        public bool Canceled { get; private set; } = false;
        /// <summary>
        /// Called by listeners responding to this event. Informs all listeners waiting to respond that the event has been
        /// canceled by another listener.
        /// </summary>
        public void CancelEvent()
        {
            if (Canceled) return;
            Canceled = true;
            IEventResponder listener;
            //First tell the listeners that would be responding at the same time to respond.
            while ((listener = GetNextCurrentResponder()) != null)
            {
                listener.RespondToEvent(this);
            }
            //Then tell all remaining listeners that the event was canceled before they responded.
            ListenerEntry[] remainingListeners = listeners.ToArray();
            foreach (ListenerEntry entry in listeners.ToArray())
            {
                entry.Listener.EventCanceled(this);
            }
        }

        /// <summary>
        /// Called by classes extending this. Informs all listeners waiting to respond that the event has been modified by
        /// another listener.
        /// Subclasses should create a public function that listeners can call, which performs the modification then calls
        /// this function.
        /// </summary>
        protected void ModifiedEvent()
        {
            if (Canceled) return;
            foreach (ListenerEntry entry in listeners.ToArray())
            {
                entry.Listener.EventModified(this);
            }
        }
        
        private class ResponseComparer : IComparer<RoomEvent>
        {
            public static IComparer<RoomEvent> instance = new ResponseComparer();
            public int Compare(RoomEvent x, RoomEvent y)
            {
                return (int)(x.calculatedStartTime - y.calculatedStartTime).Ticks;
            }
        }
        private SortedDuplicableList<RoomEvent> responses;
        /// <summary>
        /// Should only be called by listeners while this event is firing. Add a response that 'starts' at the current time,
        /// </summary>
        /// <param name="response"></param>
        public void AddEventResponse(RoomEvent response)
        {
            if (responses == null) responses = new SortedDuplicableList<RoomEvent>(ResponseComparer.instance);
            response.SetCalculatedStart(this);
            responses.Add(response);
        }
        private struct ListenerEntry
        {
            public ListenerEntry(IEventResponder listener, double listenTime)
            {
                Listener = listener;
                TickFraction = listenTime;
            }

            public IEventResponder Listener;
            public double TickFraction;
        }
        private class ListenerComparer : IComparer<ListenerEntry>
        {
            public static IComparer<ListenerEntry> instance = new ListenerComparer();
            public int Compare(ListenerEntry x, ListenerEntry y)
            {
                double result = x.TickFraction - y.TickFraction;
                return result > 0 ? 1 : (result < 0 ? -1 : 0);
            }
        }
        private SortedDuplicableList<ListenerEntry> listeners = new SortedDuplicableList<ListenerEntry>(ListenerComparer.instance);
        /// <summary>
        /// Called when adding listeners. Usually called before firing an event, but they can be added later also, as long as they would be triggered later.
        /// </summary>
        /// <param name="listener"></param>
        /// <param name="listenTime"></param>
        public bool AddResponder(IEventResponder listener, double listenTime)
        {
            if (listenTime < CurrentTickFraction) return false; //Don't add listeners that would have been triggered in the past.
            listeners.Add(new ListenerEntry(listener, listenTime));
            return true;
        }
        private DateTime calculatedStartTime;
        /// <summary>
        /// Only used by the framework. Calculated Timestamp of when this event fired, not necessarily aligned to ticks/real time.
        /// Used to calculate what tick this event should happen during.
        /// </summary>
        /// <returns></returns>
        public DateTime CalculatedStartTime()
        {
            if (calculatedStartTime == default(DateTime))
            {
                calculatedStartTime = DateTime.UtcNow;
            }
            return calculatedStartTime;
        }
        /// <summary>
        /// Only used by the framework. Sets the Calculated Timestamp.
        /// </summary>
        /// <param name="currentEvent"></param>
        public void SetCalculatedStart(RoomEvent currentEvent)
        {
            DateTime original = currentEvent.calculatedStartTime;
            if (original == default(DateTime))
            {
                calculatedStartTime = DateTime.UtcNow;
            }
            else
            {
                calculatedStartTime = currentEvent.CurrentCalculatedTime;
            }
        }
        /// <summary>
        /// Get the next listener to happen chronologically. Essentially the iterator for this event.
        /// </summary>
        /// <returns></returns>
        public IEventResponder GetNextResponder()
        {
            if (listeners.Count == 0) return null;
            ListenerEntry entry = listeners[0];
            listeners.RemoveAt(0);
            //This should be impossible.
            if (CurrentTickFraction > entry.TickFraction) throw new Exception("Listeners called in the wrong order!");
            CurrentTickFraction = entry.TickFraction;
            return entry.Listener;
        }
        /// <summary>
        /// Gets the next listener that happens at the same time. Interruptions do not prevent/interrupt other responses that
        /// also happened at the same time as the interruption.
        /// </summary>
        /// <returns></returns>
        public IEventResponder GetNextCurrentResponder()
        {
            if (listeners.Count == 0) return null;
            ListenerEntry entry = listeners[0];
            if (entry.TickFraction != CurrentTickFraction) return null;
            listeners.RemoveAt(0);
            return entry.Listener;
        }

        /// <summary>
        /// Simpler function to call instead of ThreadManager.StartEvent. Return value works the same way.
        /// </summary>
        /// <param name="mob"></param>
        /// <returns></returns>
        public IDisposable StartEventFor(MOB mob, int timeout = 1000, bool? ignorePause = null)
        {
            restart:
            Body body = mob.Body;
            Room startingRoom = body.Position.ForRoom;
            IDisposable disposable;
            using (disposable = ThreadManager.StartEvent(startingRoom, this, timeout, ignorePause))
            {
                if (disposable == null)
                {
                    return null; //Notify caller that event entirely failed.
                }
                if (mob.Body != body || body.Position.ForRoom != startingRoom)
                {
                    // Race condition, need to try again with new body/get lock for new room.
                    goto restart; //This is probably fine but might make something smarter later.
                }
                disposable = null; //Have the caller dispose this instead.
                return this;
            }
        }

        /// <summary>
        /// RoomEvents should be created for using blocks. This cleans up the event after it is finished, and triggers
        /// any events waiting to go after this event.
        /// </summary>
        public void Dispose()
        {
            // Clean up lock on the resources.
            if (lockToDispose != null)
            {
                lockToDispose.Dispose();
                lockToDispose = null;
            }
            // Clean up lock on the MUD thread states.
            ThreadManager.DisposeMUDLock();
        }
        private LockableLock lockToDispose;
        public void SetLockToDispose(LockableLock newLock)
        {
            if (lockToDispose == null)
            {
                lockToDispose = newLock;
            }
            else
            {
                throw new InvalidOperationException("Lock to use has already been set for this event.");
            }
        }
        public void AddRoomLock(Room roomToLock)
        {
            lockToDispose.AddResource(roomToLock);
        }
        public void AddMOBLock(MOB mob)
        {
            while (true)
            {
                Body body = mob.Body;
                Room startingRoom = body.Position.ForRoom;
                lockToDispose.AddResource(startingRoom);
                if (body == mob.Body && body.Position.ForRoom == startingRoom) return;
            }
        }

        public void AddItemLock(Item item)
        {
            while (true)
            {
                Room startingRoom = item.Position.ForRoom;
                lockToDispose.AddResource(startingRoom);
                if (item.Position.ForRoom == startingRoom) return;
            }
        }

        /// <summary>
        /// Check if the specified MOB can see this event happening.
        /// </summary>
        /// <param name="mob">The MOB trying to observe this event.</param>
        /// <param name="focus">What the MOB is using to see this event (their own eyes, a scrying effect, etc.)</param>
        /// <returns></returns>
        public virtual bool CanObserveThis(MOB mob, object focus)
        {
            return false;
        }

        /// <summary>
        /// The message shown to a user if they are able to observe this event happening. If null,
        /// nothing is sent. Else at least an empty line will be sent.
        /// </summary>
        /// <returns></returns>
        public virtual void SendObservedMessage(Client client)
        {
            throw new Exception("Observable events must override SendObservedMessage.");
        }
    }

    public static partial class Extensions
    {
        /// <summary>
        /// Run an event. Event should have been passed starting parameters and not run yet.
        /// </summary>
        /// <param name="roomEvent">Event to run</param>
        public static void FullRunEvent(this RoomEvent roomEvent)
        {
            RoomEvent altEvent;
        start:
            using (IDisposable roomLock = roomEvent.StarttEventLocks())
            {
                if (roomLock == null) return; //Event failed during first setup or could not get the room lock.
                if (roomEvent.FinishEventSetup(out altEvent))
                {
                    GetAllListeners(roomEvent);
                    FireEvent(roomEvent);
                }
                else if (altEvent != null)
                {
                    roomEvent = altEvent;
                    goto start;
                }
            }
        }
        private static void GetAllListeners(this RoomEvent roomEvent)
        {
            Room room = roomEvent.EventSource().Position.ForRoom;
            List<Item> items = room.FindListeningItems(roomEvent);
            foreach (Item item in items)
            {
                item.AddEventListeners(roomEvent);
            }
        }
        /// <summary>
        /// Only call this after getting listeners for an event.
        /// </summary>
        /// <param name="roomEvent"></param>
        private static void FireEvent(this RoomEvent roomEvent)
        {
            roomEvent.CalculatedStartTime(); //Make sure the event has a time.
            while(!roomEvent.Canceled)
            {
                IEventResponder listener = roomEvent.GetNextResponder();
                if (listener == null) break;
                listener.RespondToEvent(roomEvent);
            }
        }
    }
}