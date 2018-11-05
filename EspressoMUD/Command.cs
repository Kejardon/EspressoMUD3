using KejUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD
{
    public abstract class Command
    {
        public static List<CommandEntry> CommonCommands = new List<CommandEntry>();

        public CommandEntry[] CommandEntries;

        /// <summary>
        /// The user text that distinctly calls this command. This must be unique among all commands in the MUD.
        /// </summary>
        public virtual string UniqueCommand { get; protected set; }

        /// <summary>
        /// Other full text strings to call this command.
        /// </summary>
        protected virtual string[] AlternateCommands
        {
            set {
                List<CommandEntry> entries = new List<CommandEntry>();
                entries.Add(new CommandEntry(this.UniqueCommand, this));
                if(value != null) foreach (string trigger in value)
                {
                    entries.Add(new CommandEntry(trigger, this));
                }
                CommandEntries = entries.ToArray();
            }
        }

        /// <summary>
        /// TODO: Move to a more appropriate place?
        ///  Should be called for CommonCommands for all commands that are available to ALL mobs.
        ///  Should be called for a MOB's commands when a specific command is enabled on them.
        /// </summary>
        public void setCommands(List<CommandEntry> listOfCommands)
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
        public abstract bool canUse(Client user, IMOB mob);

        /// <summary>
        /// Runs this command with the given mob and arguments.
        /// </summary>
        /// <param name="mob">Mob to perform the command.</param>
        /// <param name="command">Performed command data</param>
        public abstract void execute(IMOB mob, QueuedCommand command);

        /// <summary>
        /// Runs this command with the given user and arguments.
        /// </summary>
        /// <param name="user">Logged in user to perform the command.</param>
        /// <param name="command">Performed command data</param>
        public abstract void execute(Client user, QueuedCommand command);
    }
    /// <summary>
    /// Link for a specific text string to a Command. May be from a prebuilt list for a specific Command,
    /// or without a Command for user input searches.
    /// </summary>
    public sealed class CommandEntry : IComparable<CommandEntry>
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
    }
    public static partial class Extensions
    {
        /// <summary>
        /// Searches the list for a command. 'Autocomplete' the request
        /// </summary>
        /// <param name="list"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public static CommandEntry GetCommand(this List<CommandEntry> list, string request)
        {
            int index = list.BinarySearch(new CommandEntry(request, null));
            if (index < 0)
            {
                index = -index - 1;
            }
            while (index > 0 && list[index-1].Trigger.StartsWith(request))
            {
                index--;
            }
            if (index < list.Count)
            {
                CommandEntry next = list[index];
                if (next.Trigger.StartsWith(request))
                {
                    return next;
                }
            }
            return null;
        }

        public static CommandEntry CompareRequest(this CommandEntry a, CommandEntry b, string request)
        {
            if (a == null) return b;
            if (b == null) return a;
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
    public class QueuedCommand
    {
        public long nextAct;
        public Command command;
        public String cmdString;
        //Specific commands may extend this with special command data, to use in their execute call
    }
}
