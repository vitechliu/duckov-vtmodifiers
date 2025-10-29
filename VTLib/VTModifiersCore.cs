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


    private static bool TryPatchModifier(Item item, VtModifier vtModifier, string vtm) {
        //todo
        float? val = vtModifier.GetVal(vtm);
        if (val != null && vtModifier.GetVal(vtm) != DefaultModifier.GetVal(vtm)) {
            if (ModifierLogic != null && ModifierLogic.TryGetValue(vtm, out var vtp)) {
                if (item.Modifiers == null) {
                    item.CreateModifiersComponent();
                }
                item.Modifiers.Add(new ModifierDescription(
                    ModifierTarget.Parent,
                    vtp.Item1,
                    vtp.Item2,
                    (float)val
                ));
                return true;
            }
        }

        return false;
    }
    public static string PatchItem(Item item, Sources source) {
        if (item.GetBool("IsGun")) {
            //暂时只支持热武器
            string? modifier = GetAModifierByWeight();
            if (modifier != null) {
                item.SetString(VariableVtModifierHashCode, modifier, true);
                
                VtModifier vtModifier = ModifierData[modifier];
                bool flag = false;
                foreach (string vtm in Vtms) {
                    if (TryPatchModifier(item, vtModifier, vtm)) {
                        flag = true;
                        ModBehaviour.LogStatic($"注入了Modifier:{item.DisplayName}_{vtm}");
                    }
                }
                if (flag) item.Modifiers.ReapplyModifiers();
                string modifierDisplayName = ModifierDisplayText(modifier);
                ModBehaviour.LogStatic($"注入:{item.DisplayName}为{modifierDisplayName} 来源:{source.ToString()}");
                return modifier;
            }
            else {
                // ModBehaviour.LogStatic($"未找到Modifier 无法Patch");
            }
        }

        return null;
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
        if (modifier != null && ModifierData != null && ModifierData.TryGetValue(modifier, out var modifierStruct)) {
            switch (vtm) {
                case VtmElementElectricity:
                case VtmElementFire:
                case VtmElementPoison:
                case VtmElementSpace:
                    float? add = modifierStruct.GetVal(vtm);
                    if (add != null) return original + (float)add;
                    break;
            }
        }
        return original;
    }

    //生成来源
    public enum Sources {
        LootBox, //物资
        Enemy, //敌人AI
        Debug, //测试用
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

        if (ModifierLogic == null) {
            //这里的int对应Item Stat的Hash
            ModifierLogic = new Dictionary<string, ValueTuple<string, ModifierType>>() {
                [VtmDamage] = (nameof (ItemAgent_Gun.Damage), ModifierType.Add),
                [VtmDamageMultiplier] = (nameof (ItemAgent_Gun.Damage), ModifierType.PercentageAdd),
                [VtmBulletSpeed] = (nameof (ItemAgent_Gun.BulletSpeed), ModifierType.Add),
                [VtmBulletSpeedMultiplier] = (nameof (ItemAgent_Gun.BulletSpeed), ModifierType.PercentageAdd),
                [VtmShootDistance] = (nameof (ItemAgent_Gun.BulletDistance), ModifierType.Add),
                [VtmShootDistanceMultiplier] = (nameof (ItemAgent_Gun.BulletDistance), ModifierType.PercentageAdd),
                [VtmCritRate] = (nameof (ItemAgent_Gun.CritRate), ModifierType.Add),
                [VtmCritDamage] = (nameof (ItemAgent_Gun.CritDamageFactor), ModifierType.Add),
                [VtmSoundRange] = (nameof (ItemAgent_Gun.SoundRange), ModifierType.PercentageAdd),
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
    public static Dictionary<string, ValueTuple<string, ModifierType>>? ModifierLogic;
    
    
    
    public const string VtmDamage = "Damage"; //伤害修正
    public const string VtmDamageMultiplier = "DamageMultiplier";  //乘算的
    
    public const string VtmBulletSpeed = "BulletSpeed"; //弹速，亚门！
    public const string VtmBulletSpeedMultiplier = "ShootSpeedMultiplier";
    public const string VtmShootDistance = "ShootDistance"; //射程 加算
    public const string VtmShootDistanceMultiplier = "ShootDistanceMultiplier"; //射程 加算
    public const string VtmCritRate = "CritRate"; //暴击率,加算
    public const string VtmCritDamage = "CritDamage"; //爆伤,乘算
    public const string VtmSoundRange = "SoundRange"; //声音
    
    public const string VtmBleedChance = "BleedChance"; //流血几率
    
    public const string VtmWeight = "Weight";               //暂未实装
    public const string VtmAmmoSave = "AmmoSave";           //暂未实装
    public const string VtmPriceMultiplier = "PriceMultiplier";                 //暂未实装 价值加成
    
    
    public const string VtmElementFire = "ElementFire";
    public const string VtmElementSpace = "ElementSpace";
    public const string VtmElementPoison = "ElementPoison";
    public const string VtmElementElectricity = "ElementElectricit";


    public static string[] Vtms = {
        VtmDamage,
        VtmDamageMultiplier,
        VtmBulletSpeed,
        VtmBulletSpeedMultiplier,
        VtmShootDistance,
        VtmShootDistanceMultiplier,
        VtmCritRate,
        VtmCritDamage,
        VtmSoundRange,
        VtmBleedChance,
        VtmWeight,
        VtmAmmoSave,
        VtmPriceMultiplier,
        VtmElementFire,
        VtmElementSpace,
        VtmElementPoison,
        VtmElementElectricity,
    };
    
    public enum  ModifierLogicType {
        
    }
    
    
    public struct VtModifier {
        public int ModifierWeight = 0; //出现的权重
        public int ModifierLevel = 1; //等级从-6到6(10+) 负的代表负面的
        public float Weight = 0f;
        public float PriceMultiplier = 1f; //不展示
        
        public float Damage = 0f;
        public float DamageMultiplier = 1f;
        public float AmmoSave = 0f;
        public float ShootSpeed = 0f;
        public float ShootSpeedMultiplier = 1f;
        public float ShootDistance = 0f;
        public float ShootDistanceMultiplier = 1f;
        public float CritRate = 0f;
        public float BleedChance = 0f;
        public float SoundRange = 0f;
        public float ElementFire = 0f;
        public float ElementSpace = 0f;
        public float ElementPoison = 0f;
        public float ElementElectricity = 0f;

        public bool ApplyOnGuns = true;
        public VtModifier() { }

        public readonly float? GetVal(string vtm) {
            switch (vtm) {
                case VtmDamage:
                    return this.Damage;
                case VtmDamageMultiplier:
                    return this.DamageMultiplier;
                // case VtmAmmoSave:
                //     return this.AmmoSave;
                case VtmSoundRange:
                    return this.SoundRange;
                case VtmBulletSpeed:
                    return this.ShootSpeed;
                case VtmBulletSpeedMultiplier:
                    return this.ShootSpeedMultiplier;
                case VtmShootDistance:
                    return this.ShootDistance;
                case VtmShootDistanceMultiplier:
                    return this.ShootDistanceMultiplier;
                case VtmCritRate:
                    return this.CritRate;
                case VtmBleedChance:
                    return this.BleedChance;
                case VtmElementFire:
                    return this.ElementFire;
                case VtmElementElectricity:
                    return this.ElementElectricity;
                case VtmElementPoison:
                    return this.ElementPoison;
                case VtmElementSpace:
                    return this.ElementSpace;
            }

            return null;
        }
    }
    
}