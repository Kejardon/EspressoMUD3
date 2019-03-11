using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD.Prompts
{
    /// <summary>
    /// The user has selected a specific field on an object to modify. This prompt shows what the user can do with that field
    /// and its current value.
    /// </summary>
    public class AdminModifyField : MenuPrompt
    {
        IModifiable objectToModify;
        ModifiableParser fieldParser;
        public AdminModifyField(StandardHeldPrompt previous, Client client, IModifiable target, ModifiableParser parser) : base(previous, null, client)
        {
            if (target == null) throw new ArgumentNullException("target");
            if (parser == null) throw new ArgumentNullException("parser");
            objectToModify = target;
            fieldParser = parser;
        }
        public override void OnTransition()
        {
            CheckOptions();
            base.OnTransition();
        }
        private void CheckOptions()
        {
            ClearOptions();
            if (fieldParser.CanBeNull)
            {
                AddOption("Delete the existing value", DeleteValue, "Delete");
            }
            if (fieldParser.SubObject(objectToModify) != null)
            {
                AddOption("Modify the referenced object", TryModifySubobject, "Modify");
            }
            if (fieldParser.ObjectType != null)
            {
                AddOption("Replace the referenced object with another existing object", ReplaceValue, "Replace");
            }
            if (fieldParser.CanOverwrite)
            {
                AddOption("Set a new value directly", SetValue, "Set");
            }
            if (fieldParser.Instructions != null)
            {
                AddOption("View Instructions for modifying this field", DisplayInstructions, "Instructions");
            }
            AddOption("Stop modifying this field", EndThisPromptOption, "Quit");
        }

        private void DisplayInstructions()
        {
            User.sendMessage(fieldParser.Instructions);
            CheckOptions();
        }

        private void DeleteValue()
        {
            string error;
            using (IDisposable mudLock = ThreadManager.GetMUDLock(1000))
            {
                if (mudLock == null)
                {
                    //Ideally this shouldn't happen, but in case it does notify the user.
                    User.sendMessage("The MUD is currently paused. Wait until it is unpaused and try again.");
                }
                else
                {
                    if (!fieldParser.SetValue(objectToModify, null, out error))
                    {
                        if (error != null) User.sendMessage("Error: " + error);
                        else User.sendMessage("An unspecified error occurred. The value was not deleted.");
                    }
                    else
                    {
                        ISaveable objectToSave = objectToModify as ISaveable;
                        if (objectToSave != null) objectToSave.Save();
                        else GlobalValues.GlobalsIsDirty = true;
                        User.sendMessage("The value has been deleted.");
                    }
                }
            }
            CheckOptions();
        }

        private void TryModifySubobject()
        {
            ISaveable subobject = fieldParser.SubObject(objectToModify);
            if (subobject == null)
            {
                User.sendMessage("Error: The referenced object has been removed since last checked.");
                CheckOptions();
            }
            else
            {
                // Modify the subobject.
                // Return to the current subobject (previous prompt) instead of the current field (this prompt)
                // when done modifying the subobject.
                NextPrompt = new AdminSelectModify(ReturnTo, User, subobject);
            }
        }

        private void ReplaceValue()
        {
            ObjectType type = fieldParser.ObjectType;
            throw new NotImplementedException("TODO: ObjectType search and stuff.");
        }

        private void SetValue()
        {
            NextPrompt = new AdminModifyInput(ReturnTo, User, objectToModify, fieldParser);
        }

        public override string PromptMessage
        {
            get
            {
                StringBuilder prompt = new StringBuilder("Current value: " + fieldParser.GetValue(objectToModify) + "^n"
                    + "Select an option:" + "^n");
                return prompt.ToString() + base.PromptMessage;
            }
        }
        
        //protected override void InnerRespond(string userString)
        //{
        //    throw new NotImplementedException();
        //}
    }
}
