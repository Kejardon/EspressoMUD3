using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD.Rooms
{
    public class DiscreteRoom : Room
    {
        [SaveField("Width", Default = 3)]
        private int width = 3;

        [SaveField("Height", Default = 3)]
        private int height = 3;

        [SaveField("Length", Default = 3)]
        private int length = 3;

        [SaveField("UnitSize", Default = 100)]
        private int unitSize = 100;





    }
}
