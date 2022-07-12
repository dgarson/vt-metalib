using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VTMetaLib.Data
{
    public enum CharStringProperty
    {
        Name = 1,
        Title = 5,
        FellowshipName = 10,
        MonarchName = 21,
        Patron = 35,
        DateBorn = 43,
        MonarchyDescription = 47
    }

    public enum CharQuadProperty
    {
        TotalExperience = 1,
        UnassignedExperience = 2,
        LuminanceExperience = 6
    }

    public enum CharIntProperty
    {
        #region Base Character Properties
        Species = 2,
        ContainerSlots = 7,
        BurdenUnits = 5,
        TotalValuePyreal = 20,
        SkillCreditsAvailable = 24,
        Level = 25,
        Rank = 30,
        TotalDeaths = 43,
        DateOfBirthMillis = 98,
        Gender = 11,
        AgeSeconds = 125,
        XPForVitaeReduction = 129,
        ChessRank = 181,
        Heritage = 188,
        FishingSkill = 192,
        TitlesEarned = 262,
        SocietyRibbonCountCelestialHand = 287,
        SocietyRibbonCountEldrytchWeb = 288,
        SocietyRibbonCountRadiantBlood = 289,
        MeleeMastery = 354,
        RangedMastery = 355,
        SummoningMastery = 362,
        #endregion Base Character Properties

        #region XP Augmentations
        ReinforcmentOfTheLugians = 218,
        BleearghsFortitude = 219,
        OswaldsEnchantment = 220,
        SiraluunsBlessing = 221,
        EnduringCalm = 222,
        SteadfastWill = 223,
        CiandrasEssence = 224,
        YoshisEssence = 225,
        JibrilsEssence = 226,
        CeldisethsEssence = 227,
        KogasEssence = 228,
        ShadowOfTheSeventhMule = 229,
        MightOfTheSeventhMule = 230,
        ClutchOfTheMiser = 231,
        EnduringEnchantment = 232,
        CriticalProtection = 233,
        QuickLearner = 234,
        CharmedSmith = 236,
        InnateRenewal = 237,
        ArchmagesEndurance = 238,
        EnhancementOfTheBladeTurner = 240,
        EnhancementOfTheArrowTurner = 241,
        EnhancementOfTheMaceTurner = 242,
        CausticEnhancement = 243,
        FieryEnhancement = 244,
        IcyEnhancement = 245,
        StormsEnhancement = 246,
        InfusedCreatureMagic = 294,
        InfusedItemMagic = 295,
        InfusedLifeMagic = 296,
        InfusedWarMagic = 297,
        EyeOfTheRemorseless = 298,
        HandOfTheRemorseless = 299,
        MasterOfTheSteelCircle = 300,
        MasterOfTheFocusedEye = 301,
        MasterOfTheFiveFoldPath = 302,
        FrenzyOfTheSlayer = 309,
        IronSkinOfTheInvincible = 310,
        JackOfAllTrades = 326,
        InfusedVoidMagic = 328,
        #endregion XP Augmentations

        #region Luminance Augmentations
        AuraValor = 333,
        AuraProtection = 334,
        AuraGlory = 335,
        AuraTemperance = 336,
        AuraAetheria = 338,
        AuraManaFlow = 339,
        AuraManaInfusion = 340,
        AuraPurity = 342,
        AuraCraftsman = 343,
        AuraSpecialization = 344,
        AuraWorld = 365,
        HealBootRating = 376,
        VitalityRating = 379,
        TotalDamageRating = 307,
        TotalDamageResistRating = 308,
        TotalCritDamageRating = 314,
        TotalCritDamageResistRating = 316,
        TotalDamageResistFromLuminance = 334,
        TotalCritDamageResistFromLuminance = 336,
        #endregion Luminance Augmentations
    }
}
