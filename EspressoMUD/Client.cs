using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Collections.Concurrent;
using KejUtils;
using EspressoMUD.InputHandlers;
using EspressoMUD.Prompts;

namespace EspressoMUD
{
    public class Client
    {
        private static int MAX_PROMPTS = 9;
        private static int MAX_PREVMSGS = 50;
        private static readonly string endOfLine = "\r\n";

        private Socket socket;
        private byte[] buffer;
        private readonly Builder<byte> received = new Builder<byte>();
        private readonly HeldPrompt[] prompts = new HeldPrompt[MAX_PROMPTS];
        private HeldPrompt mainPrompt = null; //Initialized to a login prompt.
        private List<InputHandlerType> inputHandlerTypes = new List<InputHandlerType>();
        private List<InputHandlerType.CheckInputHandler> inputHandlerChecks = new List<InputHandlerType.CheckInputHandler>();

        private int parsedBytes; //index for buffer
        private int receivedBytes; //end of buffer
        private List<string> termTypes;
        private string currentTermType;
        /// <summary>
        /// Flag when this login is resetting and removing all prompts. Prevents new prompts from being added.
        /// </summary>
        private bool clearingPrompts;

        /// <summary>
        /// What end of line characters this specific client prefers. This should only be used by the output filter, generally ^n should be used instead.
        /// </summary>
        public string PreferredEndOfLine { get { return endOfLine; } }

        /// <summary>
        /// If true, server is repeating data from the client back to it.
        /// </summary>
        public bool TelnetEcho = false;

        /// <summary>
        /// The last time data of any kind was received from the user's computer.
        /// </summary>
        public DateTime LastReceived { get; private set; }

        /// <summary>
        /// The previous messages received from the user.
        /// </summary>
        public CircularConcurrentQueue<string> LastMessages { get; } = new CircularConcurrentQueue<string>(MAX_PREVMSGS);

        /// <summary>
        /// Encoder to use for this client. This may later be changed to a property.
        /// </summary>
        public Encoding Encoder { get { return Encoding.GetEncoding(28591); } } //Maybe later add UTF support?

        /// <summary>
        /// Account the user is logged in as. This is not set until the user is authenticated.
        /// </summary>
        private Account loggedInAccount;

        public Account LoggedInAccount
        {
            get { return loggedInAccount; }
            set
            {
                if(loggedInAccount != null)
                {
                    loggedInAccount.CurrentLogins.Remove(this);
                }
                loggedInAccount = value;
                if (loggedInAccount != null)
                {
                    loggedInAccount.CurrentLogins.Add(this);
                }
            }
        }

        /// <summary>
        /// Filters and handles normal (or alternative, e.g. MXP) types of data being sent to the server
        /// </summary>
        public InputHandler CurrentInputHandler;


        public Client(Socket s, int packetSize)
        {
            this.socket = s;
            this.buffer = new byte[packetSize];
            this.LastReceived = DateTime.UtcNow;
            this.inputHandlerTypes.Add(IACHandlerType.Instance);
            this.TransitionToInputHandler(new DefaultInputHandler());
            this.socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, receiveData, null);
            negotiateTelnetMode(TelnetCode.TERMTYPE);
            this.mainPrompt = new LoginPrompt(this);
            //TODO: Server intro goes here
            this.sendMessage("Unnamed EspressoMUD Server"); //TODO: Move to external resource file.
            mainPrompt.OnTransition();
        }

        /// <summary>
        /// Ask the client if it will support a particular option.
        /// </summary>
        /// <param name="mode">Telnet option to support</param>
        public void negotiateTelnetMode(byte mode)
        {
            byte[] data = (mode == TelnetCode.TERMTYPE ?
              (new byte[] { TelnetCode.IAC, TelnetCode.SB, mode, TelnetCode.SEND, TelnetCode.IAC, TelnetCode.SE }) :
              (new byte[] { TelnetCode.IAC, TelnetCode.SB, mode, TelnetCode.IAC, TelnetCode.SE }));
            this.socket.BeginSend(data, 0, data.Length, SocketFlags.None, sendData, null);
        }

