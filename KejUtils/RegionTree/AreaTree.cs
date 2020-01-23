using KejUtils.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KejUtils.RegionTree
{
    public class AreaTree<T> where T : class, HasArea
    {
        public AreaTree(SimpleArea maxArea)
        {
            rootNode = new AreaNode<T>(maxArea);
        }

        public List<T> InArea(SimpleArea area, bool exact = true)
        {
            HashSet<T> possibleOverlaps = new HashSet<T>();
            List<T> list = new List<T>();
            rootNode.InArea(area, list, possibleOverlaps, true, exact);
            return list;
        }
        public void Add(T newObject)
        {
            rootNode.Add(newObject);
        }
        public void Update(T movedObject, SimpleArea previousArea)
        {
            rootNode.Update(movedObject, previousArea);
        }
        public void Remove(T removedObject, SimpleArea previousArea)
        {
            rootNode.Remove(removedObject, previousArea);
        }

        private AreaNode<T> rootNode;

    }
}
