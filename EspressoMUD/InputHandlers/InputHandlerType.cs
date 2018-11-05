using KejUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD.InputHandlers
{
    /// <summary>
    /// Singletons which are selected for a Client depending on their settings / user client program / etc.
    /// Filters input from the client and interrupts with a new InputHandler if the filter matches.
    /// </summary>
    public abstract class InputHandlerType
    {
        public delegate InputHandler CheckInputHandler(Client session, byte nextChar);

        /// <summary>
        /// Check if the session should add handlers. InputHandlerType should see if it's appropriate to add a handler, and if so
        /// call session.AddInputHandler(handler) to do so.
        /// </summary>
        /// <param name="session"></param>
        public abstract void TryAddHandlers(Client session);

        /// <summary>
        /// Checks if two InputHandlerTypes are the same. By default, only one InputHandlerType of a given C# Type can be used at a time,
        /// InputHandlerTypes may override Equals to allow multiple instances of a single InputHandlerType
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            return (obj.GetType() == this.GetType());
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return this.GetType().GetHashCode();
        }
    }
}
