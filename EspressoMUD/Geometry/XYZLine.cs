using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD.Geometry
{
    public struct XYZLine
    {
        public XYZPoint A;
        public XYZPoint B;

        public XYZLine(XYZPoint a, XYZPoint b)
        {
            A = a;
            B = b;
        }
        public XYZVector UnitVector()
        {
            XYZPoint total = A.VectorTo(B);
            double distance = A.DistanceTo(B);
            double x = total.x / distance;
            double y = total.y / distance;
            double z = total.z / distance;
            return new XYZVector(x, y, z);
        }
    }
}
