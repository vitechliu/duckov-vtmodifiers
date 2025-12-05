using HarmonyLib;
using ItemStatsSystem;

namespace VTModifiers.ThirdParty; 

//秘法纪元stat
public static class CharacterMagicStats {
    
    
    public const string MaxMana = "MaxMana";
    public const string MagicPower = "MagicPower"; //法术强度，影响伤害和治疗
    // public const string MagicDamageMultiplier = "MagicDamageMultiplier";
    public const string MagicDistanceMultiplier = "MagicDistanceMultiplier";
    public const string ManaCost = "ManaCost";
    public const string MagicCritRate = "MagicCritRate";
    public const string CastTime = "CastTime";
    public const string HealMultiplier = "HealMultiplier";

    public const string FireExtraMultiplier = "FireExtraMultiplier";
    public const string ElectricityExtraMultiplier = "ElectricityExtraMultiplier";
    public const string PoisonExtraMultiplier = "PoisonExtraMultiplier";
    public const string SpaceExtraMultiplier = "SpaceExtraMultiplier";

    public const string FireEnchant = "FireEnchant";
    public const string ElectricityEnchant = "ElectricityEnchant";
    
    public static string[] CharacterStats = new string[] {
        MaxMana,
        MagicPower,
        ManaCost,
        CastTime,
        MagicDistanceMultiplier,
        HealMultiplier,
        FireExtraMultiplier,
        ElectricityExtraMultiplier,
        PoisonExtraMultiplier,
        SpaceExtraMultiplier,
        
        FireEnchant,
        ElectricityEnchant,
    };
    
}