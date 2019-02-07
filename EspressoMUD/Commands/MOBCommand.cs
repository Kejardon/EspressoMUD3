using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD
{
    abstract public class MOBCommand : Command
    {
        public override bool canUse(Client user, IMOB mob)
        {
            return mob != null;
        }

        public override void execute(Client user, QueuedCommand command)
        {
            throw new NotImplementedException();
        }
    }
}
