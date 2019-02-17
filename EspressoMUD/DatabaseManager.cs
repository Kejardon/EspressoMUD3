using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using KejUtils;

namespace EspressoMUD
{
    /// <summary>
    /// Manager for the database
    /// </summary>
    public class DatabaseManager
    {
        public const string DatabaseFolderName = "database";
        public const string MainFile = "main.bin";
        public const string GlobalsFile = "globals.bin";
        public const string PreStagedFile = "prestaged.bin";
        public const string StagedFile = "staged.bin";
        public const string ObjectTypesFile = "objectTypes.bin";
        /// <summary>
        /// Extension for files that contain fixed-length indexed data for saveable objects.
        /// </summary>
        public const string IndexExtension = ".fix";
        /// <summary>
        /// Extension for files that contain data for specific objects of specific classes.
        /// </summary>
        public const string ClassExtension = ".var";
        /// <summary>
        /// Extension for files that contain indexes of unused/used space for the associated .var file of specific classes.
        /// </summary>
        public const string UnusedSpaceExtension = ".spc";
        /// <summary>
        /// Extension for files that contain metadata for specific classes of saveable objects.
        /// </summary>
        public const string MapExtension = ".map";

        public static string BasePath; //This should only be written to once, before loading data.
        public static string DatabasePath;

        private static System.Threading.Thread DatabaseThread;

        private static bool GlobalsIsDirty = false;

        private static int NextClassID = 0;
        
        /// <summary>
        /// The current FileStream for PreStagedFile
        /// </summary>
        private static FileStream PrestagedChanges;
        /// <summary>
        /// The current FileStream for StagedFile.
        /// </summary>
        private static FileStream MainFileStream;
        /// <summary>
        /// The current file that changes are being written into StagedFile for (e.g. "globals.bin")
        /// </summary>
        private static string CurrentStagedFile;
        /// <summary>
        /// The current FileStream for StagedFile.
        /// </summary>
        private static FileStream CurrentStagedChanges;
        /// <summary>
        /// The current BinaryWriter for StagedFile.
        /// </summary>
        private static BinaryWriter CurrentStagedWriter;
        /// <summary>
        /// Offset in CurrentStagedFile where the current chunk of data starts (e.g. 00000000 in "globals.bin")
        /// </summary>
        private static long CurrentStagedStart;
        /// <summary>
        /// Position for CurrentStagedChanges where the size of the current chunk of data is stored.
        /// Note that this should always be not-applicable (CurrentStagedFile == null) or valid - it's only invalid
        /// for short periods of time inside of SetStageFile.
        /// </summary>
        private static long LastStagedSizeOffset;
        /// <summary>
        /// If this file is supposed to be completely overwritten instead of leaving existing data.
        /// </summary>
        private static bool IsOverwrite;

        public enum DatabaseState
        {
            UpToDate = 0,
            SavingPrestaged = 1,
            StagingChanges = 2,
            WritingToDatabase = 3
        }

        /// <summary>
        /// Get the FileStream for the main.bin file.
        /// </summary>
        /// <returns></returns>
        public static FileStream GetMainFile()
        {
            if (MainFileStream == null)
            {
                MainFileStream = File.Open(Path.Combine(DatabasePath, MainFile), FileMode.OpenOrCreate);
            }
            return MainFileStream;
        }
        //public static void CloseMainFile()
        //{
        //    if (MainFileStream != null)
        //    {
        //        MainFileStream.Dispose();
        //        MainFileStream = null;
        //    }
        //}
        /// <summary>
        /// Change the database state to a new value. Saves the value to disk before returning.
        /// </summary>
        /// <param name="newState">New database state</param>
        public static void SetDatabaseState(DatabaseState newState)
        {
            FileStream stream = GetMainFile();
            #region Writing to main.bin
            stream.Seek(1, SeekOrigin.Begin);
            stream.WriteByte((byte)newState);
            #endregion
            stream.Flush();
        }
        /// <summary>
        /// Get the filename of the interface's fixed-length index of objects.
        /// </summary>
        /// <param name="owningType"></param>
        /// <returns></returns>
        private static string GetDatabaseFilename(ObjectType owningType)
        {
            return owningType.BaseClass.Name + IndexExtension;
        }
        /// <summary>
        /// Get the filename of the class's variable-length data for objects.
        /// </summary>
        /// <param name="owningType"></param>
        /// <returns></returns>
        private static string GetDatabaseFilename(Metadata owningType)
        {
            return owningType.ClassType.Name + ClassExtension;
        }
        /// <summary>
        /// Get the filename of the class's cache of used space.
        /// </summary>
        /// <param name="owningType"></param>
        /// <returns></returns>
        private static string GetDatabaseSpaceFilename(Metadata owningType)
        {
            return owningType.ClassType.Name + UnusedSpaceExtension;
        }
        /// <summary>
        /// Gets the stream writing to PrestagedFile. May change the database state to report that changes are being staged.
        /// </summary>
        /// <param name="create">True if this should be allowed to start a new PreStagedFile. False if this should return null instead.</param>
        /// <param name="crashRecovery">True if the database is trying to recover from a crash; Ignores create and does not change databae state.</param>
        /// <returns>If a FileStream exists already, that FileStream. Else a new stream if it is allowed to create one, else null.</returns>
        public static FileStream GetPrestaged(bool create = true, bool crashRecovery = false)
        {
            if (PrestagedChanges == null && (create || crashRecovery))
            {
                if (crashRecovery)
                {
                    PrestagedChanges = File.Open(Path.Combine(DatabasePath, PreStagedFile), FileMode.Open);
                }
                else
                {
                    PrestagedChanges = File.Open(Path.Combine(DatabasePath, PreStagedFile), FileMode.Create);
                    SetDatabaseState(DatabaseState.SavingPrestaged);
                }
            }
            return PrestagedChanges;
        }
        /// <summary>
        /// Gets the stream writing to StagedFile. May change the database state to report that changes are being staged.
        /// </summary>
        /// <param name="create">True if this should be allowed to start a new StagedFile. False if this should return null instead.</param>
        /// <param name="crashRecovery">True if the database is trying to recover from a crash; Ignores create and does not change databae state.</param>
        /// <returns>If a FileStream exists already, that FileStream. Else a new stream if it is allowed to create one, else null.</returns>
        public static BinaryWriter GetCurrentStaged(bool create = true, bool crashRecovery = false)
        {
            if (CurrentStagedWriter == null && (create || crashRecovery))
            {
                if (crashRecovery)
                {
                    CurrentStagedChanges = File.Open(Path.Combine(DatabasePath, StagedFile), FileMode.Open);
                    //CurrentStagedWriter is not needed in this workflow, and CurrentStagedChanges will be set to null soon in crashrecovery workflow.
                }
                else
                {
                    CurrentStagedChanges = File.Open(Path.Combine(DatabasePath, StagedFile), FileMode.Create);
                    SetDatabaseState(DatabaseState.StagingChanges);
                    CurrentStagedWriter = new BinaryWriter(CurrentStagedChanges, Encoding.UTF8, true);
                }
            }
            return CurrentStagedWriter;
        }

