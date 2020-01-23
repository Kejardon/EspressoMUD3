using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KejUtils.Geometry
{

    public struct SimpleVolume
    {
        public int MinX;
        public int MinY;
        public int MaxX;
        public int MaxY;
        public int MinZ;
        public int MaxZ;

        public bool Overlaps(SimpleVolume otherArea)
        {
            if (MaxX < otherArea.MinX || MinX > otherArea.MaxX) return false;
            if (MaxY < otherArea.MinY || MinY > otherArea.MaxY) return false;
            if (MaxZ < otherArea.MinZ || MinZ > otherArea.MaxZ) return false;
            return true;
        }
        public bool ContainedIn(SimpleVolume otherArea)
        {
            if (MinX < otherArea.MinX || MaxX > otherArea.MaxX) return false;
            if (MinY < otherArea.MinY || MaxY > otherArea.MaxY) return false;
            if (MinZ < otherArea.MinZ || MaxZ > otherArea.MaxZ) return false;
            return true;
        }
        public bool ContainsPoint(Point point, bool inclusive = true) { return ContainsPoint(point.x, point.y, point.z, inclusive); }
        public bool ContainsPoint(int x, int y, int z, bool inclusive = true)
        {
            int includeOffset = inclusive ? 1 : 0;
            if (MinX > x - includeOffset || MaxX < x + includeOffset) return false;
            if (MinY > y - includeOffset || MaxY < y + includeOffset) return false;
            if (MinZ > z - includeOffset || MaxZ < z + includeOffset) return false;
            return true;
        }
    }
}
