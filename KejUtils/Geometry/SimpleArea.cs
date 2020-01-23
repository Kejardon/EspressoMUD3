using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KejUtils.Geometry
{
    public struct SimpleArea
    {
        public int MinX;
        public int MinY;
        public int MaxX;
        public int MaxY;

        public bool Overlaps(SimpleArea otherArea)
        {
            if (MaxX < otherArea.MinX || MinX > otherArea.MaxX) return false;
            if (MaxY < otherArea.MinY || MinY > otherArea.MaxY) return false;
            return true;
        }
        public bool ContainedIn(SimpleArea otherArea)
        {
            if (MinX < otherArea.MinX || MaxX > otherArea.MaxX) return false;
            if (MinY < otherArea.MinY || MaxY > otherArea.MaxY) return false;
            return true;
        }
    }
}
