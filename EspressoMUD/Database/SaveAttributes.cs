using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;

namespace EspressoMUD
{
    /// <summary>
    /// Base class for attribute to mark fields as saved to the database.
    /// </summary>
    public abstract class SaveableFieldAttribute : Attribute
    {
        protected SaveableFieldAttribute(string Key)
        {
            this.Key = Key;
        }

        public abstract SaveableParser Parser(FieldInfo field);
        public string Key;
    }

    /// <summary>
    /// Base class for parsers that convert between binary data and object memory.
    /// </summary>
    public abstract class SaveableParser
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="source">Object to save data from</param>
        /// <param name="writer">BinaryWriter backed by an expandable MemoryStream. Calling function can use the memory stream directly if it likes, but the position must be correct when done.</param>
        public abstract void Get(object source, BinaryWriter writer);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="target">Object to load data to</param>
        /// <param name="data">BinaryReader backed by an ArraySegmentStream. Calling function can use the ArraySegmentStream directly but should stay within bounds.</param>
        public abstract void Set(object target, BinaryReader data);
        public string Key; //Maybe not needed? Key converts to ParserID depending on ClassID.
        public Type owningType; //The type that declares this parser. Not necessarily the type that is saving/loading data, may be an ancestor.
        public virtual string OwningTypeName
        {
            get { return owningType.Name; }
        }
        //public int ClassID; //This currently isn't used, but is easy to add if wanted by uncommented code in DatabaseManager.css
        public ushort ParserID;
        //public T Default; //Strongly recommended to implement, but not necessary.

