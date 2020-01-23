using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KejUtils.Geometry
{
    public static class Geometry
    {
        public static SimpleVolume ContainingVolume(Point start, Point end)
        {
            SimpleVolume volume;
            if (start.x <= end.x)
            {
                volume.MinX = start.x;
                volume.MaxX = end.x;
            }
            else
            {
                volume.MinX = end.x;
                volume.MaxX = start.x;
            }
            if (start.y <= end.y)
            {
                volume.MinY = start.y;
                volume.MaxY = end.y;
            }
            else
            {
                volume.MinY = end.y;
                volume.MaxY = start.y;
            }
            if (start.z <= end.z)
            {
                volume.MinZ = start.z;
                volume.MaxZ = end.z;
            }
            else
            {
                volume.MinZ = end.z;
                volume.MaxZ = start.z;
            }

            return volume;
        }


        public static bool IsInVolume(SimpleVolume volume, Point start, Point end)
        {
            if (volume.ContainsPoint(start) || volume.ContainsPoint(end))
                return true;
            //Neither point is inside the volume. Check if any part of the line in between those points is inside the volume.
            if (!volume.Overlaps(ContainingVolume(start, end))) return false; //Not even close
            bool allInX = (start.x >= volume.MinX) && (end.x >= volume.MinX) && (start.x <= volume.MaxX) && (end.x <= volume.MaxX);
            bool allInY = (start.y >= volume.MinY) && (end.y >= volume.MinY) && (start.y <= volume.MaxY) && (end.y <= volume.MaxY);
            bool allInZ = (start.z >= volume.MinZ) && (end.z >= volume.MinZ) && (start.z <= volume.MaxZ) && (end.z <= volume.MaxZ);
            long rangeX = end.x - start.x; //promoting these to long to prevent overflow issues later
            long rangeY = end.y - start.y;
            long rangeZ = end.z - start.z;
            //note that rangeFoo == 0 implies allInFoo == true (else GetMaxVolume() check would have returned false),
            //and so allInFoo == false implies rangeFoo != 0

            if (!allInX)
            { //Try X surfaces
                { //Try left surface
                    int diff = volume.MinX - start.x;
                    if (diff * rangeX < 0 || Math.Abs(diff) > Math.Abs(rangeX)) goto rightSurface; //This surface doesn't intersect.
                    if (!allInY)
                    {
                        int y = (int)(start.y + rangeY * diff / rangeX);
                        if (y < volume.MinY || y > volume.MaxY) goto rightSurface; //This surface doesn't intersect.
                    }
                    if (!allInZ)
                    {
                        int z = (int)(start.z + rangeZ * diff / rangeX);
                        if (z < volume.MinZ || z > volume.MaxZ) goto rightSurface; //This surface doesn't intersect.
                    }
                    return true; //This line intersects with left surface.
                }
            rightSurface:
                { //Try right surface
                    int diff = volume.MaxX - start.x;
                    if (diff * rangeX < 0 || Math.Abs(diff) > Math.Abs(rangeX)) goto foreSurface; //This surface doesn't intersect.
                    if (!allInY)
                    {
                        int y = (int)(start.y + rangeY * diff / rangeX);
                        if (y < volume.MinY || y > volume.MaxY) goto foreSurface; //This surface doesn't intersect.
                    }
                    if (!allInZ)
                    {
                        int z = (int)(start.z + rangeZ * diff / rangeX);
                        if (z < volume.MinZ || z > volume.MaxZ) goto foreSurface; //This surface doesn't intersect.
                    }
                    return true; //This line intersects with right surface.
                }
            }
        foreSurface:
            if (!allInY)
            {
                { //Try fore surface
                    int diff = volume.MinY - start.y;
                    if (diff * rangeY < 0 || Math.Abs(diff) > Math.Abs(rangeY)) goto backSurface; //This surface doesn't intersect.
                    if (!allInX)
                    {
                        int x = (int)(start.x + rangeX * diff / rangeY);
                        if (x < volume.MinX || x > volume.MaxX) goto backSurface; //This surface doesn't intersect.
                    }
                    if (!allInZ)
                    {
                        int z = (int)(start.z + rangeZ * diff / rangeY);
                        if (z < volume.MinZ || z > volume.MaxZ) goto backSurface; //This surface doesn't intersect.
                    }
                    return true; //This line intersects with the fore surface.
                }
            backSurface:
                {
                    int diff = volume.MaxY - start.y;
                    if (diff * rangeY < 0 || diff > rangeY) goto bottomSurface; //This surface doesn't intersect.
                    if (!allInX)
                    {
                        int x = (int)(start.x + rangeX * diff / rangeY);
                        if (x < volume.MinX || x > volume.MaxX) goto bottomSurface; //This surface doesn't intersect.
                    }
                    if (!allInZ)
                    {
                        int z = (int)(start.z + rangeZ * diff / rangeY);
                        if (z < volume.MinZ || z > volume.MaxZ) goto bottomSurface; //This surface doesn't intersect.
                    }
                    return true; //This line intersects with the back surface.
                }
            }
        bottomSurface:
            if (allInZ) return false;
            { //Try bottom surface
                int diff = volume.MinZ - start.z;
                if (diff * rangeZ < 0 || diff > rangeZ) goto topSurface; //This surface doesn't intersect.
                if (!allInX)
                {
                    int x = (int)(start.x + rangeX * diff / rangeZ);
                    if (x < volume.MinX || x > volume.MaxX) goto topSurface; //This surface doesn't intersect.
                }
                if (!allInY)
                {
                    int y = (int)(start.y + rangeY * diff / rangeZ);
                    if (y < volume.MinY || y > volume.MaxY) goto topSurface; //This surface doesn't intersect.
                }
                return true; //This line intersects with the bottom surface.
            }
        topSurface:
            return false;
            //On second thought, we've checked all 5 other sides. The line can't go through only one surface
            //so it's safe to say this will never return true.
            //{
            //    int diff = volume.MaxY - Start.y;
            //    if (diff * rangeZ < 0 || diff > rangeZ) return false; //This surface doesn't intersect.
            //    if (!allInX)
            //    {
            //        int x = (int)(Start.x + rangeX * diff / rangeZ);
            //        if (x < volume.MinX || x > volume.MaxX) return false; //This surface doesn't intersect.
            //    }
            //    if (!allInY)
            //    {
            //        int y = (int)(Start.y + rangeY * diff / rangeZ);
            //        if (y < volume.MinY || y > volume.MaxY) return false; //This surface doesn't intersect.
            //    }
            //    return true; //This line intersects with the top surface.
            //}
        }

        /// <summary>
        /// Assuming two lines are in the same plane, calculate if and where they intersect.
        /// </summary>
        /// <param name="startA"></param>
        /// <param name="endA"></param>
        /// <param name="startB"></param>
        /// <param name="endB"></param>
        /// <param name="intersect"></param>
        /// <returns></returns>
        public static Point LineOverlapInPlane(Point startA, Point endA, Point startB, Point endB, out bool intersect)
        {
            //aStart + aVel*n ~= bStart + bVel*m
            //
            //Start with cross product for special case check. If cross product = 0, lines are parallel. No overlap.
            //Find the least represented axis. Ignore it. Focus on the other two axis for the rest of calculations.
            //  This is the 'same plane' assumption. Since this is the least represented axis, the total error should be
            //  less than 1.
            //Calculate rise-over-run for other two axis for each line?
            //  It's possible one of these results in a division by zero. In that case need to hold a value constant and
            //  just calculate when the other line intersects that constant. Skip the next step.
            //Simple two-equations two-variables from there, uses ratios to solve for point's x, then interpolate to find
            //distance along line, then apply to line to get y and z.
            //Doublecheck distances along both lines, make sure it's in-bounds for both. Otherwise not an intersection.
            
            //Okay I've actually done all the math now and logic ends up a bit simpler than expected.
            Vector a = startA.VectorTo(endA);
            Vector b = startB.VectorTo(endB);
            Vector cross = a.CrossProduct(b); //Cross product tells us what the out-of-plane vector is.
            if (cross.x == 0 && cross.y == 0 && cross.z == 0)
            {
                //Lines are parallel. No intersection.
                //TODO: Doublecheck notes for fencepost rules. What if lines are the same?
                intersect = false;
                return default(Point);
            }
            int ignoredAxis;
            double crossproduct;
            if (Math.Abs(cross.x) > Math.Abs(cross.y) && Math.Abs(cross.x) > Math.Abs(cross.z))
            {
                ignoredAxis = 0; //ignore X
                crossproduct = cross.x;
            }
            else if (Math.Abs(cross.y) > Math.Abs(cross.z))
            {
                ignoredAxis = 1; //ignore Y. Defaults to this if X and Y are tied for most-out-of-plane.
                crossproduct = cross.y;
            }
            else
            {
                ignoredAxis = 2; //ignore Z. Defaults to this if all three happen to be equal.
                crossproduct = cross.z;
            }

            bool first = true;
            int diff1 = 0, diff2 = 0;
            float va1 = 0, va2 = 0, vb1 = 0, vb2 = 0;
            if (ignoredAxis != 0)
            {
                diff1 = startB.x - startA.x;
                va1 = endA.x - startA.x;
                vb1 = endB.x - startB.x;
                first = false;
            }
            if (ignoredAxis != 1)
            {
                if (first)
                {
                    diff1 = startB.y - startA.y;
                    va1 = endA.y - startA.y;
                    vb1 = endB.y - startB.y;
                }
                else
                {
                    diff2 = startB.y - startA.y;
                    va2 = endA.y - startA.y;
                    vb2 = endB.y - startB.y;
                }
            }
            if (ignoredAxis != 2)
            {
                diff2 = startB.z - startA.z;
                va2 = endA.z - startA.z;
                vb2 = endB.z - startB.z;
            }
            double d1 = (diff2 * vb1 - diff1 * vb2) / crossproduct;
            double d2 = (diff2 * va1 - diff1 * va2) / crossproduct;
            if (d1 < 0 || d1 > 1 || d2 < 0 || d2 > 1)
            {
                intersect = false;
                return default(Point);
            }
            intersect = true; //Note this includes special case of intersecting on a start or end point.
            Point result = startA;
            result.x += (int)Math.Round(d1 * a.x);
            result.y += (int)Math.Round(d1 * a.y);
            result.z += (int)Math.Round(d1 * a.z);
            return result;
        }
    }
}
