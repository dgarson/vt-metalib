using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VTMetaLib.Data
{
    public enum ObjectClass
    {
        Unknown = 0,
        MeleeWeapon = 1,
        Armor = 2,
        Clothing = 3,
        Jewelry = 4,
        Monster = 5,
        Food = 6,
        Money = 7,
        Misc = 8,
        MissileWeapon = 9,
        Container = 10,
        Gem = 11,
        SpellComponent = 12,
        Key = 13,
        Portal = 14,
        TradeNote = 15,
        ManaStone = 16,
        Plant = 17,
        BaseCooking = 18,
        BaseAlchemy = 19,
        BaseFletching = 20,
        CraftedCooking = 21,
        CraftedAlchemy = 22,
        CraftedFletching = 23,
        Player = 24,
        Vendor = 25,
        Door = 26,
        Corpse = 27,
        Lifestone = 28,
        HealingKit = 29,
        Lockpick = 30,
        WandStaffOrb = 31, Wand = WandStaffOrb, Staff = WandStaffOrb, Orb = WandStaffOrb,
        Bundle = 32,
        Book = 33,
        Journal = 34,
        Sign = 35,
        Housing = 36,
        NPC = 37,
        Foci = 38,
        Salvage = 39,
        Ust = 40,
        Services = 41,
        Scroll = 42,
        CombatPet = 43,

        // max value + 1
        NumObjectClasses = 44
    }

    public static class ObjectClasses
    {
        public static ObjectClass Parse(string idOrName)
        {
            int objClassId;
            ObjectClass? objClass;
            if (int.TryParse(idOrName, out objClassId))
            {
                objClass = (ObjectClass)objClassId;
                if (objClass == null)
                    throw new ArgumentException($"Invalid ObjectClass id: {objClassId}");
            }
            else
            {
                object objParsed;
                if (!Enum.TryParse(typeof(ObjectClass), idOrName, out objParsed))
                    throw new ArgumentException($"Expected either an ObjectClass name string or an integer value for ObjectClass ID, got got: {idOrName}");
                objClass = (ObjectClass)objParsed;
            }
            return objClass.Value;
        }

        public static bool TryParse(string idOrName, out ObjectClass objClass)
        {
            int objClassId;
            if (int.TryParse(idOrName, out objClassId))
            {
                ObjectClass? possibleClass = (ObjectClass)objClassId;
                if (possibleClass == null)
                {
                    objClass = ObjectClass.Unknown;
                    return false;
                }
                objClass = possibleClass.Value;
            }
            else
            {
                object objParsed;
                if (!Enum.TryParse(typeof(ObjectClass), idOrName, out objParsed))
                {
                    objClass = ObjectClass.Unknown;
                    return false;
                }
                objClass = (ObjectClass)objParsed;
            }
            return true;
        }
    }
}
