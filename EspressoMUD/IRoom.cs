using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD
{
    /// <summary>
    /// Generic idea of an area of space.
    /// </summary>
    //Some ideas this class might represent:
    // Large scale area / land mass
    // Room in a house
    // Open field
    //Some functional/implementational differences?
    // 3x3x3 cube or other sizes (most things in center?, exits in directions)
    // positional map
    // ?
    public interface IRoom : ISaveable
    {
        
    }
}
