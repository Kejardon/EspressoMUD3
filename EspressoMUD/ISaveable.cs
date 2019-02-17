using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EspressoMUD
{
    public interface ISaveable
    {
        /// <summary>
        /// Save ID of the object for a given object type. If -1, object is loading or has never been saved yet.
        /// </summary>
        /// <param name="databaseGroup">Object type to load an associated ID for. If null, a default should be used.</param>
        /// <returns></returns>
        int GetSaveID(ObjectType databaseGroup);
        /// <summary>
        /// Sets the save ID of this object for the given object type.
        /// </summary>
        /// <param name="databaseGroup">Object type to set an associated ID for. Shouldn't be null.</param>
        /// <param name="id"></param>
        void SetSaveID(ObjectType databaseGroup, int id);
        SaveValues SaveValues { get; set; }
        /* Template to copy-paste-modify in simple ISaveable classes.
        public SaveValues SaveValues { get; set; }
        [SaveID(Key="ID")]
        protected int <ObjectType>ID = -1;
        public int GetSaveID(ObjectType databaseGroup) { return <ObjectType>ID; }
        public void SetSaveID(ObjectType databaseGroup, int id) { <ObjectType>ID = id; }
         */
        /* Template to copy-paste-modify in multiple-object-type ISaveable classes.
        public SaveValues SaveValues { get; set; }
        [SaveID(Key="<ObjectType1>ID")]
        protected int <ObjectType1>ID = -1;
        [SaveID(Key="<ObjectType2>ID")]
        protected int <ObjectType2>ID = -1; //Repeat for as many object types as needed
        public int GetSaveID(ObjectType databaseGroup)
        {
            if (databaseGroup == <ObjectType1>)
                return <ObjectType1>ID;
            return <ObjectType2>ID; //Last object type should not be checked.
        }
        public void SetSaveID(ObjectType databaseGroup, int id)
        {
            if (databaseGroup == <ObjectType1>)
                <ObjectType1>ID = id;
            else if (databaseGroup == <ObjectType2>)
                <ObjectType2>ID = id;
        }
         */
    }

    public class DummySaveable : ISaveable
    {
        public ISaveable NextObjectToSave
        {
            get { return this; }
            set { throw new NotImplementedException(); }
        }
        public SaveValues SaveValues
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
        public int GetSaveID(ObjectType databaseGroup) { throw new NotImplementedException(); }
        public void SetSaveID(ObjectType databaseGroup, int id) { throw new NotImplementedException(); }
    }
    public abstract class SaveableFieldAttribute : Attribute
    {
        public SaveableFieldAttribute(string Key)
        {
            this.Key = Key;
        }

        public abstract SaveableParser Parser(FieldInfo field);
        public string Key;
    }

    /// <summary>
    /// Save status and file information for this object. Nothing here corresponds to the interface,
    /// everything is for the specific class / Metadata group.
    /// </summary>
    public class SaveValues
    {
        private class SaveableEndOfList : ISaveable
        {
            public SaveValues SaveValues { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }
            public int GetSaveID(ObjectType databaseGroup) { throw new NotImplementedException(); }
            public void SetSaveID(ObjectType databaseGroup, int id) { throw new NotImplementedException(); }
        }
        /// <summary>
        /// Sentinal placeholder when this is the last object to stage.
        /// </summary>
        public static ISaveable EndOfList = new SaveableEndOfList();


        /// <summary>
        /// Threads can wait on this event to know when the object has finished loading. If it is null, the object is not loading.
        /// </summary>
        public ManualResetEvent LoadingIndicator;
        /// <summary>
        /// Next object queued to save (to the prestaged file).
        /// </summary>
        public ISaveable NextObjectToSave;
        /// <summary>
        /// Next object to save (from prestaged to staged).
        /// </summary>
        public ISaveable NextStagedValues;
        /// <summary>
        /// Location in .var file where this object is saved to. Ignored if Capacity is 0 or -1.
        /// </summary>
        public int Offset;
        /// <summary>
        /// Amount of space reserved in .var file. If -1, this object has never been saved.
        /// </summary>
        public int Capacity;
        /// <summary>
        /// Location in prestaged.bin that contains the data for this object. Starts at old .var fileoffset.
        /// </summary>
        public int StagedOffset;
        /// <summary>
        /// If true, this object is being deleted.
        /// </summary>
        public bool Deleted;
    }


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
    /// Generic attribute that attempts to save any normal field. Supports Default for some types
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
            if (field.FieldType == typeof(int[]))
                return new SaveIntArrayParser(field);
            if (field.FieldType == typeof(string))
                return new SaveStringParser(field, (string)Default);

            if (field.FieldType == typeof(ListMOBs))
                return new SaveGenericListParser<ListMOBs, MOB>(field);
                //return new SaveListMOBsParser(field);
            if (field.FieldType == typeof(ListItems))
                return new SaveListItemsParser(field);
            if (field.FieldType == typeof(ListRooms))
                return new SaveListRoomsParser(field);
            if (field.FieldType == typeof(ListRoomLinks))
                return new SaveListRoomLinksParser(field);

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
    /// Special attribute for SaveIDs. Handled specially be database.
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
            if(currentValue != defaultValue) writer.Write(currentValue);
        }
        public override void Set(ISaveable target, BinaryReader reader)
        {
            Setter.Invoke(target, reader.ReadInt32());
        }
        private Func<ISaveable, int> Getter;
        private Action<ISaveable, int> Setter;
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
            if(str != null && str != defaultValue) writer.Write(str);
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
    
    public class SaveListMOBsParser : SaveableParser
    {
        public SaveListMOBsParser(FieldInfo field)
        {
            Getter = GenerateGetter<ListMOBs>(field);
            Setter = GenerateSetter<ListMOBs>(field);
        }
        public override void Get(ISaveable source, BinaryWriter writer)
        {
            ListMOBs list = Getter.Invoke(source);
            int[] saveIDs = list.GetIDs();
            foreach (int i in saveIDs) writer.Write(i);
        }
        public override void Set(ISaveable target, BinaryReader reader)
        {
            int[] array = new int[reader.BaseStream.Length / sizeof(int)];
            for (int i = 0; i < array.Length; i++) array[i] = reader.ReadInt32();
            ListMOBs list = new ListMOBs();
            list.SetIDs(array);
            Setter.Invoke(target, list);
        }
        private Func<ISaveable, ListMOBs> Getter;
        private Action<ISaveable, ListMOBs> Setter;
    }
    public class SaveListItemsParser : SaveableParser
    {
        public SaveListItemsParser(FieldInfo field)
        {
            Getter = GenerateGetter<ListItems>(field);
            Setter = GenerateSetter<ListItems>(field);
        }
        public override void Get(ISaveable source, BinaryWriter writer)
        {
            ListItems list = Getter.Invoke(source);
            int[] saveIDs = list.GetIDs();
            foreach (int i in saveIDs) writer.Write(i);
        }
        public override void Set(ISaveable target, BinaryReader reader)
        {
            int[] array = new int[reader.BaseStream.Length / sizeof(int)];
            for (int i = 0; i < array.Length; i++) array[i] = reader.ReadInt32();
            ListItems list = new ListItems();
            list.SetIDs(array);
            Setter.Invoke(target, list);
        }
        private Func<ISaveable, ListItems> Getter;
        private Action<ISaveable, ListItems> Setter;
    }
    public class SaveListRoomsParser : SaveableParser
    {
        public SaveListRoomsParser(FieldInfo field)
        {
            Getter = GenerateGetter<ListRooms>(field);
            Setter = GenerateSetter<ListRooms>(field);
        }
        public override void Get(ISaveable source, BinaryWriter writer)
        {
            ListRooms list = Getter.Invoke(source);
            int[] saveIDs = list.GetIDs();
            foreach (int i in saveIDs) writer.Write(i);
        }
        public override void Set(ISaveable target, BinaryReader reader)
        {
            int[] array = new int[reader.BaseStream.Length / sizeof(int)];
            for (int i = 0; i < array.Length; i++) array[i] = reader.ReadInt32();
            ListRooms list = new ListRooms();
            list.SetIDs(array);
            Setter.Invoke(target, list);
        }
        private Func<ISaveable, ListRooms> Getter;
        private Action<ISaveable, ListRooms> Setter;
    }
    public class SaveListRoomLinksParser : SaveableParser
    {
        public SaveListRoomLinksParser(FieldInfo field)
        {
            Getter = GenerateGetter<ListRoomLinks>(field);
            Setter = GenerateSetter<ListRoomLinks>(field);
        }
        public override void Get(ISaveable source, BinaryWriter writer)
        {
            ListRoomLinks list = Getter.Invoke(source);
            int[] saveIDs = list.GetIDs();
            foreach (int i in saveIDs) writer.Write(i);
        }
        public override void Set(ISaveable target, BinaryReader reader)
        {
            int[] array = new int[reader.BaseStream.Length / sizeof(int)];
            for (int i = 0; i < array.Length; i++) array[i] = reader.ReadInt32();
            ListRoomLinks list = new ListRoomLinks();
            list.SetIDs(array);
            Setter.Invoke(target, list);
        }
        private Func<ISaveable, ListRoomLinks> Getter;
        private Action<ISaveable, ListRoomLinks> Setter;
    }

    //TODO: Does this work instead? Would be cleaner.
    public class SaveGenericListParser<T,U> : SaveableParser where T : ListSaveables<U>, new() where U : ISaveable
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

    //TODO: Does this work? Would be cleaner.
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