        /// <summary>
        /// Sends a plain text message to the client.
        /// </summary>
        /// <param name="str">Message to send</param>
        public void sendMessage(String str)
        {
            if (!this.socket.Connected) return;

            StringBuilder output = null;
            //TODO: Move to common filtering code?
            bool endedLine = false;
            int i = 0;
            for (; i < str.Length - 1; i++)
            {
                endedLine = false;
                if (str[i] == '^')
                {
                    if (output == null)
                    {
                        output = new StringBuilder();
                        output.Append(str, 0, i);
                    }

                    i++;
                    switch(str[i])
                    {
                        case '^':
                            output.Append('^');
                            break;
                        case 'n':
                            output.Append(this.PreferredEndOfLine);
                            endedLine = true;
                            break;
                        default:
                            break;
                    }
                }
                else if (output != null)
                {
                    output.Append(str[i]);
                }
            }
            if (output != null && i < str.Length && str[i] != '^')
            {
                output.Append(str[i]);
            }
            if(!endedLine)
            {
                if (output == null)
                {
                    str = str + this.PreferredEndOfLine;
                }
                else
                {
                    output.Append(this.PreferredEndOfLine);
                }
            }

            byte[] data = Encoder.GetBytes(output != null ? output.ToString() : str);
            try
            {
                this.socket.BeginSend(data, 0, data.Length, SocketFlags.None, sendData, null);
            }
            catch (ObjectDisposedException) { } //TODO: Also handle SocketException? Maybe check specific errorcodes?
        }
        /// <summary>
        /// Sends a message to the client in binary, suitable for any type of data.
        /// </summary>
        /// <param name="data">Message to send</param>
        public void sendMessage(byte[] data)
        {
            try
            {
                this.socket.BeginSend(data, 0, data.Length, SocketFlags.None, sendData, null);
            }
            catch (ObjectDisposedException) { }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="toPrompt"></param>
        /// <returns></returns>
        public HeldPrompt GetPrompt(int toPrompt)
        {
            toPrompt--;
            if (toPrompt < 0 || toPrompt >= MAX_PROMPTS)
                return null;
            return this.prompts[toPrompt];
        }

        /// <summary>
        /// Handles a new prompt for this user.
        /// </summary>
        /// <param name="newPrompt"></param>
        public void Prompt(HeldPrompt newPrompt)
        {
            if (this.clearingPrompts) return;
            int foundPrompt = -1;
            lock (this.prompts)
            {
                for(int i=0; i < prompts.Length; i++)
                {
                    if (this.prompts[i] == null)
                    {
                        foundPrompt = i;
                        this.prompts[i] = newPrompt;
                        break;
                    }
                }
            }
            if(foundPrompt != -1)
            {
                StringBuilder builder = new StringBuilder();
                builder.Append("New Prompt ").Append(foundPrompt + 1).Append(endOfLine);
                newPrompt.OnTransition();
                this.sendMessage(builder.ToString());
            }
            else
            {
                //this.sendMessage("You were too busy to " + TODO ?);
                newPrompt.Respond(null);
            }
        }

        public void AddNewInputHandlerType(InputHandlerType type)
        {
            if(!this.inputHandlerTypes.Contains(type))
            {
                type.TryAddHandlers(this);
                this.inputHandlerTypes.Add(type);
            }
        }

        /// <summary>
        /// Change the current Input Handler to a new one. Updates InputHandlerTypes.
        /// </summary>
        /// <param name="newHandler"></param>
        public void TransitionToInputHandler(InputHandler newHandler)
        {
            newHandler.CalledBy(this);
            this.inputHandlerChecks.Clear();
            foreach (InputHandlerType type in inputHandlerTypes)
            {
                type.TryAddHandlers(this);
            }
        }

        /// <summary>
        /// Adds a handler to check for every character.
        /// </summary>
        /// <param name="handler"></param>
        public void AddInputChecker(InputHandlerType.CheckInputHandler handler)
        {
            if(!inputHandlerChecks.Contains(handler))
                inputHandlerChecks.Add(handler);
        }

        /// <summary>
        /// Adds a character to the user's current input.
        /// </summary>
        /// <param name="c"></param>
        public void AddInputChar(byte c)
        {
            this.received.Append(c);
        }

        /// <summary>
        /// Check if the client has more data to parse. Intended for use by input handlers.
        /// </summary>
        /// <returns></returns>
        public bool HasMoreInput()
        {
            return this.parsedBytes < this.receivedBytes || this.socket.Available > 0;
        }

        public void HandleIACTermType(byte DDWW)
        {
            if (this.currentTermType != null) return;

            if (DDWW == TelnetCode.WILL)
            {
                this.sendMessage(new byte[] { TelnetCode.IAC, TelnetCode.SB, TelnetCode.SEND, TelnetCode.IAC, TelnetCode.SE });
            }
            else
            {
                this.currentTermType = "UNKNOWN";
                informMUDOptions();
            }
        }

        public void AddTermTypeOption(string s)
        {
            if (this.setTermType("UNKNOWN")) //TODO: Pass in iacString. Ideally this will check against an updateable config file
            {
                //Continue with other things.
                this.informMUDOptions();
            }
            else
            {
                //Ask for a different type.
                this.sendMessage(new byte[] { TelnetCode.IAC, TelnetCode.SB, TelnetCode.TERMTYPE, TelnetCode.SEND, TelnetCode.IAC, TelnetCode.SE });
            }
            // Probably never try this.
            //if (this.expectTT == iacString)
            //{
            //    expectTT = null;
            //}
            //else
            //{
            //    this.sendMessage(new byte[] { TelnetCode.IAC, TelnetCode.DO, TelnetCode.TERMTYPE });
            //}
            //this.expectTT = iacString
            //this.sendMessage(new byte[] { TelnetCode.IAC, TelnetCode.DO, TelnetCode.TERMTYPE });
        }

        private void sendData(IAsyncResult result)
        {
            try
            {
                this.socket.EndSend(result);
            }
            catch (ObjectDisposedException) { }
        }
        private void receiveData(IAsyncResult result)
        {
            try
            {

                int receivedBytes = this.receivedBytes = 0;
                try
                {
                    receivedBytes = this.receivedBytes = this.socket.EndReceive(result);
                }
                catch (SocketException) { }

                if (receivedBytes == 0)
                {
                    this.Disconnect();
                    return;
                }

                this.LastReceived = DateTime.UtcNow;
                parsedBytes = 0;
                while (parsedBytes < receivedBytes)
                {
                nextByte:
                    byte c = this.buffer[parsedBytes];
                    parsedBytes++;
                    foreach (var handler in this.inputHandlerChecks)
                    {
                        InputHandler newHandler = handler(this, c);
                        if (newHandler != null)
                        {
                            this.TransitionToInputHandler(newHandler);
                            goto nextByte;
                        }
                    }
                    this.CurrentInputHandler.HandleNextChar(this, c);
                }
                if (this.socket.Connected)
                {
                    this.socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, receiveData, null);
                }
            }
            catch (Exception e)
            {
                try
                {
                    this.sendMessage("Error: " + e.ToString());
                }
                catch (Exception)
                {
                }
            }
        }

