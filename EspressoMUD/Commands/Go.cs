using KejUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EspressoMUD.TextParsing;

namespace EspressoMUD
{
    public class Go : MOBCommand
    {
        public Go() : base ("go", new string[] { "move" })
        {
        }
        public override void Execute(MOB mob, QueuedCommand command)
        {
            /*
            Form is always (optional direction/distance) (optional preposition) (optional target)
                direction/distance can work alone (implies 'from here')
                if direction/distance is missing, if preposition is not specific, 'in reach' is implied
                preposition requires a target
                target can work alone (depends on target. If target defaults to enterable, implies 'in'. Else 'near')
            Search for prepositions, get a list of them.
                Validate prepositions. Need to have nothing or a valid direction/distance before preposition.
                How many can actually be possible? I think only zero or one preposition is really possible?
                TODO: Special user input to specify a preposition instead of parser guessing. Mostly goes in StringWords but
                matching prepositions/item names needs to respect that too (might work automatically).
                    Some system similar to quotes probably. Quotes may be useful though, like book titles or something.
            If no prepositions, attempt to parse full string as direction/distance. If successful, do that
            //Search whole string for item/target matches and associated strings.
                In general, 'visible to MOB' items.
                Must match to end of string?
                Nevermind, this string will be identified but targets will not be searched for until a TryGoEvent is run.
            //Find associated strings that line up with a preposition
            Instead of above two major points:
                Identify all valid strings (start after preposition, go to end of string)
                    TODO: Special user input to specify a string instead of parser guessing?
                        This is basically implied by a preposition. However it maybe makes more sense to specify the target instead of the preposition.
                //Search all 'visible to MOB' items that exactly match any of the valid strings.
            How to pick best match?
                Maybe just not go if more than one match found, request more info. prompt?
            */
            StringWords goToInput = command.parsedCommand;

            //See if there's a preposition
            int start = 1; //Skip command ("go")
            int prepValue;
            MovementPreposition prepEnum;
            MovementDirection direction = null;
            TryGoEvent moveEvent = new TryGoEvent();
            int index = TextParsing.FindAPreposition(goToInput, typeof(MovementPreposition), out prepValue, start);
            if (index != -1)
            {
                //Is this a valid preposition? Check if it starts correctly.
                prepEnum = (MovementPreposition)prepValue;
                if (!prepEnum.AllowDirection() && !prepEnum.AllowDistance())
                {
                    if (index != start + 1)
                    {
                        //Invalid preposition for this location, needs to be at start of command.
                        goto noPrepositionFound;
                    }
                }
                else if ((prepEnum.RequireDirection() || prepEnum.RequireDistance()) && index == start + 1)
                {
                    //Invalid preposition for this location, needs to come after a direction/distance.
                    goto noPrepositionFound;
                }
                else
                {
                    direction = TextParsing.ParseAsDirection(goToInput, start, ref index, prepEnum.RequireDirection(), prepEnum.RequireDistance(), prepEnum.AllowDirection(), prepEnum.AllowDistance());
                    if (direction == null) //Not a valid preposition. Ignore it.
                    {
                        goto noPrepositionFound;
                    }
                    else //Maybe valid preposition. 
                    {
                        //Check if preposition matches direction more closely? I think the previous checks have finished that successfully at this point.

                        //Valid preposition. Parse the REST of the sentence as an object.
                        start = index;
                        goto startEvent;
                    }
                }
            }
            else
            {
                //Is the whole thing a direction?
                direction = TextParsing.ParseAsDirection(goToInput, start, ref index);
                if (direction != null)
                {
                    if (direction.direction != Directions.NoDirection && index == goToInput.Segments.Length)
                    {
                        //Mark it as ONLY a direction, no destination (goToInput has been completely parsed/consumed)
                        goToInput = null;
                        //goto startEvent;
                    }
                    else
                    {
                        //Can't go only a distance with NoDirection. Cancel this parsing.
                        direction = null;
                    }
                }
                //Else try to parse the whole thing as an object to go to
            }
        //Text parsing has decided what words go to what (i.e. direction vs. target).
        //Text parsing has NOT identified objects yet.
        noPrepositionFound:
            moveEvent.targetDescription = goToInput;
            start = 1;
            prepEnum = MovementPreposition.To; // null; ?

        startEvent: //foundPreposition
            moveEvent.targetDescriptionStart = start;
            moveEvent.direction = direction;
            moveEvent.relation = prepEnum;
            moveEvent.SetMoveSource(mob, null);

            //Raw command parsing done, target parsing has not been done.
            //Run the event to try to find a path to the target.
            moveEvent.FullRunEvent();
        }
    }


