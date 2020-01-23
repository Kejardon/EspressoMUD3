using System;

namespace EspressoMUD
{
    public interface ISaveable : IModifiable
    {
        /// <summary>
        /// Save ID of the object for a given object type. If -1, object is loading or has never been saved yet.
        /// </summary>
        /// <returns></returns>
        int GetSaveID();
        /// <summary>
        /// Sets the save ID of this object.
        /// </summary>
        /// <param name="id"></param>
        void SetSaveID(int id);
        SaveValues SaveValues { get; set; }
        /* Template to copy-paste-modify in simple ISaveable classes.
        public SaveValues SaveValues { get; set; }
        [SaveID(Key="ID")]
        protected int <ObjectType>ID = -1;
        public int GetSaveID() { return <ObjectType>ID; }
        public void SetSaveID(int id) { <ObjectType>ID = id; }
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
        public int GetSaveID() { throw new NotImplementedException(); }
        public void SetSaveID(int id) { throw new NotImplementedException(); }
    }

}
