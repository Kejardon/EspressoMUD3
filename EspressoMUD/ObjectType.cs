using KejUtils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace EspressoMUD
{
    /// <summary>
    /// Managers for specific types of objects (e.g. effect, mob, container, etc.)
    /// </summary>
    public class ObjectType
    {
        public static ObjectType[] TypeByID;
        public static Dictionary<Type,ObjectType> TypeByClass;
        private static Type[] GetBaseClasses()
        {
            return new Type[] {
                typeof(Account),
                typeof(MOB),
                typeof(Room),
                typeof(Item)
                };
        }
        //'Flag' slots. Generally only used by this and the database.
        /// <summary>
        /// Object ID was never saved or is deleted.
        /// </summary>
        public static readonly ISaveable EmptySlot = new DummySaveable();
        /// <summary>
        /// Object ID was requested but not written to yet.
        /// </summary>
        public static readonly ISaveable ClaimedSlot = new DummySaveable();
        /// <summary>
        /// Object ID is used by something but cannot be read (e.g. the class for this data was deleted)
        /// </summary>
        public static readonly ISaveable UnreadableSlot = new DummySaveable();
        /// <summary>
        /// Object ID is on the database but has not been loaded. It's not guaranteed to be readable though.
        /// </summary>
        public static readonly ISaveable DatabaseSlot = new DummySaveable();
        // If null, object ID is unknown on the database.

        private const int IsLoading = -1;
        private const int IsDeleted = -2;

        /// <summary>
        /// Load the object types from the found list of options. Sets up TypesByClass and TypesByID
        /// </summary>
        /// <param name="savedTypes">Object types that have been loaded and mapped in the past.</param>
        /// <returns>Object types that were loaded this time but have not been mapped in the past.</returns>
        public static List<ObjectType> SetObjectTypes(List<string> savedTypes)
        {
            Type[] baseClasses = GetBaseClasses();
            List<ObjectType> newTypes = new List<ObjectType>();
            List<ObjectType> objectTypes = new List<ObjectType>();
            TypeByClass = new Dictionary<Type, ObjectType>();
            while (objectTypes.Count < savedTypes.Count) objectTypes.Add(null); //Reserve spots for all the previously-loaded types, even if we won't match them.
            for (int i=0; i < baseClasses.Length; i++)
            {
                int savedIndex = savedTypes.IndexOf(baseClasses[i].Name);
                ObjectType newType;
                if (savedIndex != -1)
                {
                    //Assign previously loaded classes to their assigned spot
                    objectTypes[savedIndex] = newType = ObjectType.CreateTypeFor(baseClasses[i], savedIndex);
                }
                else
                {
                    //This is a new interface that hasn't been used before. Assign it to a new spot.
                    newType = ObjectType.CreateTypeFor(baseClasses[i], objectTypes.Count);
                    objectTypes.Add(newType);
                    newTypes.Add(newType);
                }
                TypeByClass[newType.BaseClass] = newType;
            }
            TypeByID = objectTypes.ToArray();
            return newTypes;
        }

        public Type BaseClass;
        /// <summary>
        /// List of known free ID numbers. Ideally this should be sorted in descending order.
        /// </summary>
        private List<int> FreeNumbers = new List<int>();
        /// <summary>
        /// Lowest ID number that hasn't been checked on the database. If negative, means all of database has been read and two's complement is next free ID.
        /// </summary>
        private int LowestUncheckedNumber = 0;
        /// <summary>
        /// List of objects of this type, indexed by object's SaveID for this type.
        /// This object is locked whenever it may be modified.
        /// </summary>
        private List<ISaveable> ItemDictionary = new List<ISaveable>();
        /// <summary>
        /// Listed as public, but only intended to be used by the database. FileStream associated with the file for this 
        /// </summary>
        public FileStream IndexFile;
        /// <summary>
        /// The ID of this ObjectType
        /// </summary>
        public int ID;


        //private byte[] LoadFromFile(int start, int length)
        //{
        //    return LoadFromFile(DatabaseManager.GetFile(this), start, length);
        //}
        //private static byte[] LoadFromFile(FileStream file, int start, int length)
        //{
        //    var ret = new byte[length];
        //    LoadFromFile(file, start, length, ret);
        //    return ret;
        //}
        //private static void LoadFromFile(FileStream file, int start, int length, byte[] buffer)
        //{
        //    lock (file)
        //    {
        //        file.Position = start;
        //        file.Read(buffer, 0, length);
        //    }
        //}

        public ObjectType(Type baseClass, int Id)
        {
            BaseClass = baseClass;
            this.ID = Id;
        }
        private static ObjectType CreateTypeFor(Type baseClass, int Id)
        {
            if(baseClass == typeof(Account))
            {
                return new AccountObjects(baseClass, Id);
            }
            else
            {
                return new ObjectType(baseClass, Id);
            }
        }

        /// <summary>
        /// Searches the database for IDs that are not used. Adds them to FreeNumbers and updates LowestUncheckedNumber
        /// </summary>
        /// <param name="startingFrom">ID to start from. Should be LowestUncheckedNumber + however many more IDs are known in ItemDictionary</param>
        /// <param name="howMany">Number of IDs to check.</param>
        private void CheckNextFreeNumber(int startingFrom, int howMany = 64)
        {
            FileStream stream = DatabaseManager.GetFile(this);
            byte[] data = new byte[howMany * 0x10];
            MemoryStream memStream = new MemoryStream(data);
            BinaryReader reader = new BinaryReader(memStream);
            int read = 0; //Amount of entries read (0x10 bytes each, must read a full entry to count)
            int available;
            long newPosition = (long)startingFrom * 0x10;
            lock (stream)
            {
                available = (int)(stream.Length / 0x10);
                if(available > startingFrom) //Are there more entries to read?
                {
                    stream.Position = newPosition;
                    read = stream.Read(data, 0, data.Length) / 0x10; //Read up to 'howMany' entries, or as much as in the file.
                }
            }
            EnsureSize((startingFrom - 1) + read); //EnsureSize and startingFrom are both inclusive, subtract 1 for fenceposts
            int nextIndex = startingFrom;
            while (memStream.Position < (read * 0x10)) //Parse all the entries and mark their entries in the dictionary.
            {
                int isValid = reader.ReadInt32();
                if (ItemDictionary[nextIndex] == null)
                {
                    if(isValid >= 0)
                        ItemDictionary[nextIndex] = DatabaseSlot;
                    else
                    {
                        ItemDictionary[nextIndex] = EmptySlot;
                        FreeNumbers.Insert(0, nextIndex);
                    }
                }
                nextIndex++;
                memStream.Seek(0x10 - 4, SeekOrigin.Current);
            }
            //If all entries in the database have been read, indicate that with LowestUncheckedNumber.
            if (available <= startingFrom + read) //available and startingFrom are both inclusive, so again subtract 1
            {
                //Future asks for numbers can start incrementing from the highest known number instead.
                LowestUncheckedNumber = -ItemDictionary.Count - 1; //Subtract 1 because two's complement.
            }
            else
            {
                LowestUncheckedNumber = startingFrom + read;
            }
        }
        /// <summary>
        /// Requests and claims an unused ID number for this object type.
        /// </summary>
        /// <returns>An ID to use. This is expected to be either claimed or returned in a later call, it should not be abandoned.</returns>
        private int NextFreeNumber()
        {
            lock(ItemDictionary)
            {
                int next;
                while (true)
                {
                    if (FreeNumbers.Count > 0)
                    {
                        next = FreeNumbers[FreeNumbers.Count - 1];
                        FreeNumbers.RemoveAt(FreeNumbers.Count - 1);
                        break;
                    }
                    next = LowestUncheckedNumber;
                    if (next < 0)
                    {
                        LowestUncheckedNumber = LowestUncheckedNumber - 1;
                        next = -next - 1;
                        EnsureSize(next);
                        break;
                    }
                    if (ItemDictionary.Count <= next || ItemDictionary[next] == null)
                    {
                        CheckNextFreeNumber(next);
                    }
                    else
                    {
                        LowestUncheckedNumber = next + 1;
                        if (ItemDictionary[next] == EmptySlot) //This probably shouldn't happen, but if FreeNumbers isn't comprehensive it could.
                        {
                            break;
                        }
                    }
                }

                ItemDictionary[next] = ClaimedSlot;
                return next;
            }
        }

        /// <summary>
        /// Returns a SaveID number that is now not being used.
        /// This ONLY updates FreeNumbers, nothing else.
        /// </summary>
        /// <param name="id"></param>
        private void ReturnIndex(int id)
        {
            lock (ItemDictionary)
            {
                FreeNumbers.BinaryAdd(id, new ReverseComparer<int>());
            }
        }

        /// <summary>
        /// Ensures the size of the ItemDictionary is large enough to hold all the items needed for this type.
        /// </summary>
        /// <param name="id">Highest ID that needs to be available</param>
        private void EnsureSize(int id)
        {
            lock (ItemDictionary)
            {
                if (ItemDictionary.Capacity < id / 2) ItemDictionary.Capacity = id;
                while (ItemDictionary.Count <= id)
                {
                    ItemDictionary.Add(null);
                }
            }
        }

        /// <summary>
        /// Adds an object to be tracked by this objecttype.
        /// </summary>
        /// <param name="existingObject">Object to track.</param>
        /// <param name="assumeNew">If false, ignores objects not ready to be saved (with id=-1).
        /// Else assign a new ID if the object has not been saved yet.</param>
        /// <returns>True on success. False if somehow the add failed.</returns> 
        public virtual bool Add(ISaveable existingObject, bool assumeNew=true)
        {
            int id = existingObject.GetSaveID(this);
            if(assumeNew && id == -1)
            {
                // SetSaveID is only called by two things: Loading, and this method. By locking here, this should be threadsafe.
                lock(this)
                {
                    id = existingObject.GetSaveID(this);
                    if (id == -1)
                    {
                        id = NextFreeNumber(); //Note that this may get a lock on ItemDictionary also, and then the file stream.
                        existingObject.SetSaveID(this, id);
                        existingObject.Save();
                    }
                }
            }
            if (id >= 0)
            {
                Add(existingObject, id, false);
                return true;
            }
            return false;
        }
        /// <summary>
        /// Inner Add function, skips other checks and assignments.
        /// The only external thing calling this should be the database.
        /// </summary>
        /// <param name="existingObject">Object to track in the internal index</param>
        /// <param name="atIndex">ID of the object to track</param>
        /// <param name="checkNull">Check if the object is null before trying to set it.</param>
        public void Add(ISaveable existingObject, int atIndex, bool checkNull = true)
        {
            lock (ItemDictionary)
            {
                if (!checkNull || ItemDictionary.Count <= atIndex || ItemDictionary[atIndex] == null)
                {
                    this.EnsureSize(atIndex);
                    this.ItemDictionary[atIndex] = existingObject;
                }
            }
        }


        /// <summary>
        /// Attempts to remove an object from this type.
        /// </summary>
        /// <param name="saveable"></param>
        /// <returns></returns>
        public virtual bool Remove(ISaveable saveable)
        {
            int id = saveable.GetSaveID(this);
            if (id == -1) return false;

            bool success;
            lock (ItemDictionary)
            {
                success = (ItemDictionary.Count > id && ItemDictionary[id] == saveable);
                if (success)
                {
                    ItemDictionary[id] = null;
                    ReturnIndex(id);
                }
            }
            return success;
        }

        /// <summary>
        /// Requests the object for the given ID.
        /// </summary>
        /// <param name="id">ID of object to get</param>
        /// <param name="loadIfNotFound">If the object is not currently loaded, try to load it.</param>
        /// <param name="waitForLoaded">If the object is currently loading, wait for it to finish loading first.</param>
        /// <returns>The object, or null if not found.</returns>
        public ISaveable Get(int id, bool loadIfNotFound, bool waitForLoaded)
        {
            ISaveable foundObject = null;
            if (id >= ItemDictionary.Count || (foundObject = ItemDictionary[id]) == null)
            {
                if(loadIfNotFound)
                {
                    DatabaseManager.LoadSaveable(this, id);
                    //Check now for results.
                    //  If null, mark as unloaded.
                    //  If set, can check if loaded and wait if requested.
                    //  Else do nothing.
                    foundObject = ItemDictionary.Count <= id ? null : ItemDictionary[id];
                    if (foundObject == null)
                    { //TODO: Mark as unloaded?

                    }
                    else if(waitForLoaded)
                    {
                        ManualResetEvent waitEvent = foundObject.SaveValues.LoadingIndicator;
                        if (waitEvent != null) waitEvent.WaitOne();
                    }
                }
            }
            if (foundObject == UnreadableSlot)
            { //Handle case of data being unreadable on the database. Not much that can be done.
                Log.LogText("Reference to unreadable data (" + this.BaseClass.Name + "[" + id + "]" + "): " + new Exception().ToString());
                return null;
            }
            return foundObject;

        }
    }

    public class AccountObjects : ObjectType
    {
        private ConcurrentDictionary<string, Account> usernameMap = new ConcurrentDictionary<string, Account>();
        public AccountObjects(Type baseClass, int Id) : base(baseClass, Id)
        {
        }
        /// <summary>
        /// Add a new user to this object.
        /// </summary>
        /// <param name="existingObject"></param>
        /// <param name="assumeNew"></param>
        public override bool Add(ISaveable existingObject, bool assumeNew=true)
        {
            Account account = existingObject as Account;
            if (account != existingObject) throw new ArgumentException("Can not add non-accounts to AccountObjects");

            // For loading, just return. Account name SaveParser will call FinishLoading
            if (account.SaveValues?.LoadingIndicator != null)
            {
                return base.Add(existingObject, assumeNew);
            }

            bool success = usernameMap.TryAdd(account.Name.ToUpper(), account);
            if (!success) return false;
            success = base.Add(existingObject, assumeNew);
            if (!success)
            {
                Account removed;
                usernameMap.TryRemove(account.Name.ToUpper(), out removed);
            }

            return success;
        }

        /// <summary>
        /// Called when an account loads the name. Adds the account to the name index.
        /// </summary>
        /// <param name="account"></param>
        public void FinishLoading(Account account)
        {
            if (this.Get(account.GetSaveID(this), false, false) != account)
                throw new Exception("Invalid account to finish loading.");

            usernameMap.TryAdd(account.Name.ToUpper(), account);
        }

        /// <summary>
        /// Tries to get an account with the requested name. If no account exists, returns null.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public Account GetAccount(string str)
        {
            Account act;
            usernameMap.TryGetValue(str.ToUpper(), out act);
            return act;
        }
        public override bool Remove(ISaveable saveable)
        {
            Account act = saveable as Account;
            bool success = base.Remove(saveable);
            if(success && act != null)
            {
                Account removed;
                usernameMap.TryRemove(act.Name.ToUpper(), out removed);
            }
            return success;
        }

        /// <summary>
        /// Get the current instance of the AccountObjects collection.
        /// </summary>
        /// <returns></returns>
        public static AccountObjects Get()
        {
            return ObjectType.TypeByClass[typeof(Account)] as AccountObjects;
        }

    }
}