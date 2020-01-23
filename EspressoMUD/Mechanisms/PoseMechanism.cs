using EspressoMUD.Geometry;
using KejUtils.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD
{
    public class PoseMechanism : Mechanism
    {
        public override Type type { get { return Type.Pose; } }

        public override ValidToPerform CanPerform(QueuedCommand command)
        {
            //TODO
            throw new NotImplementedException();
        }

        public override bool CanReachFrom(QueuedCommand command, IRoomPosition position, ref object targetData)
        {
            //TODO
            throw new NotImplementedException();
        }

        public override List<TargetRegion> TargetsOnSurface(QueuedCommand command, ObstacleSurface surface, ref object targetData)
        {
            //TODO
            throw new NotImplementedException();
        }


        /// <summary>
        /// What the vehicle looks like when it's moving using this.
        /// TODO: This should be calculated from the vehicle's current form. If the form changes (loses an arm, puts on oversized backpack, etc.)
        /// this should be recalculated somehow. What triggers that?
        /// </summary>
        public Hitbox Hitbox { get; set; }
        /// <summary>
        /// Where the hitbox is located when the vehicle is traveling on a given surface.
        /// e.g. Walking on a slightly slanted surface may push the hitbox up slightly from its surfaceposition
        /// to avoid its corner clipping into the surface.
        /// Maybe TODO future: Orientation instead of Position?
        /// </summary>
        public virtual Orientation HitboxOffset(ObstacleSurface surface)
        {
            //TODO: This should probably use more detail from the Hitbox (probably call a function on it).
            //For now just assuming it is a square hitbox.
            Vector normal = surface.GetNormal();
            if (normal.z > 1 / Math.Sqrt(2))
            {
                //Just going to prop the hitbox up enough to avoid clipping into the surface.
                //height = hitbox diagonal * (normal dot diagonal unit vector) / (normal dot vertical unit vector)
                double heightAdjust = this.Hitbox.MaxXIncrease(default(Rotation)) * (Math.Abs(normal.x) + Math.Abs(normal.y)) / normal.z;
                Orientation adjustment;
                adjustment.x = 0;
                adjustment.y = 0;
                adjustment.z = (int)heightAdjust;
                adjustment.Direction = 0;
                adjustment.Roll = 0;
                adjustment.Tilt = 0;
                return adjustment;
            }
            else
            {
                throw new NotImplementedException("Offset for sides isn't supported yet.");
            }

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="surface"></param>
        /// <returns></returns>
        public virtual bool CanUse(ObstacleSurface surface)
        {
            //By default can use surfaces as long as they're less than a 45 degree angle.

            if (surface.GetNormal().z > 1 / Math.Sqrt(2)) return true;
            return false;
        }

    }
}
