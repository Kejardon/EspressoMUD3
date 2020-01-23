using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KejUtils.Geometry
{
    public struct Vector
    {
        public double x;
        public double y;
        public double z;

        public double DotProduct(Vector other)
        {
            double total;
            total = other.x * this.x;
            total += other.y * this.y;
            total += other.z * this.z;
            return total;
        }
        public double DotProduct(int x, int y, int z)
        {
            double total;
            total = x * this.x;
            total += y * this.y;
            total += z * this.z;
            return total;
        }
        /// <summary>
        /// IMPORTANT NOTE: This uses axis as shown below:
        ///   z  y
        ///   | /
        ///   |/__x
        /// 
        /// and some resulting signs may be opposite of other axis configurations.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public Vector CrossProduct(Vector other)
        {
            Vector result;
            result.x = y * other.z - z * other.y;
            result.y = z * other.x - x * other.z;
            result.z = x * other.y - y * other.x;
            return result;
        }

        public static Vector operator -(Vector v)
        {
            v.x = -v.x;
            v.y = -v.y;
            v.z = -v.z;
            return v;
        }
    }
}
