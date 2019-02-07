using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static EspressoMUD.TextParsing;

namespace EspressoMUD
{
    public abstract class DirectionCommand : Command
    {
        private static List<KeyValuePair<string[], Directions>> baseDirections = new List<KeyValuePair<string[], Directions>>();
        private static KeyValuePair<string[][], Directions[]> cachedDirections = default(KeyValuePair<string[][], Directions[]>);
        private static ReaderWriterLockSlim directionsLock = new ReaderWriterLockSlim();
        public static KeyValuePair<string[][], Directions[]> GetDirectionOptions()
        {
            KeyValuePair<string[][], Directions[]> value;
            directionsLock.EnterReadLock();
            try
            {
                value = cachedDirections;
            }
            finally { directionsLock.ExitReadLock(); }

            if (value.Equals(default(KeyValuePair<string[][], Directions[]>)))
            {
                directionsLock.EnterWriteLock();
                try
                {
                    value = cachedDirections;
                    if (value.Equals(default(KeyValuePair<string[][], Directions[]>)))
                    {

                        string[][] directionStrings = new string[baseDirections.Count][];
                        Directions[] directionValues = new Directions[baseDirections.Count];
                        for (int i = 0; i < baseDirections.Count; i++)
                        {
                            KeyValuePair<string[], Directions> pair = baseDirections[i];
                            directionStrings[i] = pair.Key;
                            directionValues[i] = pair.Value;
                        }
                        value = cachedDirections = new KeyValuePair<string[][], Directions[]>(directionStrings, directionValues);
                    }
                }
                finally { directionsLock.ExitWriteLock(); }
            }
            return value;
        }

        //TODO: Not sure on scope of this, I feel like other things will need it.
        private Directions UnitDirection { get; set; }
        protected DirectionCommand(string[] triggerInputs, Directions unitDirection)
        {
            directionsLock.EnterWriteLock();
            try
            {
                cachedDirections = default(KeyValuePair<string[][], Directions[]>);
                baseDirections.Add(new KeyValuePair<string[], Directions>(triggerInputs, unitDirection));
            }
            finally
            {
                directionsLock.ExitWriteLock();
            }
        }


        public override void execute(IMOB mob, QueuedCommand command)
        {
            StringWords input = command.parsedCommand;
            int distance = -1;
            if (input.Segments.Length > 1)
            {
                TextParsing.ParseAsDistance(input, mob, out distance, 1);
            }
            //TODO: finish this

        }



    }
}
