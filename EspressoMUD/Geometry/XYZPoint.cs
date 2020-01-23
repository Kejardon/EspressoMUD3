using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD.Geometry
{
    /// <summary>
    /// A simple 3 dimensional point (or integer vector)
    /// </summary>
    //public struct XYZPoint
    //{
    //    public static implicit operator XYZVector(XYZPoint t)
    //    {
    //        return new XYZVector(t.x, t.y, t.z);
    //    }

    //    public int x;
    //    public int y;
    //    public int z;
    //    public XYZPoint(int x, int y, int z)
    //    {
    //        this.x = x;
    //        this.y = y;
    //        this.z = z;
    //    }

    //    public double SquareDistanceTo(XYZPoint other)
    //    {
    //        double total;
    //        total = (double)(other.x - x) * (double)(other.x - x);
    //        total += (double)(other.y - y) * (double)(other.y - y);
    //        total += (double)(other.z - z) * (double)(other.z - z);
    //        return total;
    //    }
    //    public double DistanceTo(XYZPoint other)
    //    {
    //        return Math.Sqrt(SquareDistanceTo(other));
    //    }
    //    public XYZPoint VectorTo(XYZPoint other)
    //    {
    //        int x = other.x - this.x;
    //        int y = other.y - this.y;
    //        int z = other.z - this.z;
    //        return new XYZPoint(x, y, z);
    //    }
    //    public double DotProduct(XYZVector other)
    //    {
    //        double total;
    //        total = other.x * this.x;
    //        total += other.y * this.y;
    //        total += other.z * this.z;
    //        return total;
    //    }
    //}
}
