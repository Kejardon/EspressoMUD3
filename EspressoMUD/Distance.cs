using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD
{
    public class Distance
    {
        public double dx;
        public double dy;
        public double dz;

        public double total
        {
            get
            {
                return Math.Sqrt(dx * dx + dy * dy + dz * dz);
            }
        }

    }
}
