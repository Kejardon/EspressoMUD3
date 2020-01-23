using KejUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD
{
    /// <summary>
    /// 
    /// Non-abstract Commands must have a default constructor. This will be used for the main list of Commands searched
    /// for matches to user input.
    /// </summary>
    public abstract class Command
    {
        #region Static Command management
        //public static List<CommandEntry> CommonCommands = new List<CommandEntry>();
        public static Dictionary<string, Command> UniqueCommands = new Dictionary<string, Command>();

        public static List<CommandEntry> SortedAllCommands; //= new List<CommandEntry>();

        /// <summary>
        /// Attempt to add a list of commands for the game to check / run from user input
        /// </summary>
        /// <param name="commands"></param>
        /// <returns>Null if all commands added successfully. Otherwise, a list of commands that were NOT added.</returns>
        public static List<Command> AddNewCommands(List<Command> commands)
        {
            List<Command> outAdd, outRemove;
            ReplaceCommands(commands, null, out outAdd, out outRemove);
            return outAdd;
        }
        /// <summary>
        /// Attempt to remove a list of commands for the game to check / run from user input.
        /// </summary>
        /// <param name="commands"></param>
        /// <returns>Null if all commands removed succesfully. Otherwise a list of commands that were not found to be removed.</returns>
        public static List<Command> RemoveCommands(List<Command> commands)
        {
            List<Command> outAdd, outRemove;
            ReplaceCommands(null, commands, out outAdd, out outRemove);
            return outRemove;
        }
        /// <summary>
        /// Attempt to replace a list of commands with another list of commands.
        /// </summary>
        /// <param name="newCommands">List of commands to add. May be null.</param>
        /// <param name="removedCommands">List of commands to remove. May be null.</param>
        /// <param name="failedAdd">Null if all commands added successfully. Otherwise, a list of commands that were NOT added.</param>
        /// <param name="failedRemove">Null if all commands removed succesfully. Otherwise a list of commands that were not found to be removed.</param>
        public static void ReplaceCommands(List<Command> newCommands, List<Command> removedCommands,
            out List<Command> failedAdd, out List<Command> failedRemove)
        {
            lock (CommandMutex)
            {
                failedAdd = null;
                failedRemove = null;
                List<CommandEntry> newList = SortedAllCommands == null ? new List<CommandEntry>() : new List<CommandEntry>(SortedAllCommands);
                Dictionary<string, Command> newUniques = UniqueCommands == null ? new Dictionary<string, Command>() : new Dictionary<string, Command>(UniqueCommands);

                bool changed = false;

                if (removedCommands != null)
                {
                    foreach (Command entry in removedCommands)
                    {
                        Command removedEntry;
                        if (newUniques.TryGetValue(entry.UniqueCommand, out removedEntry) && removedEntry == entry)
                        {
                            newUniques.Remove(entry.UniqueCommand);
                            changed = true;
                            foreach (CommandEntry otherTrigger in entry.CommandEntries)
                            {
                                //if (otherTrigger.Trigger != entry.UniqueCommand)
                                {
                                    newList.BinaryRemove(otherTrigger);
                                    //TODO: Ignore failures here right now. Maybe should log an error though.
                                }
                            }
                        }
                        else
                        {
                            if (failedRemove == null)
                                failedRemove = new List<Command>();

                            failedRemove.Add(entry);
                        }
                    }
                }

                if (newCommands != null)
                {
                    foreach (Command entry in newCommands)
                    {
                        if (newUniques.ContainsKey(entry.UniqueCommand))
                        {
                            if (failedAdd == null)
                                failedAdd = new List<Command>();

                            failedAdd.Add(entry);
                        }
                        else
                        {
                            newUniques.Add(entry.UniqueCommand, entry);
                            changed = true;
                            foreach (CommandEntry otherTrigger in entry.CommandEntries)
                            {
                                //if (otherTrigger.Trigger != entry.UniqueCommand)
                                {
                                    newList.BinaryAdd(otherTrigger);
                                    //TODO: Ignore failures here right now. Maybe should log an error though.
                                }
                            }
                        }
                    }
                }

                if (changed)
                {
                    UniqueCommands = newUniques;
                    SortedAllCommands = newList;
                }
            }
        }
        /// <summary>
        /// Startup function to load commands for the first time. Throws an exception if any commands failed to load.
        /// </summary>
        public static void GenerateCommands()
        {
            //List<CommandEntry> newList = new List<CommandEntry>();
            //Dictionary<string, Command> newUniques = new Dictionary<string, Command>();

            List<Command> startingCommands = new List<Command>();

            Type[] classes = System.Reflection.Assembly.GetExecutingAssembly().GetTypes();
            foreach (Type type in classes) if (!type.IsAbstract && typeof(Command).IsAssignableFrom(type))
                { //Loop over all existing Commands. Currently, that's all the commands there are and there's no additional things to check.
                    Command nextCommand = Activator.CreateInstance(type) as Command;
                    startingCommands.Add(nextCommand);
                }
            List<Command> errors = AddNewCommands(startingCommands);
            if (errors != null && errors.Count > 0)
            {
                throw new Exception("Commands have failed to load.");
            }
        }

        private static object CommandMutex = new object();
        #endregion
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="mainCommand">Name of this command. This must be unique among all commands, case insensitive.</param>
        /// <param name="alternateCommands">Alternative words that trigger this command.</param>
        protected Command(string mainCommand, string[] alternateCommands)
        {
            UniqueCommand = mainCommand.ToLower();
            OriginalCommandName = mainCommand;
            AlternateCommands = alternateCommands;
        }

        /// <summary>
        /// Individual trigger words that call this command.
        /// These are always lowercase.
        /// </summary>
        public CommandEntry[] CommandEntries;

        /// <summary>
        /// The user text that distinctly calls this command. This must be unique among all commands in the MUD.
        /// This is always lowercase.
        /// </summary>
        public string UniqueCommand { get; private set; } //TODO: Get these from Resource files instead?

        /// <summary>
        /// The user text that distinctly calls this command. This retains the original casing and should be preferred for
        /// user-facing information, although command matching entirely ignores casing.
        /// </summary>
        public string OriginalCommandName { get; private set; }

        /// <summary>
        /// Other full text strings to match this command.
        /// </summary>
        private string[] AlternateCommands //TODO: Get these from Resource files instead?
        {
            set {
                List<CommandEntry> entries = new List<CommandEntry>();
                entries.Add(new CommandEntry(this.UniqueCommand, this));
                if(value != null) foreach (string trigger in value)
                {
                    entries.Add(new CommandEntry(trigger.ToLower(), this));
                }
                CommandEntries = entries.ToArray();
            }
        }

        /// <summary>
        /// TODO: Move to a more appropriate place?
        ///  Should be called for CommonCommands for all commands that are available to ALL mobs.
        ///  Should be called for a MOB's commands when a specific command is enabled on them.
        /// </summary>
        public void SetCommands(List<CommandEntry> listOfCommands)
        {
            foreach(CommandEntry entry in this.CommandEntries)
            {
                listOfCommands.BinaryAdd(entry);
            }
        }
        /// <summary>
        /// TODO: Move to a more appropriate place?
        ///  Should be called for a MOB's commands when a specific command is disabled on them.
        /// </summary>
        // Honestly this probably shouldn't be used. It probably still makes sense to generate things all at once.
        //public void removeCommands(MOB mob)
        //{
        //    foreach (CommandEntry entry in this.CommandEntries)
        //    {

        //        //listOfCommands.BinaryRemove(entry);
        //    }
        //}

        /// <summary>
        /// Checks if the given user or mob has access to this command. Doesn't mean they can use it,
        /// e.g. they may know a spell but not have the mana to cast it.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="mob"></param>
        /// <returns>True if this command should be found in the mob's list of commands</returns>
        public abstract bool CanUse(Client user, MOB mob);

        /// <summary>
        /// Runs this command with the given mob and arguments.
        /// </summary>
        /// <param name="mob">Mob to perform the command.</param>
        /// <param name="command">Performed command data</param>
        public abstract void Execute(MOB mob, QueuedCommand command);

        /// <summary>
        /// Runs this command with the given user and arguments.
        /// </summary>
        /// <param name="user">Logged in user to perform the command.</param>
        /// <param name="command">Performed command data</param>
        public abstract void Execute(Client user, QueuedCommand command);

        public virtual QueuedCommand GetQueuedCommand(string input)
        {
            return new DefaultQueuedCommand(this, input);
        }

        /// <summary>
        /// A barebones command with no extra data.
        /// </summary>
        private class DefaultQueuedCommand : QueuedCommand
        {
            private Command ownCommand;
            public override Command command { get { return ownCommand; } }
            public DefaultQueuedCommand(Command self, String input)
            {
                ownCommand = self;
                cmdString = input;
            }
        }
    }
    /// <summary>
    /// Link for a specific text string to a Command. May be from a prebuilt list for a specific Command,
    /// or without a Command for user input searches.
    /// </summary>
    public struct CommandEntry : IComparable<CommandEntry>
    {
        public readonly string Trigger;
        public readonly Command Command;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="trigger">String associated with this specific entry.</param>
        /// <param name="command">Command associated with this entry. Null for user inputs searching for a command.</param>
        public CommandEntry(string trigger, Command command)
        {
            this.Command = command;
            this.Trigger = trigger;
        }

        /// <summary>
        /// Compare this trigger/command to another.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(CommandEntry other)
        {
            int comparison = this.Trigger.CompareTo(other.Trigger);
            if (comparison != 0) return comparison;

            if (this.Command == other.Command) return 0;
            if (this.Command == null || other.Command == null) return 0;

            return this.Command.UniqueCommand.CompareTo(other.Command.UniqueCommand);
        }

        public bool EqualTo(CommandEntry other)
        {
            return other.Command == Command && other.Trigger == Trigger;
        }

        public bool CanAutoComplete(string input)
        {
            return this.Trigger.StartsWith(input);
        }
    }
    public static partial class Extensions
    {
        ///// <summary>
        ///// Searches the list for a command. 'Autocomplete' the request.
        ///// </summary>
        ///// <param name="list"></param>
        ///// <param name="request">Lowercase word</param>
        ///// <returns></returns>
        //public static CommandEntry GetCommand(this List<CommandEntry> list, string request)
        //{
        //    int index = list.BinarySearch(new CommandEntry(request, null));
        //    if (index < 0)
        //    {
        //        index = -index - 1;
        //    }
        //    while (index > 0 && list[index-1].Trigger.StartsWith(request))
        //    {
        //        index--;
        //    }
        //    if (index < list.Count)
        //    {
        //        CommandEntry next = list[index];
        //        if (next.Trigger.StartsWith(request))
        //        {
        //            return next;
        //        }
        //    }
        //    return default(CommandEntry);
        //}

        public static CommandEntry CompareRequest(this CommandEntry a, CommandEntry b, string request)
        {
            if (a.EqualTo(default(CommandEntry))) return b;
            if (b.EqualTo(default(CommandEntry))) return a;
            if (a.Command.UniqueCommand == request) return a;
            if (b.Command.UniqueCommand == request) return b;
            if (a.Trigger == request) return a;
            if (b.Trigger == request) return b;
            return a.Trigger.CompareTo(b.Trigger) < 0 ? a : b;
        }
    }
    /// <summary>
    /// Contains metadata for a command that a user has inputted and is being processed.
    /// </summary>
    public abstract class QueuedCommand
    {
        public long nextAct;
        public virtual Command command { get; }
        public String cmdString;
        //Specific commands may extend this with special command data, to use in their Execute call
        private StringWords parsed;
        public StringWords parsedCommand { get
            {
                if (parsed == null)
                {
                    parsed = new StringWords(cmdString);
                }
                return parsed;
            } }
    }
}
