using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD
{
    public interface IMOB : ISaveable
    {
        List<CommandEntry> OwnCommands { get; }
        string Name { get; }

        Client Client { get; }

        Item Body { get; } //TODO: Create and replace this with a Body class that extends Item?

        //Room MainLocation { get; }

    }
}
