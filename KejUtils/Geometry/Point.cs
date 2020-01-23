using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KejUtils.Geometry
{
    public struct Point : IEquatable<Point>
    {
        public static explicit operator Vector(Point t)
        {
            Vector newVector;
            newVector.x = t.x;
            newVector.y = t.y;
            newVector.z = t.z;
            return newVector;
        }

        public int x;
        public int y;
        public int z;

        public double SquareDistanceTo(Point other)
        {
            double total;
            total = (double)(other.x - x) * (double)(other.x - x);
            total += (double)(other.y - y) * (double)(other.y - y);
            total += (double)(other.z - z) * (double)(other.z - z);
            return total;
        }
        public double DistanceTo(Point other)
        {
            return Math.Sqrt(SquareDistanceTo(other));
        }
        public Vector VectorTo(Point other)
        {
            Vector newVector;
            newVector.x = other.x - this.x;
            newVector.y = other.y - this.y;
            newVector.z = other.z - this.z;
            return newVector;
        }
        public bool Equals(Point other)
        {
            return x == other.x && y == other.y && z == other.z;
        }
    }
}
