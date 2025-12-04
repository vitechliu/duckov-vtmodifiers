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

public class VTModifiersCoreV2 {
    public static Dictionary<string, VtMKey> keys = new();

    public static void InitVtmKeys() {
        AddKey(new VtMKey("VtmDamage", "VtmDamage"));

        void AddKey(VtMKey key) {
            keys[key.key] = key;
        }
    }

    public static bool IsModifierFixed(VtModifierV2 modifier) {
        return modifier.forceFixed ? true : VTSettingManager.Setting.FixMode;
    }

    public const string ItemTagMask = "FaceMask";
    public const string ItemTagArmor = "Armor";
    public const string ItemTagHelmet = "Helmat";
    public const string ItemTagBackpack = "Backpack";
    public const string ItemTagGun = "Gun";
    public const string ItemTagMelee = "MeleeWeapon";

    public static readonly string VariableVtModifierHashCode = "VT_MODIFIER";
    public static readonly string VariableVtModifierSeedHashCode = "VT_MODIFIER_SEED";
    public static Dictionary<string, VtModifierV2> ModifierData = new();

    public static void CalcItemModifiers(Item item) {
        string modifier = item.GetString(VariableVtModifierHashCode);
        if (modifier == null) return;
        if (ModifierData.TryGetValue(modifier, out VtModifierV2 vtModifier)) {
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

            foreach (string vtmKey in vtModifier.data.Keys) {
                float value = vtModifier.data[vtmKey];
                if (TryPatchModifier(item, vtModifier, vtmKey, value)) {
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
    public const int MAGIC_ORDER = -1145141919;
    public static List<VTModifierGroup> ModifierGroups = new ();

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
                foreach (VtModifierV2 vtModifier in group.modifiers.Values) {
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
    static Dictionary<string, VtModifierV2> FilterModifierData(Item item) {
        Dictionary<string, VtModifierV2> filtered = new();
        foreach (VTModifierGroup group in ModifierGroups) {
            if (group.isCommunity && !VTSettingManager.Setting.EnableCommunityModifiers) continue;
            foreach (VtModifierV2 vtm in group.modifiers.Values) {
                if (
                    (item.Tags.Contains(ItemTagGun) && vtm.applyOnGuns)
                    || (item.Tags.Contains(ItemTagMelee) && vtm.applyOnMelee)
                    || (item.Tags.Contains(ItemTagArmor) && (vtm.applyOnArmor || vtm.applyOnEquipment))
                    || (item.Tags.Contains(ItemTagHelmet) && (vtm.applyOnHelmet || vtm.applyOnEquipment))
                    || (item.Tags.Contains(ItemTagMask) && (vtm.applyOnFaceMask || vtm.applyOnEquipment))
                    || (item.Tags.Contains(ItemTagBackpack) && (vtm.applyOnBackpack || vtm.applyOnEquipment))
                ) {
                    filtered.Add(vtm.key, vtm);
                }
            }
        }
        return filtered;
    }
    
    static string? GetAModifierByWeight(Item item) {
        int totalWeight = 0;
        Dictionary<string, VtModifierV2> filteredModifierData = FilterModifierData(item);
        foreach (VtModifierV2 modifier in filteredModifierData.Values) {
            totalWeight += modifier.weight;
        }

        int num = UnityEngine.Random.Range(0, totalWeight);
        int num2 = 0;
        foreach (KeyValuePair<string, VtModifierV2> kvp in filteredModifierData) {
            if (num > num2 && num <= num2 + kvp.Value.weight) {
                return kvp.Key;
            }
            num2 += kvp.Value.weight;
        }
        return null;
    }
    
    public static bool IsPatchedItem(Item item) {
        return item.GetString(VariableVtModifierHashCode) != null;
    }

    public static bool IsModMD(ModifierDescription modifierDescription) {
        return modifierDescription.Order == MAGIC_ORDER;
    }
    static bool TryPatchModifier(Item item, VtModifierV2 vtModifier, string key, float value) {
        if (!keys.ContainsKey(key)) return false;
        VtMKey vtMKey = keys[key];

        ModifierTarget mt = ModifierTarget.Character; //枪械是self
        Dictionary<string, ValueTuple<string, ModifierType>>? ml = null!;
        int polarity = 1;
        if (item.Tags.Contains(ItemTagGun)) {
            mt = ModifierTarget.Self;
        }
        else if (item.Tags.Contains(ItemTagMelee)) {
            mt = ModifierTarget.Self;
        }
        else {
            //元素类出现在护甲，正负逆转
            string[] elements = {
                "ElementFire",
                "ElementFire",
                "ElementPoison",
                "ElementSpace"
            };
            if (elements.Contains(key)) {
                polarity = -1;
            }
        }

        if (vtMKey.forceTarget.HasValue) mt = vtMKey.forceTarget.Value;

        string hash = vtMKey.hash;
        //特殊hash
        if (key == "Armor") {
            if (item.Tags.Contains(ItemTagHelmet) || item.Tags.Contains(ItemTagMask)) {
                hash = nameof(Health.HeadArmor);
            }

            if (item.Tags.Contains(ItemTagArmor) || item.Tags.Contains(ItemTagBackpack)) {
                hash = nameof(Health.BodyArmor);
            }
        }

        value *= polarity;
        if (item.Modifiers == null) {
            item.CreateModifiersComponent();
            ModifierDescriptionCollection mdc = item.Modifiers;
            Traverse.Create(mdc).Field("list").SetValue(new List<ModifierDescription>());
        }
        if (item.Modifiers == null) return false;
        if (item.Modifiers.Find(tmd => (tmd.Key == hash && IsModMD(tmd))) != null) {
            //避免重复添加
            return false;
        }

        
        if (!IsModifierFixed(vtModifier)) {
            value *= Random.Range(0.5f, 1f);
            if (vtMKey.roundToInt) {
                value = Mathf.Round(value);
            }
        }

        ModifierDescription md = new ModifierDescription(
            mt,
            hash,
            vtMKey.modifierType,
            value,
            false,
            MAGIC_ORDER
        );
        Traverse tmp = Traverse.Create(md);
        tmp.Field("display").SetValue(true);
        item.Modifiers.Add(md);
        return true;
    }

    public static float Modify(Item item, string key, float original) {
        if (!item) return original;
        if (!keys.ContainsKey(key)) return original;
        VtMKey vtMKey = keys[key];
        string modifier = item.GetString(VariableVtModifierHashCode);
        if (modifier != null && ModifierData.TryGetValue(modifier, out var modifierStruct)) {
            if (modifierStruct.data.TryGetValue(key, out var val)) {
                switch (vtMKey.modifierTypeCustom) {
                    case ModifierType.Add:
                        return original + val;
                    case ModifierType.PercentageAdd:
                        return original * (1f + val);
                }
            }
        }
        return original;
    }
    
    public struct VtMKey {
        public string key; //唯一键
        public string hash; //哈希
        public ModifierType modifierType = ModifierType.PercentageAdd;
        //自定义的参数的效果
        public ModifierType modifierTypeCustom = ModifierType.PercentageAdd;
        public ModifierTarget? forceTarget = null;
        public bool roundToInt = false; //是否整数

        public VtMKey(string key, string hash) {
            this.key = key;
            this.hash = hash;
        }
    }

    public struct VtModifierV2 {
        public string key;
        public string? author = null; //作者名
        public int weight = 0; //权重
        public int quality = 1; //等级从-6到10 0代表不强不弱 负的代表负面的
        public bool forceFixed = false;

        public bool applyOnGuns = false; //如果该项为true,则所有gun都会满足
        public bool applyOnMelee = false; //近战
        public bool applyOnEquipment = false; //如果该项为true,所有其他护甲（面罩背包图腾）都会满足
        public bool applyOnHelmet = false;
        public bool applyOnArmor = false;
        public bool applyOnFaceMask = false;
        public bool applyOnBackpack = false;
        
        public Dictionary<string, float> data = new();

        public VtModifierV2(string key) {
            this.key = key;
        }
    }
    public struct VTModifierGroup {
        public string author = "Official";
        public string version = "0.0.1";
        public bool isCommunity = false;
        public string key;
        public Dictionary<string, VtModifierV2> modifiers = new();

        public VTModifierGroup(string key) {
            this.key = key;
        }
    }
    public static void Log(string message, bool isError = false) {
        ModBehaviour.LogStatic(message, isError);
    }
}