using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EspressoMUD
{
    public class Account : ISaveable, IAccount //, ILockable
    {
        /// <summary>
        /// Encryption method used to 
        /// </summary>
        public static IEncrypter EncryptionMethod = new BCryptEncryption(); //TODO: Make the encryption method configurable?

        [SaveAccountName("Name")] //Custom attribute to update AccountObject's index.
        private string name;
        public string Name
        {
            get { return name; }
            set { name = value; this.Save(); }
        }
        [SaveField("Password")]
        private string password;
        public string Password
        {
            get { return password; }
            set { password = EncryptionMethod.Encrypt(value); this.Save(); }
        }
        [SaveField("Characters")]
        private ListMOBs unloadedCharacters = new ListMOBs();

        //private List<DelayedMOB> unloadedCharacters; //TODO: Call this.Save() whenever this list is modified. 

        //private List<MOB> characters;
        public IMOB[] Characters
        {
            get
            {
                return unloadedCharacters.GetAll();
            }
        }

        /// <summary>
        /// Permanently adds a character to this account.
        /// </summary>
        /// <param name="mob"></param>
        public void AddCharacter(MOB mob)
        {
            this.unloadedCharacters.Add(mob);
            this.Save();
        }

        /// <summary>
        /// Permanently removes a character from this account.
        /// </summary>
        /// <param name="mob"></param>
        /// <returns></returns>
        public bool RemoveCharacter(MOB mob)
        {
            if (unloadedCharacters.Remove(mob))
            {
                this.Save();
                return true;
            }
            return false;
        }

        public List<CommandEntry> OwnCommands { get; protected set; } = new List<CommandEntry>();

        public List<Client> CurrentLogins { get; protected set; } = new List<Client>();

        //ILockable template
        //public ReaderWriterLockSlim Lock { get; set; }
        //ISaveable template
        public SaveValues SaveValues { get; set; }
        [SaveID("ID")]
        protected int AccountID = -1; //Only supports IAccount ObjectType, so assume AccountID
        public int GetSaveID(ObjectType databaseGroup) { return AccountID; }
        public void SetSaveID(ObjectType databaseGroup, int id) { AccountID = id; }
    }
}
