using KejUtils;
using KejUtils.Geometry;
using KejUtils.RegionTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD
{
    public class ObstacleHitLineGroup
    {
        /// <summary>
        /// Obstacle that is causing this boundary. If null, this obstacle line is from the surface ending (the vehicle
        /// would fall off the surface).
        /// </summary>
        public Obstacle CausingObstacle;
        /// <summary>
        /// Mechanism / hitbox associated with this collision. If null, this applies to all hitboxes.
        /// </summary>
        public MovementMechanism Mechanism;

        public SimpleVolume Volume;

        private List<HitLineOverlap> overlaps = new List<HitLineOverlap>();
        private List<HitLineData> hitLines = new List<HitLineData>();
        //private List<ObstacleHitLineGroup> startPointOverlaps = new List<ObstacleHitLineGroup>(); //Instead of a global one, one for each HitLineData.

        private struct HitLineData
        {
            public Point startPoint;
            public int overlapIndex;
            public ObstacleHitLineGroup[] currentOverlaps;
        }

        private struct HitLineOverlap
        {
            //True if this line (from startPoint to next startPoint) is going into overlapGroup, false if this line is going out of overlapGroup.
            //public bool inside; //I kind of think I don't want this? Always toggle in/out instead, don't track multiple layers.
            public Point overlapPoint;
            public ObstacleHitLineGroup overlapGroup;
        }
        internal Point StartPointOf(int groupIndex) { return hitLines[groupIndex % hitLines.Count].startPoint; }
        internal Point EndPointOf(int groupIndex) { return hitLines[(groupIndex+1) % hitLines.Count].startPoint; }

        public void SetupPoints(List<Point> points, VolumeTree<ObstacleHitLine> collectionToPopulate, Vector surfaceNormal)
        {
            Volume.MaxX = points[0].x;
            Volume.MaxY = points[0].y;
            Volume.MaxZ = points[0].z;
            Volume.MinX = points[0].x;
            Volume.MinY = points[0].y;
            Volume.MinZ = points[0].z;
            hitLines.Add(new HitLineData() { startPoint = points[0] });
            for (int i = 1; i < points.Count; i++)
            {
                //ObstacleHitLine nextLine = new ObstacleHitLine();
                hitLines.Add(new HitLineData() { startPoint = points[i] });
                Volume.MaxX = Math.Max(Volume.MaxX, points[i].x);
                Volume.MinX = Math.Min(Volume.MinX, points[i].x);
                Volume.MaxY = Math.Max(Volume.MaxY, points[i].y);
                Volume.MinY = Math.Min(Volume.MinY, points[i].y);
                Volume.MaxZ = Math.Max(Volume.MaxZ, points[i].z);
                Volume.MinZ = Math.Min(Volume.MinZ, points[i].z);
            }
            //1 point: no lines. Shouldn't happen?
            //2 points: 1 line.
            //X points: X lines.
            int stop = points.Count;
            if (stop < 3) stop--; //I have no plans currently for this to ever happen but just to keep the logic complete
            for (int i = 0; i < stop; i++)
            {
                ObstacleHitLine nextLine;
                nextLine.owner = this;
                nextLine.groupIndex = i;
                collectionToPopulate.Add(nextLine);
            }
            if (stop < 4) return; //triangle shapes might stop early, and can never intersect themselves.

            //Find own overlaps. For each line, check collisions with each later line, mark collision on both.
            int minOverlaps = 0;
            StructListStruct<HitLineSetupOverlapData>[] setupOverlaps = new StructListStruct<HitLineSetupOverlapData>[points.Count];
            StructListStruct<int> nextLinesToSkip = default(StructListStruct<int>);
            stop = points.Count - 1; //Skip the last line for the first run - adjacent lines can't run into eachother.
            bool collide;
            HitLineSetupOverlapData newOverlap;
            //Initialize things for i loop
            Point end = points[0];
            Vector nextFirstVector = end.VectorTo(points[1]);
            Vector nextLineNormal = surfaceNormal.CrossProduct(nextFirstVector);
            for (int i = 0; i < points.Count - 2; i++)
            {
                StructListStruct<int> linesToSkip = nextLinesToSkip;
                nextLinesToSkip = default(StructListStruct<int>);
                Point start = end;
                end = points[i+1];
                Vector firstVector = nextFirstVector;
                Vector lineNormal = nextLineNormal;
                nextFirstVector = end.VectorTo(points[i + 2]); //These are useful in some cases to know ahead of time.
                nextLineNormal = surfaceNormal.CrossProduct(nextFirstVector);

                int startIndex = i + 2;
                FindOverlapsInner(startIndex, stop, linesToSkip, surfaceNormal,
                    start, end, firstVector, lineNormal, nextFirstVector, nextLineNormal,
                    i, this, setupOverlaps);
                
                stop = points.Count;
            }



        }
        
        /// <summary>
        /// Check how many layers inside this line group a point is.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="surfaceNormal"></param>
        /// <returns>0 if the point is not inside the group. Else the minimum number of lines that must be crossed to exit the
        /// group from the point</returns>
        public int PointInsideGroup(Point point, Vector surfaceNormal)
        {
            Point exitPoint;

            { //Closest Exit Point:
                exitPoint = new Point();
                int ignoredAxis = 2;
                if (surfaceNormal.x > surfaceNormal.y && surfaceNormal.x > surfaceNormal.z)
                {
                    ignoredAxis = 0;
                }
                else if (surfaceNormal.y > surfaceNormal.z)
                {
                    ignoredAxis = 1;
                }

                //Vector vector;
                int diff = int.MaxValue;
                int newDiff = point.x - Volume.MinX;
                if (newDiff <= 0) return 0;
                if (ignoredAxis != 0)
                {
                    diff = newDiff;
                    exitPoint.x = -diff - 2; exitPoint.y = point.y; exitPoint.z = point.z;
                }
                newDiff = Volume.MaxX - point.x;
                if (newDiff <= 0) return 0;
                if (ignoredAxis != 0 && newDiff < diff)
                {
                    diff = newDiff;
                    exitPoint.x = diff + 2; exitPoint.y = point.y; exitPoint.z = point.z;
                }
                newDiff = point.y - Volume.MinY;
                if (newDiff <= 0) return 0;
                if (ignoredAxis != 1 && newDiff < diff)
                {
                    diff = newDiff;
                    exitPoint.x = point.x; exitPoint.y = -diff - 2; exitPoint.z = point.z;
                }
                newDiff = Volume.MaxY - point.y;
                if (newDiff <= 0) return 0;
                if (ignoredAxis != 1 && newDiff < diff)
                {
                    diff = newDiff;
                    exitPoint.x = point.x; exitPoint.y = diff + 2; exitPoint.z = point.z;
                }
                newDiff = point.z - Volume.MinZ;
                if (newDiff <= 0) return 0;
                if (ignoredAxis != 2 && newDiff < diff)
                {
                    diff = newDiff;
                    exitPoint.x = point.x; exitPoint.y = point.y; exitPoint.z = -diff - 2;
                }
                newDiff = Volume.MaxZ - point.z;
                if (newDiff <= 0) return 0;
                if (ignoredAxis != 2 && newDiff < diff)
                {
                    diff = newDiff;
                    exitPoint.x = point.x; exitPoint.y = point.y; exitPoint.z = diff + 2;
                }

                double height = surfaceNormal.DotProduct(point.x, point.y, point.z);
                
                if (ignoredAxis == 0)
                {
                    exitPoint.x = (int)(height - (exitPoint.y * surfaceNormal.y + exitPoint.z * surfaceNormal.z) / surfaceNormal.x);
                }
                else if (ignoredAxis == 1)
                {
                    exitPoint.y = (int)(height - (exitPoint.x * surfaceNormal.x + exitPoint.z * surfaceNormal.z) / surfaceNormal.y);
                }
                else
                {
                    exitPoint.z = (int)(height - (exitPoint.x * surfaceNormal.x + exitPoint.y * surfaceNormal.y) / surfaceNormal.z);
                }
            }

            bool intersect;
            bool halfIn = false;
            bool halfOut = false;
            int layerCount = 0;
            Point end = hitLines[hitLines.Count - 1].startPoint;
            Vector lineNormal = surfaceNormal.CrossProduct(point.VectorTo(exitPoint));
            for (int i = 0; i < hitLines.Count; i++)
            {
                Point start = end;
                end = hitLines[i].startPoint;
                Point intersectPoint = KejUtils.Geometry.Geometry.LineOverlapInPlane(start, end, point, exitPoint, out intersect);
                if (!intersect) continue;
                if (intersectPoint.Equals(point)) continue;
                int sign = (lineNormal.DotProduct(start.VectorTo(end)) > 0) ? -1 : 1;
                if (intersectPoint.Equals(start) || intersectPoint.Equals(end))
                {
                    if (sign > 0)
                    {
                        halfOut = !halfOut;
                        if (!halfOut) layerCount += sign;
                    }
                    else
                    {
                        halfIn = !halfIn;
                        if (!halfIn) layerCount += sign;
                    }
                }
                else
                {
                    layerCount += sign;
                }
            }
            if (halfIn && !halfOut) layerCount--; //Half-in rounds up, because starting on a line is 'outside' and crossing a half-in would mean 'inside'.
            return layerCount;
        }

        private StructListStruct<int> FindOverlapsInner(int startIndex, int stopIndex, StructListStruct<int> linesToSkip, Vector surfaceNormal,
            Point start, Point end, Vector firstVector, Vector lineNormal, Vector nextFirstVector, Vector nextLineNormal,
            int i, ObstacleHitLineGroup otherGroup, StructListStruct<HitLineSetupOverlapData>[] setupOverlaps)
        {
            StructListStruct<int> nextLinesToSkip = default(StructListStruct<int>);
            Point secondEnd = StartPointOf(startIndex); //Initialize for j loop
            for (int j = startIndex; j < stopIndex; j++)
            {
                Point secondStart = secondEnd;
                secondEnd = otherGroup.EndPointOf(j);

                if (linesToSkip.Contains(j)) continue;

                bool collide;
                Point intersect = KejUtils.Geometry.Geometry.LineOverlapInPlane(start, end, secondStart, secondEnd, out collide);
                if (!collide) continue; //Note: Parallel lines that overlap also 'don't collide'. This is okay as long as they are handled on the lines before/after them.

                Vector secondVector = secondStart.VectorTo(secondEnd);
                Vector thirdVector = secondVector;
                double prevPrevDot = lineNormal.DotProduct(secondVector);
                double prevNextDot = prevPrevDot, nextPrevDot = prevPrevDot, nextNextDot = prevPrevDot;
                bool tiltLeft = false, nextI = false;
                if (intersect.Equals(start))
                {
                    Vector prevFirstVector;
                    if (i == 0)
                    { //Check for edge case of first line, with last line not parallel to j.
                        prevFirstVector = StartPointOf(hitLines.Count - 1).VectorTo(StartPointOf(0));
                        Vector cross = prevFirstVector.CrossProduct(secondVector);
                        if (cross.x != 0 || cross.y != 0 || cross.z != 0)
                        {
                            continue; //This will be handled later. Right now we don't want to do this.
                        }
                    }
                    else
                    {
                        Point previousStart = StartPointOf(i - 1);
                        prevFirstVector = previousStart.VectorTo(start);
                    }
                    //Previous line was parallel. Get the info for it.
                    prevPrevDot = 0;
                    prevNextDot = 0;
                    Vector prevNormal = surfaceNormal.CrossProduct(prevFirstVector);
                    tiltLeft = prevNormal.DotProduct(firstVector) > 0;

                    if (intersect.Equals(secondStart))
                    {
                        //TODO: Wait how does this case happen? I'm tempted to throw in a log statement to see if it ever happens.
                        //Backtrack second vector one index.
                        Point previousStart = otherGroup.StartPointOf(j - 1);
                        secondVector = previousStart.VectorTo(secondStart);
                        prevPrevDot = prevNormal.DotProduct(secondVector);
                        nextPrevDot = lineNormal.DotProduct(secondVector);

                    }
                    else if (intersect.Equals(secondEnd))
                    {
                        j++; //Skip the next line, it's known to start with this intersection and we're handling everything about it now.
                        Point nextEnd = otherGroup.EndPointOf(j);
                        thirdVector = secondEnd.VectorTo(nextEnd);
                        prevNextDot = prevNormal.DotProduct(thirdVector);
                        nextNextDot = lineNormal.DotProduct(thirdVector);
                    }
                    else
                    {
                        //Defaults are fine.
                    }
                }
                else if (intersect.Equals(end))
                {
                    //The end of line i is on line j, so the start of line i+1 will also be on line j.
                    //The intersection is being handled here in all cases, so always skip i+1 with j.
                    StructListStruct<int>.Add(ref nextLinesToSkip, j);
                    nextI = true;
                    if (intersect.Equals(secondStart))
                    {
                        //Seems safe to assume the previous lines were parallel.
                        //Backtrack second vector one index.
                        Point previousStart = otherGroup.StartPointOf(j - 1);
                        secondVector = previousStart.VectorTo(secondStart);
                        prevPrevDot = 0;
                        nextPrevDot = nextLineNormal.DotProduct(secondVector);
                        nextNextDot = nextLineNormal.DotProduct(thirdVector);
                        tiltLeft = lineNormal.DotProduct(nextFirstVector) > 0;
                    }
                    else if (intersect.Equals(secondEnd))
                    {
                        j++; //Skip the next line, it's known to start with this intersection and we're handling everything about it now.
                        StructListStruct<int>.Add(ref linesToSkip, j); //Also skip i+1 with j+1
                        Point nextEnd = otherGroup.EndPointOf(j);
                        thirdVector = secondEnd.VectorTo(nextEnd);
                        prevNextDot = lineNormal.DotProduct(thirdVector);
                        nextPrevDot = nextLineNormal.DotProduct(secondVector);
                        nextNextDot = nextLineNormal.DotProduct(thirdVector);
                        tiltLeft = lineNormal.DotProduct(nextFirstVector) > 0;
                    }
                    else
                    {
                        nextPrevDot = nextLineNormal.DotProduct(secondVector);
                        nextNextDot = nextPrevDot;
                        tiltLeft = lineNormal.DotProduct(nextFirstVector) > 0;
                    }
                }
                else
                {
                    if (intersect.Equals(secondStart))
                    {
                        //Seems safe to assume the previous lines were parallel.
                        //Backtrack second vector one index.
                        Point previousStart = otherGroup.StartPointOf(j - 1);
                        secondVector = previousStart.VectorTo(secondStart);
                        prevPrevDot = 0;
                        nextPrevDot = 0;
                    }
                    else if (intersect.Equals(secondEnd))
                    {
                        j++; //Skip the next line, it's known to start with this intersection and we're handling everything about it now.
                        Point nextEnd = otherGroup.EndPointOf(j);
                        thirdVector = secondEnd.VectorTo(nextEnd);
                        prevNextDot = lineNormal.DotProduct(thirdVector);
                    }
                    else
                    {
                        //Defaults are correct for this, do nothing
                    }

                }
                int jTiltLeft = -1;
                int wasI = (prevPrevDot < 0) ? 1 : 0;
                wasI += (prevNextDot < 0) ? 1 : 0;
                if (wasI == 1 && !thirdVector.Equals(secondVector))
                {
                    jTiltLeft = surfaceNormal.CrossProduct(secondVector).DotProduct(thirdVector) > 0 ? 1 : 0;
                    wasI += jTiltLeft;
                }
                int isI = (nextPrevDot > 0) ? 1 : 0;
                isI += (nextNextDot > 0) ? 1 : 0;
                if (wasI == 1 && !thirdVector.Equals(secondVector))
                {
                    if (jTiltLeft == -1) jTiltLeft = surfaceNormal.CrossProduct(secondVector).DotProduct(thirdVector) > 0 ? 1 : 0;
                    isI += jTiltLeft;
                }
                int wasJ = (prevPrevDot > 0) ? 1 : 0;
                wasJ += (nextPrevDot > 0) ? 1 : 0;
                wasJ += tiltLeft ? 1 : 0;
                int isJ = (prevNextDot < 0) ? 1 : 0;
                isJ += (nextNextDot < 0) ? 1 : 0;
                isJ += tiltLeft ? 1 : 0;

                if (wasI >= 2 != isI >= 2)
                {
                    HitLineSetupOverlapData newOverlap = new HitLineSetupOverlapData() { inside = (isI >= 2), intersect = intersect };
                    StructListStruct<HitLineSetupOverlapData>.Add(ref setupOverlaps[i + (nextI ? 1 : 0)], newOverlap);
                }
                if (wasJ >= 2 != isJ >= 2)
                {
                    HitLineSetupOverlapData newOverlap = new HitLineSetupOverlapData() { inside = (isJ >= 2), intersect = intersect };
                    StructListStruct<HitLineSetupOverlapData>.Add(ref setupOverlaps[j], newOverlap);
                }
            }
            return nextLinesToSkip;
        }

        /// <summary>
        /// Finds overlaps with another obstacle's lines.
        /// </summary>
        /// <param name="other"></param>
        public void FindOverlaps(ObstacleHitLineGroup other)
        {
            //TODO: Toggleable obstacles (like doors or bridges) should each have a independant overlap group instead
            //of using a universal overlap group here.
            //For each outside-line in this and the other group, check for overlaps. If none, also check if one is entirely
            //in the other.


        }

        private struct HitLineSetupOverlapData
        {
            //True if from out to in, false if from in to out.
            public bool inside;
            public Point intersect;
        }
    }

    /// <summary>
    /// A boundary on an obstacle surface that a particular vehicle/mechanism can not move past.
    /// To the 'left' of this line (going from start to end with surface normal as 'up') is traversible.
    /// To the 'right' of this line is where the vehicle can not move. The vehicle can not move ONTO this line either (but I might want to change that).
    /// </summary>
    public struct ObstacleHitLine : HasVolume, IEquatable<ObstacleHitLine>
    {
        internal ObstacleHitLineGroup owner;
        internal int groupIndex;

        public Point Start
        { get {
                return owner.StartPointOf(groupIndex);
        } }
        public Point End
        { get {
                return owner.EndPointOf(groupIndex);
        } }

        public SimpleVolume GetMaxVolume()
        {
            return KejUtils.Geometry.Geometry.ContainingVolume(Start, End);
        }


        public bool IsInVolume(SimpleVolume volume)
        {
            return KejUtils.Geometry.Geometry.IsInVolume(volume, Start, End);
        }
        /// <summary>
        /// The direction *into* the obstacle - e.g., the direction the vehicle may not go at this line.
        /// </summary>
        /// <param name="surfaceNormal"></param>
        /// <returns></returns>
        public Vector LineNormal(Vector surfaceNormal)
        {
            Point s = Start;
            Point e = End;
            return surfaceNormal.CrossProduct(End.VectorTo(Start));
        }

        public bool Equals(ObstacleHitLine other)
        {
            return owner == other.owner && groupIndex == other.groupIndex;
        }
    }
}
