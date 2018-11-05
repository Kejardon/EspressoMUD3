using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD.InputHandlers
{
    public abstract class InputHandler
    {
        protected InputHandler previousHandler;

        public InputHandler(InputHandler previous)
        {
            previousHandler = previous;
        }

        public void CalledBy(Client session)
        {
            //InputHandler previous = session.CurrentInputHandler;
            //if (previous != null)
            //{
                //previous.CallNextInputHandler(session, this);

                //previous.VerifyInputHandler(session);
                //this.previousHandler = previous;
            //    session.CurrentInputHandler = this;
            //}
            //else
            //{
                session.CurrentInputHandler = this;
            //}
        }

        /// <summary>
        /// Returns from the current InputHandler to the previous one for a session.
        /// </summary>
        /// <param name="session"></param>
        protected void ReturnToPrevious(Client session)
        {
            VerifyInputHandler(session);
            //session.CurrentInputHandler = previousHandler;
            if (previousHandler == null) throw new Exception("Can't return from this handler");
            session.TransitionToInputHandler(previousHandler);
        }

        /// <summary>
        /// Replace the current InputHandler for a session.
        /// </summary>
        /// <param name="session">Session to add a new Handler to</param>
        /// <param name="nextHandler">New InputHandler</param>
        protected void GoToNextInputHandler(Client session, InputHandler nextHandler)
        {
            VerifyInputHandler(session);
            nextHandler.previousHandler = this.previousHandler;
            //session.CurrentInputHandler = nextHandler;
            session.TransitionToInputHandler(nextHandler);
        }
        /// <summary>
        /// Add a new InputHandler to the bottom of the session's stack.
        /// </summary>
        /// <param name="session">Session to add a new Handler to</param>
        /// <param name="nextHandler">New InputHandler</param>
        protected void CallNextInputHandler(Client session, InputHandler nextHandler)
        {
            VerifyInputHandler(session);
            nextHandler.previousHandler = this;
            //session.CurrentInputHandler = nextHandler;
            session.TransitionToInputHandler(nextHandler);
        }

        /// <summary>
        /// For debug / error catching, verifies that this is the session's current InputHandler
        /// </summary>
        /// <param name="session">Session to check</param>
        protected void VerifyInputHandler(Client session)
        {
            InputHandler currentHandler = session.CurrentInputHandler;
            //Technically this doesn't break things, but it violates the expected codeflow and is a sign something else is broken.
            if (currentHandler != this) throw new Exception("Wrong input handler is changing the session.");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        public abstract void HandleNextChar(Client session, byte c);


    }

    public class DefaultInputHandler : InputHandler
    {
        public DefaultInputHandler() : base(null) { }

        enum PrevEndOfLine
        {
            None,
            CR,
            LF
        }
        PrevEndOfLine prev = PrevEndOfLine.None;
        public override void HandleNextChar(Client session, byte c)
        {
            if(c == 10)
            {
                if(prev == PrevEndOfLine.CR)
                {
                    session.FlushInput();
                    // \n\n, flush last one and keep going
                }
                else if (prev == PrevEndOfLine.LF)
                {
                    session.FlushInput();
                    prev = PrevEndOfLine.None;
                    // \r\n, definite end of line 
                }
                else
                {
                    if (session.HasMoreInput())
                    {
                        prev = PrevEndOfLine.CR;
                        // \n?, check next character
                    }
                    else
                    {
                        session.FlushInput();
                        prev = PrevEndOfLine.None;
                        // \n EOM, assume input is done and flush
                    }
                }
            }
            else if (c == 13)
            {
                if (prev == PrevEndOfLine.CR)
                {
                    session.FlushInput();
                    prev = PrevEndOfLine.None;
                    // \n\r, not technically correct but I've seen it so just accept it
                }
                else if (prev == PrevEndOfLine.LF)
                {
                    session.FlushInput();
                    // \r\r, flush last one and keep going
                }
                else
                {
                    if (session.HasMoreInput())
                    {
                        prev = PrevEndOfLine.LF;
                        // \r?, check next character
                    }
                    else
                    {
                        session.FlushInput();
                        prev = PrevEndOfLine.None;
                        // \r EOM, assume input is done and flush
                    }
                }
            }
            else if (prev == PrevEndOfLine.None)
            {
                session.AddInputChar(c);
            }
            else
            {
                // an \r or \n by itself. Assume end of line and continue with next line
                prev = PrevEndOfLine.None;
                session.FlushInput();
                session.AddInputChar(c);
            }
        }
    }

}
