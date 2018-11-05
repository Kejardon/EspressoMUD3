using KejUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD.Prompts
{

    public abstract class MenuPrompt : StandardHeldPrompt
    {
        protected delegate void MenuAction();
        protected ShortcutDictionary<MenuAction> options = new ShortcutDictionary<MenuAction>(true);
        protected List<Tuple<string, string>> optionList = new List<Tuple<string, string>>();
        protected List<string> numericOptions = new List<string>();

        protected virtual bool AllowCommands() { return false; }

        public MenuPrompt(HeldPrompt calledBy) : base(calledBy)
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

        protected override void InnerRespond(string userString)
        {
            if (string.IsNullOrEmpty(userString))
            {
                MenuDefault();
                return;
            }
            MenuAction action;
            List<MenuAction> alternatives;
            options.TryGet(userString, out action, out alternatives);
            if (action != null) action();
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
        }

        protected virtual void MenuDefault()
        {
            Cancel();
        }
    }
}
