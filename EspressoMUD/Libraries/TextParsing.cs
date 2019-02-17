using EspressoMUD.Prompts;
using EspressoMUD.Properties;
using KejUtils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD
{
    public static class TextParsing
    {
        private static Dictionary<Type, string[][]> prepositionOptions = new Dictionary<Type, string[][]>();

        /// <summary>
        /// Set the language used for the MUD. Affects text parsing and some output, but doesn't otherwise translate
        /// or affect any user text.
        /// </summary>
        /// <param name="str"></param>
        public static void SetCulture(string str)
        {
            CultureInfo newOption = new CultureInfo(str); //TODO: Error handling for invalid str
            lock (typeof(TextParsing))
            {
                prepositionOptions.Clear();
                CultureInfo.DefaultThreadCurrentUICulture = newOption;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <param name="mob"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="onPrompt"></param>
        /// <param name="addToPrompt"></param>
        /// <returns></returns>
        public static Item FindKnownItem(StringWords input, MOB mob, int start, int end, SelectAction onPrompt = null, StandardHeldPrompt addToPrompt = null)
        {
            List<Item> options = FindKnownItems(input, mob, start, end);
            if (options.Count == 1)
                return options[0];

            Client client = mob.Client;
            if (client == null)
            {
                //TODO: Have an NPC say they don't know what item they were told to find?
                return null;
            }

            if (options.Count == 0)
            {
                client.sendMessage("You don't see a '" + input.StringRange(start, end) + "'");
                return null;
            }

            if (onPrompt == null)
            {
                client.sendMessage("There are too many things that fit that description.");
                return null;
            }

            SelectFromList<Item> itemSelectPrompt = new SelectFromList<Item>(onPrompt.Invoke);
            if (addToPrompt == null || !addToPrompt.TransitionTo(itemSelectPrompt, true))
            {
                client.Prompt(itemSelectPrompt);
            }
            return null;
        }
        public static List<Item> FindKnownItems(StringWords input, MOB mob, int start, int end)
        {
            input.ValidateEndIndex(ref end);
            Item[] items = GeneralUtilities.VisibleItems(mob, mob.Body.Position);
            List<Item> matchingItems = new List<Item>();
            foreach (Item item in items)
            {
                if (mob.CanRecognize(item, input, start, end))
                    matchingItems.Add(item);
            }
            return matchingItems;
        }

        /// <summary>
        /// Check if 'input' (or a specific part of it) can be found inside of 'text'.
        /// </summary>
        /// <param name="input">Text to search for</param>
        /// <param name="text">Text to look inside for matches</param>
        /// <param name="startIndex">First word of 'input' to start searching for</param>
        /// <param name="endIndex">After last word of 'input' to search for (exclusive end)</param>
        /// <returns>True if text contains input in the same order.</returns>
        public static bool CheckAutoCompleteText(StringWords input, string text, int startIndex = 0, int endIndex = -1)
        {
            input.ValidateEndIndex(ref endIndex);
            if (startIndex >= endIndex) throw new ArgumentException("startIndex must be before endIndex");

            int textIndex = 0;
            for (int inputIndex = 0; textIndex < text.Length; textIndex++)
            {
                string wordToFind = input.Segments[inputIndex];

                textIndex = text.IndexOf(wordToFind, textIndex);
                if (textIndex == -1) return false; //Input word was not found.

                if (textIndex != 0)
                { //Check to make sure this could reasonably be the start of a word. If not, continue at the next character.
                    char prevChar = text[textIndex - 1];
                    //TODO: Finetune below checks.
                    //if (!Char.IsWhiteSpace(prevChar)) continue;
                    if (Char.IsLetterOrDigit(prevChar)) continue;
                }

                //Found a match. Continue with the next word.
                inputIndex++;
                if (inputIndex == endIndex) return true; //No next word, found all the words.
                textIndex += wordToFind.Length; //Skip the matched text (plus 1 character from loop counter)
            }
            //Ran out of letters before matching all the words.
            return false;
        }

        public static MovementDirection ParseAsDirection(StringWords text, MOB mob, ref int startIndex, ref int endIndex, bool requireAll = false, bool getDistance = true)
        {
            text.ValidateEndIndex(ref endIndex);

            Directions foundDirection = Directions.NoDirection;
            //List<Directions> directions = new List<Directions>();
            KeyValuePair<string[][], Directions[]> directionOptions = DirectionCommand.GetDirectionOptions();
            string[][] directionStrings = directionOptions.Key;
            Directions[] directionValues = directionOptions.Value;
            int foundDistance = -1;
            for (int i = startIndex; i < endIndex; i++)
            {
                string word = text.Segments[i];
                if (foundDirection == Directions.NoDirection)
                {
                    int found = MatchString(directionStrings, word);
                    if (found != -1)
                    {
                        foundDirection = directionValues[found];
                        continue;
                    }
                }
                if (getDistance && foundDistance == -1)
                {
                    int distanceWords = ParseAsDistance(text, mob, out foundDistance, i, Math.Min(i + 1, endIndex - 1));
                    if (distanceWords != 0)
                    {
                        if (distanceWords == 2) i++;
                        continue;
                    }
                }

                if (requireAll)
                {
                    return null;
                }
                endIndex = i;
                break;
            }
            if (foundDirection == Directions.NoDirection)
            {
                return null;
            }

            return new MovementDirection()
            {
                direction = foundDirection,
                distance = foundDistance
            };

        }
        /// <summary>
        /// Attempt to parse input as a distance. Text inputs must be a number, followed by an optional unit, to be parsed as
        /// a distance.
        /// </summary>
        /// <param name="text">Input to parse</param>
        /// <param name="mob"></param>
        /// <param name="distance">Distance to move, in millimeters. If -1, then parsing failed.</param>
        /// <param name="startIndex">Where the distance is expected to be.</param>
        /// <returns>Number of words successfully parsed. 0 if failed, up to 2 if succeeded. </returns>
        public static int ParseAsDistance(StringWords text, MOB mob, out int distance, int startIndex = 0, int endIndex = 0)
        {

            //TODO: Scaling to mob somehow? Need to consider design. Probably won't happen here, but passing mob just so it's futureproofed.
            //TODO: Alternative scalings. x paces, x rooms, etc.
            distance = -1;
            if (startIndex >= text.Segments.Length) return 0;

            string firstWord, secondWord = null;
            firstWord = text.Segments[startIndex];
            if (startIndex < text.Segments.Length - 1)
            {
                secondWord = text.Segments[startIndex + 1];
            }
            int magnitude, distanceType;
            if (!int.TryParse(firstWord, out magnitude))
            {
                if (secondWord == null || !int.TryParse(secondWord, out magnitude))
                {
                    return 0;
                }
                //firstWord MUST be a valid distance enum.
                if (FindAPreposition(text, typeof(Distances), out distanceType, startIndex, startIndex + 1) == -1)
                {
                    return 0;
                }
            }
            else
            {
                if (secondWord == null || FindAPreposition(text, typeof(Distances), out distanceType, startIndex + 1, startIndex + 2) == -1)
                {
                    //Defaulting to yards. TODO: Scale to the mob? Probably more likely allow unitless and have something else figure it out.
                    distanceType = (int)Distances.Yard;
                    secondWord = null;
                }
            }
            distance = (int)((magnitude * (long)distanceConversions[distanceType]) / 10);
            return secondWord == null ? 2 : 1;
        }


        public enum Directions
        {
            NoDirection = -1,
            North,
            Northeast,
            East,
            Southeast,
            South,
            Southwest,
            West,
            Northwest,
            Up,
            Down
        }

        public enum Distances //This probably needs some work.
        {
            Millimeter, //1
            Centimeter, //10
            Inch, //25.4
            Foot, //304.8
            Yard, //914.4
            Meter, //1000
            Kilometer, //1000000
            Mile, //1609344
        }
        private static int[] distanceConversions = new int[]
        {
            10,
            100,
            254,
            3048,
            9144,
            10000,
            10000000,
            16093440
        };

        /// <summary>
        /// Find the first word in an input that matches a preposition from a list of prepositions
        /// </summary>
        /// <param name="text">Input to search</param>
        /// <param name="typeOfPreposition">Type/List of prepositions</param>
        /// <param name="prepositionValue">Which preposition was found (can be cast to that preposition). -1 if no preposition found.</param>
        /// <param name="startIndex">Which word in input to start searching on (inclusive). Defaults to 0 (start of input)</param>
        /// <param name="endIndex">Which word in input to stop searching on (exclusive). Defaults to -1 (end of input)</param>
        /// <returns></returns>
        public static int FindAPreposition(StringWords text, Type typeOfPreposition, out int prepositionValue, int startIndex = 0, int endIndex = -1) //TODO: Replace typeOfPreposition for an easier-to-use type of variable. An enum of enums?
        {
            text.ValidateEndIndex(ref endIndex);

            //Array values = Enum.GetValues(typeOfPreposition);
            string[][] options = OptionsForPreposition(typeOfPreposition);
            for (int i = startIndex; i < endIndex; i++)
            {
                prepositionValue = MatchString(options, text.Segments[i]); //NOTE: This assumes enums always have default-assigned values (0, 1, 2, etc.)
                if (prepositionValue != -1)
                {
                    return i;
                }
            }
            prepositionValue = -1;
            return -1;
        }
        public static int FindMatchingWord(StringWords text, string[] options)
        {
            foreach (string option in options)
            {
                for (int i = 0; i < text.Segments.Length; i++)
                {
                    if (text.Segments[i].Equals(option, StringComparison.CurrentCultureIgnoreCase))
                    {
                        return i;
                    }
                }
            }
            return -1;
        }
        public static string[][] OptionsForPreposition(Type typeOfPreposition)
        {
            string[][] result;
            if (prepositionOptions.TryGetValue(typeOfPreposition, out result))
                return result;

            string[][] newEntry;
            lock (typeof(TextParsing)) //Make sure the ResourceCollection is up to date by the time we set prepositionOptions.
            {
                ResourceCollection collection = GetCollection(typeOfPreposition);
                if (typeof(EnumClass<>).IsAssignableFrom(typeOfPreposition))
                {
                    //FieldInfo field = typeOfPreposition.GetMethod("backingMetaData", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                    //EnumMetaData field.GetValue(null);
                    EnumClass[] objects = EnumClass.GetValues(typeOfPreposition);

                    newEntry = new string[objects.Length][];
                    for (int i = 0; i < objects.Length; i++)
                    {
                        string name = objects[i].Name;
                        string list = collection.GetValue(name);
                        string[] options = list.Split(',');
                        newEntry[i] = options;
                    }
                }
                else
                {
                    Array values = Enum.GetValues(typeOfPreposition);
                    newEntry = new string[values.Length][];
                    for (int i = 0; i < values.Length; i++)
                    {
                        string name = Enum.GetName(typeOfPreposition, values.GetValue(i));
                        string list = collection.GetValue(name);
                        string[] options = list.Split(',');
                        newEntry[i] = options;
                    }
                }
                prepositionOptions[typeOfPreposition] = newEntry;
            }

            return newEntry;
        }

        private static int MatchString(string[][] options, string word)
        {
            for (int j = 0; j < options.Length; j++)
            {
                string[] prepOptions = options[j];
                foreach (string prepOption in prepOptions)
                {
                    if (word.Equals(prepOption, StringComparison.CurrentCultureIgnoreCase))
                    {
                        return j;
                    }
                }
            }
            return -1;
        }

        private static ResourceCollection GetCollection(Type typeOfCollection)
        {
            if (typeOfCollection == typeof(MovementPreposition))
            {
                return new ResourceCollection(ResourcesMovePrepositions.ResourceManager, CultureInfo.DefaultThreadCurrentUICulture);
            }
            if (typeOfCollection == typeof(Distances))
            {
                return new ResourceCollection(ResourcesMoveDistances.ResourceManager, CultureInfo.DefaultThreadCurrentUICulture);

            }

            throw new ArgumentException();
        }


        struct ResourceCollection
        {
            public ResourceCollection(ResourceManager manager, CultureInfo culture)
            {
                this.manager = manager;
                this.culture = culture;
            }
            ResourceManager manager;
            CultureInfo culture;

            public string GetValue(string str)
            {
                return manager.GetString(str, culture);
            }
        }


        public class SelectFromList<T> : MenuPrompt<T>
        {
            //public SelectFromList(HeldPrompt calledBy = null) : base(calledBy) { }
            protected Action<T> actionToDo;

            public SelectFromList(Action<T> doWithThing, StandardHeldPrompt calledBy = null) : base(calledBy)
            {
                actionToDo = doWithThing;
            }

            private void UseSelectedOption()
            {
                actionToDo(SelectedValue);
            }

            public void AddOption(T choice, string description, string nameOfChoice = null)
            {
                this.AddOption(description, UseSelectedOption, choice, nameOfChoice);
            }

        }
    }

    public delegate void SelectAction(Item item);

    /// <summary>
    /// Class to aid with parsing user input.
    /// </summary>
    public class StringWords
    {
        public string WholeString { get; private set; }
        public string[] Segments { get; private set; }
        public StringWords(string str)
        {
            WholeString = str;
            Segments = str.Split(' ');
        }

        public ArraySegment<string> ArrayRange(int start, int end = -1)
        {
            if (end == -1) end = Segments.Length;
            return new ArraySegment<string>(Segments, start, end - start);
        }
        public string StringRange(int start, int end = -1)
        {
            if (end == -1) end = Segments.Length;
            return string.Join(" ", Segments, start, end - start);
        }

        public void ValidateEndIndex(ref int endIndex)
        {
            if (endIndex < 0 || endIndex > Segments.Length)
            {
                endIndex = Segments.Length;
            }
        }
    }
}
