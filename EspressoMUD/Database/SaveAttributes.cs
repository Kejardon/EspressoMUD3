using System;
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
        public abstract void Get(ISaveable source, BinaryWriter writer);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="target">Object to load data to</param>
        /// <param name="data">BinaryReader backed by an ArraySegmentStream. Calling function can use the ArraySegmentStream directly but should stay within bounds.</param>
        public abstract void Set(ISaveable target, BinaryReader data);
        public string Key; //Maybe not needed? Key converts to ParserID depending on ClassID.
        public Type owningType; //The type that declares this parser. Not necessarily the type that is saving/loading data, may be an ancestor.
        public virtual string OwningTypeName
        {
            get { return owningType.Name; }
        }
        //public int ClassID; //This currently isn't used, but is easy to add if wanted by uncommented code in DatabaseManager.css
        public ushort ParserID;
        //public T Default; //Strongly recommended to implement, but not necessary.

        protected static Action<ISaveable, T> GenerateSetter<T>(FieldInfo field)
        {
            if (typeof(T) != field.FieldType) return null;
            Type baseType = field.DeclaringType;

            ParameterExpression paramOne = Expression.Parameter(typeof(ISaveable), "argOne");
            ParameterExpression paramTwo = Expression.Parameter(typeof(T), "argTwo");
            Action<ISaveable, T> del = Expression.Lambda<Action<ISaveable, T>>(
                Expression.Assign(Expression.MakeMemberAccess(Expression.Convert(paramOne, baseType), field), paramTwo),
                new ParameterExpression[] { paramOne, paramTwo }).Compile();
            return del;
        }
        protected static Func<ISaveable, T> GenerateGetter<T>(FieldInfo field)
        {
            if (typeof(T) != field.FieldType) return null;
            Type baseType = field.DeclaringType;

            ParameterExpression paramOne = Expression.Parameter(typeof(ISaveable), "argOne");
            ParameterExpression paramTwo = Expression.Parameter(typeof(T), "result");
            Func<ISaveable, T> del = Expression.Lambda<Func<ISaveable, T>>(
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

        public override void Get(ISaveable source, BinaryWriter writer)
        {
        }
        public override void Set(ISaveable target, BinaryReader data)
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
            if (field.FieldType == typeof(int))
            {
                if (Default != null)
                    return new SaveIntDefaultParser(field, (int)Default);
                return new SaveIntParser(field);
            }
            if (field.FieldType == typeof(bool))
            {
                if (Default != null)
                    return new SaveBoolDefaultParser(field, (bool)Default);
                return new SaveBoolParser(field);
            }
            if (field.FieldType == typeof(int[]))
                return new SaveIntArrayParser(field);
            if (field.FieldType == typeof(string))
                return new SaveStringParser(field, (string)Default);

            if (field.FieldType == typeof(ListMOBs))
                return new SaveGenericListParser<ListMOBs, MOB>(field);
            //return new SaveListMOBsParser(field);
            if (field.FieldType == typeof(ListItems))
                return new SaveGenericListParser<ListItems, Item>(field);
            //return new SaveListItemsParser(field);
            if (field.FieldType == typeof(ListRooms))
                return new SaveGenericListParser<ListRooms, Room>(field);
            //return new SaveListRoomsParser(field);
            if (field.FieldType == typeof(ListRoomLinks))
                return new SaveGenericListParser<ListRoomLinks, RoomLink>(field);
            //return new SaveListRoomLinksParser(field);

            if (field.FieldType == typeof(MOB))
                return new SaveMOBParser(field);
            if (field.FieldType == typeof(Account))
                return new SaveAccountParser(field);

            throw new ArgumentException("Type " + field.FieldType.Name + " is not generically supported as a saved field.");
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
            if (field.FieldType == typeof(IPosition))
                return new SaveSubobjectParser<IPosition>(field);
            throw new ArgumentException("Type " + field.FieldType.Name + " is not generically supported as a saved subobject.");
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
        public override void Get(ISaveable source, BinaryWriter writer)
        {
            writer.Write(Getter.Invoke(source));
        }
        public override void Set(ISaveable target, BinaryReader reader)
        {
            Setter.Invoke(target, reader.ReadInt32());
        }
        private Func<ISaveable, int> Getter;
        private Action<ISaveable, int> Setter;
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
                return new SaveIntDefaultParser(field, defaultValue);
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
        public override void Get(ISaveable source, BinaryWriter writer)
        {
            writer.Write(Getter.Invoke(source));
        }
        public override void Set(ISaveable target, BinaryReader reader)
        {
            Setter.Invoke(target, reader.ReadInt32());
        }
        private Func<ISaveable, int> Getter;
        private Action<ISaveable, int> Setter;
    }
    public class SaveIntDefaultParser : SaveableParser
    {
        int defaultValue;
        public SaveIntDefaultParser(FieldInfo field, int defaultValue)
        {
            this.defaultValue = defaultValue;
            Getter = GenerateGetter<int>(field);
            Setter = GenerateSetter<int>(field);
        }
        public override void Get(ISaveable source, BinaryWriter writer)
        {
            int currentValue = Getter.Invoke(source);
            if (currentValue != defaultValue) writer.Write(currentValue);
        }
        public override void Set(ISaveable target, BinaryReader reader)
        {
            Setter.Invoke(target, reader.ReadInt32());
        }
        private Func<ISaveable, int> Getter;
        private Action<ISaveable, int> Setter;
    }

    public class SaveBoolParser : SaveableParser
    {
        public SaveBoolParser(FieldInfo field)
        {
            Getter = GenerateGetter<bool>(field);
            Setter = GenerateSetter<bool>(field);
        }
        public override void Get(ISaveable source, BinaryWriter writer)
        {
            writer.Write(Getter.Invoke(source));
        }
        public override void Set(ISaveable target, BinaryReader reader)
        {
            Setter.Invoke(target, reader.ReadBoolean());
        }
        private Func<ISaveable, bool> Getter;
        private Action<ISaveable, bool> Setter;
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
        public override void Get(ISaveable source, BinaryWriter writer)
        {
            bool currentValue = Getter.Invoke(source);
            if (currentValue != defaultValue) writer.Write(currentValue);
        }
        public override void Set(ISaveable target, BinaryReader reader)
        {
            Setter.Invoke(target, reader.ReadBoolean());
        }
        private Func<ISaveable, bool> Getter;
        private Action<ISaveable, bool> Setter;
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
        public override void Get(ISaveable source, BinaryWriter writer)
        {
            int[] array = Getter.Invoke(source);
            if (array != null)
            {
                foreach (int i in array) writer.Write(i);
            }
        }
        public override void Set(ISaveable target, BinaryReader reader)
        {
            int[] array = new int[reader.BaseStream.Length / sizeof(int)];
            for (int i = 0; i < array.Length; i++) array[i] = reader.ReadInt32();
            Setter.Invoke(target, array);
        }
        private Func<ISaveable, int[]> Getter;
        private Action<ISaveable, int[]> Setter;
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
        public override void Get(ISaveable source, BinaryWriter writer)
        {
            string str = Getter.Invoke(source);
            if (str != null && str != defaultValue) writer.Write(str);
        }
        public override void Set(ISaveable target, BinaryReader reader)
        {
            Setter.Invoke(target, reader.ReadString());
        }
        private Func<ISaveable, string> Getter;
        private Action<ISaveable, string> Setter;
    }

    /// <summary>
    /// Parser to save a subobject. Most of the time objects can probably just use this parser.
    /// If something wants to extend this, probably refactor a bit first.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SaveSubobjectParser<T> : SaveableParser where T : ISaveable
    {
        public SaveSubobjectParser(FieldInfo field)
        {
            Getter = GenerateGetter<T>(field);
            Setter = GenerateSetter<T>(field);
        }
        public override void Get(ISaveable source, BinaryWriter writer)
        {
            ISaveable child = Getter.Invoke(source);
            if (child == null) return;
            DatabaseManager.SaveSubobject(child, writer);
        }
        public override void Set(ISaveable target, BinaryReader reader)
        {
            T child = (T)DatabaseManager.LoadSubobject(reader);
            Setter.Invoke(target, child);
        }
        private Func<ISaveable, T> Getter;
        private Action<ISaveable, T> Setter;
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
        public override void Set(ISaveable target, BinaryReader reader)
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
        public override void Get(ISaveable source, BinaryWriter writer)
        {
            T list = Getter.Invoke(source);
            int[] saveIDs = list.GetIDs();
            foreach (int i in saveIDs) writer.Write(i);
        }
        public override void Set(ISaveable target, BinaryReader reader)
        {
            int[] array = new int[reader.BaseStream.Length / sizeof(int)];
            for (int i = 0; i < array.Length; i++) array[i] = reader.ReadInt32();
            T list = new T();
            list.SetIDs(array);
            Setter.Invoke(target, list);
        }
        private Func<ISaveable, T> Getter;
        private Action<ISaveable, T> Setter;
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
        public override void Get(ISaveable source, BinaryWriter writer)
        {
            T reference = Getter.Invoke(source);
            if (reference == null) return;
            int saveID = reference.GetSaveID(ObjectType.TypeByClass[ObjectsType]);
            writer.Write(saveID);
        }
        public override void Set(ISaveable target, BinaryReader reader)
        {
            int saveID = reader.ReadInt32();
            T reference = ObjectType.TypeByClass[ObjectsType].Get(saveID, true, true) as T;
            Setter.Invoke(target, reference);
        }
        private Func<ISaveable, T> Getter;
        private Action<ISaveable, T> Setter;
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

}
