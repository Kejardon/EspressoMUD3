using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EspressoMUD.Prompts
{

    /// <summary>
    /// A HeldPrompt base with more common safeguards/handling. Prevents re-entry with an exception, prevents concurrent calls silently.
    /// Subclasses should implement InnerRespond instead of Respond. InnerValid and InnerCancel may optionally be overridden.
    /// </summary>
    public abstract class StandardHeldPrompt : HeldPrompt
    {
        private ReaderWriterLockSlim Lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private bool InPrompt = false;
        protected MOB AssociatedMOB;

        /// <summary>
        /// Subclasses can check this to see if Cancel has been called before. Subclasses probably should call Cancel instead of setting this.
        /// </summary>
        protected bool Canceled = false;

        /// <summary>
        /// Prompt to return to if this one is finished. Lowest priority.
        /// </summary>
        protected StandardHeldPrompt ReturnTo { get; set; }

        /// <summary>
        /// Subclasses can set this to end this prompt and continue with the next prompt. Lower priority than
        /// returning directly from InnerRespond, higher priority than the calledBy prompt.
        /// </summary>
        protected HeldPrompt NextPrompt = null;

        //public StandardHeldPrompt() { }
        /// <summary>
        /// Create a 'subprompt' that will return to the previous prompt when this one is finished.
        /// </summary>
        /// <param name="calledBy"></param>
        public StandardHeldPrompt(StandardHeldPrompt calledBy, MOB character = null)
        {
            ReturnTo = calledBy;
            User = calledBy?.User;
            AssociatedMOB = character;
        }
        /// <summary>
        /// Create a 'subprompt' that will return to the previous prompt when this one is finished.
        /// </summary>
        /// <param name="calledBy"></param>
        public StandardHeldPrompt(StandardHeldPrompt calledBy, MOB character, Client player)
        {
            ReturnTo = calledBy;
            User = player;
            AssociatedMOB = character;
        }

        public override sealed bool IsStillValid()
        {
            if (Canceled) { return false; }
            return InnerValid();
        }

        public override void OnTransition()
        {
            NextPrompt = null;
            base.OnTransition();
        }

        /// <summary>
        /// Attempt to transition to the next prompt. This must be called during the prompt, and there must not be a next prompt yet.
        /// Caller still needs to finish processing the current prompt to continue to the next prompt.
        /// </summary>
        /// <param name="nextPrompt"></param>
        /// <param name="setThisAsReturn"></param>
        /// <returns>True if this will call the next prompt, else false.</returns>
        public bool TransitionTo(HeldPrompt nextPrompt)
        {
            if (InPrompt && Lock.IsWriteLockHeld && NextPrompt == null)
            {
                NextPrompt = nextPrompt;
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Attempt to transition to the next prompt. This must be called during the prompt, and there must not be a next prompt yet.
        /// Caller still needs to finish processing the current prompt to continue to the next prompt.
        /// </summary>
        /// <param name="nextPrompt"></param>
        /// <param name="setThisAsReturn">Attempts to Note: No warning is given if this fails.</param>
        /// <returns>True if this will call the next prompt, else false.</returns>
        public bool TransitionTo(StandardHeldPrompt nextPrompt, bool setThisAsReturn)
        {
            if (TransitionTo(nextPrompt))
            {
                if (setThisAsReturn && nextPrompt != null && nextPrompt.ReturnTo == null)
                {
                    nextPrompt.ReturnTo = this;
                }
                return true;
            }
            else
                return false;
        }

        public override sealed HeldPrompt Respond(string userString)
        {
            if (userString == null)
            {
                Cancel();
            }
            else
            {
                if (Lock.TryEnterWriteLock(0))
                {
                    try
                    {
                        if (!Canceled && !InPrompt)
                        {
                            try
                            {
                                InPrompt = true;
                                InnerRespond(userString);
                                return NextPrompt ?? (Canceled ? ReturnTo : null);
                            }
                            finally
                            {
                                InPrompt = false;
                            }
                        }
                        else if (Canceled)
                        {
                            User.sendMessage("That prompt has expired.");
                        }
                        else
                        {
                            User.sendMessage("That prompt has expired.");
                            //TODO: This is an unusual case, I think only when another thread is canceling this one while the user is attempting to respond at the same time.
                            //I think Cancel should usually have been called so just repeating the same text for canceled.
                        }
                    }
                    finally
                    {
                        Lock.ExitWriteLock();
                    }
                }
                //else ignore multiple threads attempting to enter a lock at the same time.
                //TODO: Just ignore or send some message somewhere?
            }
            return (Canceled ? ReturnTo : null);
        }
        
        protected void RecursiveCancel()
        {
            if (ReturnTo != null)
            {
                ReturnTo.InnerCancel();
                ReturnTo.RecursiveCancel();
                ReturnTo = null;
            }
        }

        protected void Cancel(bool andReturn = true)
        {
            InnerCancel();
            if (!andReturn)
            {
                RecursiveCancel();
            }
            if (!Canceled)
            {
                if (ReturnTo == null)
                {
                    User.RemovePrompt(this);
                }
                Canceled = true;
            }
        }

        protected abstract void InnerRespond(string userString);
        protected virtual void InnerCancel() { }
        protected virtual bool InnerValid() { return true; }
    }
}
