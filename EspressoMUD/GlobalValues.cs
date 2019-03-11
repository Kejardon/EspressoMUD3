using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD
{
    /// <summary>
    /// This is basically a static class, but sometimes an instance is made to represent that a function is working with
    /// 'GlobalValues' right now.
    /// </summary>
    public class GlobalValues : IModifiable
    {
        /// <summary>
        /// This is only meant to be used by this 
        /// </summary>
        public static bool GlobalsIsDirty = false;

        /// Fields only used during loading and a function to finish loading those values into actual values.
        #region Temporary loading values
        public static int defaultStartingRoomID = -1;

        public static void FinishLoading()
        {
            if (defaultStartingRoomID >= 0)
                defaultStartingRoom = ObjectType.TypeByClass[typeof(Room)].Get(defaultStartingRoomID, true, true) as Room;
        }
        #endregion
        [ModifyField]
        private static Room defaultStartingRoom;
        /// <summary>
        /// The current room where new characters are placed if they have no other location to go to.
        /// Setter shouldn't be used much - calling function should have a lock on the MUD before setting it.
        /// </summary>
        public static Room DefaultStartingRoom
        {
            get
            {
                return defaultStartingRoom;
            }
            set
            {
                if (value != defaultStartingRoom)
                {
                    GlobalsIsDirty = true;
                    defaultStartingRoom = value;
                }
            }
        }

        //private class ModifyStartingRoomAttribute : ModifiableFieldAttribute
        //{
        //    public override ModifiableParser Parser(FieldInfo field)
        //    {
        //        return new ModifyStartingRoomParser(field, DefaultName, DefaultDescription);
        //    }
        //}
        //private class ModifyStartingRoomParser : ModifyRoomFieldParser
        //{
        //    public ModifyStartingRoomParser(FieldInfo field, string defaultName, string defaultDesc) : base(field, defaultName, defaultDesc)
        //    {
        //        FieldGetter = GenerateGetter<int>(field);
        //        FieldSetter = GenerateSetter<int>(field);
        //        Getter = (IModifiable obj) =>
        //        {
        //            int value = FieldGetter(null);
        //            if (value >= 0)
        //                return ObjectType.Get(value, true, true) as Room;
        //            return null;
        //        };
        //        Setter = (IModifiable obj, Room r) =>
        //        {
        //            int roomId = -1;
        //            if (r != null) roomId = r.GetSetSaveID(ObjectType.TypeByClass[typeof(Room)]);
        //            FieldSetter(null, roomId);
        //        };
        //    }

        //    //public override ObjectType ObjectType() { return null; }
        //    //public override bool CanOverwrite { get { return true; } }
        //    public override bool CanBeNull { get { return false; } }
        //    //public override ObjectType ObjectType { get; } //parent already handles this
        //    public override ISaveable SubObject(IModifiable source) { return Getter(source); }
        //    //public override bool ModifyAsList { get { return false; } }

        //    //public override string GetValue(IModifiable source)
        //    //{
        //    //    return Getter(source) + "";
        //    //}

        //    //public override bool SetValue(IModifiable source, string input, out string validationError)
        //    //{
        //    //    int value;
        //    //    if (int.TryParse(input, out value))
        //    //    {
        //    //        Setter(source, value);
        //    //        validationError = null;
        //    //        return true;
        //    //    }
        //    //    validationError = "Not a valid number.";
        //    //    return false;
        //    //}

        //    private Func<IModifiable, int> FieldGetter;
        //    private Action<IModifiable, int> FieldSetter;
        //}
    }
}
