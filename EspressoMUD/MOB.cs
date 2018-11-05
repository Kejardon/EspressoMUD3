using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EspressoMUD.Prompts;

namespace EspressoMUD
{
    public class MOB : IMOB, ISaveable
    {
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

        [SaveField("Name")]
        private string name;
        public string Name { get { return name; } set { name = value; this.Save(); } }

        public Account OwningAccount = null;

        //ISaveable template
        public SaveValues SaveValues { get; set; }
        [SaveID("ID")]
        protected int MobID = -1; //Only supports IMOB ObjectType, so assume IMOB
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
