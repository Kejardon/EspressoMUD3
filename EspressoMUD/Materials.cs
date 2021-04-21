using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD
{
    public static class Materials
    {
        public enum Material
        {
            Air,

            Water,

            Dirt,
            Rock,

            Oak,
            Balsa,

            Copper,
            Iron,
            Tin,
        }

        public enum MaterialCategory
        {
            Gas,

            Fluid,

            Wood,
            Metal,
            FineSolid,
            Solid
        }
        public static MaterialCategory Category(this Material forMaterial)
        {
            switch (forMaterial)
            {
                case Material.Air:
                    return MaterialCategory.Gas;
                case Material.Water:
                    return MaterialCategory.Fluid;
                case Material.Oak:
                case Material.Balsa:
                    return MaterialCategory.Wood;
                case Material.Copper:
                case Material.Iron:
                case Material.Tin:
                    return MaterialCategory.Metal;
                case Material.Dirt:
                    return MaterialCategory.FineSolid;
                case Material.Rock:
                    return MaterialCategory.Solid;
            }
            throw new ArgumentOutOfRangeException("forMaterial");
        }
        //These makes more sense as a function in MovementMechanism
        //public static bool CanWalkThrough(this Material forMaterial, MovementMechanism mechanism)
        //{
        //}
        //public static bool CanWalkOn(this Material forMaterial, MovementMechanism mechanism)
        //{
        //}
    }
}
