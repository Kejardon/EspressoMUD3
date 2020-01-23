using KejUtils.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KejUtils.RegionTree
{
    internal class AreaNode<T> where T : class, HasArea
    {
        const byte ValuesSize = 100;

        public AreaNode(SimpleArea maxArea)
        {
            myArea = maxArea;
            values = new T[ValuesSize];
        }
        public AreaNode(SimpleArea maxArea, T[] startingValues)
        {
            myArea = maxArea;
            values = startingValues;
        }

        public void InArea(SimpleArea area, List<T> list, HashSet<T> possibleOverlaps, bool last, bool exact)
        {
            if (values == null)
            {
                bool inSecond = childAreaA.myArea.Overlaps(area);
                if (childAreaA.myArea.Overlaps(area))
                {
                    childAreaA.InArea(area, list, possibleOverlaps, last && !inSecond, exact);
                }
                if (inSecond)
                {
                    childAreaA.InArea(area, list, possibleOverlaps, last, exact);
                }
                return;
            }

            int startingIndex = list.Count;


            if (exact)
            {
                for (int i = 0; i < heldValues; i++)
                {
                    T item = values[i];
                    if (item.IsInArea(area) && !possibleOverlaps.Contains(item))
                    {
                        list.Add(item);
                    }
                }
            }
            else
            {
                for (int i = 0; i < heldValues; i++)
                {
                    T item = values[i];
                    if (item.GetMaxArea().Overlaps(area) && !possibleOverlaps.Contains(item))
                    {
                        list.Add(item);
                    }
                }
            }

            //Don't add things to the hashset until we're done,
            //don't add if this is the final area searched,
            //and don't add things that won't be seen in other areas.
            if (!last)
            {
                for (int i = startingIndex; i < list.Count; i++)
                {
                    if (!list[i].GetMaxArea().ContainedIn(myArea))
                    {
                        possibleOverlaps.Add(list[i]);
                    }
                }
            }
        }

        public void Add(T newObject)
        {
            if (values == null)
            {
                if (newObject.IsInArea(childAreaA.myArea))
                {
                    childAreaA.Add(newObject);
                }
                if (newObject.IsInArea(childAreaB.myArea))
                {
                    childAreaB.Add(newObject);
                }
                return;
            }

            if (heldValues < ValuesSize)
            {
                values[heldValues] = newObject;
                heldValues++;
                return;
            }

            //Need to split this into multiple areas.
            //int middleX = (myArea.MaxX + myArea.MinX) / 2;
            //int middleY = (myArea.MaxY + myArea.MinY) / 2;
            //First get the area ACTUALLY being used, to try to help skip empty areas
            int minX = myArea.MaxX, minY = myArea.MaxY, maxX = myArea.MinX, maxY = myArea.MinY;
            for (byte i = 0; i < ValuesSize; i++)
            {
                SimpleArea nextArea = values[i].GetMaxArea();
                minX = Math.Min(minX, nextArea.MinX);
                maxX = Math.Max(maxX, nextArea.MaxX);
                minY = Math.Min(minY, nextArea.MinY);
                maxY = Math.Max(maxY, nextArea.MaxY);
            }
            int middleX = (maxX + minX) / 2;
            int middleY = (maxY + minY) / 2;

            //Split the current area through the 'center' (technically the center of the USED area)
            SimpleArea newLeft, newRight, newForward, newBack;
            newLeft.MinX = myArea.MinX;
            newLeft.MaxX = middleX;
            newLeft.MinY = myArea.MinY;
            newLeft.MaxY = myArea.MaxY;
            newRight.MinX = middleX + 1;
            newRight.MaxX = myArea.MaxX;
            newRight.MinY = myArea.MinY;
            newRight.MaxY = myArea.MaxY;
            newForward.MinX = myArea.MinX;
            newForward.MaxX = myArea.MaxX;
            newForward.MinY = myArea.MinY;
            newForward.MaxY = middleY;
            newBack.MinX = myArea.MinX;
            newBack.MaxX = myArea.MaxX;
            newBack.MinY = middleY + 1;
            newBack.MaxY = myArea.MaxY;

            byte leftCount = 0, rightCount = 0, forwardCount = 0, backCount = 0;
            T[] leftItems = new T[100];
            T[] rightItems = new T[100];
            T[] forwardItems = new T[100];
            T[] backItems = new T[100];
            
            //Sort items into the area(s) they exist in
            for (byte i = 0; i < ValuesSize; i++)
            {
                T nextItem = values[i];
                if (nextItem.IsInArea(newLeft)) leftItems[leftCount++] = nextItem;
                if (nextItem.IsInArea(newRight)) rightItems[rightCount++] = nextItem;
                if (nextItem.IsInArea(newForward)) forwardItems[forwardCount++] = nextItem;
                if (nextItem.IsInArea(newBack)) backItems[backCount++] = nextItem;
            }
            byte maxYCount = Math.Max(forwardCount, backCount);
            byte maxXCount = Math.Max(leftCount, rightCount);
            //Decide which axis to split this across. If neither splits items better, try to make split areas closer to square.
            if (maxYCount > maxXCount || (maxYCount == maxXCount && (maxX - minX >= maxY - minY)))
            {
                childAreaA = new AreaNode<T>(newLeft, leftItems);
                childAreaB = new AreaNode<T>(newRight, rightItems);
            }
            else
            {
                childAreaA = new AreaNode<T>(newForward, forwardItems);
                childAreaB = new AreaNode<T>(newBack, backItems);
            }
            values = null;

            //Area split. Recurse to insert items into child areas now.
            Add(newObject);
        }

        public void Update(T movedObject, SimpleArea previousArea)
        {
            if (values == null)
            {
                childAreaA.UpdateChild(movedObject, previousArea);
                childAreaB.UpdateChild(movedObject, previousArea);
                return;
            }
            //No checks for root because object should have been here already and doesn't need to move.
        }
        public void UpdateChild(T movedObject, SimpleArea previousArea)
        {
            //If in this area and may have been, need to search still
            //  If no values, call update on children
            //  if found, do nothing
            //  if not found, add
            //If in this area and definitely wasn't
            //  just do add
            //If not in this area and may have been
            //  just do remove
            //If not in this area and wasn't, do nothing
            


            if (movedObject.IsInArea(myArea))
            {
                if (previousArea.Overlaps(myArea))
                {
                    //If in this area and may have been, need to search still
                    if (values == null)
                    {
                        //  If no values, call update on children
                        childAreaA.UpdateChild(movedObject, previousArea);
                        childAreaB.UpdateChild(movedObject, previousArea);
                        return;
                    }
                    for (byte i = 0; i < heldValues; i++)
                    {
                        //  if found, do nothing
                        if (values[i] == movedObject)
                        {
                            return;
                        }
                    }
                    //  if not found, add (fall through)
                }
                //else definitely wasn't in this area before, just add
                Add(movedObject);
                return;
            }
            if (previousArea.Overlaps(myArea))
            {
                //If not in this area and may have been, just remove
                Remove(movedObject, myArea);
            }
            //else not in this area and wasn't, do nothing

        }

        public void Remove(T removedObject, SimpleArea previousArea)
        {
            if (values == null)
            {
                if (childAreaA.myArea.Overlaps(previousArea)) childAreaA.Remove(removedObject, previousArea);
                if (childAreaB.myArea.Overlaps(previousArea)) childAreaB.Remove(removedObject, previousArea);
                return;
            }
            for (byte i = 0; i < heldValues; i++)
            {
                if (values[i] == removedObject)
                {
                    int count = heldValues - i - 1;
                    if (count > 0)
                        Array.Copy(values, i + 1, values, i, count);
                    heldValues--;
                    values[heldValues] = null;
                    return;
                }
            }
        }

        public SimpleArea myArea;

        public AreaNode<T> childAreaA;
        public AreaNode<T> childAreaB;

        //public AreaNode<T> leftNode;
        //public AreaNode<T> rightNode;
        //public AreaNode<T> forwardNode;
        //public AreaNode<T> backNode;

        public byte heldValues = 0;
        public T[] values;

    }
}
