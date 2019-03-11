using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD.Prompts
{
    public class NewCharacterNamePrompt : StandardHeldPrompt
    {
        MOB newChar;
        public NewCharacterNamePrompt(StandardHeldPrompt calledBy, MOB mob) : base(calledBy)
        {
            newChar = mob;
        }

        public override string PromptMessage
        {
            get
            {
                return "Type a new name, or enter nothing to cancel. Currently " + newChar.Name;
            }
        }

        protected override void InnerRespond(string userString)
        {
            if (!string.IsNullOrEmpty(userString))
            {
                string error = NewCharacterPrompt.ValidateCharacterName(userString);
                if (error != null)
                {
                    User.sendMessage("That is not a valid name: " + error);
                }
                else
                {
                    newChar.Name = userString;
                }
            }
            Cancel();
        }
    }

}
