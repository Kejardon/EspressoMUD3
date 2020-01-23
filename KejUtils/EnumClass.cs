using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace KejUtils
{
    /// <summary>
    /// Base class of the generic EnumClass<>. Provides static functions for working with EnumClass types.
    /// </summary>
    public abstract class EnumClass
    {
        /// <summary>
        /// Automatically convert an EnumClass enum to an integer.
        /// </summary>
        /// <param name="en"></param>
        public static implicit operator int(EnumClass en)
        {
            return en.Value;
        }


        internal static Dictionary<Type, EnumMetaData> dataForTypes = new Dictionary<Type, EnumMetaData>();
        protected internal class EnumMetaData
        {
            public Dictionary<string, EnumClass> EnumNames = new Dictionary<string, EnumClass>();
            public Dictionary<int, EnumClass> EnumOptions = new Dictionary<int, EnumClass>();
            public int NextValue;
            private List<EnumClass> sortedValues;
            public List<EnumClass> SortedValues
            {
                get
                {
                    if (sortedValues == null)
                    {
                        List<EnumClass> newList = new List<EnumClass>();
                        SortedList<int, EnumClass> values = new SortedList<int, EnumClass>();
                        foreach (KeyValuePair<int, EnumClass> pair in EnumOptions)
                        {
                            values.Add(pair.Key, pair.Value);
                        }

                        foreach (KeyValuePair<int, EnumClass> pair in values)
                        {
                            sortedValues.Add(pair.Value);
                        }

                        sortedValues = newList;
                    }
                    return sortedValues;
                }
            }
        }

        private static void validateType(Type type)
        {
            if (!typeof(EnumClass<>).IsAssignableFrom(type))
                throw new ArgumentException("Not a valid Type for an enum set. It must extend EnumClass.", "type");
        }

        private EnumMetaData getMetaData()
        {
            EnumMetaData data = metaData;
            if (data != null) return data;
            lock (dataForTypes)
            {
                if (!dataForTypes.TryGetValue(this.GetType(), out data))
                {
                    data = new EnumMetaData();
                    dataForTypes[this.GetType()] = data;
                }
            }
            return data;
        }
        private int AddEnum(int number, EnumMetaData meta)
        {
            dataForTypes[GetType()] = meta;

            while (meta.EnumOptions.ContainsKey(number)) number++;
            meta.EnumOptions.Add(number, this);
            meta.NextValue = number + 1;

            meta.EnumNames.Add(Name, this);
            return number;
        }
        
        /// <summary>
        /// Get a specific value from the enum set specified.
        /// </summary>
        /// <param name="name">Value to find.</param>
        /// <param name="type">Enum set to search from.</param>
        /// <returns></returns>
        public static T Parse<T>(string name, Type type) where T : EnumClass
        {
            validateType(type);
            return dataForTypes[type].EnumNames[name] as T;
        }
        /// <summary>
        /// Get an specific value from the enum set specified.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value">Value to find.</param>
        /// <param name="enumType">Enum set to search from.</param>
        /// <returns></returns>
        public static T GetEnum<T>(int value, Type enumType) where T : EnumClass
        {
            return dataForTypes[enumType].EnumOptions[value] as T;
        }
        /// <summary>
        /// Get a specific value from the enum set specified.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public T GetEnum<T>(int value) where T : EnumClass
        {
            return dataForTypes[this.GetType()].EnumOptions[value] as T;
        }
        /// <summary>
        /// Get all the names for the enum set specified.
        /// </summary>
        /// <param name="type">Enum set to search from.</param>
        /// <returns></returns>
        public static IEnumerable<string> GetNames(Type type)
        {
            validateType(type);
            return dataForTypes[type].SortedValues.Select(x => x.Name);
        }
        /// <summary>
        /// Get all the values for the enum set specified.
        /// </summary>
        /// <param name="type">Enum set to search from.</param>
        /// <returns></returns>
        public static EnumClass[] GetValues(Type type)
        {
            validateType(type);
            return dataForTypes[type].SortedValues.ToArray();
        }
        /// <summary>
        /// Try to get a specific value from the enum set specified.
        /// </summary>
        /// <param name="name">Value to find.</param>
        /// <param name="value">Value if it is found, else null.</param>
        /// <returns>True if the value exists.</returns>
        public static bool TryParse<T>(string name, out T value, Type type) where T : EnumClass
        {
            EnumClass intermediate;
            validateType(type);
            bool success = dataForTypes[type].EnumNames.TryGetValue(name, out intermediate);
            value = intermediate as T;
            return success;
        }

        /// <summary>
        /// Create a new enum choice. It will automatically be assigned a value, starting at 0 or the next
        /// available value above the previous enum's choice.
        /// </summary>
        /// <param name="propertyName">The name of the enum choice.</param>
        internal EnumClass(string propertyName)
        {
            Name = propertyName;
            EnumMetaData meta = getMetaData();
            Value = this.AddEnum(meta.NextValue, meta);
        }
        /// <summary>
        /// Create a new enum choice with a specified value.
        /// </summary>
        /// <param name="i">The specified value for this enum choice.</param>
        /// <param name="propertyName">The name of the enum choice.</param>
        internal EnumClass(int i, string propertyName)
        {
            Name = propertyName;
            EnumMetaData meta = getMetaData();
            Value = this.AddEnum(i, meta);
        }
        
        /// <summary>
        /// metaData doesn't need to be overwritten, but if you want custom metadata, it can generally be implemented as 
        /// private static EnumMetaDataSubclass backingMetaData = new EnumMetaDataSubclass();
        /// protected override EnumMetaDataSubclass metaData { get { return backingMetaData; } }
        /// </summary>
        protected virtual EnumMetaData metaData
        {
            get
            {
                EnumMetaData data;
                EnumClass.dataForTypes.TryGetValue(this.GetType(), out data);
                return data;
            }
        }
        
        /// <summary>
        /// The string that represents this enum choice. Unique for a given enum set.
        /// </summary>
        public readonly string Name;
        /// <summary>
        /// The integer value that represents this enum choice. Unique for a given enum set.
        /// </summary>
        public readonly int Value;

        /// <summary>
        /// The string that represents this enum choice. Unique for a given enum set.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Name;
        }
    }
    /// <summary>
    /// Alternative class for 'enum' when the enums need extra information.
    /// No thread safety is provided by this class; EnumClasses should not be created concurrently, and subclasses
    /// will need to provide thread safety wherever they require it.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class EnumClass<T> : EnumClass where T : EnumClass<T>
    {
        /// <summary>
        /// Create a new enum choice. It will automatically be assigned a value, starting at 0 or the next
        /// available value above the previous enum's choice.
        /// </summary>
        /// <param name="propertyName">The name of the enum choice.</param>
        protected EnumClass(string propertyName) : base(propertyName) { }
        /// <summary>
        /// Create a new enum choice with a specified value.
        /// </summary>
        /// <param name="i">The specified value for this enum choice.</param>
        /// <param name="propertyName">The name of the enum choice.</param>
        protected EnumClass(int i, string propertyName) : base(i, propertyName) { }
        
        /// <summary>
        /// Get a specific value from the enum set this enum belongs to.
        /// </summary>
        /// <param name="name">Value to find.</param>
        /// <returns></returns>
        public T Parse(string name)
        {
            return metaData.EnumNames[name] as T;
        }

        /// <summary>
        /// Get all the names for the enum set this enum belongs to.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetNames()
        {
            return metaData.SortedValues.Select(x => x.Name);
        }
        /// <summary>
        /// Get all the values for the enum set this enum belongs to.
        /// </summary>
        /// <returns></returns>
        public T[] GetValues() //TODO: This needs to be tested. I don't think T[] will work here, this might need to be EnumClass[]
        {
            return metaData.SortedValues.ToArray() as T[];
        }
        /// <summary>
        /// Try to get a specific value from the enum set this enum belongs to.
        /// </summary>
        /// <param name="name">Value to find.</param>
        /// <param name="value">Value if it is found, else null.</param>
        /// <returns>True if the value exists.</returns>
        public bool TryParse(string name, out T value)
        {
            EnumClass midValue;
            bool result = metaData.EnumNames.TryGetValue(name, out midValue);
            value = midValue as T;
            return result;
        }

    }

    /// <summary>
    /// Example of how to implement EnumClass<>.
    /// </summary>
    /*
    public class TestEnumClass : EnumClass<TestEnumClass>
    {
        public static TestEnumClass EnumOptionA = new TestEnumClass(nameof(EnumOptionA));
        public static TestEnumClass EnumOptionB = new TestEnumClass(nameof(EnumOptionB));
        public static TestEnumClass EnumOptionC = new TestEnumClass(nameof(EnumOptionC));

        public TestEnumClass(string propertyName) : base(propertyName)  { }

        //Above is the base all EnumClass subclasses typically always need and some example enum values.
        //Below is examples of how you might add onto it.

        /// <summary>
        /// Custom MetaData class for this type. Requires overriding metaData also.
        /// Not sure why this would be useful but there are maybe usecases.
        /// </summary>
        private class ExtraData : EnumMetaData
        {
            /// <summary>
            /// Some extra thing for this class
            /// </summary>
            public void ExtraMethod()
            {
                // ...
            }
        }
        private static ExtraData backingMetaData = new ExtraData();
        protected override ExtraData metaData { get { return backingMetaData; } }

        /// <summary>
        /// A publicly accesible property added to this particular group of enums (TestEnumClass).
        /// Things should generally be immutable/readonly to be consistent as an enum.
        /// </summary>
        public readonly int MyVar;
        public TestEnumClass(string propertyName, int extraVariable) : base(propertyName) { this.MyVar = extraVariable; }

        public static TestEnumClass EnumOptionD = new TestEnumClass(nameof(EnumOptionD), 123);
    }
    */
}
