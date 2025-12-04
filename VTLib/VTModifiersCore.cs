using Duckov.Utilities;
using HarmonyLib;
using ItemStatsSystem;
using ItemStatsSystem.Stats;
using Newtonsoft.Json;
using SodaCraft.Localizations;
using UnityEngine;
using Random = UnityEngine.Random;

// ReSharper disable All

namespace VTModifiers.VTLib;

public class VTModifiersCore {
    //通用

    public static readonly string VariableVtModifierHashCode = "VT_MODIFIER";
    public static readonly string VariableVtModifierSeedHashCode = "VT_MODIFIER_SEED";

    //测试用，导出当前的
    static void ExportCurrent() {
        Dictionary<string, VtModifier> exports = new();
        foreach (string mkey in ModifierData.Keys) {
            VtModifier tmp = ModifierData[mkey];
            tmp.key = mkey;
            exports[mkey] = tmp;
        }
        VTModifierGroup group = new VTModifierGroup("default") {
            modifiers = exports
        };
        
        JsonSerializerSettings settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented // Optional: for readable output
        };
        File.WriteAllText(Path.Combine(ModBehaviour.Instance._resourceDirectory, "default_0_5_1.json"),
            JsonConvert.SerializeObject(group, settings));
    }
    

    public static void LoadFromConfig() {
        ModifierData.Clear();
        ModifierGroups.Clear();
        string directoryPath = Path.Combine(ModBehaviour.Instance._resourceDirectory, "modifiers");
        string[] jsonFiles = Directory.GetFiles(directoryPath, "*.json");
        int loadedCount = 0;
        foreach (string path in jsonFiles) { 
            try {
                string jsonContent = File.ReadAllText(path);
                VTModifierGroup group = JsonConvert.DeserializeObject<VTModifierGroup>(jsonContent);
                ModifierGroups.Add(group);
                VT.Log($"加载来自{group.author}的{group.key}词缀组...");
                foreach (VtModifier vtModifier in group.modifiers.Values) {
                    if (ModifierData.ContainsKey(vtModifier.key)) {
                        VT.Log($"词缀键重复:{vtModifier.key}");
                        continue;
                    }
                    ModifierData[vtModifier.key] = vtModifier;
                    loadedCount++;
                }
            }
            catch (Exception ex) {
                VT.Log($"ModifierJSON解析错误， {path}: {ex.Message}");
            }
        }
        
    }
    public static void InitData() {
        if (ModifierData.Count == 0) {
            LoadFromConfig();
        }
        if (ModifierLogicGun == null) {
            //这里的int对应Item Stat的Hash
            ModifierLogicGun = new() {
                [VtmDamage] = (nameof(ItemAgent_Gun.Damage), ModifierType.Add),
                [VtmDamageMultiplier] = (nameof(ItemAgent_Gun.Damage), ModifierType.PercentageAdd),
                [VtmBulletSpeedMultiplier] = (nameof(ItemAgent_Gun.BulletSpeed), ModifierType.PercentageAdd),
                [VtmShootDistanceMultiplier] = (nameof(ItemAgent_Gun.BulletDistance), ModifierType.PercentageAdd),
                [VtmReloadTime] = (nameof(ItemAgent_Gun.ReloadTime), ModifierType.PercentageAdd),
                // [VtmCritRate] = (nameof (ItemAgent_Gun.CritRate), ModifierType.Add),
                [VtmCritDamageMultiplier] = (nameof(ItemAgent_Gun.CritDamageFactor), ModifierType.Add),
                [VtmArmorPiercing] = (nameof(ItemAgent_Gun.ArmorPiercing), ModifierType.Add),
                [VtmPenetrate] = (nameof(ItemAgent_Gun.Penetrate), ModifierType.Add),
                [VtmSoundRange] = (nameof(ItemAgent_Gun.SoundRange), ModifierType.PercentageAdd),
                [VtmShootSpeedMultiplier] = (nameof(ItemAgent_Gun.ShootSpeed), ModifierType.PercentageAdd),
                [VtmRecoilHMultiplier] = (nameof(ItemAgent_Gun.RecoilScaleH), ModifierType.PercentageAdd),
                [VtmRecoilVMultiplier] = (nameof(ItemAgent_Gun.RecoilScaleV), ModifierType.PercentageAdd),
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
            ModifierLogicEquipment = new() {
                // [VtmDamage] = (nameof (CharacterMainControl.GunDamageMultiplier), ModifierType.Add),
                // [VtmDamageMultiplier] = (nameof (CharacterMainControl.GunDamageMultiplier), ModifierType.PercentageAdd),
                [VtmBulletSpeedMultiplier] = ("BulletSpeedMultiplier", ModifierType.PercentageAdd),
                [VtmCritDamageMultiplier] = (nameof(CharacterMainControl.GunCritDamageGain), ModifierType.Add),

                [VtmBodyArmor] = (nameof(Health.BodyArmor), ModifierType.Add),
                [VtmHeadArmor] = (nameof(Health.HeadArmor), ModifierType.Add),
                [VtmInventoryCapacity] = ("InventoryCapacity", ModifierType.Add),
                [VtmGasMask] = ("GasMask", ModifierType.Add),
                [VtmMaxWeight] = ("MaxWeight", ModifierType.Add),
                [VtmMoveability] = ("Moveability", ModifierType.Add),
                [VtmViewAngle] = ("ViewAngle", ModifierType.PercentageAdd),
                [VtmMaxStamina] = ("Stamina", ModifierType.Add),

                [VtmElementElectricity] = ("ElementFactor_Electricity", ModifierType.Add),
                [VtmElementFire] = ("ElementFactor_Fire", ModifierType.Add),
                [VtmElementSpace] = ("ElementFactor_Space", ModifierType.Add),
                [VtmElementPoison] = ("ElementFactor_Poison", ModifierType.Add),

                //自定义的
                [VtmWeight] = ("VTMC_" + VtmWeight, ModifierType.PercentageAdd),
            };
        }
        if (ModifierLogicMelee == null) {
            //这里的int对应Item Stat的Hash
            ModifierLogicMelee = new() {
                [VtmDamage] = (nameof(ItemAgent_MeleeWeapon.Damage), ModifierType.Add),
                [VtmDamageMultiplier] = (nameof(ItemAgent_MeleeWeapon.Damage), ModifierType.PercentageAdd),
                [VtmShootDistanceMultiplier] = (nameof(ItemAgent_MeleeWeapon.AttackRange), ModifierType.PercentageAdd),
                [VtmCritRate] = (nameof(ItemAgent_MeleeWeapon.CritRate), ModifierType.Add),
                [VtmCritDamageMultiplier] = (nameof(ItemAgent_MeleeWeapon.CritDamageFactor), ModifierType.Add),
                [VtmArmorPiercing] = (nameof(ItemAgent_MeleeWeapon.ArmorPiercing), ModifierType.Add),
                [VtmShootSpeedMultiplier] = (nameof(ItemAgent_MeleeWeapon.AttackSpeed), ModifierType.PercentageAdd),
                [VtmBleedChance] = (nameof(ItemAgent_MeleeWeapon.BleedChance), ModifierType.PercentageAdd),
                [VtmStaminaCost] = (nameof(ItemAgent_MeleeWeapon.StaminaCost), ModifierType.PercentageAdd),

                //自定义的
                [VtmWeight] = ("VTMC_" + VtmWeight, ModifierType.PercentageAdd),
                // [VtmElementElectricity] = ("VTMC_" + VtmElementElectricity, ModifierType.Add),
                // [VtmElementFire] = ("VTMC_" + VtmElementFire, ModifierType.Add),
                // [VtmElementSpace] = ("VTMC_" + VtmElementSpace, ModifierType.Add),
                // [VtmElementPoison] = ("VTMC_" + VtmElementPoison, ModifierType.Add),
            };
        }
    }


    public static bool IsPatchedItem(Item item) {
        return item.GetString(VariableVtModifierHashCode) != null;
    }

    static bool IsModifierFixed(VTModifiersCore.VtModifier modifier) {
        return modifier.ForceFixed ? true : VTSettingManager.Setting.FixMode;
    }

    static string? GetAModifierByWeight(Item item) {
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
        else if (item.Tags.Contains(ItemTagMelee)) {
            mt = ModifierTarget.Self;
            ml = ModifierLogicMelee;
        }
        else {
            ml = ModifierLogicEquipment;
            string[] elements = { VtmElementElectricity, VtmElementFire, VtmElementPoison, VtmElementSpace };
            if (elements.Contains(vtm)) {
                polarity = -1;
            }
        }
        if (ForceCharacterVtms.Contains(vtm)) {
            mt = ModifierTarget.Character;
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
                if (item.Modifiers == null) {
                    item.CreateModifiersComponent();
                    ModifierDescriptionCollection mdc = item.Modifiers;
                    Traverse.Create(mdc).Field("list").SetValue(new List<ModifierDescription>());
                }

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
        float plus = IsPatchedItem(item)
            ? VTSettingManager.Setting.ReforgePriceFactor
            : VTSettingManager.Setting.ForgePriceFactor; //词缀化需要5倍
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
               || item.Tags.Contains(ItemTagMelee)
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
                case Sources.SCAV:
                    if (!VT.Probability(VTSettingManager.Setting.SCAVPercentage)) return null;
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
        SCAV, //SCAV
        Reforge, //重铸的
    }


    static Dictionary<string, VtModifier> FilterModifierData(Item item) {
        Dictionary<string, VtModifier> filtered = new();

        foreach (KeyValuePair<string, VtModifier> kvp in ModifierData) {
            VtModifier vtm = kvp.Value;
            if (
                (item.Tags.Contains(ItemTagGun) && vtm.ApplyOnGuns)
                || (item.Tags.Contains(ItemTagMelee) && vtm.ApplyOnMelee)
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
    public static Dictionary<string, VtModifier> ModifierData = new ();
    public static List<VTModifierGroup> ModifierGroups = new ();
    public static Dictionary<string, ValueTuple<string, ModifierType>>? ModifierLogicGun;
    public static Dictionary<string, ValueTuple<string, ModifierType>>? ModifierLogicMelee;
    public static Dictionary<string, ValueTuple<string, ModifierType>>? ModifierLogicEquipment; //身体类
    // public static Dictionary<string, string> ModifierDisplayName; //暂时用于翻译部分字段


    public const string VtmDamage = "Damage"; //伤害修正
    public const string VtmDamageMultiplier = "DamageMultiplier"; //乘算的

    public const string VtmBulletSpeedMultiplier = "BulletSpeedMultiplier";
    public const string VtmShootDistanceMultiplier = "ShootDistanceMultiplier"; //射程
    public const string VtmShootSpeedMultiplier = "ShootSpeedMultiplier"; //射速/使用速度(近战)
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
    public const string VtmAmmoSave = "AmmoSave"; //暂未实装
    public const string VtmPriceMultiplier = "PriceMultiplier";

    //元素
    public const string VtmElementFire = "ElementFire";
    public const string VtmElementSpace = "ElementSpace";
    public const string VtmElementPoison = "ElementPoison";
    public const string VtmElementElectricity = "ElementElectricity";

    //近战
    public const string VtmStaminaCost = "StaminaCost";
    public const string VtmMaxStamina = "MaxStamina";
    public const string VtmCritRate = "CritRate"; //暴击率,枪械暂未使用，因为官方写死了爆头才算暴击

    
    //这类强制ModifierType为Character
    public static string[] ForceCharacterVtms = {
        VtmMoveability,
    };
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
        VtmBulletSpeedMultiplier,
        VtmCritRate,
        VtmShootDistanceMultiplier,
        VtmReloadTime,
        VtmShootSpeedMultiplier,
        VtmRecoilHMultiplier,
        VtmRecoilVMultiplier,
        VtmScatterMultiplier,
        VtmScatterADSAMultiplier,
        VtmArmorPiercing,
        VtmPenetrate,
        VtmCritRate,
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

        VtmStaminaCost,
        VtmMaxStamina,

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
    public const string ItemTagMelee = "MeleeWeapon";

    
    public struct VTModifierGroup {
        public string author = "Official";
        public string version = "0.0.1";
        public bool isCommunity = false;
        public string key;
        public Dictionary<string, VtModifier> modifiers = new();

        public VTModifierGroup(string key) {
            this.key = key;
        }
    }
    
    
    //2.0
    
    
    
    
    
    public struct VtModifier {
        public string key; //唯一键
        private string? author = null; //作者名
        public int ModifierWeight = 0; //权重
        public int ModifierLevel = 1; //等级从-6到10 负的代表负面的

        public float? Weight = null; //重量修正
        public float? PriceMultiplier = null; //价格倍率

        public float? Damage = null; 
        public float? DamageMultiplier = null;
        public float? AmmoSave = null; //弹药节省率
        public float? ArmorPiercing = null; //穿甲等级(实际为整数)
        public float? Penetrate = null; //穿透(实际为整数)
        // public float? ShootSpeed = null;
        public float? ShootSpeedMultiplier = null; //射速倍率
        // public float? BulletSpeed = null; 
        public float? BulletSpeedMultiplier = null;
        // public float? ShootDistance = null;
        public float? ShootDistanceMultiplier = null;
        public float? ReloadTimeMultiplier = null;
        public float? RecoilScaleVMultiplier = null;
        public float? RecoilScaleHMultiplier = null;
        public float? ScatterFactorADSMultiplier = null;
        public float? ScatterFactorMultiplier = null;
        public float? CritRate = null;
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

        public float? StaminaCost = null; //耐力消耗
        public float? MaxStamina = null; //最大耐力

        public bool ApplyOnGuns = false; //如果该项为true,则所有gun都会满足
        public bool ApplyOnMelee = false; //近战
        public bool ApplyOnEquipment = false; //如果该项为true,所有其他护甲（面罩背包图腾）都会满足
        public bool ApplyOnHelmet = false;
        public bool ApplyOnArmor = false;
        public bool ApplyOnFaceMask = false;
        public bool ApplyOnBackpack = false;

        public bool ForceFixed = false;

        public VtModifier(string key = "") {
            this.key = key;
        }

        public readonly float? GetVal(string vtm) {
            switch (vtm) {
                case VtmDamage:
                    return this.Damage;
                case VtmDamageMultiplier:
                    return this.DamageMultiplier * VTSettingManager.Setting.DamageThreshold;
                case VtmAmmoSave:
                    return this.AmmoSave;
                case VtmSoundRange:
                    return this.SoundRange;
                // case VtmBulletSpeed:
                //     return this.BulletSpeed;
                case VtmBulletSpeedMultiplier:
                    return this.BulletSpeedMultiplier;
                case VtmShootSpeedMultiplier:
                    return this.ShootSpeedMultiplier;
                case VtmShootDistanceMultiplier:
                    return this.ShootDistanceMultiplier;
                case VtmReloadTime:
                    return this.ReloadTimeMultiplier;
                case VtmArmorPiercing:
                    return this.ArmorPiercing;
                case VtmPenetrate:
                    return this.Penetrate;
                case VtmCritRate:
                    return this.CritRate;
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
                    return this.Armor * VTSettingManager.Setting.ArmorThreshold;
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

                case VtmStaminaCost:
                    return this.StaminaCost;
                case VtmMaxStamina:
                    return this.MaxStamina;
                
                case VtmWeight:
                    return this.Weight;
                case VtmPriceMultiplier:
                    return this.PriceMultiplier;
            }

            return null;
        }
    }
}