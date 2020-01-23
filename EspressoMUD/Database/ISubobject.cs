using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD
{
    public interface ISubobject
    {
        /// <summary>
        /// The object that contains this subobject. Should also be a ISubobject or ISaveable.
        /// </summary>
        object Parent { get; set; }
    }
}
