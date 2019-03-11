using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD
{
    public abstract class ModifiableFieldAttribute : Attribute
    {
        protected ModifiableFieldAttribute() { }

        public abstract ModifiableParser Parser(FieldInfo field);

        /// <summary>
        /// Name must be unique (case insensitive) for all fields, and should not be 'quit' either.
        /// By default the field's name will be used.
        /// </summary>
        public string DefaultName;
        public string DefaultDescription;
    }

    public abstract class ModifiableParser
    {
        public ModifiableParser(FieldInfo field, string defaultName = null, string defaultDesc = null)
        {
            DefaultName = defaultName ?? field.Name;
            DefaultDescription = defaultDesc;
        }

        private string DefaultName;
        public string Name(IModifiable source)
        {
            return CustomName(source) ?? DefaultName;
        }
        private string DefaultDescription;
        public string Description(IModifiable source)
        {
            return CustomDescription(source) ?? DefaultDescription;
        }

        public virtual string CustomName(IModifiable source) { return null; }
        public virtual string CustomDescription(IModifiable source) { return null; }


        /// <summary>
        /// True if the user may specify a completely new value/object. Otherwise, user can only modify it as a subobject;
        /// if this returns false then SubObject must return an object.
        /// </summary>
        public virtual bool CanOverwrite { get { return true; } }

        /// <summary>
        /// True if the value may be set to null. False if there must be a non-null value.
        /// </summary>
        public virtual bool CanBeNull { get { return true; } }
        /// <summary>
        /// Returns an ObjectType if the user may specify an ID from that object type to put here.
        /// Returns null if the user can not.
        /// </summary>
        //actually rely on CanOverwrite instead of next comment? //This also implicitly allows the user to create a new object of that ObjectType.
        public virtual ObjectType ObjectType { get { return null; } }
        /// <summary>
        /// Getter when a subobject can be modified.
        /// </summary>
        public virtual ISaveable SubObject(IModifiable source) { return null; }
        //public virtual bool CanModifyAsSubobject() //Actually this should just be detected, if there is an object and it is ISaveable.
        /// <summary>
        /// Flag for list types. Allows user to add/remove/modify objects in a list. TODO: Not implemented yet.
        /// </summary>
        public virtual bool ModifyAsList { get { return false; } }
        /// <summary>
        /// Instructions the user may view when modifying this field.
        /// </summary>
        public virtual string Instructions { get { return null; } }


        public abstract string GetValue(IModifiable source);
        //public abstract bool ValidateInput(ISaveable source, string input); //TODO: Combine this into SetValue?
        /// <summary>
        /// Attempt to set the value using the input string, or delete the value if input is null.
        /// </summary>
        /// <param name="source">Object to modify the field on.</param>
        /// <param name="input">Unparsed value to set the field to.</param>
        /// <param name="validationError">If this fails, validationError can tell why setting the value failed.</param>
        /// <returns>True if the value was updated, else false.</returns>
        public abstract bool SetValue(IModifiable source, string input, out string validationError);



        protected static Action<IModifiable, T> GenerateStaticSetter<T>(FieldInfo field)
        {
            if (typeof(T) != field.FieldType) return null;
            Type baseType = field.DeclaringType;

            ParameterExpression paramOne = Expression.Parameter(typeof(IModifiable), "argOne");
            ParameterExpression paramTwo = Expression.Parameter(typeof(T), "argTwo");
            Action<IModifiable, T> del = Expression.Lambda<Action<IModifiable, T>>(
                Expression.Assign(Expression.MakeMemberAccess(null, field), paramTwo),
                new ParameterExpression[] { paramOne, paramTwo }).Compile();
            return del;
        }
        protected static Func<IModifiable, T> GenerateStaticGetter<T>(FieldInfo field)
        {
            if (typeof(T) != field.FieldType) return null;
            Type baseType = field.DeclaringType;

            ParameterExpression paramOne = Expression.Parameter(typeof(IModifiable), "argOne");
            //ParameterExpression paramTwo = Expression.Parameter(typeof(T), "result");
            Func<IModifiable, T> del = Expression.Lambda<Func<IModifiable, T>>(
                Expression.MakeMemberAccess(null, field),
                new ParameterExpression[] { paramOne }).Compile();
            return del;
        }
        protected static Action<IModifiable, T> GenerateInstanceSetter<T>(FieldInfo field)
        {
            if (typeof(T) != field.FieldType) return null;
            Type baseType = field.DeclaringType;

            ParameterExpression paramOne = Expression.Parameter(typeof(IModifiable), "argOne");
            ParameterExpression paramTwo = Expression.Parameter(typeof(T), "argTwo");
            Action<IModifiable, T> del = Expression.Lambda<Action<IModifiable, T>>(
                Expression.Assign(Expression.MakeMemberAccess(Expression.Convert(paramOne, baseType), field), paramTwo),
                new ParameterExpression[] { paramOne, paramTwo }).Compile();
            return del;
        }
        protected static Func<IModifiable, T> GenerateInstanceGetter<T>(FieldInfo field)
        {
            if (typeof(T) != field.FieldType) return null;
            Type baseType = field.DeclaringType;

            ParameterExpression paramOne = Expression.Parameter(typeof(IModifiable), "argOne");
            //ParameterExpression paramTwo = Expression.Parameter(typeof(T), "result");
            Func<IModifiable, T> del = Expression.Lambda<Func<IModifiable, T>>(
                Expression.MakeMemberAccess(Expression.Convert(paramOne, baseType), field),
                new ParameterExpression[] { paramOne }).Compile();
            return del;
        }
        protected static Action<IModifiable, T> GenerateSetter<T>(FieldInfo field)
        {
            if (field.IsStatic)
                return GenerateStaticSetter<T>(field);
            else
                return GenerateInstanceSetter<T>(field);
        }
        protected static Func<IModifiable, T> GenerateGetter<T>(FieldInfo field)
        {
            if (field.IsStatic)
                return GenerateStaticGetter<T>(field);
            else
                return GenerateInstanceGetter<T>(field);
        }

    }

    /// <summary>
    /// Generic attribute to allow modification for a field. Useful if a field does not need any custom validation,
    /// otherwise it should use a more specific ModifyXAttribute instead that can specify validation.
    /// Name must be unique (case insensitive) for all fields, and should not be 'quit' either.
    /// </summary>
    public class ModifyFieldAttribute : ModifiableFieldAttribute
    {
        public ModifyFieldAttribute() : base()
        {
        }

        public override ModifiableParser Parser(FieldInfo field)
        {
            if (field.FieldType == typeof(int))
                return new ModifiableIntParser(field, DefaultName, DefaultDescription);
            if (field.FieldType == typeof(string))
                return new ModifiableStringParser(field, DefaultName, DefaultDescription);

            if (field.FieldType == typeof(Room))
                return new ModifyRoomFieldParser(field, DefaultName, DefaultDescription);

            throw new ArgumentException("Type " + field.FieldType.Name + " is not generically supported as a modifiable field.");
        }
    }



    public class ModifiableStringParser : ModifiableParser
    {
        public ModifiableStringParser(FieldInfo field, string defaultName, string defaultDesc) : base(field, defaultName, defaultDesc)
        {
            Getter = GenerateGetter<string>(field);
            Setter = GenerateSetter<string>(field);
        }

        //Defaults here work fine
        //public override bool CanOverwrite { get { return true; } }
        //public override bool CanBeNull { get { return true; } }
        //public override ObjectType ObjectType { get { return null; } }
        //public override ISaveable SubObject(IModifiable source) { return null; }
        //public override bool ModifyAsList { get { return false; } }
        //public override string Instructions { get { return null; } }


        public override string GetValue(IModifiable source)
        {
            return Getter(source);
        }

        public override bool SetValue(IModifiable source, string input, out string validationError)
        {
            validationError = null;
            //TODO: Add validations for this.
            Setter(source, input);
            return true;
        }

        //public override bool ValidateInput(ISaveable source, string input)
        //{
        //    return true;
        //}

        private Func<IModifiable, string> Getter;
        private Action<IModifiable, string> Setter;
    }

    public class ModifiableIntParser : ModifiableParser
    {
        public ModifiableIntParser(FieldInfo field, string defaultName, string defaultDesc) : base(field, defaultName, defaultDesc)
        {
            Getter = GenerateGetter<int>(field);
            Setter = GenerateSetter<int>(field);
        }

        //public override bool CanOverwrite { get { return true; } }
        public override bool CanBeNull { get { return false; } }
        //public override ObjectType ObjectType { get { return null; } }
        //public override ISaveable SubObject(IModifiable source) { return null; }
        //public override bool ModifyAsList { get { return false; } }
        //public override string Instructions { get { return null; } }

        public override string GetValue(IModifiable source)
        {
            return Getter(source) + "";
        }

        public override bool SetValue(IModifiable source, string input, out string validationError)
        {
            //CanBeNull is false, so input should never be null.
            int value;
            if (int.TryParse(input, out value))
            {
                Setter(source, value);
                validationError = null;
                return true;
            }
            validationError = "Not a valid number.";
            return false;
        }

        private Func<IModifiable, int> Getter;
        private Action<IModifiable, int> Setter;
    }

    public abstract class ModifyGenericObjectTypeParser<T> : ModifiableParser where T : class, ISaveable
    {
        public ModifyGenericObjectTypeParser(FieldInfo field, string defaultName, string defaultDesc) : base(field, defaultName, defaultDesc)
        {
            Getter = GenerateGetter<T>(field);
            Setter = GenerateSetter<T>(field);
        }

        //public override bool CanOverwrite { get { return true; } }
        //public override bool CanBeNull { get { return true; } }
        public abstract override ObjectType ObjectType { get; } //Require that this is overridden.
        public override ISaveable SubObject(IModifiable source) { return Getter(source); }
        //public override bool ModifyAsList { get { return false; } }
        public override string Instructions { get
            {
                //TODO: Make this better.
                StringBuilder instructions = new StringBuilder("Available options for setting a new value:" + "^n");
                instructions.Append(string.Join(", ", this.ObjectType.KnownClasses.Select(x => x.Name)));
                return instructions.ToString();
            } }

        public override string GetValue(IModifiable source)
        {
            ISaveable value = Getter(source);
            if (value == null)
            {
                return "null";
            }
            else
            {
                return value.GetType().Name + " " + value.GetSaveID(null);
            }
        }

        public override bool SetValue(IModifiable source, string input, out string validationError)
        {
            validationError = null;
            T newValue;
            if (input == null)
            {
                newValue = null;
            }
            else
            {
                Type matchingType;
                List<Type> options;
                if (this.ObjectType.KnownTypeLookup().TryGet(input, out matchingType, out options))
                {
                    newValue = Activator.CreateInstance(matchingType) as T;
                    if (newValue == null)
                    {
                        validationError = "Could not create new object for '" + matchingType.Name + "'. There is probably a bug that needs to be fixed.";
                        return false;
                    }
                }
                else
                {
                    if (options != null)
                    {
                        validationError = "'" + input + "' matches multiple options, please be more specific.";
                    }
                    else
                    {
                        validationError = "No known type for '" + input + "'";
                    }
                    return false;
                }
            }
            Setter(source, newValue);
            return true;
        }

        protected Func<IModifiable, T> Getter;
        protected Action<IModifiable, T> Setter;
    }

    public class ModifyRoomFieldParser : ModifyGenericObjectTypeParser<Room>
    {
        public ModifyRoomFieldParser(FieldInfo field, string defaultName, string defaultDesc) : base(field, defaultName, defaultDesc) {}

        public override ObjectType ObjectType{ get {
                return ObjectType.TypeByClass[typeof(Room)];
            } }
    }



}
