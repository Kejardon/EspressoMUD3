using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD
{
    /// <summary>
    /// Generic idea of a transition from one room to another. Goes as Room->Transition out->Transition in->Room
    /// </summary>
    //Some ways this class might be implemented: 
    // Open path to a touching room
    // Doorway to a touching room
    // Teleporter portal/field to a distant room
    public interface IRoomLink : ISaveable
    {
    }
}
