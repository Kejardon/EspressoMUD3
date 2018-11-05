using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD
{
    public class Metadata
    {
        public static Dictionary<Type, Metadata> LoadedClasses = new Dictionary<Type, Metadata>();
        public static Metadata[] ByClassID;
        /// <summary>
        /// Marker used for NextToSave, to indicate the end of a list. Different than null because objects interpret null as
        /// 'not being saved yet' and any value as 'in the list of things to save'.
        /// </summary>
        public static ISaveable EndOfList = new DummySaveable();



        public Metadata()
        {
            NextToSave = EndOfList;
        }

        //public Type Type;
        /// <summary>
        /// List of SaveIDAttributes for this object. Only for objects that belong to multiple ObjectTypes.
        /// </summary>
        public SaveIDParser[] SaveIDParsers;
        /// <summary>
        /// List of parsers to use when saving this object. Iterate over entire list to generate data.
        /// </summary>
        //public SaveableParser[] Parsers;
        /// <summary>
        /// List of parsers to use when loading this object. Convert from Parser ID to Parser
        /// </summary>
        public SaveableParser[] ParserByID;
        /// <summary>
        /// Total number of parsers made for this class; Deleted parsers will still count for this.
        /// </summary>
        public ushort NumberParsers;
        /// <summary>
        /// Unique ID of this class.
        /// </summary>
        public int ClassID;
        /// <summary>
        /// First object of a pseudo-list of objects to save.
        /// </summary>
        private ISaveable NextToSave;
        /// <summary>
        /// List of ObjectType interfaces that are implemented by this class.
        /// </summary>
        public ObjectType[] ImplementedTypes;
        /// <summary>
        /// Listed as public, but only intended to be used by the database. FileStream associated with the file for this 
        /// </summary>
        public FileStream DataFile;
        /// <summary>
        /// Type object for the class that this Metadata represents.
        /// </summary>
        public Type ClassType;
        //TODO: Maybe 5x faster to create from a compiled expression instead of Activator, compiled expression would go here. Probably not necessary though, that's miniscule compared to file loading.

        public static void Initialize()
        {
            Type[] classes = Assembly.GetExecutingAssembly().GetTypes();
            foreach (Type type in classes) if (!type.IsAbstract)
            {
                //Right now, we only need Metadata for saveable objects. If that changes this check can be updated.
                if (typeof(ISaveable).IsAssignableFrom(type))
                {
                        LoadedClasses[type] = new Metadata();
                        LoadedClasses[type].ClassType = type;
                }
            }
        }

        /// <summary>
        /// Used by the database to start saving all objects. Returns the start of the list of objects to save, and resets to an empty list.
        /// </summary>
        /// <returns></returns>
        public ISaveable ResetNextToSave()
        {
            lock (this)
            {
                ISaveable nextObject = NextToSave;
                NextToSave = Metadata.EndOfList;
                return nextObject;
            }
        }

        /// <summary>
        /// Adds a new object to be saved to the database. next.SaveValues.NextObjectToSave must be null, i.e. it must not already be in the list of objects to save.
        /// </summary>
        /// <param name="next"></param>
        public void AddNextToSave(ISaveable next)
        {
            lock (this) //TODO: Consider a better object for locking if anything else might want to lock Metadata objects
            {
                { //Also note that next is not locked. It is assumed that multiple threads will not attempt to write to next at a time.
                    next.SaveValues.NextObjectToSave = NextToSave;
                    NextToSave = next;
                }
            }
        }

    }

    public static partial class Extensions
    {
        public static Metadata GetMetadata(this ISaveable saveable)
        {
            return Metadata.LoadedClasses[saveable.GetType()];
        }
    }
}
