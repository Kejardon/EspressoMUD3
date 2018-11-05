using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD.InputHandlers
{
    public class EscapeHandlerType : InputHandlerType
    {
        public override void TryAddHandlers(Client session)
        {
            if (!(session.CurrentInputHandler is EscapeHandler))
            {
                session.AddInputChecker(this.EscapeInputChecker);
            }
        }
        private InputHandler EscapeInputChecker(Client session, byte nextChar)
        {
            if(nextChar == 27)
            {
                return new EscapeHandler(session.CurrentInputHandler);
            }
            return null;
        }
    }
    public class EscapeHandler : InputHandler
    {

        public EscapeHandler(InputHandler previous) : base(previous) { }

        List<byte> input = new List<byte>(32);
        bool validStart = false;
        public override void HandleNextChar(Client session, byte c)
        {
            if (!validStart)
            {
                validStart = c == '[';
                if(!validStart)
                {
                    //Ignore the esc and hope things work out
                    this.ReturnToPrevious(session);
                    //session.AddInputChar(c);
                }
                return;
            }

            switch (c)
            {
                case (byte)'0':
                case (byte)'1':
                case (byte)'2':
                case (byte)'3':
                case (byte)'4':
                case (byte)'5':
                case (byte)'6':
                case (byte)'7':
                case (byte)'8':
                case (byte)'9':
                case (byte)';':
                    this.input.Add(c);
                    break;
                case (byte)'z':
                    string inputted = session.Encoder.GetString(this.input.ToArray());
                    int value;
                    if (int.TryParse(inputted, out value))
                    {
                        //TODO: Handle MXP
                    }
                    break;
                case (byte)'m':
                    //TODO: Validate as a color
                    break;
                default:
                    this.ReturnToPrevious(session);
                    break;
            }
        }
    }
}