        /// <summary>
        /// Moves all the data in StagedFile to the database files.
        /// </summary>
        public static void CommitCurrentStaged()
        {
            if (CurrentStagedChanges != null)
            {
                //Make sure all changes are finished first.
                EndStagedWrite(true);
                CurrentStagedChanges.Flush();

                //Start reading from the staged changes
                SetDatabaseState(DatabaseState.WritingToDatabase);
                CurrentStagedChanges.Seek(0, SeekOrigin.Begin);
                using (BinaryReader reader = new BinaryReader(CurrentStagedChanges, Encoding.UTF8, true))
                {
                    while (CurrentStagedChanges.Length > CurrentStagedChanges.Position)
                    {
                        //Get the next file and chunk of data to write
                        #region Read from staged.bin then write to various database files
                        string nextFile = reader.ReadString();
                        int nextSize = reader.ReadInt32();
                        using (FileStream writer = File.Open(Path.Combine(DatabasePath, nextFile), FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
                        {
                            //Keep writing to this file until there are no more chunks of data to write
                            while (CurrentStagedChanges.Length > CurrentStagedChanges.Position && nextSize > 0)
                            {
                                //Go to location for this chunk
                                int nextPosition = reader.ReadInt32();
                                writer.Seek(nextPosition, SeekOrigin.Begin);
                                //Copy data from one file to the next
                                byte[] data = reader.ReadBytes(nextSize);
                                writer.Write(data, 0, nextSize);
                                //See if there is a next chunk
                                nextSize = reader.ReadInt32();
                            }
                            if(nextSize < 0)
                            {
                                writer.SetLength(-nextSize - 1);
                            }
                            writer.Flush();
                        }
                        #endregion
                    }
                }
                //Writing to database is done. Clean up state of everything.
                if (CurrentStagedWriter != null)
                {
                    CurrentStagedWriter.Dispose();
                    CurrentStagedWriter = null;
                }
                CurrentStagedChanges.Dispose();
                CurrentStagedChanges = null;
                SetDatabaseState(DatabaseState.UpToDate);
            }
        }

        /// <summary>
        /// Get the fixed index data fileStream associated with an ObjectType. Only one such FileStream should exist at a time.
        /// </summary>
        /// <param name="objectType"></param>
        /// <returns></returns>
        public static FileStream GetFile(ObjectType objectType)
        {
            if (objectType.IndexFile == null)
            {
                lock (objectType)
                {
                    if (objectType.IndexFile == null)
                    {
                        objectType.IndexFile = File.Open(Path.Combine(DatabasePath, GetDatabaseFilename(objectType)), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                    }
                }
            }
            return objectType.IndexFile;
        }

        /// <summary>
        /// Get the variable data FileStream associated with an ISaveable class. Only one such FileStream should exist at a time.
        /// </summary>
        /// <param name="objectType"></param>
        /// <returns></returns>
        public static FileStream GetFile(ISaveable saveable)
        {
            return GetFile(saveable.GetMetadata());
        }
        /// <summary>
        /// Get the variable data fileStream associated with an ISaveable class. Only one such FileStream should exist at a time.
        /// </summary>
        /// <param name="objectType"></param>
        /// <returns></returns>
        public static FileStream GetFile(Metadata saveableType)
        {
            if (saveableType.DataFile == null)
            {
                lock (saveableType)
                {
                    if (saveableType.DataFile == null)
                    {
                        saveableType.DataFile = File.Open(Path.Combine(DatabasePath, GetDatabaseFilename(saveableType)), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                    }
                }
            }
            return saveableType.DataFile;
        }

        /// <summary>
        /// Ends the current data chunk being written to CurrentStagedChanges.
        /// </summary>
        /// <param name="endFile">If this is also the end of CurrentStagedFile</param>
        /// <param name="fileSize">Specify where the end of this file is when ending the write to this file. Ignored if -1</param>
        private static void EndStagedWrite(bool endFile, int fileSize = -1)
        {
            if (CurrentStagedFile != null)
            {
                #region Write sizes/end of files to staged.bin
                long oldPosition = CurrentStagedChanges.Position;
                long size = oldPosition - LastStagedSizeOffset - 8; //Calculate size of last chunk saved
                if (size > int.MaxValue)
                {
                    //This should never happen and is unlikely to ever happen on accident anyways.
                    //Loops that may save a lot of consecutive data should break it up by calling SetStageFile periodically (at least once per gigabyte).
                    throw new InvalidDataException("Too much data in one packet.");
                }
                CurrentStagedChanges.Seek(LastStagedSizeOffset, SeekOrigin.Begin);
                if (size > 0)
                {
                    //Save the size of the chunk of data, and return to the end of it so a next chunk can start.
                    CurrentStagedWriter.Write((int)size);
                    CurrentStagedChanges.Seek(oldPosition, SeekOrigin.Begin);
                }
                else
                {
                    //Delete the position, because it's invalid if size is 0 and will crash if not overwritten.
                    CurrentStagedChanges.SetLength(LastStagedSizeOffset);
                }
                if (endFile)
                {
                    //Mark what to do for the rest of this file. (0: Stop writing. -x: Crop file to size x-1)
                    int toWrite = IsOverwrite ? (int)(-(CurrentStagedStart + size) - 1) : 0; //If overwriting, end the file at the current position.
                    if(fileSize >= 0)
                    {
                        toWrite = (-fileSize - 1); //If fileSize is specified, end the file at fileSize instead.
                    }
                    CurrentStagedWriter.Write(toWrite);
                    IsOverwrite = false;
                    CurrentStagedFile = null;
                }
                #endregion
            }
        }
        /// <summary>
        /// Sole method to stage a new write to a file. Files should be written to all at once, and
        /// positions should be incrementing, if possible. Calling function does not need to track how much is written.
        /// </summary>
        /// <param name="file">File to stage writes to.</param>
        /// <param name="position">Position in the file to start writing at.</param>
        /// <param name="overwrite">If the entire file is being overwritten. Only valid for first write to a file, other times it is ignored.</param>
        /// <returns></returns>
        private static BinaryWriter SetStageFile(string file, int position = 0, bool overwrite = false)
        {
            BinaryWriter writer = GetCurrentStaged();
            if (CurrentStagedFile != file)
            {
                //We switched files. End the old file and start a new one.
                EndStagedWrite(true);
                CurrentStagedFile = file;
                #region Write next file to staged.bin
                writer.Write(file);
                #endregion
                IsOverwrite = overwrite;

            }
            else
            {
                //Same file. Check if we jumped to a new spot in it.
                long stagedPosition = CurrentStagedChanges.Position;
                long size = stagedPosition - LastStagedSizeOffset - 8; //Calculate the size of the chunk being saved so far.
                long currentTargetPosition = size + CurrentStagedStart; //Calculate the spot in the current file we're writing to. next.
                if (position != currentTargetPosition || size > Int32.MaxValue / 2)
                {
                    EndStagedWrite(false);
                }
                else
                {
                    //Skip position if it didn't change and we haven't saved too much already.
                    position = -1;
                }
            }
            if (position != -1)
            {
                //Save offset where we can write the size of the chunk we're saving to this file later.
                #region Write position to staged.bin
                LastStagedSizeOffset = CurrentStagedChanges.Position;
                writer.Write(0); //Size of chunk. Reserve space for writing a real int later.
                writer.Write(position); //Position of chunk in target file
                #endregion
                CurrentStagedStart = position;
            }
            return writer;
        }

        /// <summary>
        /// Save data to the Globals file for all generic MUD-state data.
        /// </summary>
        public static void StageGlobals()
        {
            #region Write everything to globals.bin
            BinaryWriter writer = SetStageFile(GlobalsFile, 0, true);
            writer.Write(NextClassID); //Currently just saves the next ID number for new class files/metadata objects.
            #endregion
        }

        /// <summary>
        /// Initialize the database. Reconciles current metadata with saved metadata, and current objecttypes with saved objecttypes.
        /// Sets up save files (if needed) and clean up from crashes (if needed).
        /// </summary>
        public static void Start()
        {
            //TODO: Maybe move/load this in configuration
            BasePath = Environment.CurrentDirectory;
            DatabasePath = Path.Combine(BasePath, DatabaseFolderName);
            Directory.CreateDirectory(DatabasePath); //Make sure the folder for the database exists.

            //Check database status
            FileStream main = GetMainFile();
            main.Seek(0, SeekOrigin.Begin);
            if (main.Length < 2)
            {
                //This is probably a new database. Start with blank and plan to save changes for metadata.
                main.Write(new byte[] { 0, 0 }, 0, 2);
                main.Flush();
            }
            else
            {
                #region Read main.bin
                //Database currently has two bytes: IsRunning flag and DatabaseState enum.
                int crashed = main.ReadByte(); //If the database was still supposed to be running, we are most likely recovering from a crash.
                if (crashed != 0)
                {
                    DatabaseState state = (DatabaseState)main.ReadByte();
                    //TODO Important: Handle other states.
                    //For prestaged to staged, need to generate dummy objects with correct savevalues/pointers to data, save, then clear out dummy objects.

                    if (state == DatabaseState.WritingToDatabase)
                    {
                        //The staged file was finished, but it did not finish writing chunks to files.
                        //Try again from start of copying staged changes to files.
                        GetCurrentStaged(false, true);
                        CommitCurrentStaged();
                    }
                }
                #endregion
            }

            //Load global information
            using (FileStream globals = File.Open(Path.Combine(DatabasePath, GlobalsFile), FileMode.OpenOrCreate))
            {
                //Verify that it's a full valid file
                if (globals.Length >= 4)
                {
                    using (BinaryReader reader = new BinaryReader(globals, Encoding.UTF8, true))
                    {
                        NextClassID = reader.ReadInt32();
                    }
                }
            }

            //Load index of object types
            List<ObjectType> NewObjectTypes;
            ushort oldNumObjectTypes = 0;
            int oldObjectTypesLength;
            using (FileStream objectTypes = File.Open(Path.Combine(DatabasePath, ObjectTypesFile), FileMode.OpenOrCreate))
            {
                List<string> savedTypes = new List<string>();
                #region Read from objectTypes.bin
                using (BinaryReader reader = new BinaryReader(objectTypes, Encoding.UTF8, true))
                {
                    oldObjectTypesLength = (int)objectTypes.Length;
                    if (objectTypes.Position < oldObjectTypesLength)
                    {
                        oldNumObjectTypes = reader.ReadUInt16();
                        for (ushort i = 0; i < oldNumObjectTypes; i++)
                        {
                            savedTypes.Add(reader.ReadString());
                        }
                    }
                }
                #endregion

                //Set up object types and see what new types have been found
                NewObjectTypes = ObjectType.SetObjectTypes(savedTypes); //This will be saved to the globals file later.
            }


            //Set up Field parsers for each ISaveable class, and related Metadata info for each class.
            List<Metadata> MetadataByID = new List<Metadata>();
            List<Type> NewClasses = new List<Type>(); //List of classes to later save to .map files, because they are new or have been modified
            Type[] classes = Assembly.GetExecutingAssembly().GetTypes();
            foreach (Type type in classes) if (!type.IsAbstract && typeof(ISaveable).IsAssignableFrom(type))
                { //Loop over all ISaveable classes
                    bool needSave = false;
                    //List of parsers that were known ahead of time
                    Dictionary<string, Dictionary<string, ushort>> savedParsers = new Dictionary<string, Dictionary<string, ushort>>();
                    int classID;
                    ushort numParsers = 0;
                    using (FileStream nextMap = File.Open(Path.Combine(DatabasePath, type.Name + MapExtension), FileMode.OpenOrCreate))
                    {
                        //Check if we have data on this ISaveable class already in the database
                        if (nextMap.Length > 0)
                        {
                            //Already in the database. Load the basic info and as much parser info as possible.
                            #region Read type.Name's .map file for parsers
                            using (BinaryReader reader = new BinaryReader(nextMap, Encoding.UTF8, true))
                            {
                                classID = reader.ReadInt32();
                                numParsers = reader.ReadUInt16();
                                while (nextMap.Length > nextMap.Position)
                                {
                                    string className = reader.ReadString();
                                    if (!savedParsers.ContainsKey(className))
                                        savedParsers[className] = new Dictionary<string, ushort>();
                                    Dictionary<string, ushort> classParserNames = savedParsers[className];
                                    string parserKey = reader.ReadString();
                                    ushort parserID = reader.ReadUInt16();
                                    classParserNames[parserKey] = parserID;
                                }
                            }
                            #endregion
                        }
                        else
                        {
                            //Not in the database. Assign a new ID and mark things as needing to save.
                            classID = NextClassID;
                            NextClassID++;
                            needSave = true;
                            GlobalsIsDirty = true;
                        }
                    }
                    //Generate parsers for this ISaveable class
                    List<SaveableParser> classParsers = new List<SaveableParser>();
                    List<SaveIDParser> saveIDParsers = new List<SaveIDParser>(); //Keep parsers that refer to a SaveID separate
                    FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Instance);
                    foreach (FieldInfo field in fields)
                    {
                        SaveableFieldAttribute attribute = (SaveableFieldAttribute)field.GetCustomAttribute(typeof(SaveableFieldAttribute));
                        if (attribute != null)
                        {
                            string owningClass = field.DeclaringType.Name;
                            string parserKey = attribute.Key;
                            ushort parserID;
                            if (savedParsers.ContainsKey(owningClass) && savedParsers[owningClass].ContainsKey(parserKey))
                            {
                                //Use the saved ID if this parser was saved to the database already
                                parserID = savedParsers[owningClass][parserKey];
                                savedParsers[owningClass].Remove(parserKey);
                            }
                            else
                            {
                                //Generate a new ID and mark this class as needing to be saved.
                                parserID = numParsers;
                                numParsers++;
                                needSave = true;
                            }
                            SaveableParser parser = attribute.Parser(field);
                            //parser.ClassID = classID;
                            parser.ParserID = parserID;
                            parser.Key = parserKey;
                            parser.owningType = field.DeclaringType;
                            SaveIDParser saveIDParser = parser as SaveIDParser;
                            if (saveIDParser != null) saveIDParsers.Add(saveIDParser);
                            classParsers.Add(parser);
                        }
                    }

                    // Preserve metadata for deleted parsers.
                    //NOTE: Data on objects for deleted parsers will NOT be currently saved,
                    //EmptySaveableParser would need to be changed to load/attach/save arbitrary binary data if that behavior is desired. 
                    foreach (var classKey in savedParsers)
                    {
                        foreach (var parserKey in classKey.Value)
                        {
                            SaveableParser parser = new EmptySaveableParser()
                            {
                                //ClassID = classID,
                                ParserID = parserKey.Value,
                                Key = parserKey.Key,
                                owningTypeName = classKey.Key
                            };
                            classParsers.Add(parser);
                            needSave = true;
                        }
                    }

                    Metadata meta = Metadata.LoadedClasses[type];
                    while (MetadataByID.Count <= classID)
                    {
                        MetadataByID.Add(null);
                    }
                    MetadataByID[classID] = meta;
                    meta.ClassID = classID;
                    //If there is only one SaveID for a class, then its interface index on loading is enough and we don't need to save SaveIDs
                    if (saveIDParsers.Count > 1)
                    {
                        //If there are multiple SaveIDs, any interface may load the object and all interfaces need all SaveIDs.
                        meta.SaveIDParsers = saveIDParsers.ToArray();
                    }
                    meta.ParserByID = new SaveableParser[numParsers];
                    foreach (SaveableParser parser in classParsers)
                    {
                        meta.ParserByID[parser.ParserID] = parser;
                    }
                    meta.NumberParsers = numParsers;
                    List<ObjectType> implementedTypes = new List<ObjectType>();
                    foreach (ObjectType owningType in ObjectType.TypeByID)
                    {
                        if (owningType != null && owningType.BaseClass.IsAssignableFrom(type))
                        {
                            implementedTypes.Add(owningType);
                        }
                    }
                    meta.ImplementedTypes = implementedTypes.ToArray();
                    if (needSave)
                    {
                        NewClasses.Add(type);
                    }
                }
            Metadata.ByClassID = MetadataByID.ToArray();
            //Runtime is up-to-date now. Check if there are things to save to the database.
            #region Write to main.bin that we are running and may be writing to files now.
            main.Seek(0, SeekOrigin.Begin);
            main.WriteByte(1);
            main.Flush();
            #endregion
            //If anything new was found, write the new parsers to associated map files (and anything else) immediately,
            //before anything might start using those mappings.
            if (GlobalsIsDirty || NewClasses.Count != 0 || NewObjectTypes.Count != 0)
            {
                if (GlobalsIsDirty) StageGlobals();
                foreach (Type type in NewClasses)
                {
                    #region Write everything to type.Name's .map file
                    BinaryWriter writer = SetStageFile(type.Name + MapExtension, 0, true);
                    Metadata data = Metadata.LoadedClasses[type];
                    writer.Write(data.ClassID);
                    writer.Write(data.NumberParsers);
                    foreach (SaveableParser parser in data.ParserByID)
                    {
                        if (parser != null) //&& !(parser is SaveIDParser))
                        {
                            writer.Write(parser.OwningTypeName);
                            writer.Write(parser.Key);
                            writer.Write(parser.ParserID);
                        }
                    }
                    #endregion
                }
                if (NewObjectTypes.Count != 0)
                {
                    #region Write to objectTypes.bin for total number of objects and any new objects
                    BinaryWriter writer = SetStageFile(ObjectTypesFile, 0);
                    oldNumObjectTypes += (ushort)NewObjectTypes.Count;
                    writer.Write(oldNumObjectTypes);
                    SetStageFile(ObjectTypesFile, Math.Max(oldObjectTypesLength, sizeof(ushort)));
                    foreach (ObjectType newOT in NewObjectTypes)
                    {
                        writer.Write(newOT.BaseClass.Name);
                    }
                    #endregion
                }
                CommitCurrentStaged();
            }
            //Database up to date, everything is ready for use.
            DatabaseThread = new System.Threading.Thread(Run);
            DatabaseThread.Start();
        }
        /// <summary>
        /// Main database loop
        /// </summary>
        private static void Run()
        {
            bool shuttingDown = false;
            while (!shuttingDown)
            {
                //Wait 10 minutes between each save pass. TODO: Make this configurable?
                shuttingDown = Program.ShutdownTrigger.WaitOne(1000 * 60 * 10);
                SaveAll();
            }
        }

        /// <summary>
        /// Tells the database to End and wait until it's finished.
        /// In practice just waits because the database is already told by ShutdownTrigger.
        /// </summary>
        public static void End()
        {
            DatabaseThread.Join();
        }

        /// <summary>
        /// Loads all objects of the requested object type from the database.
        /// </summary>
        /// <param name="type"></param>
        public static void LoadFullType(ObjectType type)
        {
            FileStream file = GetFile(type);
            byte[] fileData;
            lock (file)
            {
                file.Position = 0;
                fileData = new byte[file.Length];
                file.Read(fileData, 0, (int)file.Length);
            }
            int classID, dataSize;
            #region Reads the entirety of a type's .fix file and loads all objects
            using (BinaryReader reader = new BinaryReader(new MemoryStream(fileData)))
            {
                for (int ID = 0; ID * 0x10 < fileData.Length; ID++)
                {
                    SaveValues saveValues = new SaveValues();
                    classID = reader.ReadInt32();
                    saveValues.Offset = reader.ReadInt32();
                    dataSize = reader.ReadInt32();
                    saveValues.Capacity = reader.ReadInt32();
                    LoadSaveable(type, ID, classID, saveValues, dataSize);
                }
            }
            #endregion
        }
        /// <summary>
        /// Load a specific ID and object for a given type. Guaranteed to be found in the objecttype or not in the database afterwards.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="ID"></param>
        public static void LoadSaveable(ObjectType type, int ID)
        {
            FileStream file = GetFile(type);
            byte[] fileData = new byte[0x10];
            #region Read from type's .fix file for a specific object's index data by ID.
            lock (file)
            {
                file.Position = ID * 0x10;
                file.Read(fileData, 0, 0x10);
            }
            int classID, dataSize;
            SaveValues saveValues = new SaveValues();
            using (BinaryReader reader = new BinaryReader(new MemoryStream(fileData)))
            {
                classID = reader.ReadInt32();
                saveValues.Offset = reader.ReadInt32();
                dataSize = reader.ReadInt32();
                saveValues.Capacity = reader.ReadInt32();
            }
            #endregion
            LoadSaveable(type, ID, classID, saveValues, dataSize);
        }
        /// <summary>
        /// Load a specific ID and object from the given flat-index data.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="ID"></param>
        /// <param name="classID">If -1 or class has been deleted, returns null</param>
        /// <param name="saveValues"></param>
        /// <param name="dataSize"></param>
        private static void LoadSaveable(ObjectType type, int ID, int classID, SaveValues saveValues, int dataSize)
        {
            if (classID < 0)
            {
                //Object is deleted.
                type.Add(ObjectType.EmptySlot, ID);
                return;
            }
            Metadata data = Metadata.ByClassID[classID];
            if (data == null)
            {
                //Object is of invalid class.
                type.Add(ObjectType.UnreadableSlot, ID);
                return;
            }
            ISaveable newObject = Activator.CreateInstance(data.ClassType, false) as ISaveable;

            newObject.SetSaveID(type, ID);
            newObject.SaveValues = saveValues;
            bool setMainID = ID == newObject.GetSaveID(null); //See if this was the main ID.
            if (setMainID && !TrySetLoadingObject(type, newObject, ID)) return;

            #region Read from data type's .var file (just copy data from file to RAM, parsing in LoadOneParser)
            byte[] fileData = new byte[dataSize];
            FileStream file = GetFile(data);
            lock (file)
            {
                file.Position = saveValues.Offset;
                file.Read(fileData, 0, dataSize);
            }
            #endregion
            MemoryStream stream = new MemoryStream(fileData, 0, fileData.Length, false, true);
            using (BinaryReader reader = new BinaryReader(stream))
            {
                ArraySegmentStream array = new ArraySegmentStream();
                BinaryReader parserReader = new BinaryReader(array);
                while (!setMainID && stream.Position < stream.Length)
                { //We haven't loaded the main ID for this yet. Keep loading properties until we find a main ID.
                    SaveableParser parser = LoadOneParser(array, parserReader, stream, reader, newObject, data);
                    if (parser == null) continue;
                    if (!(parser is SaveIDParser)) break;
                    setMainID = ID != newObject.GetSaveID(null);
                    //If we've loaded the main ID, try to set it. If another thread has already claimed responsibility for this ID, abort loading.
                    if (setMainID && !TrySetLoadingObject(type, newObject, ID)) return;
                }
                //NOTE: Bad things likely to happen if main ID isn't loaded. Can happen if code is modified to implement more
                //interfaces and the default is one of the new interfaces.
                if (!setMainID) throw new Exception("Error on loading object, main ID not found");
                //We've loaded the main ID, now finish loading everything else that might be here.
                while (stream.Position < stream.Length)
                    LoadOneParser(array, parserReader, stream, reader, newObject, data);
            }
            saveValues.LoadingIndicator.Set();
            saveValues.LoadingIndicator = null;
        }

        /// <summary>
        /// Attempts to claim responsibility for loading this object from the database and add it to the objecttype's index.
        /// </summary>
        /// <param name="type">ObjectType that will index this object.</param>
        /// <param name="newObject">New object to get the loading lock for.</param>
        /// <param name="ID">ID of the new object.</param>
        /// <returns>True if this thread has the responsibility for loading the object. False if another thread already has responsibility.</returns>
        private static bool TrySetLoadingObject(ObjectType type, ISaveable newObject, int ID)
        {
            lock (type)
            {
                if (type.Get(ID, false, false) == null)
                {
                    //Indicate this is being loaded, then add it to the dictionary so other things can find it.
                    newObject.SaveValues.LoadingIndicator = new System.Threading.ManualResetEvent(false);
                    type.Add(newObject, false);
                    return true;
                }
            }
            //Something else is already loading this, stop.
            return false;
        }

        /// <summary>
        /// Saves all metadata to the prestaged file.
        /// </summary>
        /// <returns>Dictionary of start of SaveValues lists</returns>
        private static Dictionary<Metadata, ISaveable> SaveToPrestaged()
        {
            //Metadatas and the first file being saved for each one
            Dictionary<Metadata, ISaveable> prestagedLists = new Dictionary<Metadata, ISaveable>();
            FileStream prestagedStream = GetPrestaged();
            BinaryWriter writer = new BinaryWriter(prestagedStream, Encoding.UTF8, true);
            MemoryStream parserStream = new MemoryStream(8192); //Buffer/writer for an individual parser
            BinaryWriter parserWriter = new BinaryWriter(parserStream);
            MemoryStream objectStream = new MemoryStream(8192); //Buffer/writer for an entire object
            BinaryWriter objectWriter = new BinaryWriter(objectStream);
            //Do one pass saving all the objects requested. Pause the MUD, then do a second pass saving all the new objects requested.
            //TODO: There's a weird case where objects are created after this first pass, then saved in the second pass, but aren't
            //assigned an ID until the save pass at which point they are saved during the second pass. I think this is okay, but it's
            //worth considering more carefully.
            SaveToPrestagedPass(prestagedLists, prestagedStream, writer, parserStream, parserWriter, objectStream, objectWriter);
            using (ThreadManager.PauseMUD(true))
            {
                SaveToPrestagedPass(prestagedLists, prestagedStream, writer, parserStream, parserWriter, objectStream, objectWriter);
                SetDatabaseState(DatabaseState.StagingChanges); //Data is all prestaged. Now it's safe to assume it's okay and start pushing to staged changes.
            }
            writer.Dispose();
            parserWriter.Dispose();
            objectWriter.Dispose();
            return prestagedLists;
        }
        /// <summary>
        /// Helper function for SaveToPrestaged. Does a single pass on all items.
        /// </summary>
        /// <param name="prestagedLists"></param>
        /// <param name="prestagedStream"></param>
        /// <param name="writer"></param>
        /// <param name="parserStream"></param>
        /// <param name="parserWriter"></param>
        /// <param name="objectStream"></param>
        /// <param name="objectWriter"></param>
        private static void SaveToPrestagedPass(Dictionary<Metadata, ISaveable> prestagedLists, FileStream prestagedStream, BinaryWriter writer, MemoryStream parserStream, BinaryWriter parserWriter, MemoryStream objectStream, BinaryWriter objectWriter)
        {
            //Search for objects to save, one type at a time.
            foreach (Metadata data in Metadata.ByClassID) if (data != null)
                {
                    //Next object of this class to save from RAM to prestaged list.
                    ISaveable nextObject = data.ResetNextToSave();
                    if (nextObject == Metadata.EndOfList) continue; //Skip metadata if nothing in it to save.

                    ObjectType[] relevantTypes = data.ImplementedTypes;

                    #region Save to prestaged.bin - First metadata
                    //Save metadata info
                    writer.Write(data.ClassID);
                    writer.Write((byte)relevantTypes.Length);
                    foreach (ObjectType objType in relevantTypes)
                    {
                        writer.Write((ushort)objType.ID);
                    }
                    ISaveable previousObject;
                    if (!prestagedLists.TryGetValue(data, out previousObject))
                    {
                        previousObject = Metadata.EndOfList; //Mark the end of the list
                    }
                    //else resume adding to the previous start of the list.
                    while (nextObject != Metadata.EndOfList)
                    {
                        //List iteration of objects to save
                        ISaveable thisObject = nextObject;
                        SaveValues saveData = thisObject.SaveValues;
                        nextObject = saveData.NextObjectToSave;
                        saveData.NextObjectToSave = null;

                        //Add saveData to the 'list' of SaveValues/prestaged data to save.
                        if (saveData.NextStagedValues == null)
                        {
                            saveData.NextStagedValues = previousObject;
                            previousObject = thisObject;
                        }
                        saveData.StagedOffset = (int)prestagedStream.Position;
                        #region Save to prestaged.bin - Individual object data
                        writer.Write(saveData.Offset); //Save where data WAS stored before
                        writer.Write(saveData.Capacity);
                        for (int i = 0; i < relevantTypes.Length; i++)
                            writer.Write(thisObject.GetSetSaveID(relevantTypes[i]));

                        if (saveData.Deleted)
                        {
                            writer.Write(-1); //Mark this object to be removed.
                        }
                        else
                        {
                            //Save data to the temporary object buffer first
                            objectStream.Position = 0;
                            if (data.SaveIDParsers != null) foreach (SaveableParser parser in data.SaveIDParsers)
                                {
                                    SaveParserToBuffer(parserStream, parserWriter, objectStream, objectWriter, thisObject, parser);
                                }
                            foreach (SaveableParser parser in data.ParserByID)
                            {
                                if (parser != null && !(parser is SaveIDParser))
                                {
                                    SaveParserToBuffer(parserStream, parserWriter, objectStream, objectWriter, thisObject, parser);
                                }
                            }
                            //Save data to the prestaged file now
                            writer.Write((int)objectStream.Position);
                            prestagedStream.Write(objectStream.GetBuffer(), 0, (int)objectStream.Position);
                        }
                        #endregion
                    }
                    prestagedLists[data] = previousObject; //Save the start of the list 
                    writer.Write(-1); //End of metadata
                    #endregion
                }
        }

        /// <summary>
        /// Saves all objects to the database.
        /// </summary>
        private static void SaveAll()
        {
            //First save everything to the prestaged file. When that is done, we will have lists of objects that have data in the
            //prestaged file that need to be mapped and moved to the var files.
            Dictionary<Metadata, ISaveable> prestagedLists = SaveToPrestaged();

            FileStream prestagedStream = GetPrestaged();
            BinaryReader prestagedReader = new BinaryReader(prestagedStream, Encoding.UTF8, true);
            BinaryWriter writer = GetCurrentStaged();
            //Prestaged may have duplicates / data in a confusing order because of multiple passes, and crash recovery data.
            //Iterate by metadata type instead, which will point directly to the latest instance of data for objects.
            foreach (KeyValuePair<Metadata, ISaveable> kvp in prestagedLists)
            {
                //kvp.Value can be reused several times to access the start of the list.
                Metadata data = kvp.Key;
                ///List of offsets and free space.
                ///No key: Unknown, assumably middle of used space.
                ///Negative value: Start of used space (or end of file). Value + Key = Previous start of free space.
                ///0: Middle of free space, already processed.
                ///Positive value: Start of free space. Value = length of free space.
                Dictionary<int, int> freeSpace = new Dictionary<int, int>();
                int endOfFile = 0; //No data for the file starting at this spot
                string freeSpaceFilePath = Path.Combine(DatabasePath, GetDatabaseSpaceFilename(data));
                if (File.Exists(freeSpaceFilePath))
                {
                    #region Read type's .spc data
                    FileStream stream = File.Open(freeSpaceFilePath, FileMode.Open);

                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        if (stream.Length >= 8)
                        {
                            int numFreeSpaces = reader.ReadInt32();
                            while (numFreeSpaces >= 1)
                            {
                                int nextSpace = reader.ReadInt32();
                                int nextSize = reader.ReadInt32();
                                freeSpace[nextSpace] = nextSize;
                                freeSpace[nextSpace + nextSize] = -nextSize;
                                numFreeSpaces--;
                            }
                            endOfFile = reader.ReadInt32();
                        }
                    }
                    #endregion
                }
                ISaveable nextObject = kvp.Value;
                //First pass: Update free space, remove entries that are being saved/deleted now
                while (nextObject != Metadata.EndOfList)
                {
                    SaveValues nextValues = nextObject.SaveValues;
                    nextObject = nextValues.NextStagedValues;
                    if (nextValues.Capacity <= 0) continue; //Skip data that wasn't already in the database.
                    int oldSpace;
                    int currentOffset = nextValues.Offset;
                    //Set up or combine the start of free space for this data.
                    if (freeSpace.TryGetValue(currentOffset, out oldSpace))
                    {
                        //if (oldSpace >= 0) continue; //Already removed, skip. Actually this should be impossible, should always be touching.
                        //Touching a previous value, oldSpace is negative and points to a start of free space.
                        currentOffset += oldSpace; //Update the offset to the start of free space containing this space.
                        freeSpace.Remove(nextValues.Offset); //Remove this pointer because it's in the middle of free space now.
                        freeSpace[currentOffset] += nextValues.Capacity; //Update the old free space's size.
                    }
                    else
                    {
                        //Setting up a new free space block. Only need to insert the new space's position/size.
                        freeSpace[currentOffset] = nextValues.Capacity;
                    }
                    int nextOffset = nextValues.Offset + nextValues.Capacity;
                    //Set up or combine the end of free space for this data.
                    if (freeSpace.TryGetValue(nextOffset, out oldSpace))
                    {
                        //safe to assume it's positive. No other values make sense.
                        freeSpace[currentOffset] += oldSpace; //Combine next chunk of free space's size to the current chunk.
                        freeSpace[nextOffset + oldSpace] = currentOffset - (nextOffset + oldSpace); //Replace the next chunk's pointer to its start to point to point to the current chunk's start.
                        freeSpace.Remove(nextOffset); //Remove the next chunk's start of free space because it's in the middle of free space now.
                    }
                    else
                    {
                        //Just insert a pointer to the current chunk's start of free space.
                        freeSpace[nextOffset] = currentOffset - nextOffset;
                    }
                }
                //Sort the free space
                List<int> sortedOffsets = new List<int>();
                foreach (KeyValuePair<int, int> intkvp in freeSpace)
                {
                    if (intkvp.Value <= 0) continue;
                    sortedOffsets.Add(intkvp.Key);
                }
                sortedOffsets.Sort();
                /// Last index before endOfFile.
                int lastFreeSpace = sortedOffsets.Count;
                List<int> sortedSizes = new List<int>(lastFreeSpace);
                foreach (int key in sortedOffsets)
                {
                    sortedSizes.Add(freeSpace[key]);
                }
                //freeSpace is done / collectable after this, just the arrays are used now.
                //Data that is saved will try to consume the free spaces available, although the Lists won't be resized.
                /// First index that hasn't been used up yet.
                int firstFreeSpace = 0;
                //Check if we've removed the last used space and the end of file has moved.
                if (lastFreeSpace > 0 && endOfFile == sortedOffsets[lastFreeSpace - 1] + sortedSizes[lastFreeSpace - 1])
                {
                    //Move the end of file and ignore the last free space chunk, because it's past the end of file now.
                    endOfFile -= sortedSizes[lastFreeSpace - 1];
                    lastFreeSpace--;
                }
                string filename = GetDatabaseFilename(data);
                nextObject = kvp.Value;
                ///Size of data to help support crash recovery. This data is not needed for writing to the staged file.
                int skip = 8; //Skip file offset and capacity
                skip += 4 * data.ImplementedTypes.Length; //Skip interfaces
                List<int> sizes = new List<int>(); //List of sizes for each object, in same order as the linked list of objects.
                //Second pass: Assign offsets/sizes/capacities and write to staged .var
                while (nextObject != Metadata.EndOfList)
                {
                    SaveValues nextValues = nextObject.SaveValues;
                    nextObject = nextValues.NextStagedValues;
                    #region Read object data from prestaged.bin. This will eventually be saved to .var data
                    prestagedStream.Position = nextValues.StagedOffset + skip; //Skip unneeded parts, jump to size of new data
                    int size = prestagedReader.ReadInt32();
                    sizes.Add(size);
                    if (size == 0)
                    { //No data to save to database. Update SaveValues to report 0. Don't modify deleted objects in case they had capacity -1 (never been saved)
                        nextValues.Capacity = 0;
                        nextValues.Offset = 0;
                    }
                    if (size <= 0) continue; //Deleted objects and 0 size objects don't need to write to .var files so stop here.
                    byte[] objectData = prestagedReader.ReadBytes(size);
                    #endregion

                    //Find an offset to save this object to
                    int saveOffset, capacity;
                    for (int i = firstFreeSpace; i < lastFreeSpace; i++)
                    {
                        if (sortedSizes[i] == 0 && firstFreeSpace == i)
                        { //First space is actually empty. Skip it and don't count it in the list of free spaces in the future.
                            firstFreeSpace++;
                            continue;
                        }
                        if (sortedSizes[i] <= size)
                        {
                            saveOffset = sortedOffsets[i]; //It fits here. Save the data to this location.
                            if (sortedSizes[i] < 2 * size)
                            { //There's not much room left and another object from this class might not fit. Just associate all the space here.
                                capacity = sortedSizes[i];
                                sortedSizes[i] = 0;
                            }
                            else
                            { //There's plenty of space at this spot. Only claim what this object needs.
                                capacity = size;
                                sortedSizes[i] -= capacity;
                            }
                            sortedOffsets[i] += capacity; //Move the pointer ahead now that previous space has been claimed.
                            goto foundOffset;
                        }
                    }
                    //Didn't find an offset in the for loop. Save to the end of the file instead.
                    capacity = size;
                    saveOffset = endOfFile;
                    endOfFile += capacity;

                foundOffset:
                    //Update the object so the next step can save it to the correct spot
                    nextValues.Capacity = capacity;
                    nextValues.Offset = saveOffset;
                    #region Save to staged.bin to later write to .var file for this object
                    SetStageFile(filename, saveOffset).Write(objectData);
                    #endregion
                }
                //Third+ passes: Write to each staged .fix. Goes through by interface to organize writes so each file is only written once in a large chunk.
                foreach (ObjectType type in data.ImplementedTypes)
                { //Go through each associated interface / .fix file for this type
                    nextObject = kvp.Value;
                    string fileName = GetDatabaseFilename(type);
                    int sizeIndex = 0;
                    while (nextObject != Metadata.EndOfList)
                    {
                        SaveValues thisValues = nextObject.SaveValues;
                        ISaveable thisObject = nextObject;
                        nextObject = thisValues.NextStagedValues;
                        int id = data.ClassID;
                        int offset = thisValues.Offset;
                        int size = sizes[sizeIndex];
                        sizeIndex++;
                        int capacity = thisValues.Capacity;
                        if (thisValues.Deleted)
                        {
                            if (capacity == -1) continue; //If deleted and never saved, nothing in the database to modify.
                            id = -1;
                            offset = thisValues.Offset;
                        }
                        #region Write to staged.bin file to write to .fix file later
                        int index = thisObject.GetSetSaveID(type);
                        SetStageFile(fileName, index * 0x10);
                        writer.Write(id);
                        writer.Write(offset);
                        writer.Write(size);
                        writer.Write(capacity);
                        #endregion
                    }
                }
                #region Write to staged.bin for a particular type's .spc file
                SetStageFile(GetDatabaseSpaceFilename(data), 0, true);
                int numberOfSpaces = 0;
                for (int i = firstFreeSpace; i < lastFreeSpace; i++) if (sortedSizes[i] != 0) numberOfSpaces++; //Skip (and don't count) regions that have no space.
                writer.Write(numberOfSpaces);
                for (int i = firstFreeSpace; i < lastFreeSpace; i++) if (sortedSizes[i] != 0)
                    {
                        writer.Write(sortedOffsets[i]);
                        writer.Write(sortedSizes[i]);
                    }
                writer.Write(endOfFile);
                #endregion
            }
            //All the data has been saved to staged.bin now. Stop reading from prestaged.bin, clean up resources, and continue to next step of saving data.
            prestagedReader.Dispose();
            //EndStagedWrite(true);
            CommitCurrentStaged();
        }

        /// <summary>
        /// Save data for an object for a single parser to a temporary buffer.
        /// </summary>
        /// <param name="parserStream">Reused stream, temporary buffer for the parser</param>
        /// <param name="parserWriter">Reused writer, temporary buffer for the parser</param>
        /// <param name="objectStream">Reused stream, temporary buffer for the whole object</param>
        /// <param name="objectWriter">Reused writer, temporary buffer for the whole object</param>
        /// <param name="thisObject">Object being saved</param>
        /// <param name="parser">Parser saving data from the object</param>
        private static void SaveParserToBuffer(MemoryStream parserStream, BinaryWriter parserWriter, MemoryStream objectStream, BinaryWriter objectWriter, ISaveable thisObject, SaveableParser parser)
        {
            parserStream.Position = 0; //Reset the temporary parser buffer
            parser.Get(thisObject, parserWriter); //Load the data from the object to the temporary parser buffer //TODO: Try/Catch?
            int length = (int)parserStream.Position; //Check how much data was saved by the parser
            if (length > 0) //Skip it if the parser saved no data.
            { //Otherwise, save the parser ID and associated data for this object/parser to the object's buffer.
                SaveIntToStream(objectWriter, parser.ParserID);
                SaveIntToStream(objectWriter, length);
                objectStream.Write(parserStream.GetBuffer(), 0, length);
            }
        }

        /// <summary>
        /// Save an entire subobject for a property to a stream.
        /// </summary>
        /// <param name="child">Subobject to save</param>
        /// <param name="writer">Stream to save subobject to</param>
        public static void SaveSubobject(ISaveable child, BinaryWriter writer)
        {

            Metadata typeData = child.GetMetadata();
            writer.Write(typeData.ClassID);

            MemoryStream parserStream = new MemoryStream(8192); //Buffer/writer for an individual parser
            BinaryWriter parserWriter = new BinaryWriter(parserStream);
            MemoryStream objectStream = writer.BaseStream as MemoryStream;

            foreach (SaveableParser parser in typeData.ParserByID)
            {
                if (parser != null && !(parser is SaveIDParser))
                {
                    SaveParserToBuffer(parserStream, parserWriter, objectStream, writer, child, parser);
                }
            }
        }
        /// <summary>
        /// Load an entire subobject for a property from a stream. Note the stream must not have extra data after the subobject data.
        /// </summary>
        /// <param name="reader">All the data for the subobject, and stopping immediately after the data for the subobject. If
        /// there is additional data in this stream, a smaller stream/reader should be created from it that only contains the
        /// data for the subobject before calling this function.
        /// </param>
        /// <returns>Loaded subobject</returns>
        public static ISaveable LoadSubobject(BinaryReader reader)
        {
            int classID = reader.ReadInt32();
            Metadata data = Metadata.ByClassID[classID];
            ISaveable newObject = Activator.CreateInstance(data.ClassType, false) as ISaveable;

            MemoryStream stream = reader.BaseStream as MemoryStream;
            ArraySegmentStream array = new ArraySegmentStream();
            BinaryReader parserReader = new BinaryReader(array);
            while (stream.Position < stream.Length) //The end of the subobject is also the end of the 
                LoadOneParser(array, parserReader, stream, reader, newObject, data);

            return newObject;
        }

        /// <summary>
        /// Saves a positive integer to a BinaryWriter. Values from 0 to 2^28-1 are supported.
        /// </summary>
        /// <param name="output"></param>
        /// <param name="value"></param>
        private static void SaveIntToStream(BinaryWriter output, int value)
        {
            int compressedLength = value;
            while (compressedLength > 0x7F)
            {
                output.Write((byte)((byte)(compressedLength & 0x7F) | 0x80));
                compressedLength >>= 7;
            }
            output.Write((byte)(compressedLength & 0x7F));
        }
        /// <summary>
        /// Loads a positive integer from a stream. Values from 0 to 2^28-1 are supported.
        /// </summary>
        /// <param name="stream">Stream to load an int from</param>
        /// <returns>A positive int</returns>
        private static int LoadIntFromStream(Stream stream)
        {
            int length = 0;
            byte compressed;
            int mult = 1;
            do
            {
                if (mult == 0) throw new Exception("Invalid compressed int");
                compressed = (byte)stream.ReadByte();
                length += mult * (compressed & 0x7F);
                mult *= 0x80;
            }
            while ((compressed & 0x80) != 0);
            return length;
        }

        //private static void LoadParsersFromBuffer(ArraySegmentStream parserStream, BinaryReader parserReader, MemoryStream objectStream, BinaryReader objectReader, ISaveable thisObject, Metadata meta)
        //{
        //    byte[] objectData = objectStream.GetBuffer();
        //    while(objectStream.Length > objectStream.Position)
        //    {
        //        ushort parserID = objectReader.ReadUInt16();
        //        SaveableParser parser = meta.ParserByID[parserID];
        //        int length = LoadIntFromStream(objectStream);
        //        parserStream.Buffer = new ArraySegment<byte>(objectData, (int)objectStream.Position, length);
        //        parserStream.Position = 0;
        //        if(parser != null)
        //        {
        //            parser.Set(thisObject, parserReader);
        //        }
        //        objectStream.Position += length;
        //    }
        //}
        /// <summary>
        /// Load data from a temporary buffer into the object. Tries to find and return a valid parser for the data at the buffer's current position.
        /// </summary>
        /// <param name="parserStream">Reused stream to back individual parsers.</param>
        /// <param name="parserReader">Reused reader for individual parsers.</param>
        /// <param name="objectStream">Memory stream that contains all the object data.</param>
        /// <param name="objectReader">Reader for all object data.</param>
        /// <param name="thisObject">Object that is being loaded.</param>
        /// <param name="meta">Metadata for the object's type.</param>
        /// <returns>The next parser loaded from the object stream, or null if no object </returns>
        private static SaveableParser LoadOneParser(ArraySegmentStream parserStream, BinaryReader parserReader, MemoryStream objectStream, BinaryReader objectReader, ISaveable thisObject, Metadata meta)
        {
            #region Read from data type's .var file (data is already copied from file to RAM, parsing here)
            int parserID = LoadIntFromStream(objectStream);
            int length = LoadIntFromStream(objectStream);

            SaveableParser parser = meta.ParserByID[parserID];
            if (parser != null)
            { //'Copy' the data for this specific parser from the object stream to the parser stream
                parserStream.Buffer = new ArraySegment<byte>(objectStream.GetBuffer(), (int)objectStream.Position, length);
                parserStream.Position = 0;
                parser.Set(thisObject, parserReader); //Load the data with the parser
            }

            objectStream.Position += length; //This data has been used (or skipped), so move the object's pointer past this parser's data.
            #endregion
            return parser;
        }
    }

    public static partial class Extensions
    {

        private static SaveValues GetCreateSaveValues(ISaveable s, bool newSave = false)
        {
            //TODO: Not really sure about thread safety. I can't think of any reason why this would need to be thread safe but I'm not 100% sure.
            SaveValues save = s.SaveValues;
            if (save == null)
            {
                s.SaveValues = save = new SaveValues();
                save.Capacity = -1; //Mark as never saved
            }
            return save;
        }

        /// <summary>
        /// Mark an object as necessary to save to the database.
        /// </summary>
        /// <param name="s">Object to save</param>
        public static void Save(this ISaveable s, bool newSave = false)
        {
            SaveValues save = GetCreateSaveValues(s, newSave);

            if (save.NextObjectToSave != null)
                return;
            s.GetMetadata().AddNextToSave(s);
        }

        /// <summary>
        /// Mark an object as deleted, and eventually remove it from the database.
        /// </summary>
        /// <param name="s"></param>
        public static void Delete(this ISaveable s)
        {
            SaveValues save = GetCreateSaveValues(s);
            save.Deleted = true;
            Save(s);
            Metadata meta = s.GetMetadata();
        }

        /// <summary>
        /// Get an ID from a saveable object. If it hasn't been marked with an ID yet somehow, make sure it has an ID first.
        /// </summary>
        /// <param name="saveable"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static int GetSetSaveID(this ISaveable saveable, ObjectType type)
        {
            int id = saveable.GetSaveID(type);
            if(id == -1)
            {
                type.Add(saveable);
            }
            return saveable.GetSaveID(type);
        }
    }
}
