using EspressoMUD.Geometry;
using KejUtils.Geometry;
using KejUtils.RegionTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD
{
    /// <summary>
    /// Wrapper class for stationary, significant objects that impede or enable motion.
    /// Typically floors, walls, ceilings, furniture, doors, stairs, ladders...
    /// This class is specifically used when pathfinding or motion needs to do calculations for those objects. An obstacle is specific
    /// to a particular pathfinding or motion event.
    /// </summary>
    public abstract class Obstacle
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="numHitboxes">The maximum number of shapes needed to check to find collisions
        /// (e.g. tall walking hitbox + short crawling hitbox = 2 hitboxes)</param>
        public Obstacle(Item fromItem) //, int numHitboxes
        {
            FromItem = fromItem;
            //NumHitboxes = numHitboxes;
        }

        public Item FromItem;

        //protected int NumHitboxes;
        //List<Point> points;
        //List<Surface> surfaces;
        //List<SurfaceLine> lines;

        /// <summary>
        /// Get the number of surfaces on this object
        /// </summary>
        /// <returns></returns>
        public abstract int SurfaceCount();
        /// <summary>
        /// Get a specific surface on this object.
        /// </summary>
        /// <param name="surfaceIndex">A number between 0 and CountSurface-1</param>
        /// <returns></returns>
        public abstract ObstacleSurface GetSurface(int surfaceIndex);
        //{
        //    return new ObstacleSurface() { obstacle = this, surfaceIndex = surfaceIndex };
        //}
        /// <summary>
        /// Get the Normal of a specific surface. The normal is the unit vector pointing directly out of a surface.
        /// </summary>
        /// <param name="surfaceIndex">A number between 0 and CountSurface-1</param>
        /// <returns></returns>
        //public abstract Vector GetNormal(int surfaceIndex);
        /// <summary>
        /// Get the 'height' of a specific surface. This is the distance from the source item's position to the surface,
        /// purely in the direction of the Normal. Every point on the surface will have this 'height'.
        /// </summary>
        /// <param name="surfaceIndex">A number between 0 and CountSurface-1</param>
        /// <returns></returns>
        //public abstract int GetSurfaceHeight(int surfaceIndex);

        /// <summary>
        /// Get the number of corner points that exist for this obstacle or a specific surface.
        /// </summary>
        /// <param name="surfaceIndex">A number between 0 and CountSurface-1, or -1 for the entire object.</param>
        /// <returns></returns>
        public abstract int PointCount();
        /// <summary>
        /// Get a specific corner point on the obstacle
        /// </summary>
        /// <param name="surfaceIndex">A number between 0 and CountSurface-1, or -1 for the entire object.</param>
        /// <param name="pointIndex"></param>
        /// <returns></returns>
        public abstract ObstaclePoint GetPoint(int pointIndex);
        /// <summary>
        /// Get the number of edge lines that exist for this obstacle or a specific surface.
        /// </summary>
        /// <param name="surfaceIndex">A number between 0 and CountSurface-1, or -1 for the entire object.</param>
        /// <returns></returns>
        public abstract int LineCount();
        /// <summary>
        /// Get a specific edge line on the obstacle.
        /// </summary>
        /// <param name="surfaceIndex">A number between 0 and CountSurface-1, or -1 for the entire object.</param>
        /// <param name="lineIndex"></param>
        /// <returns></returns>
        public abstract SurfaceLine GetLine(int lineIndex);

        /// <summary>
        /// Obstacles that are considered near enough to affect motion on this obstacle.
        /// If this is null, it means it needs to be calculated. The containing Room(s) should be passed the largest hitbox to check and update this.
        /// </summary>
        public List<Obstacle> NearbyObstacles;

        //public abstract Hitbox ExpandByHitbox(Hitbox hitbox, Rotation rotation);

        //TODO: This probably goes elsewhere. Finds all obstacles nearby this obstacle, has each of those call MarkIntersectWithSurface on own surface with specified vehicle.
        //public void MarkIntersectWithNearbyObstacles(int ownSurfaceIndex, Hitbox vehicle);
        //TODO: Hitbox needs a size and offset. Worst case scenario is basically another obstacle.
        //  Actually on second thought, offset can be applied AFTER this, and for different offsets. Doesn't need to be appllied to generate the total 3D overlap of an obstacle.
        public abstract void MarkIntersectWithSurface(ObstacleSurface otherSurface, MovementMechanism forMechanism);
        //public abstract int CountLine(int surfaceIndex, Hitbox vehicle);

        //public abstract void AddIntersectLines(List<Position> points, int surfaceIndex, Obstacle forObstacle, MovementMechanism forMechanism);
    }
    public struct ObstaclePoint
    {
        public ObstaclePointManager manager;
        public int id;
        public bool EqualTo(SurfaceLine other)
        {
            return manager == other.manager && id == other.id;
        }
        public int LineCount { get { return manager.PointLineCount(id); } }
        public SurfaceLine GetLine(int index) { return manager.PointLine(id, index); }
        public int SurfaceCount { get { return manager.PointSurfaceCount(id); } }
        public ObstacleSurface GetSurface(int index) { return manager.PointSurface(id, index); }
        public Point Position { get { return manager.PointPosition(id); } }
    }
    public interface ObstaclePointManager
    {
        int PointLineCount(int pointId);
        SurfaceLine PointLine(int pointId, int lineIndex);
        int PointSurfaceCount(int pointId);
        ObstacleSurface PointSurface(int pointId, int surfaceIndex);
        Point PointPosition(int pointId);
    }

    public struct SurfaceLine //TODO: This should be renamed to 'ObstacleLine'. 'ObstacleLine' should be renamed, maybe 'BlockedLine'?
    {
        public ObstacleLineManager manager;
        public int id;
        public bool EqualTo(SurfaceLine other)
        {
            return manager == other.manager && id == other.id;
        }
        public ObstaclePoint Start { get { return manager.LineStart(id); } }
        public ObstaclePoint End { get { return manager.LineEnd(id); } }
        public ObstacleSurface Left { get { return manager.LineLeft(id); } }
        public ObstacleSurface Right { get { return manager.LineRight(id); } }
    }
    public interface ObstacleLineManager
    {
        ObstaclePoint LineStart(int lineId);
        ObstaclePoint LineEnd(int lineId);
        ObstacleSurface LineLeft(int lineId);
        ObstacleSurface LineRight(int lineId);
    }

    //public class ObstaclePoint
    //{
    //    public List<SurfaceLine> lines;
    //    public List<ObstacleSurface> surfaces;
    //    public WorldRelativePosition position;
    //}
    //public class SurfaceLine
    //{
    //    public ObstaclePoint start;
    //    public ObstaclePoint end;
    //    public ObstacleSurface left;
    //    public ObstacleSurface right;
    //}
    //public class Surface
    //{
    //  List<Point> points; //Maybe not? redundant?
    //  List<SurfaceLine> lines;
    //  Vector normal;
    //
    //  List<?> TransitionRegions; //Any per-hitbox thing for this? Translations are probably trivially done on the fly, but caching which sections of the transitionregion are accessible might be good.
    //
    //  //Per-Hitbox
    //  List<ObstacleHitLine> blockedLines;
    //How to index things by hitbox? Equal hitboxes *ideally* should call the same thing. Maybe pathfinding event can check for duplicates and give each hitbox an index? Semi-reasonable.
    //}

    public struct ObstacleSurface
    {
        public ObstacleSurfaceManager manager;
        public int surfaceIndex;

        public bool EqualTo(ObstacleSurface other)
        {
            return manager == other.manager && surfaceIndex == other.surfaceIndex;
        }
        public bool IsNull { get { return manager == null; } }
        public int PointCount() { return manager.SurfacePointCount(surfaceIndex); }
        public ObstaclePoint GetPoint(int pointIndex) { return manager.SurfacePoint(surfaceIndex, pointIndex); }
        public int LineCount(int surfaceIndex) { return manager.SurfaceLineCount(surfaceIndex); }
        public SurfaceLine GetLine(int lineIndex) { return manager.SurfaceLine(surfaceIndex, lineIndex); }
        public Vector GetNormal() { return manager.SurfaceNormal(surfaceIndex); }
        public int GetHeight() { return manager.SurfaceHeight(surfaceIndex); }
        public Obstacle FromObstacle() { return manager.GetObstacle(); }
        public void AddIntersectLines(List<Point> points, Obstacle forObstacle, MovementMechanism forMechanism)
        {
            manager.AddIntersectLines(points, surfaceIndex, forObstacle, forMechanism);
        }
        /// <summary>
        /// The surface is part of the obstacle. Objects against the surface need to be slightly farther to avoid
        /// overlapping hitboxes. This gives the 'slightly farther' needed.
        /// </summary>
        /// <returns></returns>
        public Vector GetAdditionalOffset()
        {
            Vector offset = manager.SurfaceNormal(surfaceIndex);
            offset.x = Math.Ceiling(offset.x);
            offset.y = Math.Ceiling(offset.y);
            offset.z = Math.Ceiling(offset.z);
            return offset;
        }

        public SurfaceData GetSurfaceData(bool createIfNeeded)
        {
            return manager.GetSurfaceData(createIfNeeded);
        }
        public bool HasSurfaceData()
        {
            return manager.GetSurfaceData(false) == null;
        }
    }
    public interface ObstacleSurfaceManager
    {
        /// <summary>
        /// Get the number of corner points that exist for this obstacle or a specific surface.
        /// </summary>
        /// <param name="surfaceIndex">A number between 0 and CountSurface-1, or -1 for the entire object.</param>
        /// <returns></returns>
        int SurfacePointCount(int surfaceIndex);
        /// <summary>
        /// Get a specific corner point on the obstacle
        /// </summary>
        /// <param name="surfaceIndex">A number between 0 and CountSurface-1, or -1 for the entire object.</param>
        /// <param name="pointIndex"></param>
        /// <returns></returns>
        ObstaclePoint SurfacePoint(int surfaceIndex, int pointIndex);
        /// <summary>
        /// Get the number of edge lines that exist for this obstacle or a specific surface.
        /// </summary>
        /// <param name="surfaceIndex">A number between 0 and CountSurface-1, or -1 for the entire object.</param>
        /// <returns></returns>
        int SurfaceLineCount(int surfaceIndex);
        /// <summary>
        /// Get a specific edge line on the obstacle.
        /// </summary>
        /// <param name="surfaceIndex">A number between 0 and CountSurface-1, or -1 for the entire object.</param>
        /// <param name="lineIndex"></param>
        /// <returns></returns>
        SurfaceLine SurfaceLine(int surfaceIndex, int lineIndex);
        /// <summary>
        /// Get the Normal of a specific surface. The normal is the unit vector pointing directly out of a surface.
        /// </summary>
        /// <param name="surfaceIndex">A number between 0 and CountSurface-1</param>
        /// <returns></returns>
        Vector SurfaceNormal(int surfaceIndex);
        /// <summary>
        /// Get the 'height' of a specific surface. This is the distance from the source item's position to the surface,
        /// purely in the direction of the Normal. Every point on the surface will have this 'height'.
        /// </summary>
        /// <param name="surfaceIndex">A number between 0 and CountSurface-1</param>
        /// <returns></returns>
        int SurfaceHeight(int surfaceIndex);

        void AddIntersectLines(List<Point> points, int surfaceIndex, Obstacle forObstacle, MovementMechanism forMechanism);
        Obstacle GetObstacle();

        SurfaceData GetSurfaceData(bool createIfNeeded);
    }


    /*
    public class MovementPosition
    {
        public WorldRelativePosition position;
        public ObstacleSurface restingOn;
    }
    */

    /// <summary>
    /// Thick class for data stored on a surface.
    /// </summary>
    public class SurfaceData
    {
        public List<ConsideredPosition> StartPoints;
        //public List<ConsideredPosition> EndPoints;

        public void AddStartPoint(ObstacleSurface mySurface, Point position, int score, ConsideredPosition previousPosition, PoseMechanism pose, TryGoEvent context)
        {
            if (StartPoints == null)
            {
                StartPoints = new List<ConsideredPosition>();
            }
            else
            {
                foreach (ConsideredPosition point in StartPoints)
                {
                    if (point.Position.Equals(position))
                    {
                        if (score < point.CostScore)
                        {
                            //TODO: I don't think this can actually be reached. It might be good to update scores of future points if it does though?
                            //There's not a good way to do that currently though, so not doing that right now.
                            point.PreviousPositions.Insert(0, previousPosition);
                            point.CostScore = score;
                        }
                        else
                        {
                            point.PreviousPositions.Add(previousPosition);
                        }
                        return;
                    }
                }
            }
            //Not found yet, need to create a new point.
            ConsideredPosition newPoint = new ConsideredPosition(position, mySurface, score, pose);
            StartPoints.Add(newPoint);
            //Since it's new, connect to all end points on this surface.
            if (EndPoints != null)
            {
                //foreach (ConsideredPosition endPoint in EndPoints)
                //{
                //    context.ConsiderSurfacePath(newPoint, endPoint);
                //}

            }
        }

    }

    public class ConsideredPosition
    {
        public ConsideredPosition(Point position, ObstacleSurface surface, int score, PoseMechanism pose)
        {
            Position = position;
            Pose = pose;
            RestingOn = surface;
            CostScore = score;
        }
        public ConsideredPosition(Point position, Room world, int score, PoseMechanism pose)
        {
            Position = position;
            Pose = pose;
            World = world;
            CostScore = score;
        }

        public Point Position;
        public PoseMechanism Pose;
        public Room World;
        public ObstacleSurface RestingOn;
        public int CostScore; //Minimum cost required to reach this position
        //public int OpportunityScore; //Lowest known score to reach a target point. Maybe not really necessary? This is important for sorting tasks, not really important for points.
        public List<ConsideredPosition> PreviousPositions; //If this finds an interesting next position, need to tell previous positions to try going straight to the new next position.
        public List<ConsideredPosition> NextPositions; //If a new start position is added / finds this position, need to tell it to try going straight to other next positions.

    }
    //public struct ConsideredPositionAndScore
    //{
    //    public ConsideredPosition Position;
    //    public int Score;
    //}
}
