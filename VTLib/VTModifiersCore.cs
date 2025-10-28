using System.Collections;
using ItemStatsSystem;
using ItemStatsSystem.Stats;

namespace VTModifiers.VTLib;

public class VTModifiersCore {
    //通用

    public static readonly string VariableVtModifierHashCode = "VT_MODIFIER";

    

    //todo 本地化
    public static string ModifierDisplayText(string modifier) {
        return TmpLangDict.GetValueOrDefault(modifier, modifier);
    }

    
    public static string? GetAModifierByWeight() {
        int totalWeight = 0;
        foreach (VtModifier modifier in ModifierData.Values) {
            totalWeight += modifier.ModifierWeight;
        }
        int num = UnityEngine.Random.Range(0, totalWeight);
        int num2 = 0;
        foreach (KeyValuePair<string, VtModifier> kvp in ModifierData) {
            if (num > num2 && num <= num2 + kvp.Value.ModifierWeight) {
                return kvp.Key;
            }
            num2 += kvp.Value.ModifierWeight;
        }
        return null;
    }


    private static void TryPatchModifier(Item item, VtModifier vtModifier, string vtm, float value) {
        //todo
    }
    public static void PatchItem(Item item, Sources source) {
        if (item.GetBool("IsGun")) {
            //暂时只支持热武器
            string? modifier = GetAModifierByWeight();
            if (modifier != null) {
                VtModifier vtModifier = ModifierData[modifier];
                
                TryPatchModifier(item, vtModifier, VtmDamageMultiplier, vtModifier.DamageMultiplier);
                
                item.SetString(VariableVtModifierHashCode, modifier, true);
                ModBehaviour.LogStatic($"注入成功:{item.DisplayName}_{source.ToString()}");
            }
        }
    }


    public static string PatchItemDisplayName(Item item) {
        if (item == null) return "";
        string modifier = item.GetString(VariableVtModifierHashCode);
        string itemDisplayName = item.DisplayName;
        if (modifier != null) {
            string modifierDisplayText = ModifierDisplayText(modifier);
            return modifierDisplayText + " " + itemDisplayName;
        }
        return itemDisplayName;
    }
    public static float Modify(Item item, string vtm, float original) {
        string modifier = item.GetString(VariableVtModifierHashCode);
        if (modifier != null && ModifierData.ContainsKey(modifier)) {
            VtModifier modifierStruct = ModifierData[modifier];
            switch (vtm) {
                case VtmDamage:
                    return original + modifierStruct.Damage;
                case VtmDamageMultiplier:
                    return original * modifierStruct.DamageMultiplier;
                // case VtmAmmoSave:
                //     return original + modifierStruct.AmmoSave;
                case VtmShootSpeed:
                    return original + modifierStruct.ShootSpeed;
                case VtmShootSpeedMultiplier:
                    return original * modifierStruct.ShootSpeedMultiplier;
                case VtmShootDistance:
                    return original + modifierStruct.ShootDistance;
                case VtmShootDistanceMultiplier:
                    return original * modifierStruct.ShootDistanceMultiplier;
                case VtmCritRate:
                    return original + modifierStruct.CritRate;
                case VtmBleedChance:
                    return original + modifierStruct.BleedChance;
                case VtmElementFire:
                    return original + modifierStruct.ElementFire;
                case VtmElementElectricity:
                    return original + modifierStruct.ElementElectricity;
                case VtmElementPoison:
                    return original + modifierStruct.ElementPoison;
                case VtmElementSpace:
                    return original + modifierStruct.ElementSpace;
                
            }
        }
        return original;
    }

    //生成来源
    public enum Sources {
        LootBox, //物资
        Enemy //敌人AI
    }

    public static void InitData() {
        //todo 从config加载
        if (ModifierData == null) {
            ModifierData = new Dictionary<string, VtModifier>() {
                ["Unreal"] =                  new() { ModifierWeight = 100, DamageMultiplier = 1.15f, ShootSpeed = 0.1f, CritRate = 0.05f, PriceMultiplier = 2.0985f },
                ["Sighted"] =                 new() { ModifierWeight = 300, DamageMultiplier = 1.05f, CritRate = 0.03f, PriceMultiplier = 1.2f },
                ["WithElectricity"] =         new() { ModifierWeight = 100,ElementElectricity = 0.2f, DamageMultiplier = 0.9f, PriceMultiplier = 1.5f },
                ["WithFire"] =                new() { ModifierWeight = 100,ElementFire = 0.2f, DamageMultiplier = 0.9f, PriceMultiplier = 1.5f },
                ["WithSpace"] =               new() { ModifierWeight = 100,ElementSpace = 0.2f, DamageMultiplier = 0.9f, PriceMultiplier = 1.5f },
                ["WithPoison"] =              new() { ModifierWeight = 100,ElementPoison = 0.2f, DamageMultiplier = 0.9f, PriceMultiplier = 1.5f },
            };
        }
    }
    
    public static readonly VtModifier DefaultModifier = new VtModifier();
    public static readonly Dictionary<string, string> TmpLangDict = new() {
        { "Unreal", "虚幻" },
        { "Sighted", "精准" },
        
        
        { "WithElectricity", "带电" },
        { "WithFire", "带火" },
        { "WithSpace", "空间" },
        { "WithPoison", "剧毒" },
    };
        

    public static Dictionary<string, VtModifier> ModifierData;
    
    
    public const string VtmDamage = "Damage"; 
    public const string VtmDamageMultiplier = "DamageMultiplier";  //乘算的
    
    public const string VtmShootSpeed = "ShootSpeed"; //弹速，亚门！
    public const string VtmShootSpeedMultiplier = "ShootSpeedMultiplier";
    public const string VtmShootDistance = "ShootDistance"; //射程 加算
    public const string VtmShootDistanceMultiplier = "ShootDistanceMultiplier"; //射程 加算
    public const string VtmCritRate = "CritRate"; //暴击率,加算
    public const string VtmBleedChance = "BleedChance"; //流血几率
    
    public const string VtmWeight = "Weight";               //暂未实装
    public const string VtmAmmoSave = "AmmoSave";           //暂未实装
    public const string VtmPriceMultiplier = "PriceMultiplier";                 //暂未实装 价值加成
    
    
    public const string VtmElementFire = "ElementFire";
    public const string VtmElementSpace = "ElementSpace";
    public const string VtmElementPoison = "ElementPoison";
    public const string VtmElementElectricity = "ElementElectricit";
    public struct VtModifier {
        public int ModifierWeight = 0; //出现的权重
        public float Damage = 0f;
        public float DamageMultiplier = 1f;
        public float Weight = 0f;
        public float AmmoSave = 0f;
        public float PriceMultiplier = 1f; //不展示
        public float ShootSpeed = 0f;
        public float ShootSpeedMultiplier = 1f;
        public float ShootDistance = 0f;
        public float ShootDistanceMultiplier = 1f;
        public float CritRate = 0f;
        public float BleedChance = 0f;
        public float ElementFire = 0f;
        public float ElementSpace = 0f;
        public float ElementPoison = 0f;
        public float ElementElectricity = 0f;
        public VtModifier() { }
    }
    
}