    public enum MovementPreposition
    {

        /// <summary>
        /// Go through a room link in the 'In' direction.
        /// </summary>
        In,
        /// <summary>
        /// Go through a room link in the 'Out' direction.
        /// </summary>
        Out,
        /// <summary>
        /// Stand anywhere on top of a thing.
        /// </summary>
        //allow with distance as a 'don't move off this' thing? probably not.
        On,
        /// <summary>
        /// Go to or near an object. Defaults to trying to be in arms reach.
        /// TODO: Defaults to 'In' for enterable things?
        /// </summary>
        To,
        /// <summary>
        /// Same as 'To' but requires a distance.
        /// </summary>
        From,
        /// <summary>
        /// Same as 'To' but requires a direction and optional distance.
        /// </summary>
        Of,
        /// <summary>
        /// Same as 'To' but defaults to be somewhere close above the object.
        /// </summary>
        Above,
        /// <summary>
        /// Same as 'To' but defaults to be somewhere close below the object.
        /// </summary>
        Below,
        /// <summary>
        /// Go around an object so the current position would be out of sight, or if a direction is specified
        /// this acts like 'of'.
        /// </summary>
        Behind,
        /// <summary>
        /// Go from the current position some distance closer to the target. If unspecified, travels for 1 tick.
        /// </summary>
        Closer,
        /// <summary>
        /// Go from the current position some distance farther from the target. If unspecified, travels for 1 tick.
        /// </summary>
        Farther
    }

    /*
    public class MovementPreposition : EnumClass<MovementPreposition>
    {
        /// <summary>
        /// Go into an enterable thing.
        /// </summary>
        public static MovementPreposition In = new MovementPreposition(nameof(In), false, false, false, false);
        /// <summary>
        /// Stand anywhere on top of a thing.
        /// </summary>
        //allow with distance as a 'don't move off this' thing? probably not.
        public static MovementPreposition On = new MovementPreposition(nameof(On), false, false, false, false);
        /// <summary>
        /// Go to or near an object. Defaults to trying to be in arms reach.
        /// TODO: Defaults to 'In' for enterable things?
        /// </summary>
        public static MovementPreposition To = new MovementPreposition(nameof(To), false, false, false, false);
        /// <summary>
        /// Same as 'To' but requires a distance.
        /// </summary>
        public static MovementPreposition From = new MovementPreposition(nameof(From), false, false, true, true);
        /// <summary>
        /// Same as 'To' but requires a direction and optional distance.
        /// </summary>
        public static MovementPreposition Of = new MovementPreposition(nameof(Of), true, true, true, false);
        /// <summary>
        /// Same as 'To' but defaults to be somewhere close above the object.
        /// </summary>
        public static MovementPreposition Above = new MovementPreposition(nameof(Above), true, false);
        /// <summary>
        /// Same as 'To' but defaults to be somewhere close below the object.
        /// </summary>
        public static MovementPreposition Below = new MovementPreposition(nameof(Below), true, false);
        /// <summary>
        /// Go around an object so the current position would be out of sight, or if a direction is specified
        /// this acts like 'of'.
        /// </summary>
        public static MovementPreposition Behind = new MovementPreposition(nameof(Behind), true, false);
        /// <summary>
        /// Go from the current position some distance closer to the target. If unspecified, travels for 1 tick.
        /// </summary>
        public static MovementPreposition Closer = new MovementPreposition(nameof(Closer), true, false);
        /// <summary>
        /// Go from the current position some distance farther from the target. If unspecified, travels for 1 tick.
        /// </summary>
        public static MovementPreposition Farther = new MovementPreposition(nameof(Farther), true, false);

        public readonly bool AllowDirection;
        public readonly bool RequireDirection;


        public MovementPreposition(string propertyName, bool allowDirection, bool requireDirection, bool allowDistance, bool requireDistance) : base(propertyName)
        {
            AllowDirection = allowDirection;
            RequireDirection = requireDirection;
        }


    }
    */

