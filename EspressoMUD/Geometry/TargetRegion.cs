using KejUtils.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD
{
    //Maybe just a Point called TargetSpot for PathTargetRegions?
    //public class TargetSpot
    //{
    //}
    public abstract class TargetRegion
    {
        //public List<Line> outline? For 2D shapes?

        /// <summary>
        /// 
        /// </summary>
        public abstract ObstacleSurface Surface { get; }
        public abstract Room World { get; }
        public abstract Point TargetPoint { get; }

        public abstract Point NearestPointTo(Point startPoint);
    }
    public abstract class PathTargetRegion : TargetRegion
    {
        public override Point TargetPoint { get }
        public override Point NearestPointTo(Point startPoint)
        {
            throw new NotImplementedException();
        }
    }
    public abstract class LocalTargetRegion : TargetRegion
    {

    }
}
