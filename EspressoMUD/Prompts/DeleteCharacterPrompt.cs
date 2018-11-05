using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD.Prompts
{

    public class DeleteCharacterPrompt : MenuPrompt
    {
        IMOB selectedMOB = null;

        public DeleteCharacterPrompt(HeldPrompt calledBy) : base(calledBy)
        {
        }

        public override string PromptMessage
        {
            get
            {
                SetUpOptions();
                return "Select a character to delete, or nothing to cancel." + ClientFilter.DynamicEndOfLine + base.PromptMessage;
            }
        }

        protected void SetUpOptions()
        {
            ClearOptions();
            IMOB[] characters = User.LoggedInAccount.Characters;
            for (int i = 0; i < characters.Length; i++)
            {
                IMOB nextMob = characters[i];
                AddOption(nextMob.Name, () => { this.selectedMOB = nextMob; });
            }
        }

        protected override void InnerRespond(string str)
        {
            base.InnerRespond(str);
            if (this.selectedMOB != null)
            {
                NextPrompt = new ConfirmDeleteCharacter(selectedMOB, ReturnTo);
            }
        }
    }
}
