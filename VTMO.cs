using ItemStatsSystem;
using UnityEngine;
using HarmonyLib;
using Duckov.UI;
using Duckov.Utilities;
using FX;
using ItemStatsSystem.Data;
using ItemStatsSystem.Items;
using SodaCraft.Localizations;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VTLib.ThirdParty;
using VTModifiers.VTLib;
using VTModifiers.VTLib.Items;
using VTLib;
using VTModifiers.Patches;
using VTModifiers.ThirdParty;

// ReSharper disable Unity.PerformanceCriticalCodeInvocation

namespace VTModifiers;

[HarmonyPatch]
public class VTMO : VTModBehaviour<VTMO> {
    public override string ModName => "VTModifiers";
    public override string Version => "0.8.0";

    // public string _logFilePath;
    public string _modifiersDirectoryPersistant = null!;
    public string _modifiersDirectoryCustom = null!;

    public GameObject coreObj;
    public Harmony _harmony;

    public void Update() {
        if (Input.GetKeyDown(VTSettingManager.Setting.ReforgeKey)) {
            if (LevelManager.Instance) {
                if (LevelManager.Instance.IsBaseLevel) ReforgePatch.KeyReforge();
            }
        }
    }

    //OnHurtPatch，解决吸血、耐久等问题
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Health), "Hurt")]
    public static void Health_Hurt_PrePatch(Health __instance, ref DamageInfo damageInfo) {
        //闪避
        CharacterMainControl character = __instance.TryGetCharacter();
        if (!character) return;
        Item armor = character.GetArmorItem();
        if (armor && damageInfo.damageValue > 0f) {
            float dodgeRate = VTModifiersCoreV2.Modify(armor, VTModifiersCoreV2.VtmDodgeRate);
            if (VT.Probability(dodgeRate)) {
                // if (VTSettingManager.Setting.Debug) {
                //     VTMO.Log($"闪避触发！");
                // }
                PopText.Pop("VTMC_Dodged".ToPlainText(),
                    character.transform.position + Vector3.up * 2f, Color.white, 1f, null);
                damageInfo.damageValue = 0f;
            }
        }

        if (
            damageInfo.damageType != DamageTypes.realDamage
            && damageInfo is { ignoreArmor: false, armorBreak: > 0 }) {
            Item helmet = character.GetHelmatItem();
            float helmetEnduranceProb = 0f;
            float armorEnduranceProb = 0f;
            if (helmet) {
                helmetEnduranceProb = VTModifiersCoreV2.Modify(helmet, VTModifiersCoreV2.VtmEndurance, 0f);
            }

            if (armor) {
                armorEnduranceProb = VTModifiersCoreV2.Modify(armor, VTModifiersCoreV2.VtmEndurance, 0f);
            }

            float fnEnduranceProb = (armorEnduranceProb + helmetEnduranceProb) / 2;
            if (fnEnduranceProb > 0 && VT.Probability(fnEnduranceProb)) {
                // if (VTSettingManager.Setting.Debug) {
                //     VTMO.Log($"耐久触发！");
                // }
                damageInfo.armorBreak = 0f;
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Health), "Hurt")]
    public static void Health_Hurt_PostPatch(Health __instance, DamageInfo damageInfo) {
        if (
            damageInfo.finalDamage > 0
            && damageInfo.fromCharacter
            && damageInfo.fromCharacter.CurrentHoldItemAgent
            && damageInfo.fromCharacter.CurrentHoldItemAgent.Item
        ) {
            Item holdItem = damageInfo.fromCharacter.CurrentHoldItemAgent.Item;
            float lifeSteal = VTModifiersCoreV2.Modify(holdItem, VTModifiersCoreV2.VtmLifeSteal, 0f);
            if (lifeSteal > 0) {
                float lifeStealAmount = lifeSteal * damageInfo.finalDamage;
                // if (VTSettingManager.Setting.Debug) {
                //     VTMO.Log($"LifeSteal:{lifeStealAmount}");
                // }
                PopText.Pop(lifeStealAmount.ToString("F1"),
                    damageInfo.fromCharacter.transform.position + Vector3.up * 2f, Color.green, 1f, null);
                damageInfo.fromCharacter.AddHealth(lifeStealAmount);
            }
        }
    }

    //ItemOperationMenu 物品操作相关Button
    public static Button btn_Reforge = null!;


    //词缀化来源：敌人生成
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CharacterSpawnerRoot), "AddCreatedCharacter")]
    public static void CharacterSpawnerRoot_AddCreatedCharacter_PostFix(
        CharacterSpawnerRoot __instance,
        CharacterMainControl c
    ) {
        if (LevelManager.Instance.IsBaseLevel) return;
        int csrInstanceId = __instance.GetInstanceID();
        if (c.CharacterItem && c.CharacterItem.Inventory) {
            Inventory inventory = c.CharacterItem.Inventory;

            foreach (Item item in inventory) {
                VTModifiersCoreV2.PatchItem(item, VTModifiersCoreV2.Sources.Enemy);
            }

            foreach (Slot slot in c.CharacterItem.Slots) {
                if (slot.Content == null) continue;
                VTModifiersCoreV2.PatchItem(slot.Content, VTModifiersCoreV2.Sources.Enemy);
            }
            // VTMO.Log($"CSRSetup:{csrInstanceId}, itemCount:{itemCount}");
        }
        else {
            // VTMO.Log($"CSRSetupFailed:{csrInstanceId}, cannot find inventory");
        }
    }

    //词缀化来源：物资箱
    [HarmonyPostfix]
    [HarmonyPatch(typeof(LootBoxLoader), "Setup")]
    public static void LootBoxLoader_Setup_PostFix(LootBoxLoader __instance) {
        int lootBoxLoaderId = __instance.GetInstanceID();
        InteractableLootbox lootbox =
            __instance._lootBox;

        if (lootbox != null) {
            string lootBoxName = lootbox.InteractName;
            Inventory inventory = lootbox.Inventory;
            if (inventory != null) {
                int inventoryCount = inventory.Count();
                // VTMO.Log($"LBLSetup:{lootBoxLoaderId}, name:{lootBoxName}, count:{inventoryCount}");
                foreach (Item item in inventory) {
                    VTModifiersCoreV2.PatchItem(item, VTModifiersCoreV2.Sources.LootBox);
                }
            }
            else {
                // VTMO.Log($"LBLSetupFailed:{lootBoxLoaderId}, name:{lootBoxName},, nullInventory");
            }
        }
        else {
            // VTMO.Log($"LBLSetupFailed:{lootBoxLoaderId}, nullLootBox");
        }
    }

    //词缀化来源：合成
    public void OnItemCrafted(CraftingFormula formula, Item item) {
        VTModifiersCoreV2.PatchItem(item, VTModifiersCoreV2.Sources.Craft);
    }

    //词缀化来源：地面词缀卡
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CharacterItemControl), "PickupItem")]
    public static void CharacterItemControl_PickupItem_PostPatch(
        CharacterItemControl __instance,
        Item item,
        bool __result
    ) {
        if (__result) {
            if (item
                && VTModifiersCoreV2.IsModifiersCard(item)
                && item.FromInfoKey == "Ground"
                && !VTModifiersCoreV2.IsPatchedItem(item)) {
                VTModifiersCoreV2.PatchItem(item, VTModifiersCoreV2.Sources.Card);
            }
        }
    }

    static Color VTLabelColor = Color.magenta;
    static Color VTLabelColorLight = new Color(1f, 0.6f, 0.9f);
    static Color VTLabelColorDefault = Color.white;

    //修复整数被保留多位小数
    //物品InventoryView键值对UI改颜色
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ItemModifierEntry), "Refresh")]
    public static void ItemModifierEntry_Refresh_PostFix(ItemModifierEntry __instance) {
        TextMeshProUGUI labelGUI = __instance.displayName;
        TextMeshProUGUI valueGUI = __instance.value;
        string label = labelGUI.text;
        valueGUI.text = VT.RoundToOneDecimalIfNeeded(valueGUI.text);
        if (label.StartsWith("VTM_")) {
            labelGUI.color = VTLabelColorLight;
            labelGUI.text = labelGUI.text.Substring(4);
        }
        else if (label.StartsWith("VTMC_")) {
            labelGUI.color = VTLabelColorLight;
            labelGUI.text = labelGUI.text.Substring(5);
        }
        else {
            labelGUI.color = VTLabelColorDefault;
        }
    }

    //物品HoveringUI参数键值对UI改颜色
    [HarmonyPostfix]
    [HarmonyPatch(typeof(LabelAndValue), "Setup")]
    public static void LabelAndValue_Setup_PostFix(LabelAndValue __instance, string label, Polarity valuePolarity) {
        TextMeshProUGUI labelGUI = __instance.labelText;
        TextMeshProUGUI valueGUI = __instance.valueText;
        valueGUI.text = VT.RoundToOneDecimalIfNeeded(valueGUI.text);

        if (label.StartsWith("VTM_")) {
            labelGUI.color = VTLabelColor;
            labelGUI.text = labelGUI.text.Substring(4);
        }
        else if (label.StartsWith("VTMC_")) {
            labelGUI.color = VTLabelColor;
            labelGUI.text = labelGUI.text.Substring(5);
        }
        else {
            labelGUI.color = VTLabelColorDefault;
        }
    }


    public const string MOD_VTMAGIC = "VTMagic";
    public const string MOD_ELEMENT = "VTElements";
    public const string MOD_SETTING = "ModSetting";
    public const string MOD_CILV = "CustomItemLevelValue";
    public const string MOD_SCAV = "RandomNpc";
    
    protected override void OnAfterSetup() {
        base.OnAfterSetup();
        LoadPathCustom();
        ReadLang();
        VTSettingManager.LoadSetting();
        RegisterDebouncer(VTSettingManager.OnSettingChanged, 1000);
        VTModifiersCoreV2.InitData();
        ItemUtil.InitItem();
        LoadFormulas();
        RegisterEvents();
        _harmony = new Harmony("com.vitech.duckov_vt_modifiers_patch");
        _harmony.PatchAll();
        if (coreObj == null) coreObj = new GameObject("VTModifier_Core_Instance");
        coreObj.AddComponent<VTModifiersUI>();
        DontDestroyOnLoad(coreObj);
        TryInitSetting();
        RegisterModConnector(new ModListener(MOD_VTMAGIC) {
            predicate = modInfo => AssemblyHelper.IsAssemblyLoaded(modInfo.name)
        });
        RegisterModConnector(new ModListener(MOD_ELEMENT) {
            predicate = modInfo => AssemblyHelper.IsAssemblyLoaded(modInfo.name)
        });
        RegisterModConnector(new ModListener(MOD_SETTING) {
            predicate = modInfo => AssemblyHelper.IsAssemblyLoaded(modInfo.name) && !ModSettingAPI.IsInit,
            onConnect = modInfo => TryInitSetting()
        });
        RegisterModConnector(new ModListener(MOD_CILV) {
            predicate = modInfo => AssemblyHelper.IsAssemblyLoaded(modInfo.name)
        });
        RegisterModConnector(new ModListener(MOD_SCAV) {
            predicate = modInfo => AssemblyHelper.IsAssemblyLoaded(modInfo.name),
            onConnect = modInfo => {
                coreObj.AddComponent<LootBoxEventListener>();
            },
            onDisconnect = modInfo => {
                if (coreObj) {
                    LootBoxEventListener el = coreObj.GetComponent<LootBoxEventListener>();
                    if (el) {
                        Destroy(el);
                    }
                }
            }
        });
    }

    protected override void OnBeforeDeactivate() {
        _harmony.UnpatchAll(_harmony.Id);
        ItemUtil.UnloadItems();
        UnregisterEvents();
        if (btn_Reforge != null) Destroy(btn_Reforge);
        if (coreObj != null) Destroy(coreObj);
        VTMO.Log("模组已卸载");
        base.OnBeforeDeactivate();
    }

    //配方
    void LoadFormulas() {
        if (!VTSettingManager.Setting.EnableModifiersCard) return;
        AddFormulaSimple(0, new[] { (754, 1), (308, 10), (58, 1) }, ItemUtil.MC_CARD_v1);
        AddFormulaSimple(0, new[] { (755, 1), (309, 10), (58, 1) }, ItemUtil.MC_CARD_v2);
        AddFormulaSimple(0, new[] { (756, 1), (1165, 30), (58, 1) }, ItemUtil.MC_CARD_v3);
    }

    void TryInitSetting() {
        if (!ModSettingAPI.IsInit) {
            if (!ModSettingAPI.Init(info)) return;
            ModSettingConnector.Init();
        }
        ModSettingConnector.TryInitSCAV();
    }
    
    void RegisterEvents() {
        CraftingManager.OnItemCrafted += OnItemCrafted;
        ItemUtilities.OnItemSentToPlayerInventory += OnItemSentToPlayerInventory;
        ItemTreeData.OnItemLoaded += OnItemLoaded;
    }

    void UnregisterEvents() {
        CraftingManager.OnItemCrafted -= OnItemCrafted;
        ItemUtilities.OnItemSentToPlayerInventory -= OnItemSentToPlayerInventory;
        ItemTreeData.OnItemLoaded -= OnItemLoaded;
    }

    void OnItemSentToPlayerInventory(Item item) {
        // VTMO.Log($"OnItemSentToPlayerInventory: {item.DisplayName}");
        VTModifiersCoreV2.CalcItemModifiers(item);
    }

    //修复
    void OnItemLoaded(Item item) {
        VTModifiersCoreV2.CalcItemModifiers(item);
    }

    void LoadPathCustom() {
        _modifiersDirectoryPersistant = Path.Combine(_resourceDirectory, "modifiers");
        _modifiersDirectoryCustom = Path.Combine(_persistantFilePath, "modifiers");
        Directory.CreateDirectory(_modifiersDirectoryCustom);
    }
}