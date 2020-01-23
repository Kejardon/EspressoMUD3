using KejUtils.Geometry;
using KejUtils.RegionTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD.Geometry
{
    /// <summary>
    /// A simple rectangle obstacle. Has 6 sides, all straight and flat and all lines parallel/perpindicular.
    /// </summary>
    public class RectangleObstacle : Obstacle, ObstaclePointManager, ObstacleSurfaceManager, ObstacleLineManager
    {
        public RectangleObstacle(Item backingItem, SimpleVolume volume) : base(backingItem)
        {
            Room origin = backingItem.Position.OriginRoom;
            MyVolume = volume;
        }

        protected SimpleVolume MyVolume;


        public override int LineCount()
        {
            return 12;
        }
        public override SurfaceLine GetLine(int lineIndex)
        {
            return new SurfaceLine() { manager = this, id = lineIndex };
        }

        public override int PointCount()
        {
            return 8;
        }
        public override ObstaclePoint GetPoint(int pointIndex)
        {
            return new ObstaclePoint() { manager = this, id = pointIndex };
        }

        public override int SurfaceCount()
        {
            return 6;
        }
        public override ObstacleSurface GetSurface(int surfaceIndex)
        {
            return new ObstacleSurface() { manager = this, surfaceIndex = surfaceIndex };
        }

        public override void MarkIntersectWithSurface(ObstacleSurface otherSurface, MovementMechanism forMechanism)
        {
            Hitbox hitbox = forMechanism.Hitbox;
            switch (hitbox.Type)
            {
                case Hitbox.HitboxType.Square:
                    {
                        //SquareHitbox square = hitbox as SquareHitbox;

                    }
                    break;
                default:
                    throw new NotImplementedException("Unknown hitbox type");
            }
            SimpleVolume vol = MyVolume;
            Orientation offset = forMechanism.HitboxOffset(otherSurface);
            //Reverse changes to find how this object intersects with original surface instead of the other way around.
            vol.MinX = vol.MinX - offset.x - hitbox.MaxXIncrease((Rotation)offset);
            vol.MaxX = vol.MaxX - offset.x + hitbox.MaxXDecrease((Rotation)offset);
            vol.MinY = vol.MinY - offset.y - hitbox.MaxYIncrease((Rotation)offset);
            vol.MaxY = vol.MaxY - offset.y + hitbox.MaxYDecrease((Rotation)offset);
            vol.MinZ = vol.MinZ - offset.z - hitbox.MaxZIncrease((Rotation)offset);
            vol.MaxZ = vol.MaxZ - offset.z + hitbox.MaxZDecrease((Rotation)offset);

            Vector normal = otherSurface.GetNormal();
            int height = otherSurface.GetHeight();

            //Important: These indices match the obstacle point indices.
            int[] differences = new int[8];
            differences[0] = (int)normal.DotProduct(vol.MinX, vol.MinY, vol.MinZ) - height;
            differences[1] = (int)normal.DotProduct(vol.MaxX, vol.MinY, vol.MinZ) - height;
            differences[2] = (int)normal.DotProduct(vol.MaxX, vol.MaxY, vol.MinZ) - height;
            differences[3] = (int)normal.DotProduct(vol.MinX, vol.MaxY, vol.MinZ) - height;
            differences[4] = (int)normal.DotProduct(vol.MinX, vol.MinY, vol.MaxZ) - height;
            differences[5] = (int)normal.DotProduct(vol.MaxX, vol.MinY, vol.MaxZ) - height;
            differences[6] = (int)normal.DotProduct(vol.MaxX, vol.MaxY, vol.MaxZ) - height;
            differences[7] = (int)normal.DotProduct(vol.MinX, vol.MaxY, vol.MaxZ) - height;

            List<int> negativeHeights = new List<int>(8);
            List<int> positiveHeights = new List<int>(8);
            if (differences[0] > 0) positiveHeights.Add(0); else negativeHeights.Add(0);
            if (differences[1] > 0) positiveHeights.Add(1); else negativeHeights.Add(1);
            if (differences[2] > 0) positiveHeights.Add(2); else negativeHeights.Add(2);
            if (differences[3] > 0) positiveHeights.Add(3); else negativeHeights.Add(3);
            if (differences[4] > 0) positiveHeights.Add(4); else negativeHeights.Add(4);
            if (differences[5] > 0) positiveHeights.Add(5); else negativeHeights.Add(5);
            if (differences[6] > 0) positiveHeights.Add(6); else negativeHeights.Add(6);
            if (differences[7] > 0) positiveHeights.Add(7); else negativeHeights.Add(7);

            if (positiveHeights.Count == 0 || negativeHeights.Count == 0) return;
            for (int i = 0; i < negativeHeights.Count; i++)
            {
                int pointId = negativeHeights[i];
                for (int j = 0; j < 3; j++)
                {
                    SurfaceLine line = PointLine(pointId, j);
                    ObstaclePoint otherPoint = line.Start;
                    if (otherPoint.id == pointId) otherPoint = line.End;
                    if (differences[otherPoint.id] > 0)
                    {
                        double ratio = differences[pointId] / (differences[pointId] - differences[otherPoint.id]);
                        Point start = PointPosition(pointId);
                        Point end = otherPoint.Position;
                        Point intersectPoint;
                        intersectPoint.x = start.x + (int)((end.x - start.x) * ratio);
                        intersectPoint.y = start.y + (int)((end.y - start.y) * ratio);
                        intersectPoint.z = start.z + (int)((end.z - start.z) * ratio);

                        List<Point> IntersectingPoints = new List<Point>();
                        IntersectingPoints.Add(intersectPoint);
                        SurfaceLine previousLine = line;
                        SurfaceLine nextLine = new SurfaceLine(); //compiler has failed me for once, this shouldn't need initialization here.
                        int previousSurfaceId = -1;
                        do
                        {
                            ObstacleSurface nextSurface = previousLine.Left;
                            if (previousSurfaceId == nextSurface.surfaceIndex)
                            {
                                nextSurface = previousLine.Right;
                            }
                            for (int k = 0; k < 4; k++)
                            {
                                nextLine = nextSurface.GetLine(k);
                                if (nextLine.id == previousLine.id) continue;
                                ObstaclePoint nextStart = nextLine.Start;
                                ObstaclePoint nextEnd = nextLine.End;
                                if ((differences[nextStart.id] > 0 && differences[nextEnd.id] <= 0) ||
                                        (differences[nextStart.id] <= 0 && differences[nextEnd.id] > 0))
                                {
                                    start = nextStart.Position;
                                    end = nextEnd.Position;
                                    ratio = differences[nextStart.id] / (differences[nextStart.id] - differences[nextEnd.id]);
                                    intersectPoint.x = start.x + (int)((end.x - start.x) * ratio);
                                    intersectPoint.y = start.y + (int)((end.y - start.y) * ratio);
                                    intersectPoint.z = start.z + (int)((end.z - start.z) * ratio);

                                    IntersectingPoints.Add(intersectPoint);

                                    break;
                                }
                            }
                            
                            previousSurfaceId = nextSurface.surfaceIndex;
                            previousLine = nextLine;
                        }
                        while (nextLine.id != line.id);

                        otherSurface.AddIntersectLines(IntersectingPoints, this, forMechanism);

                        return;
                    }
                }
            }
            
        }


        #region point handling
        /// Bottom (MinZ) 4 first: 0 1
        ///                        3 2
        /// Top (MaxZ) 4 last: 4 5
        ///                    7 6
        public int PointLineCount(int pointId)
        {
            return 3;
        }
        public SurfaceLine PointLine(int pointId, int lineIndex)
        {
            int lineId;
            if (lineIndex == 2) { lineId = pointId % 4 + 4; } //vertical lines
            else if (lineIndex == 1)
            {
                //line 'after' the point
                lineId = pointId;
                if (pointId >= 4) lineId += 4;
            }
            else
            {
                //line 'before' the point
                lineId = (pointId + 3) % 4;
                if (pointId >= 4) lineId += 4;
            }

            return new SurfaceLine() { id = lineId, manager = this };
        }

        public int PointSurfaceCount(int pointId)
        {
            return 3;
        }

        public ObstacleSurface PointSurface(int pointId, int surfaceIndex)
        {
            int surfaceId;
            if (surfaceIndex == 0)
            {
                surfaceId = pointId >= 4 ? 1 : 0;
            }
            else if (surfaceIndex == 1)
            {
                surfaceId = (pointId % 4) + 2;
            }
            else
            {
                surfaceId = ((pointId + 3) % 4) + 2;
            }

            return new ObstacleSurface() { surfaceIndex = surfaceId, manager = this };
        }

        public Point PointPosition(int pointId)
        {
            //MinX,MinY,MinZ
            //MaxX,MinY,MinZ
            //MaxX,MaxY,MinZ
            //MinX,MaxY,MinZ
            //MinX,MinY,MaxZ
            //MaxX,MinY,MaxZ
            //MaxX,MaxY,MaxZ
            //MinX,MaxY,MaxZ

            Point position;
            position.x = ((pointId + 1) & 2) == 2 ? MyVolume.MaxX : MyVolume.MinX;
            position.y = (pointId & 2) == 2 ? MyVolume.MaxY : MyVolume.MinY;
            position.z = (pointId & 4) == 4 ? MyVolume.MaxZ : MyVolume.MinZ;

            return position;
        }
        #endregion

        #region surface handling
        protected VolumeTree<ObstacleHitLine>[] SurfaceObstacleLines = new VolumeTree<ObstacleHitLine>[6];

        /// MinZ, MaxZ (0, 1)
        /// MinY, MaxX, MaxY, MinX (2, 3, 4, 5)
        
        public int SurfacePointCount(int surfaceIndex)
        {
            return 4;
        }

        public ObstaclePoint SurfacePoint(int surfaceIndex, int surfacePointIndex)
        {
            int pointId;
            if (surfaceIndex == 0) pointId = surfacePointIndex;
            else if (surfaceIndex == 1) pointId = surfacePointIndex + 4;
            else
            {
                pointId = (surfaceIndex - 2);
                if (surfacePointIndex < 2) pointId = (pointId + surfacePointIndex) % 4;
                else pointId = (pointId + 3 - surfacePointIndex) % 4 + 4;
            }
            return new ObstaclePoint() { id = surfaceIndex, manager = this };
        }

        public int SurfaceLineCount(int surfaceIndex)
        {
            return 4;
        }

        private static readonly int[] lineOptions = new int[] {0, 5, 8, 4};
        public SurfaceLine SurfaceLine(int surfaceIndex, int lineIndex)
        {
            int lineId;
            if (surfaceIndex == 0) lineId = lineIndex;
            else if (surfaceIndex == 1) lineId = lineIndex + 8;
            else
            {
                //0 5 8 4 ... 3 4 11 7
                lineId = lineOptions[lineIndex] + surfaceIndex - 2;
                if (surfaceIndex == 5 && lineIndex == 3) lineId = 4;
            }
            return new SurfaceLine() { id = lineId, manager = this };
        }

        public Vector SurfaceNormal(int surfaceIndex)
        {
            switch (surfaceIndex)
            {
                case 0:
                    return new Vector() { z = -1 };
                case 1:
                    return new Vector() { z = 1 };
                case 2:
                    return new Vector() { y = -1 };
                case 3:
                    return new Vector() { x = 1 };
                case 4:
                    return new Vector() { y = 1 };
                case 5:
                    return new Vector() { x = -1 };
            }
            throw new IndexOutOfRangeException("surfaceIndex out of range");
        }

        public int SurfaceHeight(int surfaceIndex)
        {
            switch (surfaceIndex)
            {
                case 0:
                    return MyVolume.MinZ;
                case 1:
                    return MyVolume.MaxZ;
                case 2:
                    return MyVolume.MinY;
                case 3:
                    return MyVolume.MaxX;
                case 4:
                    return MyVolume.MaxY;
                case 5:
                    return MyVolume.MinX;
            }
            throw new IndexOutOfRangeException("surfaceIndex out of range");
        }

        public void AddIntersectLines(List<Point> points, int surfaceIndex, Obstacle forObstacle, MovementMechanism forMechanism)
        {
            VolumeTree<ObstacleHitLine> collection = SurfaceObstacleLines[surfaceIndex];
            if (collection == null)
            {
                SimpleVolume surfaceVolume = MyVolume;
                switch (surfaceIndex)
                {
                    case 0:
                        surfaceVolume.MaxZ = surfaceVolume.MinZ;
                        break;
                    case 1:
                        surfaceVolume.MinZ = surfaceVolume.MaxZ;
                        break;
                    case 2:
                        surfaceVolume.MaxY = surfaceVolume.MinY;
                        break;
                    case 3:
                        surfaceVolume.MinX = surfaceVolume.MaxX;
                        break;
                    case 4:
                        surfaceVolume.MinY = surfaceVolume.MaxY;
                        break;
                    case 5:
                        surfaceVolume.MaxX = surfaceVolume.MinX;
                        break;
                }
                collection = new VolumeTree<ObstacleHitLine>(surfaceVolume);
                SurfaceObstacleLines[surfaceIndex] = collection;
            }
            ObstacleHitLineGroup lineGroup = new ObstacleHitLineGroup();
            lineGroup.CausingObstacle = forObstacle;
            lineGroup.Mechanism = forMechanism;

            lineGroup.SetupPoints(points, collection, SurfaceNormal(surfaceIndex));

        }

        public Obstacle GetObstacle()
        {
            return this;
        }
        #endregion

        #region line handling
        /// Bottom (MinZ) 4 lines first: top (MinY), right (MaxX), bottom, left
        /// Mid 4 vertical lines second: 4 5
        ///    (same order as points)    7 6
        /// Top (MaxZ) 4 lines last: top, right, bottom, left
        public ObstaclePoint LineStart(int lineId)
        {
            int pointId = lineId % 4;
            if (lineId >= 8)
            {
                pointId += 4;
            }

            return new ObstaclePoint() { id = pointId, manager = this };
        }

        public ObstaclePoint LineEnd(int lineId)
        {
            int pointId;
            if (lineId < 4)
            {
                pointId = (lineId + 1) % 4;
            }
            else if (lineId >= 8)
            {
                pointId = (lineId + 1) % 4 + 4;
            }
            else
            {
                pointId = lineId;
            }
            return new ObstaclePoint() { id = pointId, manager = this };
        }

        public ObstacleSurface LineLeft(int lineId)
        {
            int surfaceId;
            if (lineId < 4)
            {
                surfaceId = 0;
            }
            else if (lineId >= 8)
            {
                surfaceId = lineId - 6;
            }
            else
            {
                surfaceId = lineId - 2;
            }

            return new ObstacleSurface() { surfaceIndex = surfaceId, manager = this };
        }

        public ObstacleSurface LineRight(int lineId)
        {
            int surfaceId;
            if (lineId < 4)
            {
                surfaceId = lineId + 2;
            }
            else if (lineId >= 8)
            {
                surfaceId = 1;
            }
            else
            {
                surfaceId = (lineId - 1) % 4 + 2;
            }

            return new ObstacleSurface() { surfaceIndex = surfaceId, manager = this };
        }
        #endregion
    }
}