        public void FlushInput()
        {
            string input = Encoder.GetString(this.received.ToArray());
            this.received.TrimToLength(0);
            if (this.TelnetEcho) this.sendMessage(input);
            if (input.Length > 0) this.LastMessages.Add(input);
            
            //Handle specific prompts
            if (trySpecificPrompt(input)) return;

            //Handle current prompt
            RespondToPrompt(input, mainPrompt);
            //if(this.mainPrompt != null)
            //{
            //    if(mainPrompt.IsStillValid())
            //    {
            //        RespondToPrompt(input, mainPrompt);
            //        return;
            //    }
            //    else
            //    {
            //        mainPrompt = null;
            //        //TODO: Maybe something here for elegantly handling prompts expiring at the same time you input something.
            //    }
            //}
        }
        private bool trySpecificPrompt(string input)
        {
            string[] words = input.Split(' ');
            int promptIndex = words[0].IndexOf('.');
            if (promptIndex == -1) return false;
            int toPrompt;
            if (!Int32.TryParse(words[0].Substring(0, promptIndex - 1), out toPrompt)) return false;
            //words[0] = words[0].Substring(promptIndex + 1);
            HeldPrompt prompt = this.GetPrompt(toPrompt);
            if (prompt == null)
            {
                this.sendMessage(toPrompt.ToString() + " is not a valid prompt index. Do not start a message with something like \"1.\" without an associated prompt.");
            }
            else if (!prompt.IsStillValid())
            {
                this.sendMessage("That prompt has recently expired.");
                this.RemovePrompt(toPrompt);
            }
            else if (words.Length == 1)
            {
                this.sendMessage(prompt.PromptMessage);
            }
            else
            {
                RespondToPrompt(input.Substring(promptIndex + 1), prompt, toPrompt);
            }
            return true;
        }

