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

    
    public static void InitData() {
        //todo 从config加载
        if (ModifierData == null || ModifierData.Count == 0) {
            //Debug
            ModifierData = new () {
                ["Debug"] =                   new() { ApplyOnGuns = true, ApplyOnEquipment = true, ForceFixed = true, ModifierWeight = 0, Penetrate = 2, ArmorPiercing = 2},
                
                //头盔
                ["Open"] =                    new() { ApplyOnHelmet = true, ModifierWeight = 200, ViewAngle = 0.4f, PriceMultiplier = 0.2f},

                //护甲
                ["Hard"] =                    new() { ApplyOnEquipment = true, ModifierWeight = 300, Armor = 1f, PriceMultiplier = 0.1f},
                ["Armored"] =                 new() { ApplyOnEquipment = true, ModifierWeight = 150, Armor = 2f, Weight = 0.5f, PriceMultiplier = 0.6f},
                ["Warding"] =                 new() { ForceFixed = true, ApplyOnEquipment = true, ModifierWeight = 30, Armor = 4f, Weight = 1f, PriceMultiplier = 3f},

                //面罩
                // [""] =                new() { ApplyOnFaceMask = true, ModifierWeight = 500, ViewAngle = 2f, Weight = 2f},
                
                //背包
                ["Huge"] =                    new() { ApplyOnBackpack = true, ModifierWeight = 80, InventoryCapacity = 12f, MaxWeight = 5f, PriceMultiplier = 1f},
                ["Compressed"] =              new() { ApplyOnBackpack = true, ModifierWeight = 50, InventoryCapacity = -5, Armor = 3f, PriceMultiplier = 0.3f},
                ["Expanded"] =                new() { ApplyOnBackpack = true, ModifierWeight = 150, InventoryCapacity = 5, MaxWeight = 3f, PriceMultiplier = 0.4f},
                ["High-Capacity"] =           new() { ApplyOnBackpack = true, ModifierWeight = 100, InventoryCapacity = 17, PriceMultiplier = 0.9f},

                
                //通用
                ["High-Tech"] =               new() { ApplyOnGuns = true, ApplyOnEquipment = true, ModifierWeight = 70, Armor = 2f, ViewAngle = 0.2f, Weight = -0.5f, ShootSpeedMultiplier = 0.4f, PriceMultiplier = 3f},
                ["Light"] =                   new() { ApplyOnGuns = true, ApplyOnEquipment = true, ModifierWeight = 300, Weight = -0.7f, Armor = -0.1f, Moveability = 0.1f, DamageMultiplier = -0.05f, ReloadTimeMultiplier = -0.2f, PriceMultiplier = 0.2f },
                ["Heavy"] =                   new() { ApplyOnGuns = true, ApplyOnEquipment = true, ModifierWeight = 300, Weight = 0.3f, Armor = 2f, Moveability = -0.1f, RecoilScaleVMultiplier = -0.1f, RecoilScaleHMultiplier = 0.15f, DamageMultiplier = 0.35f, PriceMultiplier = 0.3f },
                ["Legendary"] =               new() { ApplyOnGuns = true, ApplyOnEquipment = true, ModifierWeight = 20, Armor = 3f, InventoryCapacity = 5f, MaxWeight = 3f, ShootSpeedMultiplier = 0.2f, Weight = -0.3f, DamageMultiplier = 0.5f, ShootDistanceMultiplier = 0.3f, PriceMultiplier = 4f },
                ["Fast"] =                    new() { ApplyOnGuns = true, ApplyOnEquipment = true, ModifierWeight = 150, Moveability = 0.1f, ReloadTimeMultiplier = -0.3f, ShootSpeedMultiplier = 0.2f, PriceMultiplier = 0.2f },

                ["WithElectricity"] =         new() { ApplyOnGuns = true, ApplyOnEquipment = true, ModifierWeight = 100, ElementElectricity = 0.5f, PriceMultiplier = 0.5f },
                ["WithFire"] =                new() { ApplyOnGuns = true, ApplyOnEquipment = true, ModifierWeight = 100, ElementFire = 0.5f, PriceMultiplier = 0.5f },
                ["WithSpace"] =               new() { ApplyOnGuns = true, ApplyOnEquipment = true, ModifierWeight = 100, ElementSpace = 0.5f, PriceMultiplier = 0.5f },
                ["WithPoison"] =              new() { ApplyOnGuns = true, ApplyOnEquipment = true, ModifierWeight = 100, ElementPoison = 0.5f, PriceMultiplier = 0.5f },
                ["Chaos"] =                   new() { ApplyOnGuns = true, ApplyOnEquipment = true, ModifierWeight = 50, ElementElectricity = 0.2f, ElementFire = 0.2f, ElementSpace = 0.2f, ElementPoison = 0.2f, PriceMultiplier = 0.8f },
                ["Apollyon"] =                new() { ApplyOnGuns = true, ApplyOnEquipment = true, ModifierWeight = 10, ForceFixed = true, BulletSpeedMultiplier = 2f, PriceMultiplier = 0.2f },

                ["Broken"] =                  new() { ApplyOnGuns = true, ApplyOnEquipment = true, ModifierWeight = 200, Armor = -1f, DamageMultiplier = -0.2f, ShootDistanceMultiplier = -0.2f, PriceMultiplier = -0.5f },
                
                //枪械专属
                ["Unreal"] =                  new() { ApplyOnGuns = true, ModifierWeight = 100, ForceFixed = true, ShootSpeedMultiplier = 0.1f, DamageMultiplier = 0.15f, BulletSpeedMultiplier = 0.1f, CritDamageMultiplier = 0.15f, PriceMultiplier = 2.0985f },
                ["Sighted"] =                 new() { ApplyOnGuns = true, ModifierWeight = 300, ScatterFactorADSMultiplier = -0.2f, RecoilScaleHMultiplier = -0.2f, PriceMultiplier = 0.2f },
                ["Deadly"] =                  new() { ApplyOnGuns = true, ModifierWeight = 200, DamageMultiplier = 0.25f, CritDamageMultiplier = 0.25f, PriceMultiplier = 0.6f },
                ["Eagle-Eye"] =               new() { ApplyOnGuns = true, ModifierWeight = 100, DamageMultiplier = 0.1f, ShootDistanceMultiplier = 0.3f, PriceMultiplier = 0.2f },
                ["Silver"] =                  new() { ApplyOnGuns = true, ModifierWeight = 100, DamageMultiplier = 0.05f, BleedChance = 0.2f, ElementPoison = 0.1f, PriceMultiplier = 0.5f },
                ["Penetrating"] =             new() { ApplyOnGuns = true, ModifierWeight = 150, Penetrate = 1, ArmorPiercing = 1, PriceMultiplier = 0.5f },
                ["Heartbroken"] =             new() { ApplyOnGuns = true, ModifierWeight = 100, DamageMultiplier = 0.2f, ArmorPiercing = 3, PriceMultiplier = 0.8f },
                ["Thrifty"] =                 new() { ApplyOnGuns = true, ModifierWeight = 150, AmmoSave = 0.3f, PriceMultiplier = 0.3f },
               
                ["Alienated"] =               new() { ApplyOnGuns = true, ModifierWeight = 50, CritDamageMultiplier = -0.1f, ElementPoison = 0.3f, ShootSpeedMultiplier = 0.4f, PriceMultiplier = 0.1f },
                ["Scalding"] =                new() { ApplyOnGuns = true, ModifierWeight = 50, DamageMultiplier = 0.2f, Weight = 0.4f, ElementFire = 0.4f, ShootSpeedMultiplier = -0.2f, PriceMultiplier = 0.1f },
                ["Gauss"] =                   new() { ApplyOnGuns = true, ModifierWeight = 50, Penetrate = 3, ElementElectricity = 0.4f, ShootSpeedMultiplier = 0.1f, PriceMultiplier = 0.7f },
                ["Concentrated"] =            new() { ApplyOnGuns = true, ModifierWeight = 200, RecoilScaleHMultiplier = -0.5f, ShootSpeedMultiplier = 0.2f, PriceMultiplier = 0.3f },
                ["Portable"] =                new() { ApplyOnGuns = true, ModifierWeight = 300, ScatterFactorMultiplier = -0.4f, ScatterFactorADSMultiplier = 0.2f, PriceMultiplier = 0.1f },
                ["Brutal"] =                  new() { ApplyOnGuns = true, ModifierWeight = 300, DamageMultiplier = 0.4f, BleedChance = 0.3f, ShootSpeedMultiplier = -0.2f, PriceMultiplier = 0.2f },
                ["Cheap"] =                   new() { ApplyOnGuns = true, ModifierWeight = 150, DamageMultiplier = -0.1f, AmmoSave = 0.4f, Weight = -0.5f, PriceMultiplier = -0.2f },

                ["Silent"] =                  new() { ApplyOnGuns = true, ModifierWeight = 200, ShootDistanceMultiplier = -0.1f, SoundRange = -0.5f, PriceMultiplier = 0.1f },
                ["Violent"] =                 new() { ApplyOnGuns = true, ModifierWeight = 200, DamageMultiplier = 0.35f, SoundRange = 0.8f, PriceMultiplier = 0.2f },

                
                ["Broken"] =                  new() { ApplyOnGuns = true, ModifierWeight = 200, DamageMultiplier = -0.2f, ShootDistanceMultiplier = -0.2f, PriceMultiplier = -0.5f },
            };

            // VtModifier fm = new VtModifier();
            //
            // fm.ForceFixed = true;
            // fm.ModifierWeight = 0;
            // fm.Armor = 1f;
            // fm.Weight = 1f;
            // ModifierData["Full"] = fm;
        }

        if (ModifierLogicGun == null) {
            //这里的int对应Item Stat的Hash
            ModifierLogicGun = new () {
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
                [VtmViewAngle] = ("ViewAngle", ModifierType.PercentageAdd),

                
                //自定义的
                [VtmBleedChance] = ("VTMC_" + VtmBleedChance, ModifierType.PercentageAdd), //通过patch
                [VtmWeight] = ("VTMC_" + VtmWeight, ModifierType.PercentageAdd),
                [VtmAmmoSave] = ("VTMC_" + VtmAmmoSave, ModifierType.Add),
                [VtmElementElectricity] = ("VTMC_" + VtmElementElectricity, ModifierType.Add),
                [VtmElementFire] = ("VTMC_" + VtmElementFire, ModifierType.Add),
                [VtmElementSpace] = ("VTMC_" + VtmElementSpace, ModifierType.Add),
                [VtmElementPoison] = ("VTMC_" + VtmElementPoison, ModifierType.Add),
            };
        }
        
        if (ModifierLogicEquipment == null) {
            //这里的int对应Item Stat的Hash
            ModifierLogicEquipment = new () {
                
                // [VtmDamage] = (nameof (CharacterMainControl.GunDamageMultiplier), ModifierType.Add),
                // [VtmDamageMultiplier] = (nameof (CharacterMainControl.GunDamageMultiplier), ModifierType.PercentageAdd),
                [VtmBulletSpeed] = ("BulletSpeedMultiplier", ModifierType.Add), //角色拥有
                [VtmBulletSpeedMultiplier] = ("BulletSpeedMultiplier", ModifierType.PercentageAdd),
                [VtmCritDamageMultiplier] = (nameof (CharacterMainControl.GunCritDamageGain), ModifierType.Add),
                
                [VtmBodyArmor] = (nameof (Health.BodyArmor), ModifierType.Add),
                [VtmHeadArmor] = (nameof (Health.HeadArmor), ModifierType.Add),
                [VtmInventoryCapacity] = ("InventoryCapacity", ModifierType.Add),
                [VtmGasMask] = ("GasMask", ModifierType.Add),
                [VtmMaxWeight] = ("MaxWeight", ModifierType.Add),
                [VtmMoveability] = ("Moveability", ModifierType.Add),
                [VtmViewAngle] = ("ViewAngle", ModifierType.PercentageAdd),

                [VtmElementElectricity] = ("ElementFactor_Electricity", ModifierType.Add),
                [VtmElementFire] = ("ElementFactor_Fire", ModifierType.Add),
                [VtmElementSpace] = ("ElementFactor_Space", ModifierType.Add),
                [VtmElementPoison] = ("ElementFactor_Poison", ModifierType.Add),
                
                //自定义的
                [VtmWeight] = ("VTMC_" + VtmWeight, ModifierType.PercentageAdd),
            };
        }
        
        //初始化Language
        SystemLanguage language = SodaCraft.Localizations.LocalizationManager.CurrentLanguage;
        if (
            language == SystemLanguage.ChineseSimplified 
            || language == SystemLanguage.Chinese 
           || language == SystemLanguage.ChineseTraditional 
        ) {
            Log("加载中文翻译...");
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
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("High-Tech", "高科技");
            
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("Hard", "坚硬");
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("Armored", "装甲");
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("Warding", "护佑");
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("Compressed", "压缩");
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("Expanded", "扩容的");
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("Huge", "巨大");
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("High-Capacity", "高容量");
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("Open", "广角");
            
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
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("Tag_vttag", "词缀");
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("Btn_reforge", "重铸");
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("Btn_forge", "词缀附加");
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("Bubble_lack_of_coin", "重铸需求的金额不足");
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("Bubble_reforge_success", "重铸成功!");
        }
        else {
            //todo english more
            foreach (string key in ModifierData.Keys) {
                SodaCraft.Localizations.LocalizationManager.SetOverrideText(key, key);
            }
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("VTMC_FIX", "Fix");
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("Tag_vttag", "VtModifier");
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("Btn_reforge", "Reforge");
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("Btn_forge", "Forge");
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("Bubble_lack_of_coin", "Reforge missing money!");
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("Bubble_reforge_success", "Reforge Success!");
        }
    }


    public static bool IsPatchedItem(Item item) {
        return item.GetString(VariableVtModifierHashCode) != null;
    }
    
    public static bool IsModifierFixed(VTModifiersCore.VtModifier modifier) {
        return modifier.ForceFixed ? true : false;
    }
    public static string? GetAModifierByWeight(Item item) {
        int totalWeight = 0;
        Dictionary<string, VtModifier> filteredModifierData = FilterModifierData(item);
        foreach (VtModifier modifier in filteredModifierData.Values) {
            totalWeight += modifier.ModifierWeight;
        }
        int num = UnityEngine.Random.Range(0, totalWeight);
        int num2 = 0;
        foreach (KeyValuePair<string, VtModifier> kvp in filteredModifierData) {
            if (num > num2 && num <= num2 + kvp.Value.ModifierWeight) {
                return kvp.Key;
            }
            num2 += kvp.Value.ModifierWeight;
        }
        Log($"没有找到适配的ModifierData!");
        return null;
    }

    private static bool TryPatchModifier(Item item, VtModifier vtModifier, string vtm) {
        ModifierTarget mt = ModifierTarget.Character; //枪械是self
        Dictionary<string, ValueTuple<string, ModifierType>>? ml = null!;
        int polarity = 1;
        if (item.Tags.Contains(ItemTagGun)) {
            mt = ModifierTarget.Self;
            ml = ModifierLogicGun;
        }
        else {
            ml = ModifierLogicEquipment;
            string[] elements = { VtmElementElectricity, VtmElementFire, VtmElementPoison, VtmElementSpace };
            if (elements.Contains(vtm)) {
                polarity = -1;
            }
        }
        if (vtm == VtmBodyArmor && (item.Tags.Contains(ItemTagHelmet) || item.Tags.Contains(ItemTagMask))) {
            return false;
        } 
        if (vtm == VtmHeadArmor && (item.Tags.Contains(ItemTagArmor) || item.Tags.Contains(ItemTagBackpack))) {
            return false;
        }
        float? val = vtModifier.GetVal(vtm);
        if (val.HasValue) {
            val *= polarity;
            if (ml != null && ml.TryGetValue(vtm, out var vtp)) {
                if (item.Modifiers == null) item.CreateModifiersComponent();
                if (item.Modifiers == null) return false;

                if (item.Modifiers.Find(tmd => (tmd.Key == vtp.Item1 && IsModMD(tmd))) != null) {
                    //避免重复添加
                    return false;
                }

                float finalVal = (float)val;
                if (!IsModifierFixed(vtModifier)) {
                    if (!FixedVtms.Contains(vtm)) {
                        finalVal = finalVal * Random.Range(0.5f, 1f);
                    }
                    if (RoundedVtms.Contains(vtm)) {
                        finalVal = Mathf.Round(finalVal);
                    }
                }
                

                ModifierDescription md = new ModifierDescription(
                    mt,
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

    public static int ReforgePrice(Item item) {
        float plus = IsPatchedItem(item) ? VTSettingManager.Setting.ReforgePriceFactor : VTSettingManager.Setting.ForgePriceFactor; //词缀化需要5倍
        return Mathf.RoundToInt(Modify(item, VTModifiersCore.VtmPriceMultiplier, item.Value) * plus);
    }
    public static void TryUnpatchItem(Item item) {
        string modifier = item.GetString(VariableVtModifierHashCode);
        if (modifier == null) return;
        CustomDataCollection variables = Traverse.Create(item).Field("variables").GetValue<CustomDataCollection>();
        if (variables != null) {
            CustomData cd = variables.GetEntry(VariableVtModifierHashCode);
            if (cd != null) variables.Remove(cd);
            
            CustomData seedCd = variables.GetEntry(VariableVtModifierSeedHashCode);
            if (seedCd != null) variables.Remove(seedCd);
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
        Log($"移除词缀:{item.DisplayName}成功，同时移除了{removedModifiersCount}个原生Modifier");
    }

    public static void Log(string message, bool isError = false) {
        ModBehaviour.LogStatic(message, isError);
    }

    public static bool ItemCanBePatched(Item item) {
        return item.Tags.Contains(ItemTagGun)
               || item.Tags.Contains(ItemTagHelmet)
               || item.Tags.Contains(ItemTagArmor)
               || item.Tags.Contains(ItemTagMask)
               || item.Tags.Contains(ItemTagBackpack);
    }
    public static string PatchItem(Item item, Sources source) {
        if (ItemCanBePatched(item)) {
            switch (source) {
                case Sources.LootBox:
                    if (!VT.Probability(VTSettingManager.Setting.LootBoxPatchedPercentage)) return null;
                    break;
                case Sources.Enemy:
                    if (!VT.Probability(VTSettingManager.Setting.EnemyPatchedPercentage)) return null;
                    break;
                case Sources.Craft:
                    if (!VT.Probability(VTSettingManager.Setting.CraftPatchedPercentage)) return null;
                    break;
            }
            string? modifier = GetAModifierByWeight(item);
            if (modifier != null) {
                return PatchItem(item, source, modifier);
            }
            // Log($"未找到Modifier 无法Patch");
        }
        return null;
    }

    public static string PatchItem(Item item, Sources source, string modifier) {
        item.SetString(VariableVtModifierHashCode, modifier, true);
        // Tag vtTag = new Tag()
        // item.Tags.Add("vttag");
        string modifierDisplayName = modifier.ToPlainText();
        if (VTSettingManager.Setting.Debug)
            Log($"注入:{item.DisplayName}为{modifierDisplayName}, source:{source}");
        CalcItemModifiers(item);
        return modifier;
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
                    // Log($"注入了Modifier:{item.DisplayName}_{vtm}");
                }
            }
            if (flag) {
                item.Modifiers.ReapplyModifiers();
            }
            Random.state = originalState;
        }
        else {
            Log($"找不到modifier:{modifier}");
        }
        
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
        if (!item) return original;
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
        Reforge, //重铸的
    }

    
    static Dictionary<string, VtModifier> FilterModifierData(Item item) {
        Dictionary<string, VtModifier> filtered = new();
        
        foreach (KeyValuePair<string, VtModifier> kvp in ModifierData) {
            VtModifier vtm = kvp.Value;
            if (
                (item.Tags.Contains(ItemTagGun) && vtm.ApplyOnGuns)
                || (item.Tags.Contains(ItemTagArmor) && (vtm.ApplyOnArmor || vtm.ApplyOnEquipment))
                || (item.Tags.Contains(ItemTagHelmet) && (vtm.ApplyOnHelmet || vtm.ApplyOnEquipment))
                || (item.Tags.Contains(ItemTagMask) && (vtm.ApplyOnFaceMask || vtm.ApplyOnEquipment))
                || (item.Tags.Contains(ItemTagBackpack) && (vtm.ApplyOnBackpack || vtm.ApplyOnEquipment))
            ) {
                filtered.Add(kvp.Key, vtm);
            } 
        }
        return filtered;
    }
    
    // public static readonly VtModifier DefaultModifier = new VtModifier();
    public static Dictionary<string, VtModifier> ModifierData;
    public static Dictionary<string, ValueTuple<string, ModifierType>>? ModifierLogicGun;
    public static Dictionary<string, ValueTuple<string, ModifierType>>? ModifierLogicEquipment; //身体类
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
    
    //护甲
    public const string VtmArmor = "Armor";
    public const string VtmBodyArmor = "BodyArmor";
    public const string VtmHeadArmor = "HeadArmor";
    public const string VtmViewAngle = "ViewAngle";
    public const string VtmGasMask = "GasMask";
    public const string VtmInventoryCapacity = "InventoryCapacity";
    public const string VtmMaxWeight = "MaxWeight";
    public const string VtmMoveability = "Moveability";

    
    public const string VtmBleedChance = "BleedChance"; //流血几率
    public const string VtmWeight = "Weight"; 
    public const string VtmAmmoSave = "AmmoSave";           //暂未实装
    public const string VtmPriceMultiplier = "PriceMultiplier";
    
    //元素
    public const string VtmElementFire = "ElementFire";
    public const string VtmElementSpace = "ElementSpace";
    public const string VtmElementPoison = "ElementPoison";
    public const string VtmElementElectricity = "ElementElectricit";
    

    //不浮动的
    public static string[] FixedVtms = {
    };
    
    //变成整型的
    public static string[] RoundedVtms = {
        VtmArmorPiercing,
        VtmPenetrate,
        // VtmMaxWeight,
        VtmInventoryCapacity,
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
        
        VtmBodyArmor,
        VtmHeadArmor,
        VtmViewAngle,
        VtmGasMask,
        VtmInventoryCapacity,
        VtmMaxWeight,
        VtmMoveability,
    };

    public const string ItemTagMask = "FaceMask";
    public const string ItemTagArmor = "Armor";
    public const string ItemTagHelmet = "Helmat";    
    public const string ItemTagBackpack = "Backpack";
    public const string ItemTagGun = "Gun";
    
    public struct VtModifier {
        public int ModifierWeight = 0; //出现的权重
        public int ModifierLevel = 1; //等级从-6到6(10+) 负的代表负面的 todo
        
        public float? Weight = null;
        public float? PriceMultiplier = null; //不展示
        
        public float? Damage = null;
        public float? DamageMultiplier = null;
        public float? AmmoSave = null;
        public float? ArmorPiercing = null;
        public float? Penetrate = null;
        public float? ShootSpeed = null;
        public float? ShootSpeedMultiplier = null;
        public float? BulletSpeed = null;
        public float? BulletSpeedMultiplier = null;
        public float? ShootDistance = null;
        public float? ShootDistanceMultiplier = null;
        public float? ReloadTimeMultiplier = null;
        public float? RecoilScaleVMultiplier = null;
        public float? RecoilScaleHMultiplier = null;
        public float? ScatterFactorADSMultiplier = null;
        public float? ScatterFactorMultiplier = null;
        // public float CritRate;
        public float? CritDamageMultiplier = null;
        public float? BleedChance = null;
        public float? SoundRange = null;
        
        public float? ElementFire = null;
        public float? ElementSpace = null;
        public float? ElementPoison = null;
        public float? ElementElectricity = null;

        public float? Armor = null; //灵活的
        // public float? BodyArmor = null;
        // public float? HeadArmor = null;
        public float? InventoryCapacity = null;
        public float? MaxWeight = null; //最大负重
        public float? ViewAngle = null;
        public float? GasMask = null;
        public float? Moveability = null;

        public bool ApplyOnGuns = false; //如果该项为true,则所有gun都会满足
        public bool ApplyOnEquipment = false; //如果该项为true,所有其他护甲（面罩背包图腾）都会满足
        public bool ApplyOnHelmet = false;
        public bool ApplyOnArmor = false;
        public bool ApplyOnFaceMask = false;
        public bool ApplyOnBackpack = false;

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
                
                case VtmMaxWeight:
                    return this.MaxWeight;
                case VtmBodyArmor:
                case VtmHeadArmor:
                    return this.Armor;
                case VtmInventoryCapacity:
                    return this.InventoryCapacity;
                case VtmViewAngle:
                    return this.ViewAngle;
                case VtmGasMask:
                    return this.GasMask;
                case VtmMoveability:
                    return this.Moveability;
                
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