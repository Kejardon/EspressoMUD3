using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KejUtils
{
    /// <summary>
    /// Manages a set of bits that are on or off. Bits can trivially be set on, but turning a bit off requires
    /// that all sources of the union do not want to turn that bit on. This can be more efficient when sources do not
    /// change often or usually only are added. If sources are often removed or decide to disable bits, this may be
    /// less efficient.
    /// To use this properly, all sources of this union must subscribe to EnableBits, and turn all bits on for that source
    /// when EnableBits fires.
    /// </summary>
    public class LazyBitUnion
    {
        /// <summary>
        /// Fires when a bit needs to be read but hasn't been generated yet. All sources should subcribe to this event,
        /// and toggle their associated bits on when it is fired. Subscribers should ONLY set bits to true, and should
        /// not attempt to read bits.
        /// </summary>
        public event Action<LazyBitUnion> EnableBits;

        /// <summary>
        /// The number of bits handled by this union.
        /// </summary>
        public readonly int Length;

        private BitArray bits;
        private bool getting;

        public LazyBitUnion(int capacity)
        {
            this.Length = capacity;
        }

        /// <summary>
        /// Check if a bit in this union is on or off.
        /// Set this to true to turn a bit on or false to 'turn a bit off' (actually force a reset that will eventually
        /// regenerate the whole array later).
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool this[int index]
        {
            get
            {
                if (bits == null)
                {
                    bits = new BitArray(Length);
                    getting = true;
                    EnableBits(this);
                    getting = false;
                }
                else if (getting)
                {
                    throw new Exception("Attempted to read from LazyBitUnion while populating it.");
                }
                return bits[index];
            }
            set
            {
                if (value)
                {
                    if (bits != null)
                    {
                        bits[index] = value;
                    }
                }
                else
                {
                    if (getting)
                    {
                        throw new Exception("Attempted to clear LazyBitUnion while populating it.");
                    }
                    bits = null;
                }
            }
        }
    }
}
