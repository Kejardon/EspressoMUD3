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

    }
}
