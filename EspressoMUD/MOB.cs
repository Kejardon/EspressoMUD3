using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EspressoMUD.Prompts;

namespace EspressoMUD
{
    public class MOB : IMOBContainer, ISaveable, IEventListener
    {
        /// <summary>
        /// Get the MOB associated with this IMOBContainer
        /// </summary>
        public MOB MOBObject { get { return this; } }


        /// <summary>
        /// 
        /// </summary>
        //public List<CommandEntry> OwnCommands { get; protected set; } = new List<CommandEntry>();

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


        [SaveField("Body")]
        private Body body;
        public Body Body { get { return body; } set { body = value; this.Save(); } }

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
        public int GetSaveID() { return MobID; }
        public void SetSaveID(int id) { MobID = id; }

        public void Delete()
        {
            if (OwningAccount != null)
            {
                OwningAccount.RemoveCharacter(this);
            }
            Extensions.Delete(this);
        }

        public void AddEventListener(RoomEvent forEvent)
        {
            switch (forEvent.Type)
            {
                case EventType.TryGo:
                    if ((forEvent as TryGoEvent).MoveSource() == this)
                    {
                        forEvent.AddResponder(new ResponderWrapper(RespondToOwnTryGo), forEvent.TickDuration());
                    }
                    break;
            }

            Client currentClient = Client;
            if (currentClient != null)
            {
                forEvent.AddResponder(new ResponderWrapper<Body>(this.Body, AttemptToObserve, AttemptToObserve), forEvent.TickDuration());
            }
        }
        public void RespondToOwnTryGo(RoomEvent firedEvent)
        {
            TryGoEvent tryGoEvent = firedEvent as TryGoEvent;
            tryGoEvent.StandardPathfinding(); //TODO: Is this all that's needed? look at this again once StandardPathFinding is finished.


        }

        private void AttemptToObserve(RoomEvent forEvent, Body body)
        {
            Client currentClient = Client;
            if (currentClient == null) return;

            if (forEvent.CanObserveThis(this, body))
            {
                forEvent.SendObservedMessage(currentClient);
            }
        }

        private List<Mechanism> availableMechanisms = new List<Mechanism>();
        /// <summary>
        /// Check the full list of ways this MOB can perform actions.
        /// </summary>
        /// <returns></returns>
        public List<Mechanism> AvailableMechanisms()
        {
            return availableMechanisms;
        }
        /// <summary>
        /// Get a list of ways this MOB can perform a specific kind of action.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public List<Mechanism> AvailableMechanisms(Mechanism.Type type)
        {
            List<Mechanism> list = new List<Mechanism>();
            foreach (Mechanism mechanism in availableMechanisms)
            {
                if (mechanism.type == type)
                {
                    list.Add(mechanism);
                }
            }
            return list;
        }
    }
}
