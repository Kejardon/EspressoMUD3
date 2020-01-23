using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD
{
    public abstract class Shape : ISaveable
    {

        //Dimensions for a whole containing square.

        public abstract int TotalHeight { get; }
        public abstract int TotalWidth { get; }
        public abstract int TotalThickness { get; }


        //Not sure if I need these, so leaving alone for now.
        //public abstract int TotalRotatedHeight { get; }
        //public abstract int TotalRotatedWidth { get; }
        //public abstract int TotalRotatedThickness { get; }


        ///For now all shapes will be rectangular prisms.
        private int Height;
        private int Width;
        private int Thickness;

        //As far as I can think, these shouldn't ever actually be used.
        public SaveValues SaveValues {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
        public int GetSaveID() { throw new NotImplementedException(); }
        public void SetSaveID(int id) { throw new NotImplementedException(); }
        
        //TODO later: Joints to connect shapes, alternative shape options.
    }
    public class RectangularShape : Shape
    {
        public override int TotalHeight { get { return Height; } }
        public override int TotalWidth { get { return Width; } }
        public override int TotalThickness { get { return Thickness; } }

        //These aren't right. Not sure if I need them, so leaving alone for now.
        //public override int TotalRotatedHeight { get { return (int)Math.Round(Height * Math.Cos(Tilt * 2 * Math.PI)); } }
        //public override int TotalRotatedWidth { get { return (int)Math.Round(Height * Math.Cos(Tilt * 2 * Math.PI)); } }
        //public override int TotalRotatedThickness { get { return (int)Math.Round(Height * Math.Cos(Tilt * 2 * Math.PI)); } }


        ///For now all shapes will be rectangular prisms.
        public int Height;
        public int Width;
        public int Thickness;

    }
}
