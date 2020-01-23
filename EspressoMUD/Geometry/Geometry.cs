using KejUtils.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD.Geometry
{
    public static class MUDGeometry
    {
        /// <summary>
        /// Convert from an orientation to a set of coefficients to convert positions.
        /// </summary>
        public static void GetTranslationCoefficients(Rotation context,
            out double xTox, out double xToy, out double xToz,
            out double yTox, out double yToy, out double yToz,
            out double zTox, out double zToy, out double zToz)
        {
            double cosD = Math.Cos(context.Direction * 2 * Math.PI);
            double sinD = Math.Sin(context.Direction * 2 * Math.PI);
            double cosT = Math.Cos(context.Tilt * Math.PI / 2);
            double sinT = Math.Sin(context.Tilt * Math.PI / 2);
            double cosR = Math.Cos(context.Roll * Math.PI);
            double sinR = Math.Sin(context.Roll * Math.PI);

            //D -> T -> R
            //xTox = cosD * cosR - sinR * sinT * sinD;
            //yTox = sinD * cosR + sinR * sinT * cosD;
            //zTox = cosT * sinR;
            //xToy = -sinD * cosT;
            //yToy = cosD * cosT;
            //zToy = -sinT;
            //xToz = -cosD * sinR - sinT * sinD * cosR;
            //yToz = sinT * cosD * cosR - sinD * sinR;
            //zToz = cosT * cosR;

            //On second thought, this doesn't match my mental model. Also I'm reversing the y axis (see KejUtils.Geometry's axis)
            //T -> R -> D gives results that match my mental model.
            xTox = cosD * cosR;
            yTox = -cosD * sinR * sinT + sinD * cosT;
            zTox = cosD * sinR * cosT + sinD * sinT;
            xToy = -sinD * cosR;
            yToy = cosD * cosT + sinD * sinR * sinT;
            zToy = cosD * sinT - sinD * sinR * cosT;
            xToz = -sinR;
            yToz = -cosR * sinT;
            zToz = cosR * cosT;
        }
        public static Point ApplyRotationToPosition(Rotation context, Point innerPoint)
        {
            double xTox, xToy, xToz, yTox, yToy, yToz, zTox, zToy, zToz;
            GetTranslationCoefficients(context, out xTox, out xToy, out xToz, out yTox, out yToy, out yToz, out zTox, out zToy, out zToz);
            Point outerPoint;
            outerPoint.x = (int)(xTox * innerPoint.x + yTox * innerPoint.y + zTox * innerPoint.z);
            outerPoint.y = (int)(xToy * innerPoint.x + yToy * innerPoint.y + zToy * innerPoint.z);
            outerPoint.z = (int)(xToz * innerPoint.x + yToz * innerPoint.y + zToz * innerPoint.z);
            return outerPoint;
        }
        public static Rotation ApplyRotationToRotation(Rotation context, Rotation innerRotation)
        {
            double xTox, xToy, xToz, yTox, yToy, yToz, zTox, zToy, zToz;
            GetTranslationCoefficients(context, out xTox, out xToy, out xToz, out yTox, out yToy, out yToz, out zTox, out zToy, out zToz);
            // I think this might work? Very quick conceptual thought says it can.
            Rotation outerRotation;
            outerRotation.Tilt = (int)(xTox * innerRotation.Tilt * 1 + yTox * innerRotation.Roll * 2 + zTox * innerRotation.Direction * 4);
            outerRotation.Roll = (int)(xToy * innerRotation.Tilt / 2 + yToy * innerRotation.Roll * 1 + zToy * innerRotation.Direction * 2);
            outerRotation.Direction = (int)(xToz * innerRotation.Tilt / 4 + yToz * innerRotation.Roll / 2 + zToz * innerRotation.Direction * 1);
            CapAndFixRotation(ref outerRotation);
            return outerRotation;
        }
        public static Orientation ApplyRotationToOrientation(Rotation context, Orientation innerOrientation)
        {
            double xTox, xToy, xToz, yTox, yToy, yToz, zTox, zToy, zToz;
            GetTranslationCoefficients(context, out xTox, out xToy, out xToz, out yTox, out yToy, out yToz, out zTox, out zToy, out zToz);
            Orientation outerOrientation;
            outerOrientation.x = (int)(xTox * innerOrientation.x + yTox * innerOrientation.y + zTox * innerOrientation.z);
            outerOrientation.y = (int)(xToy * innerOrientation.x + yToy * innerOrientation.y + zToy * innerOrientation.z);
            outerOrientation.z = (int)(xToz * innerOrientation.x + yToz * innerOrientation.y + zToz * innerOrientation.z);
            outerOrientation.Tilt = (int)(xTox * innerOrientation.Tilt * 1 + yTox * innerOrientation.Roll * 2 + zTox * innerOrientation.Direction * 4);
            outerOrientation.Roll = (int)(xToy * innerOrientation.Tilt / 2 + yToy * innerOrientation.Roll * 1 + zToy * innerOrientation.Direction * 2);
            outerOrientation.Direction = (int)(xToz * innerOrientation.Tilt / 4 + yToz * innerOrientation.Roll / 2 + zToz * innerOrientation.Direction * 1);
            CapAndFixRotation(ref outerOrientation);
            return outerOrientation;
        }
        /// <summary>
        /// Fix any overflow for direction, tilt, and roll. Need to all be done at once because overflow tilt is shifted to direction and roll.
        /// </summary>
        public static void CapAndFixRotation(ref Rotation rotation)
        {
            rotation.Tilt = rotation.Tilt % 4;
            if (rotation.Tilt > 2) rotation.Tilt -= 4;
            else if (rotation.Tilt < -2) rotation.Tilt += 4;
            bool invert = false;
            if (rotation.Tilt > 1) { invert = true; rotation.Tilt -= 2; }
            else if (rotation.Tilt < -1) { invert = true; rotation.Tilt += 2; }

            if (invert) rotation.Direction += (float)0.5;
            rotation.Direction = rotation.Direction % 1;
            if (rotation.Direction < 0) rotation.Direction += 1;

            if (invert) rotation.Roll += 1;
            rotation.Roll = rotation.Roll % 2;
            if (rotation.Roll > 1) rotation.Roll -= 2;
            else if (rotation.Roll <= -1) rotation.Roll += 2;
        }
        /// <summary>
        /// Fix any overflow for direction, tilt, and roll. Need to all be done at once because overflow tilt is shifted to direction and roll.
        /// </summary>
        public static void CapAndFixRotation(ref Orientation rotation)
        {
            rotation.Tilt = rotation.Tilt % 4;
            if (rotation.Tilt > 2) rotation.Tilt -= 4;
            else if (rotation.Tilt < -2) rotation.Tilt += 4;
            bool invert = false;
            if (rotation.Tilt > 1) { invert = true; rotation.Tilt -= 2; }
            else if (rotation.Tilt < -1) { invert = true; rotation.Tilt += 2; }

            if (invert) rotation.Direction += (float)0.5;
            rotation.Direction = rotation.Direction % 1;
            if (rotation.Direction < 0) rotation.Direction += 1;

            if (invert) rotation.Roll += 1;
            rotation.Roll = rotation.Roll % 2;
            if (rotation.Roll > 1) rotation.Roll -= 2;
            else if (rotation.Roll <= -1) rotation.Roll += 2;
        }
    }
}
