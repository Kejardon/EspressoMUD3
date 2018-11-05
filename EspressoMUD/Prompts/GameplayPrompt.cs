using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD.Prompts
{
    public class GameplayPrompt : HeldPrompt
    {
        IMOB MainCharacter;
        public GameplayPrompt(Client user, IMOB character)
        {
            MainCharacter = character;
            User = user;
        }

        public override bool IsStillValid()
        {
            return true;
        }

        public override HeldPrompt Respond(string userString)
        {
            if (userString == null)
            {
                //This is basically a logging-out message. TODO: Cleanup this prompt / character.
                return null;
            }

            //TODO: other things or just always go straight to MUD commands?
            User.TryFindCommand(userString, MainCharacter);
            return null;
        }
    }
}
