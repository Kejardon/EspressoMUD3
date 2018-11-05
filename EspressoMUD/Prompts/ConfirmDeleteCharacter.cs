using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD.Prompts
{

    public class ConfirmDeleteCharacter : StandardHeldPrompt
    {
        protected IMOB selectedMOB;
        public ConfirmDeleteCharacter(IMOB mob, HeldPrompt previous) : base(previous)
        {
            selectedMOB = mob;
        }

        public override string PromptMessage
        {
            get
            {
                return "Warning: This will delete your character for good. Are you sure you want to delete " + selectedMOB.Name + "? (y/N)";
            }
        }

        protected override void InnerRespond(string userString)
        {

            if (userString.ToUpper().StartsWith("Y"))
            {
                User.sendMessage("Deleteing " + selectedMOB.Name);
                selectedMOB.Delete();
            }
            Cancel();
        }
    }
}
