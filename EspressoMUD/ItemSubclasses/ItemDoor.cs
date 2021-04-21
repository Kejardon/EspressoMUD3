using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD
{
    public class ItemDoor : Item
    {
        /// <summary>
        /// If the door is 'open'. A door being open basically makes it intangible to pathfinding/vision, but it can still be
        /// interacted with in other ways.
        /// </summary>
        [SaveField("Open")]
        private bool open;
        public bool Open { get { return open; } set { open = value; this.Save(); } }
    }
}
