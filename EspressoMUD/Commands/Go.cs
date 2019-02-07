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
        public Go()
        {
            UniqueCommand = "go";
            AlternateCommands = new string[] { "move" };
        }
        public override void execute(IMOB mob, QueuedCommand command)
        {
            /*TODO: magic parsing to get a destination from the user's input
            Form is always (optional direction/distance) (optional preposition) (optional target)
                direction/distance can work alone (implies 'from here')
                if direction/distance is missing, if preposition is not specific, 'in reach' is implied
                preposition requires a target
                target can work alone (depends on target. If target defaults to enterable, implies 'in'. Else 'near')
            Search for prepositions, get a list of them.
                Validate prepositions. Need to have nothing or a valid direction/distance before preposition.
                How many can actually be possible? I think only zero or one preposition is really possible?
                TODO: Special user input to specify a preposition instead of parser guessing.
                    Some system similar to quotes probably. Quotes may be useful though, like book titles or something.
                    Should at least have a hook to use this right away, even if currently there's no implementation
            If no prepositions, attempt to parse full string as direction/distance. If successful, do that
            Search whole string for item/target matches and associated strings.
                In general, 'visible to MOB' items.
                Must match to end of string?
            Find associated strings that line up with a preposition
            Instead of above two major points:
                Identify all valid strings (start after preposition, go to end of string)
                    TODO: Special user input to specify a string instead of parser guessing?
                        This is basically implied by a preposition. However it maybe makes more sense to specify the target instead of the preposition.
                Search all 'visible to MOB' items that exactly match any of the valid strings.
            How to pick best match?
                Maybe just not go if more than one match found, request more info. prompt?
            */
            StringWords goToInput = command.parsedCommand;

            //See if there's a preposition
            int start = 1; //Skip command ("go")
            int prepValue;
            MovementPreposition prepEnum;
            MovementDirection direction = null;
            int index = TextParsing.FindAPreposition(goToInput, typeof(MovementPreposition), out prepValue, start);
            if (index != -1)
            {
                //Is this a valid preposition? Check if it starts correctly.
                prepEnum = EnumClass.GetValues(typeof(MovementPreposition))[prepValue] as MovementPreposition;
                if (!prepEnum.AllowDirection)
                {
                    if (index != start + 1)
                    {
                        //Invalid preposition for this location, needs to be at start of command.
                        goto noPrepositionFound;
                    }
                }
                else if (prepEnum.RequireDirection && index == start + 1)
                {
                    //Invalid preposition for this location, needs to come after a direction/distance.
                    goto noPrepositionFound;
                }
                else
                {
                    direction = TextParsing.ParseAsDirection(goToInput, mob, ref start, ref index, true);
                    if (direction == null) //Not a valid preposition. Ignore it.
                    {
                        goto noPrepositionFound;
                    }
                    else //Maybe valid preposition. 
                    {
                        //TODO: Check if preposition matches direction more closely?

                        //Valid preposition. Parse the REST of the sentence as an object.
                        start = index + 1;
                        goto foundPreposition;
                    }
                }
            }
            else
            {
                //Is the whole thing a direction?
                direction = TextParsing.ParseAsDirection(goToInput, mob, ref start, ref index, true);
                if (direction != null)
                {
                    //TODO: Handle going to a direction instead of to a destination.
                }
            }

        noPrepositionFound:
            start = 1;
            prepEnum = null;

        foundPreposition:
            index = -1;
            Item target = TextParsing.FindKnownItem(goToInput, mob, ref start, ref index); //TODO: Implement this to find visible items for a player. This should maybe also allow a prompt?
            //TODO: Handle going to the target found / if a target is found.


        }
    }

    public class MovementPreposition : EnumClass<MovementPreposition>
    {
        /// <summary>
        /// Go into an enterable thing.
        /// </summary>
        public static MovementPreposition In = new MovementPreposition(nameof(In), false, false);
        /// <summary>
        /// Stand anywhere on top of a thing.
        /// </summary>
        //allow with distance as a 'don't move off this' thing? probably not.
        public static MovementPreposition On = new MovementPreposition(nameof(On), false, false);
        /// <summary>
        /// Go to or near an object. Defaults to trying to be in arms reach. Can specify how far, or even in what direction,
        /// to be from the object.
        /// </summary>
        public static MovementPreposition To = new MovementPreposition(nameof(To), true, false);
        /// <summary>
        /// Same as 'To' but requires distance.
        /// </summary>
        public static MovementPreposition Of = new MovementPreposition(nameof(Of), true, true);
        /// <summary>
        /// Same as 'To' but defaults to be somewhere close above the object.
        /// </summary>
        public static MovementPreposition Above = new MovementPreposition(nameof(Above), true, false);
        /// <summary>
        /// Same as 'To' but defaults to be somewhere close below the object.
        /// </summary>
        public static MovementPreposition Below = new MovementPreposition(nameof(Below), true, false);

        public readonly bool AllowDirection;
        public readonly bool RequireDirection;


        public MovementPreposition(string propertyName, bool allowDirection, bool requireDirection) : base(propertyName)
        {
            AllowDirection = allowDirection;
            RequireDirection = requireDirection;
        }


    }

    public class MovementDirection
    {
        public Directions direction;

        public int distance; //-1 = distance not given, only a direction. Else distance in millimeters.
        //TODO: Add a scaling type?
    }

    public class MovementDestination
    {
        /// <summary>
        /// Physical object that is the destination of this movement action. If null, character is just moving
        /// in a direction without a specific destination.
        /// </summary>
        public Item item;

        /// <summary>
        /// If no item, these tell how far the character is trying to move.
        /// </summary>
        public int deltaX;
        public int deltaY;
        public int deltaZ;

        /// <summary>
        /// What the action is trying to do; move 'onto' or 'into' or just 'near' the target.
        /// </summary>
        public MovementPreposition relation;

        /// <summary>
        /// How close to the destination is acceptable. Only used with 'near'?
        /// </summary>
        public int howClose;


    }
}
