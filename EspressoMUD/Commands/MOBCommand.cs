using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD
{
    abstract public class MOBCommand : Command
    {
        public MOBCommand(string mainCommand, string[] alternateCommands) : base(mainCommand, alternateCommands)
        {
        }

        public override bool CanUse(Client user, MOB mob)
        {
            return mob != null;
        }

        public override void Execute(Client user, QueuedCommand command)
        {
            throw new NotImplementedException();
        }
    }
}