        protected static Action<object, T> GenerateSetter<T>(FieldInfo field)
        {
            if (typeof(T) != field.FieldType) return null;
            Type baseType = field.DeclaringType;

            ParameterExpression paramOne = Expression.Parameter(typeof(object), "argOne");
            ParameterExpression paramTwo = Expression.Parameter(typeof(T), "argTwo");
            Action<object, T> del = Expression.Lambda<Action<object, T>>(
                Expression.Assign(Expression.MakeMemberAccess(Expression.Convert(paramOne, baseType), field), paramTwo),
                new ParameterExpression[] { paramOne, paramTwo }).Compile();
            return del;
        }
        protected static Func<object, T> GenerateGetter<T>(FieldInfo field)
        {
            if (typeof(T) != field.FieldType) return null;
            Type baseType = field.DeclaringType;

            ParameterExpression paramOne = Expression.Parameter(typeof(object), "argOne");
            ParameterExpression paramTwo = Expression.Parameter(typeof(T), "result");
            Func<object, T> del = Expression.Lambda<Func<object, T>>(
                Expression.MakeMemberAccess(Expression.Convert(paramOne, baseType), field),
                //Expression.Assign(Expression.MakeMemberAccess(paramOne, field), paramTwo),
                new ParameterExpression[] { paramOne }).Compile();
            return del;
        }
    }

    /// <summary>
    /// Dummy class for database data that might not have an associated field.
    /// </summary>
    public class EmptySaveableParser : SaveableParser
    {
        public string owningTypeName;
        public override string OwningTypeName
        {
            get
            {
                return owningTypeName;
            }
        }

        public override void Get(object source, BinaryWriter writer)
        {
        }
        public override void Set(object target, BinaryReader data)
        {
        }
    }

    /// <summary>
    /// Generic attribute that attempts to save any normal field. Supports Default for some types.
    /// Most of the time, this specifically should be used to mark fields as saved.
    /// </summary>
    public class SaveFieldAttribute : SaveableFieldAttribute
    {
        public object Default;
        public SaveFieldAttribute(string Key) : base(Key)
        {
        }

        public override SaveableParser Parser(FieldInfo field)
        {
            Type subType = field.FieldType;
            if (subType == typeof(int))
            {
                if (Default != null)
                    return new SaveIntParser(field, (int)Default);
                return new SaveIntParser(field);
            }
            if (subType.IsEnum)
            {
                Type parserType = typeof(SaveEnumParser<>).MakeGenericType(subType);
                if (Default != null)
                    return (SaveableParser)Activator.CreateInstance(parserType, field, Default);
                return (SaveableParser)Activator.CreateInstance(parserType, field);
            }
            if (subType == typeof(bool))
            {
                if (Default != null)
                    return new SaveBoolDefaultParser(field, (bool)Default);
                return new SaveBoolParser(field);
            }
            if (subType == typeof(int[]))
                return new SaveIntArrayParser(field);
            if (subType == typeof(string))
                return new SaveStringParser(field, (string)Default);

            if (subType == typeof(ListMOBs))
                return new SaveGenericListParser<ListMOBs, MOB>(field);
            //return new SaveListMOBsParser(field);
            if (subType == typeof(ListItems))
                return new SaveGenericListParser<ListItems, Item>(field);
            //return new SaveListItemsParser(field);
            if (subType == typeof(ListRooms))
                return new SaveGenericListParser<ListRooms, Room>(field);
            //return new SaveListRoomsParser(field);
            if (subType == typeof(ListRoomLinks))
                return new SaveGenericListParser<ListRoomLinks, RoomLink>(field);
            //return new SaveListRoomLinksParser(field);

            if (subType == typeof(MOB))
                return new SaveMOBParser(field);
            if (subType == typeof(Account))
                return new SaveAccountParser(field);

            throw new ArgumentException("Type " + subType.Name + " is not generically supported as a saved field.");
        }
    }

    /// <summary>
    /// Generic attribute that attempts to save any ISaveable field directly to this object. Should only be
    /// used for child objects that can never be reassigned to a different parent.
    /// </summary>
    public class SaveSubobjectAttribute : SaveableFieldAttribute
    {
        public SaveSubobjectAttribute(string Key) : base(Key)
        {
        }

        public override SaveableParser Parser(FieldInfo field)
        {
            Type type = field.FieldType;
            if (ApprovedSubobjectType(type))
            {
                Type parserType = typeof(SaveSubobjectParser<>).MakeGenericType(type);
                return (SaveableParser)Activator.CreateInstance(parserType, field);
            }
            //if (field.FieldType == typeof(IRoomPosition))
            //    return new SaveSubobjectParser<IRoomPosition>(field);
            throw new ArgumentException("Type " + type.Name + " is not generically supported as a saved subobject.");
        }
        /// <summary>
        /// Check if a specific type is okay to use as a type for a subobject parser.
        /// Long term: This should return data for what special info the subobject parser needs to handle the subobject type.
        /// Right now none of them need special info so I don't need to figure that out now.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static bool ApprovedSubobjectType(Type t)
        {
            if (t == typeof(IRoomPosition)) return true;

            return false;
        }
    }
    
    public class SaveSubobjectListAttribute : SaveableFieldAttribute
    {
        public SaveSubobjectListAttribute(string Key, bool NullDefault = true) : base(Key)
        {
            nullDefault = NullDefault;
        }

        private bool nullDefault;
        public override SaveableParser Parser(FieldInfo field)
        {
            Type info = field.FieldType;
            if (info.IsGenericType && info.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type subType = info.GetGenericArguments()[0];

                if (SaveSubobjectAttribute.ApprovedSubobjectType(subType))
                {
                    Type parserType = typeof(SaveSubobjectListParser<>).MakeGenericType(subType);
                    return (SaveableParser)Activator.CreateInstance(parserType, field, nullDefault);
                }
            }

            throw new ArgumentException("Type " + info.Name + " is not supported as a collection of subobjects.");
        }
    }

    /// <summary>
    /// Special attribute for SaveIDs. Handled specially by database. This should always be used for SaveIDs.
    /// </summary>
    public class SaveIDAttribute : SaveableFieldAttribute
    {
        public SaveIDAttribute(string Key) : base(Key) { }
        public override SaveableParser Parser(FieldInfo field)
        {
            SaveableParser parser = new SaveIDParser(field);
            return parser;
        }
    }
    public class SaveIDParser : SaveableParser
    {
        public SaveIDParser(FieldInfo field)
        {
            Getter = GenerateGetter<int>(field);
            Setter = GenerateSetter<int>(field);
        }
        public override void Get(object source, BinaryWriter writer)
        {
            writer.Write(Getter.Invoke(source));
        }
        public override void Set(object target, BinaryReader reader)
        {
            Setter.Invoke(target, reader.ReadInt32());
        }
        private Func<object, int> Getter;
        private Action<object, int> Setter;
    }

    public class SaveIntAttribute : SaveableFieldAttribute
    {
        public SaveIntAttribute(string Key) : base(Key) { }
        private int defaultValue;
        private bool hasDefault = false;
        public int DefaultValue
        {
            get { return defaultValue; }
            set { defaultValue = value; hasDefault = true; }
        }
        public override SaveableParser Parser(FieldInfo field)
        {
            if (hasDefault)
                return new SaveIntParser(field, defaultValue);
            return new SaveIntParser(field);
        }
    }
    public class SaveIntParser : SaveableParser
    {
        public SaveIntParser(FieldInfo field)
        {
            Getter = GenerateGetter<int>(field);
            Setter = GenerateSetter<int>(field);
        }
        public SaveIntParser(FieldInfo field, int defaultValue)
        {
            Getter = GenerateGetter<int>(field);
            Setter = GenerateSetter<int>(field);
            hasDefault = true;
            defaultInt = defaultValue;
        }
        private bool hasDefault = false;
        private int defaultInt;
        public override void Get(object source, BinaryWriter writer)
        {
            int currentValue = Getter.Invoke(source);
            if (!hasDefault || defaultInt != currentValue) writer.Write(Getter.Invoke(source));
        }
        public override void Set(object target, BinaryReader reader)
        {
            Setter.Invoke(target, reader.ReadInt32());
        }
        private Func<object, int> Getter;
        private Action<object, int> Setter;
    }
    public class SaveEnumParser<T> : SaveableParser where T : Enum, IConvertible
    {
        public SaveEnumParser(FieldInfo field)
        {
            Getter = GenerateGetter<T>(field);
            Setter = GenerateSetter<T>(field);
        }
        public SaveEnumParser(FieldInfo field, T defaultValue)
        {
            Getter = GenerateGetter<T>(field);
            Setter = GenerateSetter<T>(field);
            hasDefault = true;
            this.defaultValue = defaultValue;
        }
        private T defaultValue;
        private bool hasDefault = false;
        public override void Get(object source, BinaryWriter writer)
        {
            T value = Getter.Invoke(source);
            if (!hasDefault || !defaultValue.Equals(value)) writer.Write(value.ToInt32(null));
        }
        public override void Set(object target, BinaryReader reader)
        {
            int value = reader.ReadInt32();
            Setter.Invoke(target, EnumConverter<T>.Convert(reader.ReadInt32()));
        }
        private Func<object, T> Getter;
        private Action<object, T> Setter;
    }

    public class SaveBoolParser : SaveableParser
    {
        public SaveBoolParser(FieldInfo field)
        {
            Getter = GenerateGetter<bool>(field);
            Setter = GenerateSetter<bool>(field);
        }
        public override void Get(object source, BinaryWriter writer)
        {
            writer.Write(Getter.Invoke(source));
        }
        public override void Set(object target, BinaryReader reader)
        {
            Setter.Invoke(target, reader.ReadBoolean());
        }
        private Func<object, bool> Getter;
        private Action<object, bool> Setter;
    }
    public class SaveBoolDefaultParser : SaveableParser
    {
        bool defaultValue;
        public SaveBoolDefaultParser(FieldInfo field, bool defaultValue)
        {
            this.defaultValue = defaultValue;
            Getter = GenerateGetter<bool>(field);
            Setter = GenerateSetter<bool>(field);
        }
        public override void Get(object source, BinaryWriter writer)
        {
            bool currentValue = Getter.Invoke(source);
            if (currentValue != defaultValue) writer.Write(currentValue);
        }
        public override void Set(object target, BinaryReader reader)
        {
            Setter.Invoke(target, reader.ReadBoolean());
        }
        private Func<object, bool> Getter;
        private Action<object, bool> Setter;
    }

    public class SaveIntArrayAttribute : SaveableFieldAttribute
    {
        public SaveIntArrayAttribute(string Key) : base(Key) { }
        public override SaveableParser Parser(FieldInfo field)
        {
            SaveableParser parser = new SaveIntArrayParser(field);
            return parser;
        }
    }
    public class SaveIntArrayParser : SaveableParser
    {
        public SaveIntArrayParser(FieldInfo field)
        {
            Getter = GenerateGetter<int[]>(field);
            Setter = GenerateSetter<int[]>(field);
        }
        public override void Get(object source, BinaryWriter writer)
        {
            int[] array = Getter.Invoke(source);
            if (array != null)
            {
                foreach (int i in array) writer.Write(i);
            }
        }
        public override void Set(object target, BinaryReader reader)
        {
            int[] array = new int[reader.BaseStream.Length / sizeof(int)];
            for (int i = 0; i < array.Length; i++) array[i] = reader.ReadInt32();
            Setter.Invoke(target, array);
        }
        private Func<object, int[]> Getter;
        private Action<object, int[]> Setter;
    }

    public class SaveStringAttribute : SaveableFieldAttribute
    {
        public SaveStringAttribute(string Key) : base(Key)
        {

        }

        public string DefaultValue { get; set; }
        public override SaveableParser Parser(FieldInfo field)
        {
            SaveableParser parser = new SaveStringParser(field, DefaultValue);
            return parser;
        }
    }
    public class SaveStringParser : SaveableParser
    {
        string defaultValue;
        public SaveStringParser(FieldInfo field, string defaultValue)
        {
            this.defaultValue = defaultValue;
            Getter = GenerateGetter<string>(field);
            Setter = GenerateSetter<string>(field);
        }
        public override void Get(object source, BinaryWriter writer)
        {
            string str = Getter.Invoke(source);
            if (str != null && str != defaultValue) writer.Write(str);
        }
        public override void Set(object target, BinaryReader reader)
        {
            Setter.Invoke(target, reader.ReadString());
        }
        private Func<object, string> Getter;
        private Action<object, string> Setter;
    }

    /// <summary>
    /// Parser to save a subobject. Most of the time objects can probably just use this parser.
    /// If something wants to extend this, probably refactor a bit first.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SaveSubobjectParser<T> : SaveableParser where T : ISubobject
    {
        public SaveSubobjectParser(FieldInfo field)
        {
            Getter = GenerateGetter<T>(field);
            Setter = GenerateSetter<T>(field);
        }
        public override void Get(object source, BinaryWriter writer)
        {
            ISubobject child = Getter.Invoke(source);
            if (child == null) return;
            DatabaseManager.SaveSubobject(child, writer);
        }
        public override void Set(object target, BinaryReader reader)
        {
            T child = (T)DatabaseManager.LoadSubobject(reader);
            child.Parent = target;
            Setter.Invoke(target, child);
        }
        private Func<object, T> Getter;
        private Action<object, T> Setter;
    }

    public class SaveSubobjectListParser<T> : SaveableParser where T : ISubobject
    {
        public SaveSubobjectListParser(FieldInfo field, bool defaultNull)
        {
            Getter = GenerateGetter<List<T>>(field);
            Setter = GenerateSetter<List<T>>(field);
            this.defaultNull = defaultNull;
        }

        private bool defaultNull;
        public override void Get(object source, BinaryWriter writer)
        {
            List<T> list = Getter.Invoke(source);
            if (list == null)
            {
                if (!defaultNull) writer.Write((byte)0);
                return;
            }
            if (list.Count == 0)
            {
                if (defaultNull) writer.Write((byte)1);
                return;
            }
            for (int i = 0; i < list.Count; i++)
            {
                DatabaseManager.SaveSubobject(list[i], writer);
            }
        }
        public override void Set(object target, BinaryReader reader)
        {
            List<T> list;
            if (reader.BaseStream.Length == 1)
            {
                if (reader.ReadByte() == 1)
                {
                    list = new List<T>();
                }
                else
                {
                    list = null;
                }
            }
            else
            {
                list = new List<T>();
                while (reader.BaseStream.Length > reader.BaseStream.Position)
                {
                    T child = (T)DatabaseManager.LoadSubobject(reader);
                    child.Parent = target;
                    list.Add(child);
                }
            }
            Setter.Invoke(target, list);
        }

        private Func<object, List<T>> Getter;
        private Action<object, List<T>> Setter;
    }

    /// <summary>
    /// Special attribute for account names. Adds the account to the dictionary of account names.
    /// </summary>
    public class SaveAccountNameAttribute : SaveStringAttribute
    {
        public SaveAccountNameAttribute(string Key) : base(Key) { }
        public override SaveableParser Parser(FieldInfo field)
        {
            SaveableParser parser = new SaveAccountNameParser(field);
            return parser;
        }
    }
    public class SaveAccountNameParser : SaveStringParser
    {
        public SaveAccountNameParser(FieldInfo field) : base(field, null)
        {
        }
        public override void Set(object target, BinaryReader reader)
        {
            base.Set(target, reader);
            AccountObjects.Get().FinishLoading(target as Account);
        }
    }

    /// <summary>
    /// Save a list of objects of a specific type.
    /// </summary>
    /// <typeparam name="T">Type of list to save</typeparam>
    /// <typeparam name="U">Type of object in the list to save references to</typeparam>
    public class SaveGenericListParser<T, U> : SaveableParser where T : ListSaveables<U>, new() where U : ISaveable
    {
        public SaveGenericListParser(FieldInfo field)
        {
            Getter = GenerateGetter<T>(field);
            Setter = GenerateSetter<T>(field);
        }
        public override void Get(object source, BinaryWriter writer)
        {
            T list = Getter.Invoke(source);
            int[] saveIDs = list.GetIDs();
            foreach (int i in saveIDs) writer.Write(i);
        }
        public override void Set(object target, BinaryReader reader)
        {
            int[] array = new int[reader.BaseStream.Length / sizeof(int)];
            for (int i = 0; i < array.Length; i++) array[i] = reader.ReadInt32();
            T list = new T();
            list.SetIDs(array);
            Setter.Invoke(target, list);
        }
        private Func<object, T> Getter;
        private Action<object, T> Setter;
    }

    /// <summary>
    /// Save a single reference to another object. The referenced object will be loaded if it isn't already.
    /// </summary>
    /// <typeparam name="T">Type of object being referenced.</typeparam>
    public abstract class SaveGenericObjectTypeObject<T> : SaveableParser where T : class, ISaveable
    {
        protected abstract Type ObjectsType { get; }
        public SaveGenericObjectTypeObject(FieldInfo field)
        {
            Getter = GenerateGetter<T>(field);
            Setter = GenerateSetter<T>(field);
        }
        public override void Get(object source, BinaryWriter writer)
        {
            T reference = Getter.Invoke(source);
            if (reference == null) return;
            int saveID = reference.GetSaveID();
            writer.Write(saveID);
        }
        public override void Set(object target, BinaryReader reader)
        {
            int saveID = reader.ReadInt32();
            T reference = ObjectType.TypeByClass[ObjectsType].Get(saveID, true, true) as T;
            Setter.Invoke(target, reference);
        }
        private Func<object, T> Getter;
        private Action<object, T> Setter;
    }
    public class SaveMOBParser : SaveGenericObjectTypeObject<MOB>
    {
        public SaveMOBParser(FieldInfo field) : base(field) { }
        protected override Type ObjectsType { get { return typeof(MOB); } }
    }
    public class SaveAccountParser : SaveGenericObjectTypeObject<Account>
    {
        public SaveAccountParser(FieldInfo field) : base(field) { }
        protected override Type ObjectsType { get { return typeof(Account); } }
    }

    /// <summary>
    /// Safely convert from an integer to a generic enum.
    /// From Raif Atef on stackoverflow, slightly modified because I never expect to have long enums so just int is fine for me.
    /// </summary>
    /// <typeparam name="TEnum"></typeparam>
    public static class EnumConverter<T> where T : Enum
    {
        public static readonly Func<int, T> Convert = GenerateConverter();

        static Func<int, T> GenerateConverter()
        {
            var parameter = Expression.Parameter(typeof(int));
            var dynamicMethod = Expression.Lambda<Func<int, T>>(
                Expression.Convert(parameter, typeof(T)),
                parameter);
            return dynamicMethod.Compile();
        }
    }
}
