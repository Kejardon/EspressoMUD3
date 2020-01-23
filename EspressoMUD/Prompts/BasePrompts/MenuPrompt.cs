using KejUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD.Prompts
{
    public abstract class MenuPromptBase : StandardHeldPrompt
    {
        public delegate void MenuAction();
        protected List<Tuple<string, string>> optionList = new List<Tuple<string, string>>();
        protected List<string> numericOptions = new List<string>();

        protected virtual bool AllowCommands() { return false; }
        protected string UserInput = null;

        protected MenuPromptBase(StandardHeldPrompt calledBy, MOB character = null) : base(calledBy, character)
        {
        }
        protected MenuPromptBase(StandardHeldPrompt calledBy, MOB character, Client player) : base(calledBy, character, player)
        {
        }


        /// <summary>
        /// Usable delegate for when a user decides to quit the menu.
        /// </summary>
        protected void EndThisPromptOption()
        {
            Cancel(true);
        }

        public override string PromptMessage
        {
            get
            {
                StringBuilder builder = new StringBuilder();
                foreach (Tuple<string, string> option in optionList)
                {
                    builder.Append(option.Item1 + ": " + option.Item2 + "^n");
                }
                for (int i = 0; i < numericOptions.Count; i++)
                {
                    builder.Append((i + 1) + ": " + numericOptions[i] + "^n");
                }

                return builder.ToString();
            }
        }

        protected virtual void MenuDefault()
        {
            Cancel();
        }
    }


    /// <summary>
    /// A prompt designed to ask the user to select an option from a list.
    /// Subclasses should implement PromptMessage for the overall prompt the user sees, and call AddOption() for each
    /// option and action available for the user to select.
    /// User's string can be read from SelectedOption if needed.
    /// </summary>
    public abstract class MenuPrompt : MenuPromptBase
    {
        protected ShortcutDictionary<MenuAction> options = new ShortcutDictionary<MenuAction>(true);
        
        protected MenuPrompt(StandardHeldPrompt calledBy, MOB character = null) : base(calledBy, character)
        {
        }
        protected MenuPrompt(StandardHeldPrompt calledBy, MOB character, Client player) : base(calledBy, character, player)
        {
        }

        /// <summary>
        /// Add a new option to this menu
        /// </summary>
        /// <param name="description">Description shown to the user for this option.</param>
        /// <param name="act">Function to call when the user selects this option.</param>
        /// <param name="s">String to select for this value, should not be numeric. If not set, a numerical option will be used automatically.</param>
        protected void AddOption(string description, MenuAction act, string s = null)
        {
            if (s == null)
            {
                int next = numericOptions.Count + 1;
                s = next.ToString();
                numericOptions.Add(description);
            }
            else
            {
                optionList.Add(new Tuple<string, string>(s, description));
            }
            options.Add(s, act);
        }

        protected void ClearOptions()
        {
            options.Clear();
            optionList.Clear();
            numericOptions.Clear();
        }

        protected override void InnerRespond(string userString)
        {
            if (string.IsNullOrEmpty(userString))
            {
                MenuDefault();
                return;
            }
            MenuAction action;
            List<MenuAction> alternatives;
            UserInput = userString;
            if (options.TryGet(userString, out action, out alternatives))
            {
                action();
            }
            else if (AllowCommands())
            {
                User.TryFindCommand(userString, AssociatedMOB);
            }
            else
            {
                if (alternatives != null)
                {
                    if (alternatives.All(alt => alt == alternatives[0]))
                    {
                        alternatives[0]();
                    }
                    else
                    {
                        User.sendMessage("\"" + userString + "\" is not specific enough.");
                    }
                }
                else
                {
                    User.sendMessage("\"" + userString + "\" is not a valid option.");
                }
            }
            UserInput = null;
        }
    }
    public abstract class MenuPrompt<T> : MenuPromptBase
    {
        protected ShortcutDictionary<DictionaryEntry> options = new ShortcutDictionary<DictionaryEntry>(true);
        protected T SelectedValue;


        protected MenuPrompt(StandardHeldPrompt calledBy, MOB character = null) : base(calledBy, character)
        {
        }
        protected MenuPrompt(StandardHeldPrompt calledBy, MOB character, Client player) : base(calledBy, character, player)
        {
        }



        /// <summary>
        /// Add a new option to this menu
        /// </summary>
        /// <param name="description">Description shown to the user for this option.</param>
        /// <param name="act">Function to call when the user selects this option.</param>
        /// <param name="s">String to select for this value, should not be numeric. If not set, a numerical option will be used automatically.</param>
        protected void AddOption(string description, MenuAction act, T value, string s = null)
        {
            if (s == null)
            {
                int next = numericOptions.Count + 1;
                s = next.ToString();
                numericOptions.Add(description);
            }
            else
            {
                optionList.Add(new Tuple<string, string>(s, description));
            }
            options.Add(s, new DictionaryEntry(act, value));
        }

        protected void ClearOptions()
        {
            options.Clear();
            optionList.Clear();
            numericOptions.Clear();
        }

        protected override void InnerRespond(string userString)
        {
            if (string.IsNullOrEmpty(userString))
            {
                MenuDefault();
                return;
            }
            DictionaryEntry option;
            List<DictionaryEntry> alternatives;
            UserInput = userString;
            if (options.TryGet(userString, out option, out alternatives))
            {
                SelectedValue = option.data;
                option.action();
                SelectedValue = default(T);
            }
            else if (AllowCommands())
            {
                User.TryFindCommand(userString, AssociatedMOB);
            }
            else
            {
                if (alternatives != null)
                {
                    if (alternatives.All(alt => alt.EqualTo(alternatives[0])))
                    {
                        SelectedValue = alternatives[0].data;
                        alternatives[0].action();
                        SelectedValue = default(T);
                    }
                    else
                    {
                        User.sendMessage("\"" + userString + "\" is not specific enough.");
                    }
                }
                else
                {
                    User.sendMessage("\"" + userString + "\" is not a valid option.");
                }
            }
            UserInput = null;
        }


        protected struct DictionaryEntry
        {
            public MenuAction action;
            public T data;

            public DictionaryEntry(MenuAction action, T data)
            {
                this.action = action;
                this.data = data;
            }

            public bool EqualTo(DictionaryEntry other)
            {
                return action == other.action && data.Equals(other.data);
            }
        }
    }
}
