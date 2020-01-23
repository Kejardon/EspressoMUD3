using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD
{
    /// <summary>
    /// Link between MOBs and Commands. Mechanisms are how a MOB is able to perform an action, and typically include
    /// an Item as the tool.
    /// </summary>
    public abstract class Mechanism
    {
        public enum Type
        {
            Pose,
            Movement, //Change in position - doesn't count turning
            Look,
            Reach, //Able to touch the target
            Attack, //Able to (try to) cause damage to the target
            Get, //Able to pick up the target
        }

        /// <summary>
        /// The type of action that this mechanism can be used for.
        /// </summary>
        public abstract Type type { get; }

        public enum ValidToPerform
        {
            No = 0, //This mechanism can never perform this command.
            Yes = 1, //This mechanism can perform this command immediately.
            MoveFirst = 2 //This mechanism could perform this command but cannot right now. Must move to a valid position first.
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        /// <returns>
        /// </returns>
        public abstract ValidToPerform CanPerform(QueuedCommand command);

        #region Placement related, shared with TargetData class
        /// <summary>
        /// 
        /// </summary>
        /// <param name="command">Action that is trying to be satisfied/accomplished.</param>
        /// <param name="surface">Surface this is on. If the obstacle is null, this is in air instead of on a surface.</param>
        /// <param name="targetData">Cached data for calculations from this mechanism for this command.</param>
        /// <returns>Null if there are no targets on the surface. Else a list of regions that might allow the acting MOB to
        /// satisfy the action - it doesn't have to be guaranteed that all spots in the region will satisfy the action, but
        /// all spots that satisfy the action must be in the region.</returns>
        public abstract List<TargetRegion> TargetsOnSurface(QueuedCommand command, ObstacleSurface surface, ref object targetData);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        /// <param name="position"></param>
        /// <param name="vehicle"></param>
        /// <param name="targetData"></param>
        /// <returns></returns>
        public abstract bool CanReachFrom(QueuedCommand command, IRoomPosition position, ref object targetData);
        #endregion

        public struct Cost
        {
            public float Fatigue;
            public float Time;

            /// <summary>
            /// Checks if this cost is better in ANY way than another cost.
            /// </summary>
            /// <param name="otherCost"></param>
            /// <returns>True if ANY of the costs are lower than otherCost. False if all of the costs are equal or greater than otherCost.</returns>
            public bool StrictlyNoBetterThan(Cost otherCost)
            {
                return (Fatigue >= otherCost.Fatigue &&
                        Time >= otherCost.Time);
            }
        }
    }
}
