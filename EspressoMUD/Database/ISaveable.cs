using System;

namespace EspressoMUD
{
    public interface ISaveable : IModifiable
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

}
