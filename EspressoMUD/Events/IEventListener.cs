using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD
{
    public interface IEventListener
    {
        /// <summary>
        /// Check if this EventListener wants to add a responder to the event. AddEventListener should call forEvent.AddResponder
        /// if it wants to add a responder.
        /// </summary>
        /// <param name="forEvent"></param>
        void AddEventListener(RoomEvent forEvent);
    }
    public interface IEventResponder
    {
        /// <summary>
        /// Called when the event has fired and it is this responder's turn to react to the event.
        /// </summary>
        /// <param name="firedEvent"></param>
        void RespondToEvent(RoomEvent firedEvent);
        /// <summary>
        /// Called when the event has fired but another responder has modified the event before this responder was called.
        /// If nothing is done, this responder will still be called later.
        /// </summary>
        /// <param name="firedEvent"></param>
        void EventModified(RoomEvent firedEvent);
        /// <summary>
        /// Called when the event has fired but another responder has stopped the event before this responder was called.
        /// This responder will NOT be called later.
        /// </summary>
        /// <param name="firedEvent"></param>
        void EventCanceled(RoomEvent firedEvent);
    }

    /// <summary>
    /// Simple light wrapper for directly responding to a known event. Useful if several callbacks are available for different
    /// types of events and the listener wants to pre-emptively choose a specific callback.
    /// </summary>
    public struct ResponderWrapper : IEventResponder
    {
        public ResponderWrapper(Action<RoomEvent> fire, Action<RoomEvent> cancel = null, Action<RoomEvent> modify = null)
        {
            fireCallback = fire;
            cancelCallback = cancel;
            modifyCallback = modify;
        }

        Action<RoomEvent> fireCallback;
        Action<RoomEvent> cancelCallback;
        Action<RoomEvent> modifyCallback;

        public void EventCanceled(RoomEvent firedEvent)
        {
            fireCallback(firedEvent);
        }

        public void EventModified(RoomEvent firedEvent)
        {
            modifyCallback?.Invoke(firedEvent);
        }

        public void RespondToEvent(RoomEvent firedEvent)
        {
            cancelCallback?.Invoke(firedEvent);
        }
    }
    /// <summary>
    /// Simple light wrapper for directly responding to a known event, with additional context data.
    /// Common case is passing a body, for events being seen by a MOB (usually a player).
    /// </summary>
    public struct ResponderWrapper<T> : IEventResponder
    {
        public ResponderWrapper(T focus, Action<RoomEvent, T> fire, Action<RoomEvent, T> cancel = null, Action<RoomEvent, T> modify = null)
        {
            this.focus = focus;
            fireCallback = fire;
            cancelCallback = cancel;
            modifyCallback = modify;
        }
        private T focus;

        Action<RoomEvent, T> fireCallback;
        Action<RoomEvent, T> cancelCallback;
        Action<RoomEvent, T> modifyCallback;

        public void EventCanceled(RoomEvent firedEvent)
        {
            fireCallback(firedEvent, focus);
        }

        public void EventModified(RoomEvent firedEvent)
        {
            modifyCallback?.Invoke(firedEvent, focus);
        }

        public void RespondToEvent(RoomEvent firedEvent)
        {
            cancelCallback?.Invoke(firedEvent, focus);
        }
    }
}
