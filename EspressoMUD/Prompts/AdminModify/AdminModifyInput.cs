using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD.Prompts
{
    public class AdminModifyInput : StandardHeldPrompt
    {
        IModifiable objectToModify;
        ModifiableParser fieldParser;
        public AdminModifyInput(StandardHeldPrompt calledBy, Client user, IModifiable modifiedObject, ModifiableParser parser) : base(calledBy, null, user)
        {
            objectToModify = modifiedObject;
            fieldParser = parser;
        }

        public override string PromptMessage
        {
            get
            {
                //TODO: Validation instructions here?
                return "Current value: " + fieldParser.GetValue(objectToModify) + "^n" +
                    "Enter a new value:";
            }
        }

        protected override void InnerRespond(string userString)
        {
            //No matter what the input is, we try to set it once, and then this prompt returns.
            //Avoids infinite loops of validation failure, allows empty strings and any other weird things.
            using (IDisposable mudLock = ThreadManager.GetMUDLock(1000))
            {
                if (mudLock == null)
                {
                    //Ideally this shouldn't happen, but in case it does notify the user.
                    User.sendMessage("The MUD is currently paused. Wait until it is unpaused and try again.");
                }
                else
                {
                    string error;
                    if (!fieldParser.SetValue(objectToModify, userString, out error))
                    {
                        if (error != null) User.sendMessage("Error: " + error);
                        else User.sendMessage("An unspecified error occurred. The value was not set.");
                    }
                    else
                    {
                        ISaveable objectToSave = objectToModify as ISaveable;
                        if (objectToSave != null) objectToSave.Save();
                        else GlobalValues.GlobalsIsDirty = true;
                        User.sendMessage("Value set.");
                    }
                }
            }
            Cancel(true);
        }
    }
}
