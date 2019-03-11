using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD.Prompts
{
    /// <summary>
    /// The user has selected a specific object to modify. This prompt shows the user all the fields in the prompt
    /// that may be modified, and allows the user to select one, or finish modifying this object.
    /// </summary>
    public class AdminSelectModify : MenuPrompt<ModifiableParser>
    {
        IModifiable objectToModify;
        public AdminSelectModify(StandardHeldPrompt previous, Client client, IModifiable target) : base(previous, null, client)
        {
            objectToModify = target;
            Metadata meta = Metadata.LoadedClasses[target.GetType()];
            ModifiableParser[] parsers = meta.GetModifyParsers();
            string quit = "Quit";
            foreach (ModifiableParser parser in parsers)
            {
                string parserName = parser.Name(target);
                if (parserName.Equals(quit, StringComparison.InvariantCultureIgnoreCase))
                    parserName = "Field" + parserName;
                AddOption(parser.Description(target), ModifyField, parser, parserName);
            }
            AddOption("Finished editing this object.", EndThisPromptOption, null, quit);
        }

        private void ModifyField()
        {
            if (SelectedValue.ModifyAsList)
            {
                //TODO: New prompt that handles lists.

            }
            else if (!SelectedValue.CanOverwrite)
            {
                //Implies the only action that can be done is to modify the subobject
                ISaveable subobject = SelectedValue.SubObject(objectToModify);
                if (subobject != null)
                {
                    NextPrompt = new AdminSelectModify(this, User, subobject);
                }
                else
                {
                    User.sendMessage("Error: No subobject found for a modify-only field.");
                }
            }
            else
            {
                NextPrompt = new AdminModifyField(this, User, objectToModify, SelectedValue);
            }
        }
    }
}
