using KejUtils.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KejUtils.RegionTree
{
    public class VolumeTree<T> where T : HasVolume, IEquatable<T>
    {
        public VolumeTree(SimpleVolume maxVolume)
        {
            rootNode = new VolumeNode<T>(maxVolume);
        }

        public List<T> InVolume(SimpleVolume volume, bool exact = true)
        {
            HashSet<T> possibleOverlaps = new HashSet<T>();
            List<T> list = new List<T>();
            rootNode.InVolume(volume, list, possibleOverlaps, true, exact);
            return list;
        }
        public void Add(T newObject)
        {
            rootNode.Add(newObject);
        }
        public void Update(T movedObject, SimpleVolume previousVolume)
        {
            rootNode.Update(movedObject, previousVolume);
        }
        public void Remove(T removedObject, SimpleVolume previousVolume)
        {
            rootNode.Remove(removedObject, previousVolume);
        }

        private VolumeNode<T> rootNode;

    }
}
