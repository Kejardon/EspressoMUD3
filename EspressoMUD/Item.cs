using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD
{
    public class Item : IItemContainer, ISaveable
    {
        public Item ItemObject { get { return this; } }

        [SaveField("Name")]
        private string name;
        public string Name
        {
            get { return name; }
            set { name = value; this.Save(); }
        }

        [SaveField("Desc")]
        private string description;
        public string Description
        {
            get { return description; }
            set { description = value;  this.Save(); }
        }

        [SaveSubobject("Position")]
        private IPosition position;
        public IPosition Position
        {
            get { return position; }
            set { position = value;  this.Save(); }
        }
        
        //ISaveable template
        public SaveValues SaveValues { get; set; }
        [SaveID("ID")]
        protected int ItemID = -1; //Only supports IItem ObjectType, so assume IItem
        public int GetSaveID(ObjectType databaseGroup) { return ItemID; }
        public void SetSaveID(ObjectType databaseGroup, int id) { ItemID = id; }
    }
}
