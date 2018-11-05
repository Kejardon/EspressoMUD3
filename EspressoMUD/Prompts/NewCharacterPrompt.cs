using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD.Prompts
{
    /// <summary>
    /// Steps a user through character creation.
    /// </summary>
    public class NewCharacterPrompt : StandardHeldPrompt
    {
        protected MOB newChar;
        protected NewCharacterState state = NewCharacterState.Name;
        public NewCharacterPrompt(HeldPrompt calledBy) : base(calledBy)
        {
            newChar = new MOB();
        }
        //TODO: Chargen goes here

        protected enum NewCharacterState
        {
            Name,

            End
        }

        public override string PromptMessage
        {
            get
            {
                switch (state)
                {
                    case NewCharacterState.Name:
                        return "Enter a name for the new character: ";

                    default:
                        return "";
                }
            }
        }

        /// <summary>
        /// Validates that a name is valid. If it fails, returns an error message.
        /// </summary>
        /// <param name="name">Name to validate</param>
        /// <returns>Null if passes validation. Error message if it fails validation.</returns>
        public static string ValidateCharacterName(string name)
        {
            //TODO: Anything that should be invalid? Probably all prepositions, self, me
            return null;
        }

        protected override void InnerRespond(string userString)
        {
            switch (state)
            {
                case NewCharacterState.Name:
                    string error = ValidateCharacterName(userString);
                    if (error == null)
                    {
                        newChar.Name = userString;
                        state = NewCharacterState.End;
                    }
                    else
                    {
                        User.sendMessage("That is not a valid name: " + error);
                    }
                    break;
                default:
                    break;
            }
            if (state == NewCharacterState.End)
            {
                NextPrompt = new ConfirmNewCharacterMenu(this.ReturnTo, newChar);
            }
        }
    }
}
