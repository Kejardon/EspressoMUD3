using EspressoMUD.Geometry;
using KejUtils.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD
{
    public class MovementMechanism : PoseMechanism
    {
        public override Type type { get { return Type.Movement; } }

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

        public MovementEvent.MovementTypes SubType { get; set; }


        /// TODO future: More complicated costs for things that also need mana.
        /// Ideal solution is probably something like a structure that contains all standard costs and an optional extra object for special costs
        //public Cost CostForPath(PathStep step)
        //{
        //    //TODO future: Distance calculation should be more efficient.
        //    double distance = step.StartPosition.DistanceTo(step.EndPosition).total;
        //    Cost result = new Cost();
        //    result.Fatigue = (float)distance * FatiguePerUnit;
        //    result.Time = (float)distance * TimePerUnit;

        //    return result;
        //}

        /// <summary>
        /// Fatigue cost per 1 unit moved. 
        /// </summary>
        public float FatiguePerUnit { get; set; }

        /// <summary>
        /// How much time it takes to go 
        /// </summary>
        public float TimePerUnit { get; set; }



        /// <summary>
        /// For surface motion types. How far this can go in a single 'step'.
        /// Used to calculate things like ignoring holes in a surface or going to other surfaces.
        /// Generally this is expected to be at least half the size of a square Hitbox, otherwise some simple geometry like a
        /// ramp may be impossible to use for this mechanism.
        /// </summary>
        public int StepSize { get; set; }

    }
}
