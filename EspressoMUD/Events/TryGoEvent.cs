using EspressoMUD.Geometry;
using KejUtils;
using KejUtils.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD
{
    /// <summary>
    /// Intermediate event. A target has been decided for a vehicle to move to. This event decides what exact destination
    /// and path that vehicle will take.
    /// </summary>
    public class TryGoEvent : RoomEvent
    {
        public override EventType Type { get { return EventType.TryGo; } }

        /// <summary>
        /// Which of the vehicle's movement types are allowed. Usually not specified and all are checked.
        /// </summary>
        public MovementEvent.MovementTypes SubType { get; set; }

        #region Variables only used to setup the event
        /// <summary>
        /// Full phrase used to trigger this action.
        /// </summary>
        public StringWords targetDescription;
        /// <summary>
        /// The 'target' of the action is the last set of words. This is the StringWords index that the set of words starts at.
        /// </summary>
        public int targetDescriptionStart;
        /// <summary>
        /// If a specific item is selected somehow, use this instead of the above.
        /// </summary>
        //public Item targetChosen;

        /// <summary>
        /// Physical object that is the destination of this movement action. If null, character is just moving
        /// in a direction without a specific destination. Longest term goal.
        /// </summary>
        //public Item target;

        /// <summary>
        /// Which direction and/or how far to try to move, after the current room. If null, the object isn't going to an
        /// arbitrary point outside the room (going to a specific item or a point inside the current room instead). Longest term goal
        /// or modifier for longest term goal if another exists.
        /// </summary>
        public MovementDirection direction;
        /// <summary>
        /// What the action is trying to do; move 'onto' or 'into' or just 'near' the target. Modifier for longest term
        /// goal.
        /// </summary>
        public MovementPreposition relation;
        #endregion

        /// <summary>
        /// What goal is supposed to be accomplished by this motion.
        /// </summary>
        private TargetData targetData;

        private TargetData GetOrGenerateTarget()
        {
            if (targetData == null)
            {
                Item target;
                //Generate it from other data.
                if (targetDescription != null)
                {
                    //TODO: This should maybe also have a prompt - if multiple items are found, allow user to specify which one. Then return to this. Probably need some additional refactoring because this should only be prompted at specific times.
                    target = TextParsing.FindKnownItem(targetDescription, movementSource, targetDescriptionStart, -1);
                }
                else
                {
                    target = null;
                }
                switch(relation)
                {
                    case MovementPreposition.To:
                        if (target != null)
                        {
                            //TODO: What to do with direction if one was specified?
                            targetData = new MechanismForItemTargetData(movementSource, target, null);
                        }
                        else
                        {
                            if (direction != null)
                            {
                                throw new NotImplementedException("Not supported yet");
                                //targetData = new DirectionTargetData(movementSource, direction);
                            }
                            else
                            {
                                movementSource.Client?.sendMessage("Go where?");
                                return null;
                            }
                        }
                        break;
                    case MovementPreposition.Above:
                    case MovementPreposition.Behind:
                    case MovementPreposition.Below:
                    case MovementPreposition.From:
                    case MovementPreposition.Of:
                        throw new NotImplementedException("Not supported yet");
                        //targetData = new NearItemTargetData(movementSource, target, relation, direction);
                        break;
                    case MovementPreposition.Closer:
                    case MovementPreposition.Farther:
                        throw new NotImplementedException("Not supported yet");
                        //targetData = new DistanceToItemTargetData(movementSource, target, relation, direction);
                        break;
                    case MovementPreposition.In:
                    case MovementPreposition.Out:
                        throw new NotImplementedException("Not supported yet");
                        //targetData = new ThroughItemTargetData(movementSource, target, relation);
                        break;
                    case MovementPreposition.On:
                        throw new NotImplementedException("Not supported yet");
                        //targetData = new OnItemTargetData(movementSource, target);
                        break;
                }
            }
            return targetData;
        }

        /// <summary>
        /// Path to use. Right now this is just the destination, later this can be replaced with a Path object. Actual
        /// target being checked by TryGoEvent immediately before MovementEvent.
        /// </summary>
        public IRoomPosition pathDestination;
        /// <summary>
        /// Rough position that the vehicle is trying to travel to in the current room. May be adjusted to something close
        /// and more convenient.
        /// </summary>
        public IRoomPosition targetPosition;

        /// <summary>
        /// List of obstacles that must be considered for movement (from vehicle to target).
        /// </summary>
        //public List<Obstacle> obstaclesForMovement;
        /// <summary>
        /// List of obstacles that must be considered for blocking acceptableTargetArea (from target to acceptableTargetArea
        /// intersection). This might be part of acceptableTargetArea instead.
        /// </summary>
        //public List<Obstacle> obstaclesForTargetArea;
        /// <summary>
        /// Hitbox for where the vehicle can move to to satisfy the movement request. Note that this won't actually be
        /// considered valid until a path all the way to the target is calculated. Obstacles inside the target area are handled
        /// differently than obstacles the vehicle must pass through.
        /// </summary>
        //public Obstacle acceptableTargetArea;

        /// <summary>
        /// Exit that the object is trying to travel through. If null, character is not trying to travel to another room.
        /// Takes priority over target. Usually an intermediate goal, but may be the last goal.
        /// </summary>
        public RoomLink targetExit; //TODO: Do I actually want this variable?


        /// <summary>
        /// When a movement spans multiple rooms, the leftover distance to move is dumped here. This will be reused if the MOB reaches
        /// the first room successfully.
        /// </summary>
        private MovementDirection leftoverDirection;


        private MovementEvent eventToFire;


        private List<KeyValuePair<Item, Obstacle>> FoundObstacles;

        /// <summary>
        /// How close to the destination is acceptable. Only used with 'near'?
        /// </summary>
        //public int howClose;

        protected Body eventSource;
        protected MOB movementSource;
        public override Item EventSource()
        {
            //TODO future: This and things that call it will need to change to support MOBs with multiple bodies.
            if (eventSource == null) eventSource = movementSource.Body;
            return eventSource;
        }
        public MOB MoveSource() { return movementSource; }
        public void SetMoveSource(MOB mob, Body vehicle)
        {
            movementSource = mob;
            eventSource = vehicle;
        }

        public override double TickDuration()
        {
            //TODO: Calculate this (and cache?) to check how long it takes to decide on a path.
            return 0.01;
        }

        /// <summary>
        /// For TryGoEvents, this should finish parsing and get the lock of all known obstacles, targets, and immediately
        /// involved rooms/exits.
        /// Generating the path is optional.
        /// </summary>
        /// <returns></returns>
        public override IDisposable StarttEventLocks()
        {
            IDisposable disposable;
            using (disposable = StartEventFor(movementSource))
            {
                MOB mob = movementSource;
                if (disposable == null)
                {
                    //TODO: Error of some kind? Log error and return?
                    movementSource.Client?.sendMessage("Error: Could not get the resource lock. Something else is busy for the room, contact an administrator if this problem persists.");
                    return null;
                }

                TargetData whereToGo = GetOrGenerateTarget();
                if (whereToGo == null) return null; //No valid destination. GetOrGenerateTarget should tell the source why it failed.
                Item vehicle = EventSource();
                if (whereToGo.SatisfiesTarget(vehicle.Position, vehicle) == 1)
                {
                    movementSource.Client?.sendMessage("You're already there!");
                    return null;
                }
                Body movingVehicle = vehicle as Body;
                if (movingVehicle == null)
                {
                    //Mechanism is apart from the MOB's body. Maybe should see if MOB is holding the EventSource? But generally this probably means the MOB is remote controlling something that isn't a Body but can do some specific thing.
                    //TODO future: Report it as Vehicle can't move instead of 'You can't move'?
                    movementSource.Client?.sendMessage("You can't move to get there.");
                    return null;
                }

                //TODO later: Look at nearby rooms and expand locking into those rooms
                //until it's reasonable to think all the important rooms are locked.


                //All of this is out of date and hopefully can be deleted once the actually planned method is finished and implemented.
                ////eventToFire = new MovementEvent();
                ////Currently, this will set up either the target (end item to go to) or 'direction' (how far to go).
                ////Setting up the direction will also set up the movement command pretty much entirely.
                ////So after this is done, if target is null, then destination is in the MovementEvent.
                ////  If destination is in a different room, the path is also in the MovementEvent.
                ////  If destination is in the same room, only the actual destination is set. The path still needs to be
                ////  decided.
                ////If the target isn't null, the path needs to be decided from the target's position.
                //if (target != null)
                //{
                //    //already set up from something before, don't need to parse text again.
                //    //AddItemLock(target); //Not locking because the target might not be observable to the MOB.
                //        //If the target is observable to the MOB, then any attempt to move it would need to lock this MOB which prevents it from moving away during this movement anyways.
                //        //This seems overly optimistic and may need reconsidering. Should only lock if the MOB can observe the target.
                //}
                //else if (targetDescription != null)
                //{
                //    target = TextParsing.FindKnownItem(targetDescription, movementSource, targetDescriptionStart, -1); //TODO: This should maybe also have a prompt? //, out exit
                //    if (target == null) return null; //No need to do anything, FindKownItem will report to the user what happened.
                //    //AddItemLock(target); //Not locking because the target might not be observable to the MOB.
                //}

                ////TODO around here: Use preposition and target and direction to generate 

                //if (direction != null)
                //{
                //    if (target == null)
                //    {
                //        //Using: preposition, current vehicle, and direction, (also MOB pathfinder and movement type)
                //        //Find:
                //        //  target position + targetArea obstacle, OR
                //        //  exit to go to + followup pathfinding to do once in the exit
                //        //      Not sure what 'followup pathfinding' consists of precisely, but basically needs to repeat this but with a different direction.
                //        //If a target position is found:
                //        //  Go to generic pathfinding sequence
                //        //If an exit is found instead:
                //        //  Set exit as current pathfinding destination
                //        //  Run generic pathfinding sequeunce
                //        //    If that sequence succeeds, repeat with followup pathfinding.
                //        //    If it fails, come back here with new obstacles and try a different exit.


                //        //Only pure MovementDirection inputs are handled here.
                //        IRoomPosition originalPosition = eventSource.Position;
                //        MovementEvent.MovementTypes decidedMoveType = SubType;
                //        IRoomPosition newPosition;
                //        MovementDirection leftoverDirection;
                //        RoomLink exit;
                //        bool shouldMove = originalPosition.FindNewPositionOrExit(mob, eventSource, direction, ref decidedMoveType, out newPosition, out leftoverDirection, out exit);
                //        if (!shouldMove)
                //        {
                //            Client client = mob.Client;
                //            if (client != null)
                //            {
                //                client.sendMessage("There doesn't seem to be a way to go that direction.");
                //            }
                //            return null;
                //        }
                //        if (exit != null)
                //        {
                //            targetExit = exit;
                //            eventToFire.relation = MovementPreposition.In;
                //            this.leftoverDirection = leftoverDirection;
                //        }
                //        else
                //        {
                //            eventToFire.relation = this.relation;
                //        }
                //        //if (newPosition != null)
                //        //{
                //        eventToFire.originalPosition = originalPosition;
                //        eventToFire.targetPosition = newPosition;
                //        eventToFire.SubType = decidedMoveType;
                //        //targetPosition = newPosition;
                //        //}
                //    }
                //    else
                //    {
                //        //Something like 3 feet north of table.
                //        //TODO Soonish. Implement this.
                //        //Probably use a switch on relation, to figure out where it goes
                //        //To, Of, Above, Below, Behind, Closer, Farther
                        

                //    }

                //}

                IDisposable lockToUse = disposable;
                disposable = null;
                return lockToUse;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="alternativeEvent"></param>
        /// <returns></returns>
        public override bool FinishEventSetup(out RoomEvent alternativeEvent)
        {
            alternativeEvent = null;
            return true;

            //This is an out of date design and will probably all be deleted.
            //Body source = EventSource() as Body;
            //MOB mob = MoveSource();
            //Client client = mob.Client;
            //MovementPreposition currentPreposition = relation;
            //restart:
            //IRoomPosition roughTargetPosition = targetPosition;
            //IRoomPosition currentPosition = source.Position;
            //if (roughTargetPosition == null && target != null)
            //{
            //    if (target.Position.forRoom != source.Position.forRoom)
            //    {
            //        client?.sendMessage("That is too far away (finding things in different rooms is not implemented yet).");
            //        return false;
            //        //TODO: Find paths from other rooms.
            //        //  Take known obstacles into account when deciding which exit can be reached.
            //        //  if succeed, set moveEvent.targetExit here, fall into next case (or maybe if that logic needs to be done already, set position and skip next case).
            //        //sendMessage and return if fail.
            //    }
            //    else
            //    {
            //        roughTargetPosition = target.Position;
            //    }
            //}

            //if (roughTargetPosition == null && direction != null && targetExit == null)
            //{
            //    IRoomPosition originalPosition = eventSource.Position;
            //    IRoomPosition newPosition;
            //    MovementDirection leftoverDirection;
            //    RoomLink exit;
            //    if (originalPosition.FindNewPositionOrExit(mob, eventSource, direction, ref SubType, out newPosition, out leftoverDirection, out exit))
            //    {
            //        roughTargetPosition = newPosition;
            //        targetExit = exit;
            //    }

            //}

            //if (roughTargetPosition == null && targetExit != null)
            //{
            //    List<PositionAndShape> passages = targetExit.EnterThrough(source);
            //    if (passages == null || passages.Count == 0)
            //    {
            //        //TODO: Explain failure better?
            //        client?.sendMessage("You can't go that way.");
            //    }
            //    //TODO: Look through obstacles and figure out which passages are actually viable.
            //    //PositionAndShape chosenOption = passages[0];
            //    //for (int i = 1; i < passages.Count; i++)
            //    //{
            //    //    PositionAndShape option = passages[i];
            //    //    //TODO: Comparison to decide closer passage. If any are 0, same as below Position.Equals
            //    //    continue;

            //    //    chosenOption = option;
            //    //}
            //    roughTargetPosition = passages[0].position;
            //    if (currentPosition.Equals(roughTargetPosition))
            //    {
            //        Room nextRoom;
            //        IRoomPosition nextPosition;
            //        if (targetExit.RoomThrough(roughTargetPosition, out nextRoom, out nextPosition))
            //        {
            //            //Make a new type of MovementEvent for switching rooms, create it here, only call RunFullEvent here,
            //            //check the result of it (success/fail), then continue working here. Maybe recheck some things like body and such.
            //            MovementEvent switchRooms = new MovementEvent();
            //            switchRooms.SetMoveSource(mob, source);
            //            switchRooms.SubType = SubType;
            //            switchRooms.targetPosition = nextPosition;
            //            switchRooms.targetExit = targetExit;
            //            switchRooms.relation = MovementPreposition.In;

            //            switchRooms.FullRunEvent(); //TODO important: Make this work (Body listeners/responses)

            //            if (switchRooms.Canceled || mob.Body.Position.forRoom != nextRoom)
            //            {
            //                return false;
            //            }

            //            // Movement succeeded, now continue pathfinding in the new room.
            //            //TODO important: Update 'direction'. Probably need to consider this in more general.
            //            targetExit = null;
            //            goto restart;
            //        }
            //        else
            //        {
            //            //This shouldn't really happen right now. Maybe there will be a reason for it to happen in the future.
            //            client?.sendMessage("You are mysteriously stopped.");
            //            return false;
            //        }
            //    }
            //    else
            //    {
            //        //Check if this exit isn't the target.
            //        if (target != null) //TODO later: && !target.contains(targetExit) ? 
            //        {
            //            //Going to something *past* the exit, so first step is to go into the exit.
            //            currentPreposition = MovementPreposition.In;
            //        }
            //    }
            //}

            //if (roughTargetPosition == null)
            //{
            //    if (target != null || targetExit != null)
            //        client?.sendMessage("You're not sure how to get there.");
            //    return false;
            //}
        }

        private List<ConsideredPosition> ConsideredPositions;
        private List<PathfindingConsideration> InterestingOptions;
        private List<PathfindSearchType> MovementTypes;
        public void StandardPathfinding()
        {
            Body movingVehicle = EventSource() as Body;

            //Start looking around for places to go.
            List<Mechanism> movementOptions = movingVehicle.AvailableMechanisms(Mechanism.Type.Movement);
            MovementTypes = new List<PathfindSearchType>();
            foreach (Mechanism mech in movementOptions)
            {
                MovementMechanism moveMech = mech as MovementMechanism;
                foreach (PathfindSearchType knownType in MovementTypes)
                {
                    if (knownType.AddMechanism(moveMech))
                    {
                        goto nextMechanism;
                    }
                }
                PathfindSearchType newType = PathfindSearchType.SearchTypeForMovement(moveMech.SubType);
                MovementTypes.Add(newType);
                newType.AddMechanism(moveMech);
            nextMechanism:;
            }

            ConsideredPositions = new List<ConsideredPosition>();
            InterestingOptions = new List<PathfindingConsideration>();
            //MovementPosition startingPosition = new MovementPosition();
            Obstacle startingObstacle;
            ObstacleSurface startingSurface;
            WorldRelativePosition startingPosition = movingVehicle.Position.WorldPosition();
            Room startingWorld;
            {
                Item startingOnItem = movingVehicle.Position.RestingOn;
                if (startingOnItem == null)
                {
                    startingObstacle = null;
                    startingSurface = default(ObstacleSurface);
                    startingWorld = startingPosition.OriginRoom;
                }
                else
                {
                    if (FoundObstacles == null)
                    {
                        FoundObstacles = new List<KeyValuePair<Item, Obstacle>>();
                    }
                    else
                    {
                        startingObstacle = FoundObstacles.FirstOrDefault((x) => x.Key == startingOnItem).Value;
                        if (startingObstacle != null) goto foundObstacle;
                    }
                    startingObstacle = startingOnItem.AsObstacle();
                    FoundObstacles.Add(new KeyValuePair<Item, Obstacle>(startingOnItem, startingObstacle));
                foundObstacle:
                    startingSurface = startingObstacle.GetSurface(movingVehicle.Position.RestingOnId);
                    //startingPosition.restingOn = startingObstacle.GetSurface(movingVehicle.Position.RestingOnId);
                    startingWorld = null;
                }
            }
            AddStartPosition((Point)startingPosition, startingSurface, startingWorld, 0, null, movingVehicle.Position.RestingPose);
            //AddConsideredPoint(startingPosition, 0, null, default(Mechanism.Cost), movingVehicle.Position.RestingPose);

            while (InterestingOptions.Count > 0)
            {
                PathfindingConsideration nextOption = InterestingOptions[0];
                InterestingOptions.RemoveAt(0);

                nextOption.InvestigateIfNeeded(this);
            }

            //if (FoundSolution != null) ?

            //Data needs:
            //  PathfindSearchType will need 


            //TODO here: Get a list of all types of movement mechanism among the mechanisms. Get an appropriate search method
            //for each mechanism. For now, just doing / assuming ground. In pseudocode:
            //ListOfTypes;
            //ListOfFoundTargets; (new class. Same as ConsideredPositions? + degree of satisfaction?)
            //ListOfConsideredPositions; (new class. Point, previous ConsideredPositions + costs to reach, min score to reach?, ConsideredPositions reachable from here?)
            //ListOfInterestingOptions; (new class. Score, method + arguments basically)
            //foreach mech in movementOptions, get type from ListOfTypes (create and add if missing), add mech to type
            //foreach type in ListOfTypes, run setup (for ground, get starting point's surface, set it up)
            //Add starting point (including surface and score to reach and from-point)
            //  If in ListOfConsideredPositions, add from-point/cost and return?
            //  Add to ListOfConsideredPositions
            //  //Compare score to BestScore for fully satisfied (from ListOfFoundTargets or cached or something). If worse, return.
            //  Calculate degree it satisfies target.
            //  Compare score to BestScore for that degree. If worse, return.
            //  If satisfies target to at least some degree...
            //      If fully satisfied, because of sorting order, this should be the best score also? If so this is done and we return.
            //      Else, add to ListOfFoundTargets.
            //  foreach type in ListOfTypes
            //      find interesting goals to go to from new point, binary sorted add to ListOfInterestingOptions
            //          Typically mech, startpoint, endpoint?
            //iterate over ListOfInterestingOptions


            //Ground movement types look around at current surface, find targets on surface and nearby other surfaces.
            //  
            //Air movement types look around at where target is in the air. Maybe also things to fly to?
            //  At least looks at portals to fly through.
            //All movement types use the same base size for the vehicle, and same set of obstacles. They may have different
            //rules for which obstacles can be ignored.
            //Obstacles are mainly a list of surfaces.
            //For ground motions:
            //  For finding places on the surface to attach to, the surface's plane and outline are used.
            //  For finding blockages from the surface, the surface's raw points are used.
            //Same for air motions? Future thing to figure out, not now.
            //Vehicles will generally have square, unrotating prisms. This will probably be updated in the future but pathfinding gets
            //a lot more complicated to handle more complicated things. Different mechanisms can still have different prisms though.
            //The largest sizes will be used to generate a total size for finding obstacles and drawing their boundaries on a possible path.
            //Individual mechanisms can be checked later for if an obstacle's boundaries applies to them.
            
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="newPoint">World-relative position</param>
        /// <param name="newSurface">ObstacleSurface this is on. If default/no manager, in the air, needs a world.</param>
        /// <param name="newWorld">Outermost room representing the 'world' this position is relative to. Not necessary if there's an obstaclesurface.</param>
        /// <param name="costScore">Total score needed from a TryGoEvent start point to here.</param>
        ///// <param name="pathScore">Score difference from the previous point to here.</param>
        /// <param name="previousPoint">The point that was used to get to here.</param>
        /// <param name="pose">Pose that the vehicle starts in in this point</param>
        private void AddStartPosition(Point newPoint, ObstacleSurface newSurface, Room newWorld, int costScore, ConsideredPosition previousPoint, PoseMechanism pose)
        {
            if (!newSurface.IsNull)
            {
                //Try to add to surface's data
                SurfaceData data = newSurface.GetSurfaceData(true);
                data.AddStartPoint(newSurface, newPoint, costScore, previousPoint, pose, this);
            }
            else
            {
                //TODO
                //Try to add to event's data for that world
                throw new NotImplementedException();
            }
        }

        /*
        private void AddConsideredPoint(MovementPosition newPoint, int score, ConsideredPosition previousPoint, Mechanism.Cost cost)
        {
            foreach (ConsideredPosition oldPosition in ConsideredPositions)
            {
                if (oldPosition.MyPosition == newPoint) //This is a point that's already considered.
                {
                    oldPosition.AddPreviousPoint(previousPoint, cost);
                    return;
                }
            }
            ConsideredPosition newPosition = new ConsideredPosition(newPoint, score);
            newPosition.AddPreviousPoint(previousPoint, cost);

            foreach (PathfindSearchType type in MovementTypes)
            {
                type.SearchFromNewPoint(newPosition, this);
            }
        }
        */

        public abstract class TargetData
        {
            /// <summary>
            /// Check if a planned motion will satisfy what requested the motion to begin with.
            /// </summary>
            /// <returns>0 if doesn't satisfy. 1 if fully satisfies.
            /// Higher number if it partially satisfies (lower is better), in an attempt to sort options.</returns>
            public abstract int SatisfiesTarget(
                //TODO: Some path class here? Probably not
                IRoomPosition theoreticalPosition, Item vehicle
                );
            //public List<InTargetArea>

            #region Placement related, shared with TargetData class
            /// <summary>
            /// 
            /// </summary>
            /// <param name="command">Action that is trying to be satisfied/accomplished.</param>
            /// <param name="surface">Surface this is on. If the obstacle is null, this is in air instead of on a surface.</param>
            /// <param name="targetData">Cached data for calculations from this TargetData for this command.</param>
            /// <returns>Null if there are no targets on the surface. Else a list of regions that might allow the acting MOB to
            /// satisfy the action - it doesn't have to be guaranteed that all spots in the region will satisfy the action, but
            /// all spots that satisfy the action must be in the region.</returns>
            public abstract List<TargetRegion> TargetsOnSurface(QueuedCommand command, ObstacleSurface surface, ref object targetData);

            /// <summary>
            /// 
            /// </summary>
            /// <param name="command">Action that is trying to be satisfied/accomplished.</param>
            /// <param name="position">Specific position that this action could be attempted from.</param>
            /// <param name="targetData">Cached data for calculations from this TargetData for this command.</param>
            /// <returns></returns>
            public abstract bool CanReachFrom(QueuedCommand command, IRoomPosition position, ref object targetData);
            #endregion

        }
        private class MechanismForItemTargetData : TargetData
        {
            public MechanismForItemTargetData(MOB mob, Item itemToReach, Mechanism tool)
            {
                if (tool == null)
                {

                }
            }

            private class NearMechanism : Mechanism
            {
                public override Type type
                {
                    get
                    {
                        return Type.Reach;
                    }
                }

                public override ValidToPerform CanPerform(QueuedCommand command)
                { throw new NotImplementedException(); }

                public override bool CanReachFrom(QueuedCommand command, IRoomPosition position, ref object targetData)
                {
                    throw new NotImplementedException();
                }

                public override List<TargetRegion> TargetsOnSurface(QueuedCommand command, ObstacleSurface surface, ref object targetData)
                {
                    throw new NotImplementedException();
                }
            }

        }
        private class InTargetArea
        {
            /// <summary>
            /// World this target area is in. Only needed for points in air, points on surface are relative surface.
            /// </summary>
            public Room World;
            /// <summary>
            /// Main point for this target, mandatory.
            /// If size isn't 0, then target is a circle/sphere around this point. 
            /// Otherwise, if B isn't null, then target is a line.
            /// </summary>
            public Point A;
            public Point B;
            public int size = 0;
        }


        private abstract class PathfindingConsideration
        {
            public abstract void Investigate(TryGoEvent context);
            /// <summary>
            /// List of consideration groups that succeeding with this consideration would fulfill. If they are all already
            /// fulfilled, this consideration may not be necessary.
            /// </summary>
            protected LazyList<PathfindingConsiderationGroup> Groups;
            /// <summary>
            /// True if something without a group made this a consideration, i.e. this always needs to be tried.
            /// </summary>
            protected bool GrouplessConsideration = false;

            public void InvestigateIfNeeded(TryGoEvent context)
            {
                if (!GrouplessConsideration && Groups != null)
                {
                    for(int i = 0; i < Groups.Count; i++)
                    {
                        PathfindingConsiderationGroup next = Groups[i];
                        if (next == null || !next.HasBeenMet)
                        {
                            goto investigate;
                        }
                    }
                    return;
                }
            investigate:
                Investigate(context);
            }
        }
        private class PathfindingConsiderationGroup
        {
            public bool HasBeenMet { get; private set; }
            public void Met() { HasBeenMet = true; }
        }

        private abstract class PathfindSearchType
        {
            public static PathfindSearchType SearchTypeForMovement(MovementEvent.MovementTypes type)
            {
                switch(type)
                {
                    //IMPORTANT: These should match the class's MatchesType method.
                    case MovementEvent.MovementTypes.Walk:
                        return new WalkSearchType();
                    case MovementEvent.MovementTypes.Fly:
                    case MovementEvent.MovementTypes.Teleport:
                        throw new NotImplementedException("TODO: Write this code for " + type.ToString());
                    default:
                        throw new Exception("Unexpected movement type: " + type.ToString());
                }
            }

            protected List<MovementMechanism> Mechanisms = new List<MovementMechanism>();

            /// <summary>
            /// 
            /// </summary>
            /// <param name="position"></param>
            public abstract void SearchFromNewPoint(ConsideredPosition position, TryGoEvent context);

            protected abstract bool MatchesType(MovementEvent.MovementTypes type);

            public bool AddMechanism(MovementMechanism mechanism)
            {
                if (MatchesType(mechanism.SubType))
                {
                    Mechanisms.Add(mechanism);
                    return true;
                }
                else return false;
            }

            private class WalkSearchType : PathfindSearchType
            {
                //public static WalkSearchType Instance = new WalkSearchType();

                private List<ObstacleSurface> Surfaces = new List<ObstacleSurface>();
                /// <summary>
                /// The smallest simple hitbox that's guaranteed to find all interesting obstacles to deal with when handling
                /// the given surface.
                /// </summary>
                /// <param name="surface"></param>
                /// <returns></returns>
                private MaxExtendedHitbox GetMaxHitboxRange(ObstacleSurface surface)
                {
                    //int maxReach = 0;
                    MaxExtendedHitbox newHitbox = new MaxExtendedHitbox();
                    //Start with a hitbox that accounts for all shapes and reaches from a fixed position
                    Rotation nonrotation = new Rotation();
                    foreach (Mechanism mech in Mechanisms)
                    {
                        MovementMechanism movementMech = mech as MovementMechanism;
                        Hitbox moveHitbox = movementMech.Hitbox;
                        Orientation offset = movementMech.HitboxOffset(surface);
                        int reach = movementMech.StepSize;

                        int temp = moveHitbox.MaxXIncrease(nonrotation) + offset.x + reach;
                        if (temp > newHitbox.MaxX) newHitbox.MaxX = temp;
                        temp = moveHitbox.MaxXDecrease(nonrotation) - offset.x + reach;
                        if (temp > newHitbox.MinX) newHitbox.MinX = temp;
                        temp = moveHitbox.MaxYIncrease(nonrotation) + offset.y + reach;
                        if (temp > newHitbox.MaxY) newHitbox.MaxY = temp;
                        temp = moveHitbox.MaxYDecrease(nonrotation) - offset.y + reach;
                        if (temp > newHitbox.MinY) newHitbox.MinY = temp;
                        temp = moveHitbox.MaxZIncrease(nonrotation) + offset.z + reach;
                        if (temp > newHitbox.MaxZ) newHitbox.MaxZ = temp;
                        temp = moveHitbox.MaxZDecrease(nonrotation) - offset.z + reach;
                        if (temp > newHitbox.MinZ) newHitbox.MinZ = temp;

                        //newHitbox.IncreaseToInclude(moveHitbox, offset, nonrotation);
                        //if (maxReach < movementMech.StepSize) maxReach = movementMech.StepSize;
                    }
                    //newHitbox.MaxX = Math.Max(newHitbox.MaxX, maxReach);
                    //newHitbox.MinX = Math.Max(newHitbox.MinX, maxReach);
                    //newHitbox.MaxY = Math.Max(newHitbox.MaxY, maxReach);
                    //newHitbox.MinY = Math.Max(newHitbox.MinY, maxReach);
                    //newHitbox.MaxZ = Math.Max(newHitbox.MaxZ, maxReach);
                    //newHitbox.MinZ = Math.Max(newHitbox.MinZ, maxReach);

                    //Go over all the points on the surface, find the farthest positions possible
                    int stop = surface.PointCount();
                    MaxExtendedHitbox positionOptions = new MaxExtendedHitbox();
                    ObstaclePoint origin = surface.GetPoint(0);
                    Point originPosition = origin.Position;
                    for (int i = 1; i < stop; i++)
                    {
                        Point point = surface.GetPoint(i).Position;
                        positionOptions.MaxX = Math.Max(positionOptions.MaxX, point.x - originPosition.x);
                        positionOptions.MinX = Math.Max(positionOptions.MinX, originPosition.x - point.x);
                        positionOptions.MaxY = Math.Max(positionOptions.MaxY, point.y - originPosition.y);
                        positionOptions.MinY = Math.Max(positionOptions.MinY, originPosition.y - point.y);
                        positionOptions.MaxZ = Math.Max(positionOptions.MaxZ, point.z - originPosition.z);
                        positionOptions.MinZ = Math.Max(positionOptions.MinZ, originPosition.z - point.z);
                    }
                    //Expand the hitbox to allow for any position
                    newHitbox.MaxX += positionOptions.MaxX;
                    newHitbox.MinX += positionOptions.MinX;
                    newHitbox.MaxY += positionOptions.MaxY;
                    newHitbox.MinY += positionOptions.MinY;
                    newHitbox.MaxZ += positionOptions.MaxZ;
                    newHitbox.MinZ += positionOptions.MinZ;

                    return newHitbox;
                }

                public override void SearchFromNewPoint(ConsideredPosition position, TryGoEvent context)
                {
                    ObstacleSurface currentSurface = position.MyPosition.restingOn;
                    if (currentSurface.manager == null) return; //Can't travel without a surface.
                    foreach (ObstacleSurface surface in Surfaces)
                    {
                        if (currentSurface.EqualTo(surface))
                        {
                            //Probably TODO: I feel like I'll need something here to mark a new point on the surface but haven't gotten there yet.

                            goto foundSurface;
                        }
                    }

                    Surfaces.Add(currentSurface);
                    MaxExtendedHitbox moveHitbox = GetMaxHitboxRange(currentSurface);

                    Point rootPosition = currentSurface.GetPoint(0).Position;
                    WorldRelativeOrientation worldPosition = new WorldRelativeOrientation(position.MyPosition.position);

                    List<Obstacle> nearbyObstacles = context.GetObstaclesNear(currentSurface, moveHitbox, worldPosition);
                    foreach (Obstacle obstacle in nearbyObstacles)
                    {
                        foreach (MovementMechanism mech in Mechanisms)
                        {
                            if (mech.CanUse(currentSurface))
                                obstacle.MarkIntersectWithSurface(currentSurface, mech); //TODO: Get a rotation from the mechanism's orientation with currentSurface
                        }

                        //TODO: Transitions to other nearby surfaces?
                    }
                    
                    foundSurface:
                    //TODO here: Add investigation options for trying to go to points reachable from here.
                }

                protected override bool MatchesType(MovementEvent.MovementTypes type)
                {
                    return type == MovementEvent.MovementTypes.Walk;
                }

                /// <summary>
                /// Simplistic hitbox that functions assumes everything is an aligned rectangular prism.
                /// Used as a first-pass check to see if things are worth checking in detail.
                /// </summary>
                private class MaxExtendedHitbox : Hitbox
                {
                    public MaxExtendedHitbox() //int maxX, int minX, int maxY, int minY, int maxZ, int minZ
                    {
                        //MaxX = maxX;
                        //MinX = minX;
                        //MaxY = maxY;
                        //MinY = minY;
                        //MaxZ = maxZ;
                        //MinZ = minZ;
                    }
                    public int MaxX;
                    public int MinX;
                    public int MaxY;
                    public int MinY;
                    public int MaxZ;
                    public int MinZ;

                    public override HitboxType Type
                    { get { throw new NotImplementedException(); } }

                    public override int MaxXDecrease(Rotation rotation) { return MinX; }
                    public override int MaxXIncrease(Rotation rotation) { return MaxX; }
                    public override int MaxYDecrease(Rotation rotation) { return MinY; }
                    public override int MaxYIncrease(Rotation rotation) { return MaxY; }
                    public override int MaxZDecrease(Rotation rotation) { return MinZ; }
                    public override int MaxZIncrease(Rotation rotation) { return MaxZ; }

                    protected override bool SubInRange(WorldRelativeOrientation ownLocation, Hitbox target, WorldRelativeOrientation targetLocation)
                    {
                        Rotation otherRotation = (Rotation)targetLocation;

                        if (targetLocation.x - target.MaxXDecrease(otherRotation) > ownLocation.x + MaxX) return false;
                        if (targetLocation.x + target.MaxXIncrease(otherRotation) < ownLocation.x - MinX) return false;
                        if (targetLocation.y - target.MaxYDecrease(otherRotation) > ownLocation.y + MaxY) return false;
                        if (targetLocation.y + target.MaxYIncrease(otherRotation) < ownLocation.y - MinY) return false;
                        if (targetLocation.z - target.MaxZDecrease(otherRotation) > ownLocation.z + MaxZ) return false;
                        if (targetLocation.z + target.MaxZIncrease(otherRotation) < ownLocation.z - MinZ) return false;

                        return true;
                    }

                    //public void IncreaseToInclude(Hitbox moveHitbox, Position offset, Rotation rotation)
                    //{
                    //    int temp = moveHitbox.MaxXIncrease(rotation) + offset.x;
                    //    if (temp > MaxX) MaxX = temp;
                    //    temp = moveHitbox.MaxXDecrease(rotation) - offset.x;
                    //    if (temp > MinX) MinX = temp;
                    //    temp = moveHitbox.MaxYIncrease(rotation) + offset.y;
                    //    if (temp > MaxY) MaxY = temp;
                    //    temp = moveHitbox.MaxYDecrease(rotation) - offset.y;
                    //    if (temp > MinY) MinY = temp;
                    //    temp = moveHitbox.MaxZIncrease(rotation) + offset.z;
                    //    if (temp > MaxZ) MaxZ = temp;
                    //    temp = moveHitbox.MaxZDecrease(rotation) - offset.z;
                    //    if (temp > MinZ) MinZ = temp;
                    //}
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="currentSurface"></param>
        /// <param name="hitbox">Currently, this has orientation factored into it and only needs a position.</param>
        /// <param name="hitboxPosition"></param>
        /// <returns></returns>
        private List<Obstacle> GetObstaclesNear(ObstacleSurface currentSurface, Hitbox hitbox, WorldRelativeOrientation hitboxPosition)
        {
            //TODO eventually: Support searching for obstacles in multiple rooms. For now, only single-room pathfinding will be done.
            if (FoundObstacles == null)
            {
                FoundObstacles = new List<KeyValuePair<Item, Obstacle>>();
                Room room = eventSource.Position.ForRoom;
                List<Item> items = room.FindNearbyInterestingItems((x) => x.IsObstacle(), default(WorldRelativeOrientation), null);
                foreach (Item item in items)
                {
                    FoundObstacles.Add(new KeyValuePair<Item, Obstacle>(item, item.AsObstacle()));
                }
            }

            List<Obstacle> obstacles = new List<Obstacle>();
            foreach (KeyValuePair<Item, Obstacle> options in FoundObstacles)
            {
                if (hitbox.InRange(hitboxPosition, options.Key.Size, options.Key.Position.WorldOrientation()))
                {
                    obstacles.Add(options.Value);
                }
            }

            return obstacles;
        }
        /*
        private class ConsideredPosition
        {
            public ConsideredPosition(MovementPosition position, int score)
            {
                MyPosition = position;
                MinimumScore = score;
            }
            public MovementPosition MyPosition;
            public int MinimumScore;

            private List<ConsideredPositionAndCost> FromPoints; //= new List<ConsideredPositionAndScore>();

            public void AddPreviousPoint(ConsideredPosition previous, Mechanism.Cost cost)
            {
                if (FromPoints == null) FromPoints = new List<ConsideredPositionAndCost>();
                else
                {
                    foreach (ConsideredPositionAndCost previousOption in FromPoints)
                    {
                        if (previousOption.Position == previous && cost.StrictlyNoBetterThan(previousOption.Cost))
                        {
                            //No reason to handle this option, a strictly better or equal option exists.
                            return;
                        }
                    }
                }
                ConsideredPositionAndCost newPoint;
                newPoint.Position = previous;
                newPoint.Cost = cost;
                FromPoints.Add(newPoint);
                //TODO probably: Handle minimum costs? Or Path costs?
                //TODO probably: Add to points the previous point can go to?
            }


            private struct ConsideredPositionAndCost
            {
                public ConsideredPosition Position;
                public Mechanism.Cost Cost;
            }
        }
        */
    }

}
