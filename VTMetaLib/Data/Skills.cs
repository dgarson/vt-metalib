using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VTMetaLib.Data
{
    public enum SkillId
    {
        MeleeDefense = 6,
        MissileDefense = 7,
        ArcaneLore = 14,
        MagicDefense = 15,
        ManaConversion = 16,
        ItemTinkering = 18,
        AssessPerson = 19,
        Deception = 20,
        Healing = 21,
        Jump = 22,
        Lockpick = 23,
        Run = 24,
        AssessCreature = 27,
        WeaponTinkering = 28,
        ArmorTinkering = 29,
        MagicItemTinkering = 30,
        CreatureEnchantment = 31, CreatureMagic = CreatureEnchantment,
        ItemEnchantment = 32, ItemMagic = ItemEnchantment,
        LifeMagic = 33,
        WarMagic = 34,
        Leadership = 35,
        Loyalty = 36,
        Fletching = 37,
        Alchemy = 38,
        Cooking = 39,
        Salvaging = 40,
        TwoHandedCombat = 41, TwoHand = TwoHandedCombat,
        Void = 43,
        HeavyWeapons = 44, Heavy = HeavyWeapons,
        LightWeapons = 45, Light = LightWeapons,
        FinesseWeapons = 46, Finesse = FinesseWeapons,
        MissileWeapons = 47,
        Summoning = 54
    }

    public enum RecallSpell
    {
        [Description("Primary Portal Recall")]
        Primary,
        [Description("Secondary Portal Recall")]
        Secondary,
        [Description("Lifestone Recall")]
        Lifestone,
        [Description("Lifestone Sending")]
        LifestoneSending,
        [Description("Portal Recall")]
        Portal,
        [Description("Recall Aphus Lassel")]
        AphusLassel,
        [Description("Recall the Sanctuary")]
        Sanctuary,
        [Description("Recall to the Singularity Caul")]
        SingularityCaul,
        [Description("Glenden Wood Recall")]
        Glendenwood,
        [Description("Aerlinthe Recall")]
        Aerlinthe,
        [Description("Mount Lethe Recall")]
        MountLethe,
        [Description("Ulgrim's Recall")]
        Ulgrim,
        [Description("Bur Recall")]
        Bur,
        [Description("Paradox-touched Olthoi Infested Area Recall")]
        ParadoxOlthoi,
        [Description("Call of the Mhoire Forge")]
        MhoireForge,
        [Description("Colosseum Recall")]
        Colosseum,
        [Description("Facility Hub Recall")]
        FacilityHub,
        [Description("Gear Knight Invasion Area Camp Recall")]
        GearKnight,
        [Description("Lost City of Neftet Recall")]
        Neftet,
        [Description("Return to the Keep")]
        Candeth,
        [Description("Rynthid Recall")]
        Rynthid,
        [Description("Viridian Rise Recall")]
        ViridianRise,
        [Description("Viridian Rise Great Tree Recall")]
        ViridianRiseTree,
        [Description("Celestial Hand Stronghold Recall")]
        CelestialHandStronghold,
        [Description("Eldrytch Web Stronghold Recall")]
        EldrytchWebStronghold,
        [Description("Radiant Blood Stronghold Recall")]
        RadiantBloodStronghold

    }
}
