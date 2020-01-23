using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD
{
    /// <summary>
    /// 'Body' represents a physical object that is some whole thing that can be controlled by a MOB, and contains
    /// complicated subobjects used to perform functionality while it is being controlled.
    /// Typically this will be an ordinary body that the MOB literally possesses. It can also be a vehicle, or drone,
    /// or something else independant of the MOB's body that they are somehow controlling.
    /// </summary>
    public class Body : Item, IEventListener
    {
        [SaveField("MOBs")]
        private ListMOBs mobs = new ListMOBs();
        
        public MOB[] MOBs { get { return mobs.GetAll(); } }


        //TODO: Race/genes, body data specific to race(s). Hybrids will probably have a HybridBodyData with
        //sub-BodyData objects for each race and sort things out that way... also need Race/Body data stuff for vehicles,
        //which won't have genes but do have bodies. Maybe a modifications section for bodies that vehicles use entirely.
        //Also need indications of what functionality the body has and how to execute those functionalities / what they
        //cost.
        //Side concern: What if a spell or something can make an Item a Body? Probably need to figure out that edge case
        //at some point. Most inclined to make a special Body class that wraps the Item, but that definitely has some
        //issues I don't like.
        //MOB Bodys may be a subclass of this later? Pretty certain yes. Those bodies will contain stat lines and maybe
        //other stuff I figure out later. At the moment I can't actually think of anything other than a statline, basically.

        public bool AddMOB(MOB mob)
        {
            if (mobs.Contains(mob)) return false;
            mobs.Add(mob);
            this.Save();
            return true;
        }

        public bool RemoveMOB(MOB mob)
        {
            if (!mobs.Remove(mob)) return false;
            this.Save();
            return true;
        }

        public override void AddEventListener(RoomEvent forEvent)
        {
            switch(forEvent.Type)
            {
                case EventType.TryGo:
                    if (forEvent.EventSource() == this)
                    {
                        forEvent.AddResponder(new ResponderWrapper(RespondToOwnTryGo), 0);
                    }
                    break;
                case EventType.Movement:
                    if (forEvent.EventSource() == this)
                    {
                        forEvent.AddResponder(new ResponderWrapper(RespondToOwnMove, CancelOwnMove), forEvent.TickDuration());
                    }
                    break;
            }

        }


        private void RespondToOwnTryGo(RoomEvent firedEvent)
        {
            TryGoEvent goEvent = firedEvent as TryGoEvent;
            

        }
        private void RespondToOwnMove(RoomEvent firedEvent)
        {
            MovementEvent moveEvent = firedEvent as MovementEvent;
            double dt = moveEvent.CurrentTickFraction;
            double dx;
            double dy;
            double dz;


            //this.Position
            //TODO: Update position
        }
        private void CancelOwnMove(RoomEvent firedEvent)
        {
            //TODO: Update position
            throw new NotImplementedException();
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