    public class MovementDirection
    {
        public Directions direction;

        public int distanceCount;
        public MovementUnit distanceUnit;
    }
    public class PreciseMovementDirection
    {
        public double dx;
        public double dy;
        public double dz;
    }

    public enum MovementUnit
    {
        NoDistance, //There was no distance component.
        Unspecified, //Default (or user said 'rooms'). Room-relative unit (slightly MOB-relative), whatever the MOB would currently consider a room from their current location.
        Step, //Time-relative unit (mostly MOB-relative). One 'step' for the MOB. Something like 6 steps to a tick?
        Absolute, //Absolute unit, 1/10 of a millimeter. Smallest measurement in EspressoMUD.  Other absolute units will be converted to this.
    }
    /*
     * 
                case MovementPreposition.Above:
                case MovementPreposition.Behind:
                case MovementPreposition.Below:
                case MovementPreposition.Closer:
                case MovementPreposition.Farther:
                case MovementPreposition.From:
                case MovementPreposition.In:
                case MovementPreposition.Of:
                case MovementPreposition.On:
                case MovementPreposition.To:

     */

    public static partial class Extensions
    {
        public static bool AllowDirection(this MovementPreposition type)
        {
            switch(type)
            {
                case MovementPreposition.Above:
                case MovementPreposition.Below:
                case MovementPreposition.Closer:
                case MovementPreposition.Farther:
                case MovementPreposition.From:
                case MovementPreposition.In:
                case MovementPreposition.On:
                case MovementPreposition.Out:
                case MovementPreposition.To:
                    return false;
                case MovementPreposition.Behind:
                case MovementPreposition.Of:
                    return true;
            }
            throw new Exception("Unexpected type: " + type);
        }
        public static bool RequireDirection(this MovementPreposition type)
        {
            switch (type)
            {
                case MovementPreposition.Above:
                case MovementPreposition.Behind:
                case MovementPreposition.Below:
                case MovementPreposition.Closer:
                case MovementPreposition.Farther:
                case MovementPreposition.From:
                case MovementPreposition.In:
                case MovementPreposition.On:
                case MovementPreposition.Out:
                case MovementPreposition.To:
                    return false;
                case MovementPreposition.Of:
                    return true;
            }
            throw new Exception("Unexpected type: " + type);
        }
        public static bool AllowDistance(this MovementPreposition type)
        {
            switch (type)
            {
                case MovementPreposition.Behind:
                case MovementPreposition.In:
                case MovementPreposition.On:
                case MovementPreposition.Out:
                case MovementPreposition.To:
                    return false;
                case MovementPreposition.Above:
                case MovementPreposition.Below:
                case MovementPreposition.Closer:
                case MovementPreposition.Farther:
                case MovementPreposition.From:
                case MovementPreposition.Of:
                    return true;
            }
            throw new Exception("Unexpected type: " + type);
        }
        public static bool RequireDistance(this MovementPreposition type)
        {
            switch (type)
            {
                case MovementPreposition.Above:
                case MovementPreposition.Behind:
                case MovementPreposition.Below:
                case MovementPreposition.Closer:
                case MovementPreposition.Farther:
                case MovementPreposition.From:
                case MovementPreposition.In:
                case MovementPreposition.Of:
                case MovementPreposition.On:
                case MovementPreposition.Out:
                case MovementPreposition.To:
                    return false;
                    return true;
            }
            throw new Exception("Unexpected type: " + type);
        }
    }
}
