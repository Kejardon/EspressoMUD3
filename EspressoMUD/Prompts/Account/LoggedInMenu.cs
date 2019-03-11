using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD.Prompts
{
    /// <summary>
    /// Handles users that have logged in and are at the main menu for their account.
    /// </summary>
    public class LoggedInMenu : MenuPrompt
    {
        public LoggedInMenu(StandardHeldPrompt calledBy) : base(calledBy)
        {
        }

        protected override bool AllowCommands()
        {
            return true;
        }

        public override string PromptMessage
        {
            get
            {
                SetUpOptions();
                return "Select an action for this account, or select a character ID to log in as that character." + ClientFilter.DynamicEndOfLine + base.PromptMessage;
            }
        }

        protected void SetUpOptions()
        {
            ClearOptions();
            AddOption("Create a new character.", () => { NextPrompt = new NewCharacterPrompt(this); }, "New");
            AddOption("Delete a character.", () => { NextPrompt = new DeleteCharacterPrompt(this); }, "Delete");
            AddOption("Log out to log into another account.", () => { User.LogOut(); Cancel(false); }, "Log");
            AddOption("Disconnect from the MUD.", () => { User.Disconnect(); }, "Disconnect");
            if (User.LoggedInAccount.IsAdmin)
            {
                AddOption("Modify MUD-wide settings", () => { NextPrompt = new AdminSelectModify(this, User, new GlobalValues()); }, "Modify");
            }
            MOB[] characters = User.LoggedInAccount.Characters;
            for (int i = 0; i < characters.Length; i++)
            {
                MOB nextMob = characters[i];
                AddOption(nextMob.Name, () => { NextPrompt = new GameplayPrompt(this.User, nextMob); Cancel(false); });
            }
        }

        protected override void MenuDefault()
        {
            User.sendMessage(PromptMessage);
        }
    }
}
