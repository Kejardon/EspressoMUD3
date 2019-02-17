using KejUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EspressoMUD.Prompts
{
    public abstract class HeldPrompt
    {
        //Not sure I want this, but there's a good chance I do.
        //private static Dictionary<Thread, HeldPrompt> promptMap = new Dictionary<Thread, HeldPrompt>();

        //public static HeldPrompt CurrentPrompt()
        //{
        //    return CurrentPrompt(Thread.CurrentThread);
        //}
        //public static HeldPrompt CurrentPrompt(Thread currentThread)
        //{
        //    HeldPrompt value;
        //    if (promptMap.TryGetValue(currentThread, out value))
        //    {
        //        return value;
        //    }
        //    return null;
        //}

        /// <summary>
        /// The user that this prompt goes to.
        /// </summary>
        public Client User;

        /// <summary>
        /// Check if this prompt can still be responded to.
        /// </summary>
        /// <returns></returns>
        public abstract bool IsStillValid();
        
        /// <summary>
        /// The ID the client will recognize this prompt under.
        /// </summary>
        public int PromptID;

        /// <summary>
        /// Get the string 
        /// </summary>
        public virtual string PromptMessage { get; }

        /// <summary>
        /// Called whenever this prompt is transitioned to from another place. By default, sends the prompt message to the user.
        /// </summary>
        public virtual void OnTransition()
        {
            if (User != null)
            {
                string message = PromptMessage;
                if (message != null)
                {
                    User.sendMessage(PromptMessage);
                }
            }
        }

        /// <summary>
        /// Call this held prompt back with the user's response.
        /// </summary>
        /// <param name="userString">String the user replied with. "" for blank reply, null for timeout or similar.</param>
        /// <returns>If the response results in another prompt, the next prompt will be returned here to reuse the
        /// same prompt ID instead of requesting a new ID.</returns>
        //Note: This probably needs a bit of extra work to be better thread safe, in case multiple things cause a Respond at once.
        public abstract HeldPrompt Respond(string userString);

        //Probably will delete this.
        ///// <summary>
        ///// Enter a prompt context. Should probably only be used by Client for 
        ///// </summary>
        ///// <returns></returns>
        //public IDisposable Enter()
        //{
        //    Thread currentThread = Thread.CurrentThread;
        //    HeldPrompt value;
        //    if (promptMap.TryGetValue(currentThread, out value))
        //    {
        //        throw new InvalidOperationException("Can't enter multiple prompts in a thread.");
        //    }
        //    promptMap[currentThread] = this;
        //    return new PromptContext(this);
        //}

        //private struct PromptContext : IDisposable
        //{
        //    HeldPrompt parent;
        //    public PromptContext(HeldPrompt prompt)
        //    {
        //        parent = prompt;
        //    }
        //    public void Dispose()
        //    {
        //        Thread currentThread = Thread.CurrentThread;
        //        HeldPrompt value;
        //        if (!promptMap.TryGetValue(currentThread, out value))
        //        {
        //            throw new InvalidOperationException("Must enter a prompt context before leaving it.");
        //        }
        //        promptMap[currentThread] = null;
        //    }
        //}
    }
}
