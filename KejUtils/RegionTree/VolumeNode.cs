using KejUtils.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KejUtils.RegionTree
{
    internal class VolumeNode<T> where T : HasVolume, IEquatable<T>
    {
        const byte ValuesSize = 100;


        public VolumeNode(SimpleVolume maxVolume)
        {
            myVolume = maxVolume;
            values = new T[ValuesSize];
        }
        public VolumeNode(SimpleVolume maxVolume, T[] startingValues)
        {
            myVolume = maxVolume;
            values = startingValues;
        }

        public void InVolume(SimpleVolume vol, List<T> list, HashSet<T> possibleOverlaps, bool last, bool exact)
        {
            if (values == null)
            {
                bool inSecond = childVolumeA.myVolume.Overlaps(vol);
                if (childVolumeA.myVolume.Overlaps(vol))
                {
                    childVolumeA.InVolume(vol, list, possibleOverlaps, last && !inSecond, exact);
                }
                if (inSecond)
                {
                    childVolumeA.InVolume(vol, list, possibleOverlaps, last, exact);
                }
                return;
            }

            int startingIndex = list.Count;


            if (exact)
            {
                for (int i = 0; i < heldValues; i++)
                {
                    T item = values[i];
                    if (item.IsInVolume(vol) && !possibleOverlaps.Contains(item))
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
                    if (item.GetMaxVolume().Overlaps(vol) && !possibleOverlaps.Contains(item))
                    {
                        list.Add(item);
                    }
                }
            }
            
            if (!last)
            {
                for (int i = startingIndex; i < list.Count; i++)
                {
                    if (!list[i].GetMaxVolume().ContainedIn(myVolume))
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
                if (newObject.IsInVolume(childVolumeA.myVolume))
                {
                    childVolumeA.Add(newObject);
                }
                if (newObject.IsInVolume(childVolumeB.myVolume))
                {
                    childVolumeB.Add(newObject);
                }
                return;
            }

            if (heldValues < ValuesSize)
            {
                values[heldValues] = newObject;
                heldValues++;
                return;
            }

            int minX = myVolume.MaxX, minY = myVolume.MaxY, minZ = myVolume.MaxZ,
                maxX = myVolume.MinX, maxY = myVolume.MinY, maxZ = myVolume.MinZ;
            for (byte i = 0; i < ValuesSize; i++)
            {
                SimpleVolume nextVolume = values[i].GetMaxVolume();
                minX = Math.Min(minX, nextVolume.MinX);
                maxX = Math.Max(maxX, nextVolume.MaxX);
                minY = Math.Min(minY, nextVolume.MinY);
                maxY = Math.Max(maxY, nextVolume.MaxY);
                minZ = Math.Min(minZ, nextVolume.MinZ);
                maxZ = Math.Max(maxZ, nextVolume.MaxZ);

            }
            int middleX = (maxX + minX) / 2;
            int middleY = (maxY + minY) / 2;
            int middleZ = (maxZ + minZ) / 2;

            //Split the current area through the 'center' (technically the center of the USED area)
            SimpleVolume newLeft, newRight, newForward, newBack, newTop, newBottom;
            newLeft.MinX = myVolume.MinX;
            newLeft.MaxX = middleX;
            newLeft.MinY = myVolume.MinY;
            newLeft.MaxY = myVolume.MaxY;
            newLeft.MinZ = myVolume.MinZ;
            newLeft.MaxZ = myVolume.MaxZ;
            newRight.MinX = middleX + 1;
            newRight.MaxX = myVolume.MaxX;
            newRight.MinY = myVolume.MinY;
            newRight.MaxY = myVolume.MaxY;
            newRight.MinZ = myVolume.MinZ;
            newRight.MaxZ = myVolume.MaxZ;
            newForward.MinX = myVolume.MinX;
            newForward.MaxX = myVolume.MaxX;
            newForward.MinY = myVolume.MinY;
            newForward.MaxY = middleY;
            newForward.MinZ = myVolume.MinZ;
            newForward.MaxZ = myVolume.MaxZ;
            newBack.MinX = myVolume.MinX;
            newBack.MaxX = myVolume.MaxX;
            newBack.MinY = middleY + 1;
            newBack.MaxY = myVolume.MaxY;
            newBack.MinZ = myVolume.MinZ;
            newBack.MaxZ = myVolume.MaxZ;
            newTop.MinX = myVolume.MinX;
            newTop.MaxX = myVolume.MaxX;
            newTop.MinY = myVolume.MinY;
            newTop.MaxY = myVolume.MaxY;
            newTop.MinZ = myVolume.MinZ;
            newTop.MaxZ = middleZ;
            newBottom.MinX = myVolume.MinX;
            newBottom.MaxX = myVolume.MaxX;
            newBottom.MinY = myVolume.MinY;
            newBottom.MaxY = myVolume.MaxY;
            newBottom.MinZ = middleZ + 1;
            newBottom.MaxZ = myVolume.MaxZ;

            byte leftCount = 0, rightCount = 0, forwardCount = 0, backCount = 0, topCount = 0, bottomCount = 0;
            T[] leftItems = new T[100];
            T[] rightItems = new T[100];
            T[] forwardItems = new T[100];
            T[] backItems = new T[100];
            T[] topItems = new T[100];
            T[] bottomItems = new T[100];

            //Sort items into the area(s) they exist in
            for (byte i = 0; i < ValuesSize; i++)
            {
                T nextItem = values[i];
                if (nextItem.IsInVolume(newLeft)) leftItems[leftCount++] = nextItem;
                if (nextItem.IsInVolume(newRight)) rightItems[rightCount++] = nextItem;
                if (nextItem.IsInVolume(newForward)) forwardItems[forwardCount++] = nextItem;
                if (nextItem.IsInVolume(newBack)) backItems[backCount++] = nextItem;
                if (nextItem.IsInVolume(newTop)) topItems[topCount++] = nextItem;
                if (nextItem.IsInVolume(newBottom)) bottomItems[bottomCount++] = nextItem;
            }
            byte maxYCount = Math.Max(forwardCount, backCount);
            byte maxXCount = Math.Max(leftCount, rightCount);
            byte maxZCount = Math.Max(topCount, bottomCount);

            if (maxYCount < maxZCount || //Is Y obviously better to split than Z
                (maxZCount == maxYCount && (maxZ - minZ <= maxY - minY)) || //Is Y less-obviously better-or-equal to split than Z
                maxXCount < maxZCount || //Is X obviously better to split than Z
                (maxZCount == maxXCount && (maxZ - minZ <= maxX - minX))) //Is X less-obviously better-or-equal to split than Z
            {
                if (maxYCount > maxXCount || //Is X obviously better to split than Y
                    (maxYCount == maxXCount && (maxX - minX >= maxY - minY))) //Is X less-obviously better-or-equal to split than Y
                {
                    //Split on X
                    childVolumeA = new VolumeNode<T>(newLeft, leftItems);
                    childVolumeB = new VolumeNode<T>(newRight, rightItems);
                }
                else
                {
                    //Split on Y
                    childVolumeA = new VolumeNode<T>(newForward, forwardItems);
                    childVolumeB = new VolumeNode<T>(newBack, backItems);
                }
            }
            else
            {
                //Split on Z
                childVolumeA = new VolumeNode<T>(newTop, topItems);
                childVolumeB = new VolumeNode<T>(newBottom, bottomItems);
            }
            values = null;

            Add(newObject);
        }

        public void Update(T movedObject, SimpleVolume previousVolume)
        {
            if (values == null)
            {
                childVolumeA.UpdateChild(movedObject, previousVolume);
                childVolumeB.UpdateChild(movedObject, previousVolume);
                return;
            }
        }
        public void UpdateChild(T movedObject, SimpleVolume previousVolume)
        {
            if (movedObject.IsInVolume(myVolume))
            {
                if (previousVolume.Overlaps(myVolume))
                {
                    if (values == null)
                    {
                        childVolumeA.UpdateChild(movedObject, previousVolume);
                        childVolumeB.UpdateChild(movedObject, previousVolume);
                        return;
                    }
                    for (byte i = 0; i < heldValues; i++)
                    {
                        if (values[i].Equals(movedObject))
                        {
                            return;
                        }
                    }
                }
                Add(movedObject);
                return;
            }
            if (previousVolume.Overlaps(myVolume))
            {
                Remove(movedObject, myVolume);
            }
        }
        public void Remove(T removedObject, SimpleVolume previousVolume)
        {
            if (values == null)
            {
                if (childVolumeA.myVolume.Overlaps(previousVolume)) childVolumeA.Remove(removedObject, previousVolume);
                if (childVolumeB.myVolume.Overlaps(previousVolume)) childVolumeB.Remove(removedObject, previousVolume);
                return;
            }
            for (byte i = 0; i < heldValues; i++)
            {
                if (values[i].Equals(removedObject))
                {
                    int count = heldValues - i - 1;
                    if (count > 0)
                        Array.Copy(values, i + 1, values, i, count);
                    heldValues--;
                    values[heldValues] = default(T);
                    return;
                }
            }
        }


        public SimpleVolume myVolume;

        public VolumeNode<T> childVolumeA;
        public VolumeNode<T> childVolumeB;

        public byte heldValues = 0;
        public T[] values;
    }
}
