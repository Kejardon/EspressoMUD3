using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD
{
    public interface IMOBContainer : ISaveable
    {
        MOB MOBObject { get; }
    }
}
