using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EspressoMUD.Prompts;

namespace EspressoMUD
{
    public class MOB : IMOBContainer, ISaveable
    {
        /// <summary>
        /// Get the MOB associated with this IMOBContainer
        /// </summary>
        public MOB MOBObject { get { return this; } }


        /// <summary>
        /// 
        /// </summary>
        public List<CommandEntry> OwnCommands { get; protected set; } = new List<CommandEntry>();

        public Client Client { get; set; }

        public void Prompt(HeldPrompt prompt)
        {
            if(Client != null)
            {
                Client.Prompt(prompt);
            }
            else
            {
                prompt.Respond(null); //Use default from prompt.
                //TODO / NOTE: Caller should do everything it's supposed to do before calling Prompt.
            }
        }

        public Item Body { get; } //TODO: Create and replace this with a Body class that extends Item?

        //Room MainLocation { get; }

        public bool CanRecognize(Item item, StringWords input, int start, int end)
        {
            //TODO eventually: Features like sight flags and ignore color codes and so on. For now...

            if(TextParsing.CheckAutoCompleteText(input,item.Name,start,end) || TextParsing.CheckAutoCompleteText(input,item.Description,start,end))
            {
                return true;
            }
            return false;
        }



        [SaveField("Name")]
        private string name;
        public string Name { get { return name; } set { name = value; this.Save(); } }

        [SaveField("Account")]
        private Account owningAccount;
        public Account OwningAccount { get { return owningAccount; } set { owningAccount = value; this.Save(); } }

        //ISaveable template
        public SaveValues SaveValues { get; set; }
        [SaveID("ID")]
        protected int MobID = -1; //Only supports IMOBContainer ObjectType, so assume IMOBContainer
        public int GetSaveID(ObjectType databaseGroup) { return MobID; }
        public void SetSaveID(ObjectType databaseGroup, int id) { MobID = id; }

        public void Delete()
        {
            if (OwningAccount != null)
            {
                OwningAccount.RemoveCharacter(this);
            }
            Extensions.Delete(this);
        }
    }
}
