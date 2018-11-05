using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KejUtils
{
    public class ReverseComparer<T> : IComparer<T> where T : IComparable
    {
        public int Compare(T x, T y)
        {
            return y.CompareTo(x);
        }
    }
}
