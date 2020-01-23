using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD.Prompts
{
    public class GameplayPrompt : HeldPrompt
    {
        MOB MainCharacter;
        public GameplayPrompt(Client user, MOB character)
        {
            MainCharacter = character;
            User = user;

            if (character.Body != null && character.Body.Position == null)
            {
                Room room = GlobalValues.DefaultStartingRoom;
                if (room != null)
                {
                    using (RoomEvent enterEvent = ThreadManager.StartEvent(room, new SpawnPlayerEvent(character), -1, false))
                    {
                        if (enterEvent == null)
                        {
                            //With no timeout, I don't think this will ever be possible.
                            User.sendMessage("Unknown error with default starting room.");
                            User.ReturnToLoggedInPrompt();
                        }
                        else
                        {
                            //Get listeners for event, fire event.
                            enterEvent.FullRunEvent();
                        }
                    }
                }
                else
                {
                    User.sendMessage("No starting room available. Message an admin for help.");
                    User.ReturnToLoggedInPrompt();
                }
            }
        }

        public override bool IsStillValid()
        {
            return true;
        }

        public override HeldPrompt Respond(string userString)
        {
            if (userString == null)
            {
                //This is basically a logging-out message. TODO: Cleanup this prompt / character, probably just User.ReturnToLoggedInPrompt(); ?
                return null;
            }

            //TODO: other things or just always go straight to MUD commands?
            User.TryFindCommand(userString, MainCharacter);
            return null;
        }

        private class SpawnPlayerEvent : MovementEvent
        {
            public SpawnPlayerEvent(MOB mob)
            {
                player = mob;
                SubType = MovementTypes.Spawn;
            }

            public override double TickDuration()
            {
                return 0;
            }

            private MOB player;
            public override Item EventSource()
            {
                return player.Body;
            }

            public override bool CanObserveThis(MOB mob, object focus)
            {
                //Maybe TODO: Make this depend on mob having any working senses.
                return true;
            }
            public override void SendObservedMessage(Client client)
            {
                //TODO: Message + object
                client.sendMessage("");
            }
        }
    }
}