        public void TryFindCommand(string input, MOB currentMob)
        {
            string[] words = input.Split(' ');
            
            CommandEntry foundCommand = null;
            if (currentMob != null)
            {
                foundCommand = currentMob.OwnCommands.GetCommand(words[0]);
            }
            Account currentAccount = LoggedInAccount;
            if (currentAccount != null)
            {
                foundCommand = foundCommand.CompareRequest(currentAccount.OwnCommands.GetCommand(words[0]), words[0]);
            }
            if (foundCommand != null)
            {
                QueuedCommand userCommand = new QueuedCommand()
                {
                    command = foundCommand.Command,
                    cmdString = input
                };
                if (currentMob != null)
                    foundCommand.Command.execute(currentMob, userCommand);
                else
                    foundCommand.Command.execute(this, userCommand);
            }
            else
            {
                sendMessage("No command available for \"" + words[0] + "\".");
            }
        }

        /// <summary>
        /// Responds to a prompt and handles the prompt's return value. Non-null responses should usually call this instead of calling Respond manually.
        /// </summary>
        /// <param name="input">User's response to the prompt.</param>
        /// <param name="prompt">Prompt being responded to.</param>
        /// <param name="toPrompt">Internal 'position' of the prompt being responded to in this.prompts</param>
        private void RespondToPrompt(string input, HeldPrompt prompt, int toPrompt = -1)
        {
            HeldPrompt next;
            try
            {
                next = prompt.Respond(input); //Skip prompt number + space
            }
            catch (Exception e)
            {
                //TODO: Error logging
                sendMessage("An error has occurred while processing your input: ^n" + e.ToString());
                if (!prompt.IsStillValid())
                {
                    if (this.mainPrompt == prompt)
                    {
                        sendMessage("Logging out to get back to a working state.");
                        LogOut();
                    }
                    else
                    {
                        sendMessage("Aborting broken prompt " + toPrompt);
                        this.RemovePrompt(prompt);
                    }
                }
                return;
            }
            if (next != null && !this.clearingPrompts)
            {
                lock (this.prompts)
                {
                    if (toPrompt != -1 && this.prompts[toPrompt] == prompt)
                    {
                        this.prompts[toPrompt] = next;
                        next.OnTransition();
                    }
                    else if (toPrompt == -1 && this.mainPrompt == prompt)
                    {
                        this.mainPrompt = next;
                        next.OnTransition();
                    }
                    else
                    {
                        this.Prompt(next);
                    }
                }
            }
        }

        /// <summary>
        /// Removes the HeldPrompt specified from this User's response options. Caller is responsible for cleaning up game state
        /// and other things associated with the prompt; prompt is not told to clean itself up.
        /// </summary>
        /// <param name="prompt"></param>
        public void RemovePrompt(HeldPrompt prompt)
        {
            lock (this.prompts)
            {
                for(int i=0;i<prompts.Length;i++)
                    if(prompts[i] == prompt)
                    {
                        prompts[i] = null;
                        return;
                    }
            }
        }
        /// <summary>
        /// Removes the HeldPrompt at the specified index because it's been unused and detected as invalid. Tells the prompt to
        /// clean itself up.
        /// </summary>
        /// <param name="toPrompt"></param>
        private void RemovePrompt(int toPrompt)
        {
            toPrompt--;
            HeldPrompt prompt;
            lock (this.prompts)
            {
                prompt = this.prompts[toPrompt];
                this.prompts[toPrompt] = null;
            }
            if (prompt != null && prompt.IsStillValid())
                prompt.Respond(null); //TODO: Some threadsafety concerns. May need to handle two threads trying to respond at once.
        }

