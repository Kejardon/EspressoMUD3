using EspressoMUD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD
{
    /// <summary>
    /// Wrapper class for a moving vehicle (body, mount, actual vehicle, projectile...). Defines the size and shape that the vehicle can
    /// possess while it is moving, used to check where it can fit without issue.
    /// This class is specifically used when pathfinding or motion needs to do calculations for those objects. A hitbox is specific
    /// to a particular pathfinding or motion event.
    /// </summary>
    public abstract class Hitbox : ISubobject
    {
        public enum HitboxType
        {
            Square //Only supported type for now. Square prism, any height.
        }

        public object Parent { get; set; }

        /// <summary>
        /// What type of hitbox this is. Used to quickly identify what kind of collision detection is sufficient for this.
        /// </summary>
        public abstract HitboxType Type { get; }

        /// <summary>
        /// Maximum X offset for the hitbox.
        /// Used to quickly identify which obstacles need to do any collision detection with this hitbox.
        /// </summary>
        public abstract int MaxXIncrease(Rotation rotation);
        /// <summary>
        /// Minimum X offset for the hitbox.
        /// Used to quickly identify which obstacles need to do any collision detection with this hitbox.
        /// </summary>
        public abstract int MaxXDecrease(Rotation rotation);
        /// <summary>
        /// Maximum Y offset for the hitbox.
        /// Used to quickly identify which obstacles need to do any collision detection with this hitbox.
        /// </summary>
        public abstract int MaxYIncrease(Rotation rotation);
        /// <summary>
        /// Minimum Y offset for the hitbox.
        /// Used to quickly identify which obstacles need to do any collision detection with this hitbox.
        /// </summary>
        public abstract int MaxYDecrease(Rotation rotation);
        /// <summary>
        /// Maximum Z offset for the hitbox.
        /// Used to quickly identify which obstacles need to do any collision detection with this hitbox.
        /// </summary>
        public abstract int MaxZIncrease(Rotation rotation);
        /// <summary>
        /// Minimum Z offset for the hitbox.
        /// Used to quickly identify which obstacles need to do any collision detection with this hitbox.
        /// </summary>
        public abstract int MaxZDecrease(Rotation rotation);

        /// <summary>
        /// Check if the source item's hitbox includes the target. This includes just barely touching.
        /// Surfaces at the same position are considered touching, but not overlapping hitboxes.
        /// </summary>
        /// <param name="ownLocation">Hitbox's position. Not necessarily an item's position, an offset may be applied due to some obstaclesurface alignment or something similar</param>
        /// <param name="target"></param>
        /// <param name="targetLocation">Other hitbox's position. Not necessarily an item's position, an offset may be applied due to some obstaclesurface alignment or something similar</param>
        /// <returns></returns>
        public bool InRange(WorldRelativeOrientation ownLocation, Hitbox target, WorldRelativeOrientation targetLocation)
        {
            if (ownLocation.OriginRoom != targetLocation.OriginRoom) return false;
            return SubInRange(ownLocation, target, targetLocation);
        }
        protected abstract bool SubInRange(WorldRelativeOrientation ownLocation, Hitbox target, WorldRelativeOrientation targetLocation);

        protected bool GenericInRange(WorldRelativeOrientation ownLocation, Hitbox target, WorldRelativeOrientation targetLocation)
        {
            throw new NotImplementedException("Generic case not implemented yet.");
        }
        //{
            ////Use the item's location to compare. If the worlds are different the hitbox will never overlap,
            ////so a virtual item is necessary for portals.
            //WorldRelativeOrientation end = target.Position.WorldOrientation();
            //Hitbox other = target.
            //if (source.Position.OriginRoom != end.OriginRoom) return false;

            ////Going to assume standard hitbox shape if nothing else specified; position at bottom center.
            ////Source should be set before this to 
            //int width = MaxWidth / 2;
            //if (source.Position.x > end.Position.x + width || source.Position.x < end.Position.x - width) return false;
            //int length = MaxLength / 2;
            //if (source.Position.y > end.Position.y + length || source.Position.y < end.Position.y - length) return false;
            //int height = MaxHeight;
            //if (source.Position.z )

        //}
    }
    /// <summary>
    /// The simplest hitbox. Rotating doesn't change it's width/length, doesn't rotate it's height.
    /// </summary>
    public class SquareHitbox : Hitbox
    {
        public override HitboxType Type { get { return HitboxType.Square; } }

        [SaveField("Width")]
        private int halfWidth;
        //Extends in all 4 cardinal directions by the same distance.
        public int HalfWidth
        {
            get { return halfWidth; }
            set { halfWidth = value; this.Save(); }
        }
        [SaveField("Height")]
        private int height;
        //Extends up a fixed distance. Doesn't go down.
        public int Height
        {
            get { return height; }
            set { height = value; this.Save(); }
        }

        public override int MaxXIncrease(Rotation rotation) {
            return (rotation.IsVertical() ? halfWidth : ((height + 1) / 2)); }
        public override int MaxXDecrease(Rotation rotation) {
            return (rotation.IsVertical() ? halfWidth : ((height + 1) / 2)); }
        public override int MaxYIncrease(Rotation rotation) {
            return (rotation.IsVertical() ? halfWidth : ((height + 1) / 2)); }
        public override int MaxYDecrease(Rotation rotation) {
            return (rotation.IsVertical() ? halfWidth : ((height + 1) / 2)); }
        public override int MaxZIncrease(Rotation rotation) {
            return (rotation.IsVertical() ? height : (halfWidth * 2)); }
        public override int MaxZDecrease(Rotation rotation) { return 0; }

        protected override bool SubInRange(WorldRelativeOrientation ownLocation, Hitbox target, WorldRelativeOrientation targetLocation)
        {

            switch(target.Type)
            {
                case HitboxType.Square:
                    //Square hitboxes essentially do not rotate. If a square hitbox would be laying down, the sizes swap instead.
                    //TODO: This may not entirely make sense if/when there are rooms that rotate outside of 90 degree multiples.
                    SquareHitbox otherSquare = target as SquareHitbox;
                    bool selfIsVertical = ((Rotation)ownLocation).IsVertical();
                    bool otherIsVertical = ((Rotation)targetLocation).IsVertical();
                    int totalWidth = (selfIsVertical ? halfWidth : ((height + 1) / 2)) +
                        (otherIsVertical ? otherSquare.halfWidth : ((otherSquare.height + 1) / 2));
                    if (ownLocation.x > targetLocation.x + totalWidth || ownLocation.x < targetLocation.x - totalWidth) return false;
                    if (ownLocation.y > targetLocation.y + totalWidth || ownLocation.y < targetLocation.y - totalWidth) return false;
                    if (ownLocation.z > targetLocation.z + (otherIsVertical ? otherSquare.height : (otherSquare.halfWidth * 2))) return false;
                    if (targetLocation.z > ownLocation.z + (selfIsVertical ? height : (halfWidth * 2))) return false;

                    return true;
                        
                default:
                    return GenericInRange(ownLocation, target, targetLocation);
            }
        }
    }
}
