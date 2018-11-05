using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD.Prompts
{
    /// <summary>
    /// After a new character is created, the user may finetune things, confirm creation, or cancel.
    /// </summary>
    public class ConfirmNewCharacterMenu : MenuPrompt
    {
        MOB newChar;
        public ConfirmNewCharacterMenu(HeldPrompt calledBy, MOB mob) : base(calledBy)
        {
            newChar = mob;
        }

        public override string PromptMessage
        {
            get
            {
                SetUpOptions();
                string partial = base.PromptMessage;
                return partial + ClientFilter.DynamicEndOfLine + "Press enter to finish character creation.";
            }
        }

        private void SetUpOptions()
        {
            ClearOptions();
            AddOption("Enter a new name. Currently " + newChar.Name, () => { NextPrompt = new NewCharacterNamePrompt(this, newChar); }, "Name");
            AddOption("Discard your work and return to the character menu.", () => { newChar = null; Cancel(); }, "Cancel");
        }

        protected override void MenuDefault()
        {
            Account account = User.LoggedInAccount;
            if (account != null && newChar != null)
            {
                newChar.Save(true);
                account.AddCharacter(newChar);
            }
            Cancel();
        }
    }
}
