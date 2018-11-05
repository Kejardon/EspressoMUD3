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
        public LoggedInMenu(HeldPrompt calledBy) : base(calledBy)
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
            IMOB[] characters = User.LoggedInAccount.Characters;
            for (int i = 0; i < characters.Length; i++)
            {
                IMOB nextMob = characters[i];
                AddOption(nextMob.Name, () => { NextPrompt = new GameplayPrompt(this.User, nextMob); });
            }
        }

        protected override void MenuDefault()
        {
            User.sendMessage(PromptMessage);
        }
    }
}
