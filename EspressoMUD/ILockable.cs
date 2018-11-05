using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace EspressoMUD
{
    public interface ILockable
    {
        /// <summary>
        /// Lock associated with this object. Set is optional if get always returns non-null.
        /// </summary>
        ReaderWriterLockSlim Lock { get; set; }
    }
}
