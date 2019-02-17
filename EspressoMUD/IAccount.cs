using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD
{
    public interface IAccountContainer : ISaveable
    {
        Account AccountObject { get; }

    }
}
