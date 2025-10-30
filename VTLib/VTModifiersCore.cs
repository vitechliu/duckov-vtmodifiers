using Duckov.UI;
using Duckov.Utilities;
using HarmonyLib;
using ItemStatsSystem;
using ItemStatsSystem.Stats;
using SodaCraft.Localizations;
using UnityEngine;
using Random = UnityEngine.Random;

// ReSharper disable All

namespace VTModifiers.VTLib;

public class VTModifiersCore {
    //通用

    public static readonly string VariableVtModifierHashCode = "VT_MODIFIER";
    public static readonly string VariableVtModifierSeedHashCode = "VT_MODIFIER_SEED";

    public const bool DEBUG = false;

    public static bool IsModifierFixed(VTModifiersCore.VtModifier modifier) {
        return modifier.ForceFixed ? true : false;
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
                if (item.Modifiers == null) item.CreateModifiersComponent();
                if (item.Modifiers == null) return false;

                if (item.Modifiers.Find(tmd => (tmd.Key == vtp.Item1 && IsModMD(tmd))) != null) {
                    //避免重复添加
                    return false;
                }

                float finalVal = (float)val;
                if (!IsModifierFixed(vtModifier) && !FixedVtms.Contains(vtm)) {
                    finalVal = finalVal * Random.Range(0.5f, 1f);
                }

                ModifierDescription md = new ModifierDescription(
                    ModifierTarget.Self,
                    vtp.Item1,
                    vtp.Item2,
                    finalVal,
                    false,
                    MAGIC_ORDER
                );
                Traverse tmp = Traverse.Create(md);
                tmp.Field("display").SetValue(true);
                // tmp.Field("displayNamekey").SetValue("VTM_" + vtp.Item1);
                
                item.Modifiers.Add(md);
                return true;
            }
        }

        return false;
    }

    public const int MAGIC_ORDER = -1145141919;
    public static void TryUnpatchItem(Item item) {
        string modifier = item.GetString(VariableVtModifierHashCode);
        if (modifier == null) return;
        CustomDataCollection variables = Traverse.Create(item).Field("variables").GetValue<CustomDataCollection>();
        if (variables != null) {
            CustomData cd = variables.GetEntry(VariableVtModifierHashCode);
            if (cd != null) variables.Remove(cd);
        }

        int removedModifiersCount = 0;
        List<ModifierDescription> toRemove = new();
        if (item.Modifiers != null) {
            foreach (ModifierDescription md in item.Modifiers) {
                if (IsModMD(md)) {
                    toRemove.Add(md);
                    removedModifiersCount++;
                }
            }
            foreach (ModifierDescription md in toRemove) {
                item.Modifiers.Remove(md);
            }
            item.Modifiers.ReapplyModifiers();
        }
        ModBehaviour.LogStatic($"移除词缀:{item.DisplayName}成功，同时移除了{removedModifiersCount}个原生Modifier");
        
    }

    public static string PatchItem(Item item, Sources source) {
        if (item.GetBool("IsGun")) {
            //暂时只支持热武器
            switch (source) {
                case Sources.LootBox:
                    if (!VTLib.Probability(0.75f)) return null;
                    break;
                case Sources.Enemy:
                    if (!VTLib.Probability(0.4f)) return null;
                    break;
                case Sources.Craft:
                    if (!VTLib.Probability(0.75f)) return null;
                    break;
            }
            string? modifier = GetAModifierByWeight();
            if (modifier != null) {
                return PatchItem(item, source, modifier);
            }
            // ModBehaviour.LogStatic($"未找到Modifier 无法Patch");
        }
        return null;
    }

    public static bool IsModMD(ModifierDescription modifierDescription) {
        return modifierDescription.Order == MAGIC_ORDER;
    }
    //为Item Patch Modifier
    public static void CalcItemModifiers(Item item) {
        string modifier = item.GetString(VariableVtModifierHashCode);
        if (modifier == null) return;
        if (ModifierData.TryGetValue(modifier, out VtModifier vtModifier)) {
            bool flag = false;
            Random.State originalState = Random.state;
            if (!IsModifierFixed(vtModifier)) {
                int modifierSeed = item.GetInt(VariableVtModifierSeedHashCode, -1);
                if (modifierSeed == -1) {
                    modifierSeed = Random.Range(0, 1000000);
                    item.SetInt(VariableVtModifierSeedHashCode, modifierSeed);
                }
                Random.InitState(modifierSeed);
            }
        
            foreach (string vtm in Vtms) {
                if (TryPatchModifier(item, vtModifier, vtm)) {
                    flag = true;
                    // ModBehaviour.LogStatic($"注入了Modifier:{item.DisplayName}_{vtm}");
                }
            }
            if (flag) {
                item.Modifiers.ReapplyModifiers();
            }
            Random.state = originalState;
        }
        else {
            ModBehaviour.LogStatic($"找不到modifier:{modifier}");
        }
        
    }
    
    public static string PatchItem(Item item, Sources source, string modifier) {
        item.SetString(VariableVtModifierHashCode, modifier, true);
        string modifierDisplayName = modifier.ToPlainText();
        if (DEBUG)
            ModBehaviour.LogStatic($"注入:{item.DisplayName}为{modifierDisplayName}, source:{source}");
        CalcItemModifiers(item);
        return modifier;
    }
    
    public static string PatchItemDisplayName(Item item) {
        return PatchItemDisplayName(item, item.DisplayName);
    }
    public static string PatchItemDisplayName(Item item, string before) {
        if (item == null) return "";
        string modifier = item.GetString(VariableVtModifierHashCode);
        if (modifier != null) {
            string modifierDisplayText = modifier.ToPlainText();
            return modifierDisplayText + " " + before;
        }
        return before;
    }

    public static float? GetItemVtm(Item item, string vtm) {
        string modifier = item.GetString(VariableVtModifierHashCode);
        if (modifier != null && ModifierData.TryGetValue(modifier, out var modifierStruct)) {
            return modifierStruct.GetVal(vtm);
        }
        return null;
    }
    public static float Modify(Item item, string vtm, float original) {
        string modifier = item.GetString(VariableVtModifierHashCode);
        if (modifier != null && ModifierData.TryGetValue(modifier, out var modifierStruct)) {
            float? val = modifierStruct.GetVal(vtm);
            if (val.HasValue) {
                switch (vtm) {
                    case VtmElementElectricity:
                    case VtmElementFire:
                    case VtmElementPoison:
                    case VtmElementSpace:
                    case VtmBleedChance:
                        return original + val.Value;
                    case VtmWeight:
                    case VtmPriceMultiplier:
                        return original * (1f + val.Value);
                }
            }
        }
        return original;
    }

    //生成来源
    public enum Sources {
        LootBox, //物资
        Enemy, //敌人AI
        Debug, //测试用
        Craft, //制作的
    }

    
    public static void InitData() {
        //todo 从config加载
        if (ModifierData == null || ModifierData.Count == 0) {
            ModifierData = new () {
                ["Legendary"] =               new() { ModifierWeight = 50, ShootSpeedMultiplier = 0.2f, Weight = -0.3f, DamageMultiplier = 0.5f, ShootDistanceMultiplier = 0.3f, PriceMultiplier = 3f },
                ["Unreal"] =                  new() { ModifierWeight = 100, ForceFixed = true, ShootSpeedMultiplier = 0.1f, DamageMultiplier = 0.15f, BulletSpeedMultiplier = 0.1f, CritDamageMultiplier = 0.15f, PriceMultiplier = 2.0985f },
                ["Sighted"] =                 new() { ModifierWeight = 300, ScatterFactorADSMultiplier = -0.2f, RecoilScaleHMultiplier = -0.2f, PriceMultiplier = 0.2f },
                ["Light"] =                   new() { ModifierWeight = 300, Weight = -0.4f, DamageMultiplier = -0.05f, ReloadTimeMultiplier = -0.2f, PriceMultiplier = 0.2f },
                ["Heavy"] =                   new() { ModifierWeight = 300, Weight = 0.3f, RecoilScaleVMultiplier = -0.1f, RecoilScaleHMultiplier = 0.15f, DamageMultiplier = 0.35f, PriceMultiplier = 0.3f },
                ["Deadly"] =                  new() { ModifierWeight = 200, DamageMultiplier = 0.25f, CritDamageMultiplier = 0.25f, PriceMultiplier = 0.6f },
                ["Eagle-Eye"] =               new() { ModifierWeight = 100, DamageMultiplier = 0.1f, ShootDistanceMultiplier = 0.3f, PriceMultiplier = 0.2f },
                ["Silver"] =                  new() { ModifierWeight = 100, DamageMultiplier = 0.05f, BleedChance = 0.2f, ElementPoison = 0.1f, PriceMultiplier = 0.5f },
                ["Chaos"] =                   new() { ModifierWeight = 50, ElementElectricity = 0.2f, ElementFire = 0.2f, ElementSpace = 0.2f, ElementPoison = 0.2f, PriceMultiplier = 0.8f },
                ["Penetrating"] =             new() { ModifierWeight = 150, Penetrate = 1, ArmorPiercing = 1, PriceMultiplier = 0.5f },
                ["Heartbroken"] =             new() { ModifierWeight = 100, DamageMultiplier = 0.2f, ArmorPiercing = 3, PriceMultiplier = 0.8f },
                ["Thrifty"] =                 new() { ModifierWeight = 150, AmmoSave = 0.3f, PriceMultiplier = 0.3f },
                ["Fast"] =                    new() { ModifierWeight = 150, ReloadTimeMultiplier = -0.3f, ShootSpeedMultiplier = 0.2f, PriceMultiplier = 0.2f },
               
                ["Alienated"] =               new() { ModifierWeight = 50, CritDamageMultiplier = -0.1f, ElementPoison = 0.3f, ShootSpeed = 0.4f, PriceMultiplier = 0.1f },
                ["Scalding"] =                new() { ModifierWeight = 50, DamageMultiplier = 0.2f, Weight = 0.4f, ElementFire = 0.4f, ShootSpeed = -0.2f, PriceMultiplier = 0.1f },
                ["Gauss"] =                   new() { ModifierWeight = 50, Penetrate = 3, ElementElectricity = 0.4f, ShootSpeed = 0.1f, PriceMultiplier = 0.7f },
                ["Concentrated"] =            new() { ModifierWeight = 200, DamageMultiplier = -0.1f, RecoilScaleHMultiplier = -0.5f, ShootSpeed = 0.2f, PriceMultiplier = 0.1f },
                ["Portable"] =                new() { ModifierWeight = 300, ScatterFactorMultiplier = -0.4f, ScatterFactorADSMultiplier = 0.2f, PriceMultiplier = 0.1f },
                ["Brutal"] =                  new() { ModifierWeight = 300, DamageMultiplier = 0.4f, BleedChance = 0.3f, ShootSpeedMultiplier = -0.2f, PriceMultiplier = 0.2f },
                ["Cheap"] =                   new() { ModifierWeight = 150, DamageMultiplier = -0.1f, AmmoSave = 0.3f, Weight = -0.3f, PriceMultiplier = -0.2f },

                ["Apollyon"] =                new() { ModifierWeight = 10, ForceFixed = true, BulletSpeedMultiplier = 2f, PriceMultiplier = 0.2f },
                ["Silent"] =                  new() { ModifierWeight = 200, ShootDistanceMultiplier = -0.1f, SoundRange = -0.5f, PriceMultiplier = 0.1f },
                ["Violent"] =                 new() { ModifierWeight = 200, DamageMultiplier = 0.35f, SoundRange = 0.8f, PriceMultiplier = 0.2f },

                
                ["Broken"] =                  new() { ModifierWeight = 200, DamageMultiplier = -0.2f, ShootDistanceMultiplier = -0.2f, PriceMultiplier = -0.5f },
                
                
                ["WithElectricity"] =         new() { ModifierWeight = 100, ElementElectricity = 0.5f, PriceMultiplier = 0.5f },
                ["WithFire"] =                new() { ModifierWeight = 100, ElementFire = 0.5f, PriceMultiplier = 0.5f },
                ["WithSpace"] =               new() { ModifierWeight = 100, ElementSpace = 0.5f, PriceMultiplier = 0.5f },
                ["WithPoison"] =              new() { ModifierWeight = 100, ElementPoison = 0.5f, PriceMultiplier = 0.5f },
                
                
                ["Debug"] =                   new() { ModifierWeight = 0, CritDamageMultiplier = 0.7f, Penetrate = 2, PriceMultiplier = 9f},
            };
        }

        if (ModifierLogic == null) {
            //这里的int对应Item Stat的Hash
            ModifierLogic = new () {
                [VtmDamage] = (nameof (ItemAgent_Gun.Damage), ModifierType.Add),
                [VtmDamageMultiplier] = (nameof (ItemAgent_Gun.Damage), ModifierType.PercentageAdd),
                [VtmBulletSpeed] = (nameof (ItemAgent_Gun.BulletSpeed), ModifierType.Add),
                [VtmBulletSpeedMultiplier] = (nameof (ItemAgent_Gun.BulletSpeed), ModifierType.PercentageAdd),
                [VtmShootDistance] = (nameof (ItemAgent_Gun.BulletDistance), ModifierType.Add),
                [VtmShootDistanceMultiplier] = (nameof (ItemAgent_Gun.BulletDistance), ModifierType.PercentageAdd),
                [VtmReloadTime] = (nameof (ItemAgent_Gun.ReloadTime), ModifierType.PercentageAdd),
                // [VtmCritRate] = (nameof (ItemAgent_Gun.CritRate), ModifierType.Add),
                [VtmCritDamageMultiplier] = (nameof (ItemAgent_Gun.CritDamageFactor), ModifierType.Add),
                [VtmArmorPiercing] = (nameof (ItemAgent_Gun.ArmorPiercing), ModifierType.Add),
                [VtmPenetrate] = (nameof (ItemAgent_Gun.Penetrate), ModifierType.Add),
                [VtmSoundRange] = (nameof (ItemAgent_Gun.SoundRange), ModifierType.PercentageAdd),
                [VtmShootSpeedMultiplier] = (nameof (ItemAgent_Gun.ShootSpeed), ModifierType.PercentageAdd),
                [VtmRecoilHMultiplier] = (nameof (ItemAgent_Gun.RecoilScaleH), ModifierType.PercentageAdd),
                [VtmRecoilVMultiplier] = (nameof (ItemAgent_Gun.RecoilScaleV), ModifierType.PercentageAdd),
                [VtmScatterMultiplier] = ("ScatterFactor", ModifierType.PercentageAdd),
                [VtmScatterADSAMultiplier] = ("ScatterFactorADS", ModifierType.PercentageAdd),

                
                [VtmBleedChance] = ("VTMC_" + VtmBleedChance, ModifierType.PercentageAdd), //通过patch
                [VtmWeight] = ("VTMC_" + VtmWeight, ModifierType.PercentageAdd),
                [VtmAmmoSave] = ("VTMC_" + VtmAmmoSave, ModifierType.Add),
                [VtmElementElectricity] = ("VTMC_" + VtmElementElectricity, ModifierType.Add),
                [VtmElementFire] = ("VTMC_" + VtmElementFire, ModifierType.Add),
                [VtmElementSpace] = ("VTMC_" + VtmElementSpace, ModifierType.Add),
                [VtmElementPoison] = ("VTMC_" + VtmElementPoison, ModifierType.Add),
            };
        }
        
        //初始化Language
        SystemLanguage language = SodaCraft.Localizations.LocalizationManager.CurrentLanguage;
        if (
            language == SystemLanguage.ChineseSimplified 
            || language == SystemLanguage.Chinese 
           || language == SystemLanguage.ChineseTraditional 
        ) {
            ModBehaviour.LogStatic("加载中文翻译...");
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("Stat_VTMC_" + VtmWeight, "重量");
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("Stat_VTMC_" + VtmAmmoSave, "弹药节省率");
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("Stat_VTMC_" + VtmElementElectricity, "电元素附加");
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("Stat_VTMC_" + VtmElementFire, "火元素附加");
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("Stat_VTMC_" + VtmElementSpace, "空间元素附加");
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("Stat_VTMC_" + VtmElementPoison, "毒元素附加");
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("Stat_VTMC_" + VtmBleedChance, "流血概率");
            
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("Unreal", "虚幻");
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("Sighted", "精准");
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("Light", "轻便");
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("Heavy", "重量级");
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("Deadly", "致命");
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("Eagle-Eye", "鹰眼");
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("Legendary", "传说");
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("Silver", "银质");
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("Chaos", "混沌");
            
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("Penetrating", "穿透");
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("Heartbroken", "碎心");
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("Thrifty", "节约");
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("Fast", "快速");
            
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("Alienated", "异化");
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("Scalding", "高热");
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("Gauss", "高斯");
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("Concentrated", "集束");
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("Portable", "便携");
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("Brutal", "残暴");
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("Cheap", "廉价");
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("Apollyon", "亚神");
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("Silent", "寂静");
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("Violent", "狂暴");
            
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("WithElectricity", "带电");
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("WithFire", "带火");
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("WithSpace", "空间");
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("WithPoison", "剧毒");
            
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("Broken", "破损");
            
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("Debug", "测试");
            
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("VTMC_FIX", "修正");
        }
        else {
            //todo english more
            foreach (string key in ModifierData.Keys) {
                SodaCraft.Localizations.LocalizationManager.SetOverrideText(key, key);
            }
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("VTMC_FIX", "Fix");
        }
        
    }
    
    public static readonly VtModifier DefaultModifier = new VtModifier();
    public static Dictionary<string, VtModifier> ModifierData;
    public static Dictionary<string, ValueTuple<string, ModifierType>>? ModifierLogic;
    // public static Dictionary<string, string> ModifierDisplayName; //暂时用于翻译部分字段
    
    
    public const string VtmDamage = "Damage"; //伤害修正
    public const string VtmDamageMultiplier = "DamageMultiplier";  //乘算的
    
    public const string VtmBulletSpeed = "BulletSpeed"; //弹速，亚门！
    public const string VtmBulletSpeedMultiplier = "BulletSpeedMultiplier";
    public const string VtmShootDistance = "ShootDistance"; //射程 加算
    public const string VtmShootDistanceMultiplier = "ShootDistanceMultiplier"; //射程 加算
    public const string VtmShootSpeedMultiplier = "ShootSpeedMultiplier";
    // public const string VtmCritRate = "CritRate"; //暴击率,暂未使用，因为官方写死了爆头才算暴击
    public const string VtmCritDamageMultiplier = "CritDamageMultiplier"; //爆伤因子,乘算
    public const string VtmArmorPiercing = "ArmorPiercing"; //穿甲等级 实际用整数
    public const string VtmPenetrate = "Penetrate"; //穿透 实际证书
    public const string VtmSoundRange = "SoundRange"; //声音
    public const string VtmReloadTime = "ReloadTime"; //换弹速度
    public const string VtmScatterMultiplier = "ScatterMultiplier";
    public const string VtmScatterADSAMultiplier = "ScatterADSMultiplier";
    public const string VtmRecoilVMultiplier = "RecoilVMultiplier";
    public const string VtmRecoilHMultiplier = "RecoilHMultiplier";
    
    
    public const string VtmBleedChance = "BleedChance"; //流血几率
    public const string VtmWeight = "Weight"; 
    public const string VtmAmmoSave = "AmmoSave";           //暂未实装
    public const string VtmPriceMultiplier = "PriceMultiplier";
    
    
    public const string VtmElementFire = "ElementFire";
    public const string VtmElementSpace = "ElementSpace";
    public const string VtmElementPoison = "ElementPoison";
    public const string VtmElementElectricity = "ElementElectricit";


    //不浮动的
    public static string[] FixedVtms = {
        VtmArmorPiercing,
        VtmPenetrate,
    };
    
    public static string[] Vtms = {
        VtmDamage,
        VtmDamageMultiplier,
        VtmBulletSpeed,
        VtmBulletSpeedMultiplier,
        VtmShootDistance,
        VtmShootDistanceMultiplier,
        VtmReloadTime,
        VtmShootSpeedMultiplier,
        VtmRecoilHMultiplier,
        VtmRecoilVMultiplier,
        VtmScatterMultiplier,
        VtmScatterADSAMultiplier,
        VtmArmorPiercing,
        VtmPenetrate,
        // VtmCritRate,
        VtmCritDamageMultiplier,
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
    
    
    public struct VtModifier {
        public int ModifierWeight = 0; //出现的权重
        public int ModifierLevel = 1; //等级从-6到6(10+) 负的代表负面的 todo
        
        public float Weight = 0f;
        public float PriceMultiplier = 0f; //不展示
        
        public float Damage = 0f;
        public float DamageMultiplier = 0f;
        public float AmmoSave = 0f;
        public float ArmorPiercing = 0;
        public float Penetrate = 0;
        public float ShootSpeed = 0f;
        public float ShootSpeedMultiplier = 0f;
        public float BulletSpeed = 0f;
        public float BulletSpeedMultiplier = 0f;
        public float ShootDistance = 0f;
        public float ShootDistanceMultiplier = 0f;
        public float ReloadTimeMultiplier = 0f;
        public float RecoilScaleVMultiplier = 0f;
        public float RecoilScaleHMultiplier = 0f;
        public float ScatterFactorADSMultiplier = 0f;
        public float ScatterFactorMultiplier = 0f;
        // public float CritRate = 0f;
        public float CritDamageMultiplier = 0f;
        public float BleedChance = 0f;
        public float SoundRange = 0f;
        
        public float ElementFire = 0f;
        public float ElementSpace = 0f;
        public float ElementPoison = 0f;
        public float ElementElectricity = 0f;

        public bool ApplyOnGuns = true;
        public bool ApplyOnHelmets = false;

        public bool ForceFixed = false;
        public VtModifier() { }

        public readonly float? GetVal(string vtm) {
            switch (vtm) {
                case VtmDamage:
                    return this.Damage;
                case VtmDamageMultiplier:
                    return this.DamageMultiplier;
                case VtmAmmoSave:
                    return this.AmmoSave;
                case VtmSoundRange:
                    return this.SoundRange;
                case VtmBulletSpeed:
                    return this.BulletSpeed;
                case VtmBulletSpeedMultiplier:
                    return this.BulletSpeedMultiplier;
                case VtmShootSpeedMultiplier:
                    return this.ShootSpeedMultiplier;
                case VtmShootDistance:
                    return this.ShootDistance;
                case VtmShootDistanceMultiplier:
                    return this.ShootDistanceMultiplier;
                case VtmReloadTime:
                    return this.ReloadTimeMultiplier;
                case VtmArmorPiercing:
                    return this.ArmorPiercing;
                case VtmPenetrate:
                    return this.Penetrate;
                // case VtmCritRate:
                //     return this.CritRate;
                case VtmCritDamageMultiplier:
                    return this.CritDamageMultiplier;
                case VtmScatterMultiplier:
                    return this.ScatterFactorMultiplier;
                case VtmScatterADSAMultiplier:
                    return this.ScatterFactorADSMultiplier;
                case VtmRecoilHMultiplier:
                    return this.RecoilScaleHMultiplier;
                case VtmRecoilVMultiplier:
                    return this.RecoilScaleVMultiplier;
                
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
                
                
                
                case VtmWeight:
                    return this.Weight;
                case VtmPriceMultiplier:
                    return this.PriceMultiplier;
            }

            return null;
        }
    }
    
}