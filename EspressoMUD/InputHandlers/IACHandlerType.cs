using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD.InputHandlers
{
    public class IACHandlerType : InputHandlerType
    {
        public static IACHandlerType Instance = new IACHandlerType();

        public override void TryAddHandlers(Client session)
        {
            if (session.CurrentInputHandler is IACHandler)  return;
            session.AddInputChecker(IACCheckInput);
        }
        private InputHandler IACCheckInput(Client session, byte c)
        {
            if (c == 255)
            {
                return new IACHandler(session.CurrentInputHandler);
            }
            return null;
        }
    }

    public class IACHandler : InputHandler
    {
        public IACHandler(InputHandler previous) : base(previous) { }

        List<byte> input = new List<byte>(32);
        bool holdingIAC = false;
        public override void HandleNextChar(Client session, byte c)
        {
            if (input.Count == 0)
            {
                //Note: Depending on the first char, this could end the IACHandler immediately.
                handleFirstChar(session, c);
                return;
            }
            byte firstChar = input[0];
            if (firstChar != TelnetCode.SB)
            {
                handleOneCharIAC(session, c);
                this.ReturnToPrevious(session);
                return;
            }
            if(holdingIAC)
            {
                switch (c)
                {
                    case 255:
                        input.Add(c);
                        break;
                    case TelnetCode.SE:
                        finishMultiChar(session);
                        this.ReturnToPrevious(session);
                        return;
                    default: //Probably an error. Ignore and hope things work?
                        this.ReturnToPrevious(session);
                        return;
                }
                holdingIAC = false;
                return;
            }
            if(c == 255)
            {
                holdingIAC = true;
                return;
            }
            input.Add(c);
        }
        /// <summary>
        /// Handle finishing an IAC SB ... IAC SE sequence.
        /// </summary>
        /// <param name="session"></param>
        private void finishMultiChar(Client session)
        {
            //Input contains SB ... so skip the SB.
            if (input.Count > 1) switch (input[1])
                {
                    case TelnetCode.TERMTYPE:
                        if (input.Count < 2 || input[2] != TelnetCode.IS)
                        {
                            break;
                        }
                        byte[] data = input.ToArray();
                        string iacString = session.Encoder.GetString(data, 3, data.Length - 3);
                        session.AddTermTypeOption(iacString);
                        break;
                    //TODO later: Implement these
                    //case TelnetCode.MSDP:

                    //    break;
                    //case TelnetCode.ATCP:

                    //    break;
                    //case TelnetCode.NAWS: //Not used atm
                    //case TelnetCode.CHARSET: //Only default atm
                    default:
                        break;
                }
        }
        /// <summary>
        /// Handle the first char after an IAC started.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="c"></param>
        private void handleFirstChar(Client session, byte c)
        {
            switch (c)
            {
                case 255:
                    this.ReturnToPrevious(session); //Do this first to set CurrentInputHandler
                    session.CurrentInputHandler.HandleNextChar(session, c);
                    return;
                case TelnetCode.AYT:
                    session.sendMessage(" \b"); //I don't really know why " \b" exactly. This is what CoffeeMUD does though.
                    this.ReturnToPrevious(session);
                    return;
                case TelnetCode.DO:
                case TelnetCode.DONT:
                case TelnetCode.WILL:
                case TelnetCode.WONT:
                case TelnetCode.SB:
                    input.Add(c);
                    return;
                default: //Probably an error
                    this.ReturnToPrevious(session);
                    return;
            }
        }
        /// <summary>
        /// Handle an IAC with only a single character argument.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="c"></param>
        private void handleOneCharIAC(Client session, byte c)
        {
            switch (c)
            {
                case TelnetCode.TERMTYPE:
                    session.HandleIACTermType(input[0]);
                    break;
                    //TODO later: Implement these
                    //case TelnetCode.MSDP:
                    //    break;
                    //case TelnetCode.MSSP:
                    //    break;
                    //case TelnetCode.MCCP: //Not supported yet
                    //    break;
                    //case TelnetCode.MSP:
                    //    break;
                    //case TelnetCode.MXP:
                    //    break;
                    //case TelnetCode.ATCP:
                    //    break;

            }
            return;
        }
    }
}
