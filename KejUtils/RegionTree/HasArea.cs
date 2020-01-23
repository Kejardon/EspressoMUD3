using KejUtils.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KejUtils.RegionTree
{
    public interface HasArea
    {
        /// <summary>
        /// Report the smallest bounding box that fits this object.
        /// </summary>
        /// <returns></returns>
        SimpleArea GetMaxArea();
        /// <summary>
        /// Check if ANY part of this is in a given area.
        /// </summary>
        /// <param name="area"></param>
        /// <returns></returns>
        bool IsInArea(SimpleArea area);

    }
}
