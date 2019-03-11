using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EspressoMUD
{

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
}
