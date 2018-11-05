using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KejUtils
{
    /// <summary>
    /// Class whose sole purpose is to be a specific type of list that can't be confused with another list.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class PrivateList<T> : List<T>
    {
    }
}