        private void informMUDOptions()
        {

            //{(byte)TELNET_IAC, (byte)TELNET_SB, (byte)TELNET_TERMTYPE, (byte)1, (byte)TELNET_IAC, (byte)TELNET_SE},
            ////{TELNET_IAC, TELNET_DO, TELNET_NAWS},	//NAWS. We don't really care about NAWS at the moment
            ////{TELNET_IAC, TELNET_DO, TELNET_CHARSET},	//Only supporting one charset for the time being. also TELNET_CHARSET is not defined yet
            //{(byte)TELNET_IAC, (byte)TELNET_WILL, (byte)TELNET_MSDP},
            //{(byte)TELNET_IAC, (byte)TELNET_WILL, (byte)TELNET_MSSP},
            //{(byte)TELNET_IAC, (byte)TELNET_DO, (byte)TELNET_ATCP},
            //{(byte)TELNET_IAC, (byte)TELNET_WILL, (byte)TELNET_MSP},
            //{(byte)TELNET_IAC, (byte)TELNET_DO, (byte)TELNET_MXP},
            ////{TELNET_IAC, TELNET_WILL, TELNET_MCCP},	//TODO eventually: Reimplement compression
        }

        /// <summary>
        /// Attempts to set the term type for this client.
        /// </summary>
        /// <param name="type">Client's TERMTYPE response.</param>
        /// <returns>True if this type is known and can be handled. False if type is not known or cannot be supported.</returns>
        private bool setTermType(string type)
        {
            //if (has config for type)
            //{
            //  Handle specific config
            //}
            //else {} //include all the below

            if (this.termTypes == null)
            {
                this.termTypes = new List<String>();
            }
            if (this.termTypes.Contains(type))
            {
                type = "UNKNOWN";
            }
            else
            {
                this.termTypes.Add(type);
                return false;
            }

            this.termTypes = null;
            this.currentTermType = type;
            return true;
        }

        /// <summary>
        /// Forces this connection to log out of the current account and basically reset.
        /// </summary>
        public void LogOut()
        {
            lock (this.prompts)
            {
                ClearPrompts();
                mainPrompt = new LoginPrompt(this);
            }
            //TODO: Probably account.LogOut()?
            this.LoggedInAccount = null;
            mainPrompt.OnTransition();
        }

        /// <summary>
        /// Forces this connection to disconnect.
        /// </summary>
        public void Disconnect()
        {
            ClearPrompts();
            this.LoggedInAccount = null;
            this.socket.Close();
        }

        ~Client()
        {
            Console.WriteLine("Collected a Client"); //TODO: Debug testing, delete this later.
        }

        private void ClearPrompts()
        {
            this.clearingPrompts = true;
            lock (this.prompts)
            {
                HeldPrompt next;
                for (int i = 0; i < prompts.Length; i++)
                {
                    next = prompts[i];
                    if (next != null && next.IsStillValid())
                    {
                        next.Respond(null);
                    }
                    prompts[i] = null;
                }
                if (mainPrompt != null && mainPrompt.IsStillValid())
                {
                    mainPrompt.Respond(null);
                }
            }
            this.clearingPrompts = false;
        }

    }

    public static class ClientFilter
    {
        public static readonly string DynamicEndOfLine = "^n";

    }

    //public static partial class Extensions
    //{
    //    /// <summary>
    //    /// Comprehensive prompt handler
    //    /// </summary>
    //    /// <typeparam name="T">Return type of prompt</typeparam>
    //    /// <param name="user"></param>
    //    /// <param name="promptMessage"></param>
    //    /// <param name="promptCommandWrapper"></param>
    //    /// <param name="promptParser"></param>
    //    /// <param name="defaultValue"></param>
    //    /// <returns></returns>
    //    public static T Prompt<T>(this Client user, string promptMessage, ref HeldPrompt promptCommandWrapper, Func<string, HeldPrompt, T> promptParser, T defaultValue)
    //    {
    //        if (user != null)
    //        {
    //            if (promptCommandWrapper != null)
    //            {

    //            }
    //            string result = user
    //        }
    //        return defaultValue;
    //    }
    //}
}
