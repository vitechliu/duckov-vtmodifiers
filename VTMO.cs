using ItemStatsSystem;
using UnityEngine;
using Duckov.Economy;
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
                if (LevelManager.Instance.IsBaseLevel) KeyReforge();
            }
        }
    }

    //重量Patch
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Item), "get_SelfWeight")]
    public static void Item_SelfWeight_PostFix(Item __instance, ref float __result) {
        __result = VTModifiersCoreV2.Modify(__instance, VTModifiersCoreV2.VtmWeight, __result);
    }

    //MD:DisplayName patch
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ModifierDescription), "get_DisplayName")]
    public static void ModifierDescription_DisplayName_PostFix(ModifierDescription __instance, ref string __result) {
        if (
            VTModifiersCoreV2.IsModMD(__instance)
            && !__result.StartsWith("VTMC_")
            && !__result.StartsWith("VTM_")
        ) {
            __result = "VTM_" + __result;
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
                if (VTSettingManager.Setting.Debug) {
                    VTMO.Log($"闪避触发！");
                }

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
                if (VTSettingManager.Setting.Debug) {
                    VTMO.Log($"耐久触发！");
                }
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
                    damageInfo.fromCharacter.transform.position + Vector3.up * 2f, Color.red, 1f, null);
                damageInfo.fromCharacter.AddHealth(lifeStealAmount);
            }
        }
    }

    //ItemOperationMenu 物品操作相关Button
    public static Button btn_Reforge = null!;

    //重铸
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ItemOperationMenu), "Initialize")]
    public static void ItemOperationMenu_Initialize_PostFix(ItemOperationMenu __instance) {
        if (btn_Reforge == null) {
            Button btnSample = __instance.btn_Equip;
            if (btnSample == null) return;
            GameObject newBtn = Instantiate(btnSample.gameObject, btnSample.transform.parent);
            btn_Reforge = newBtn.GetComponent<Button>();
            btn_Reforge.name = "Btn_Reforge";

            btn_Reforge.onClick.RemoveAllListeners();
            btn_Reforge.onClick.AddListener(OnReforge);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ItemOperationMenu), "Setup")]
    public static void ItemOperationMenu_Setup_PostFix(ItemOperationMenu __instance) {
        if (btn_Reforge) {
            if (LevelManager.Instance.IsBaseLevel) {
                Item targetItem = __instance.TargetItem;
                if (targetItem && VTModifiersCoreV2.ItemCanBePatched(targetItem)) {
                    bool patched = VTModifiersCoreV2.IsPatchedItem(targetItem);
                    if ((patched && VTSettingManager.Setting.AllowReforge)
                        || (!patched && VTSettingManager.Setting.AllowForge)) {
                        btn_Reforge.gameObject.SetActive(true);
                        EnsureButtonStyle(targetItem);
                        return;
                    }
                }
            }

            btn_Reforge.gameObject.SetActive(false);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ItemOperationMenu), "OnOpen")]
    public static void ItemOperationMenu_OnOpen_PostFix(ItemOperationMenu __instance) {
        if (btn_Reforge) {
            if (LevelManager.Instance.IsBaseLevel) {
                Item targetItem = __instance.TargetItem;
                if (targetItem && VTModifiersCoreV2.ItemCanBePatched(targetItem)) {
                    bool patched = VTModifiersCoreV2.IsPatchedItem(targetItem);
                    if ((patched && VTSettingManager.Setting.AllowReforge)
                        || (!patched && VTSettingManager.Setting.AllowForge)) {
                        EnsureButtonStyle(targetItem);
                    }
                }
            }
        }
    }

    static void EnsureButtonStyle(Item targetItem) {
        bool patched = VTModifiersCoreV2.IsPatchedItem(targetItem);
        int price = VTModifiersCoreV2.ReforgePrice(targetItem);
        long userMoney = EconomyManager.Money;
        string buttonText = patched ? "Btn_reforge".ToPlainText() : "Btn_forge".ToPlainText();
        VT.SetButtonText(btn_Reforge, buttonText + $"(${price})");

        if (userMoney >= price) {
            btn_Reforge.interactable = true;
            VT.SetButtonColor(btn_Reforge, new Color(0.6f, 0f, 0.7f));
        }
        else {
            btn_Reforge.interactable = false;
            VT.SetButtonColor(btn_Reforge, new Color(0.8f, 0.4f, 0.9f));
        }
    }

    public static void KeyReforge() {
        Item targetItem = ItemUIUtilities.SelectedItem;
        ItemDisplay display = ItemUIUtilities.SelectedItemDisplay;

        if (!targetItem || !display) {
            VT.BubbleUserDebug("Bubble_no_item_select".ToPlainText());
            return;
        }

        if (!VTModifiersCoreV2.ItemCanBePatched(targetItem)) return;
        int price = VTModifiersCoreV2.ReforgePrice(targetItem);
        if (!EconomyManager.Pay(new Cost(price))) {
            VT.BubbleUserDebug("Bubble_lack_of_coin".ToPlainText());
            return;
        }

        VTModifiersCoreV2.TryUnpatchItem(targetItem);
        VTModifiersCoreV2.PatchItem(targetItem, VTModifiersCoreV2.Sources.Reforge);
        PostCustomSFX("Terraria_reforging.wav");
        VT.BubbleUserDebug("Bubble_reforge_success".ToPlainText());

        //更新仓库里面的名称
        display.nameText.text = display.Target.DisplayName;
    }

    public static void OnReforge() {
        ItemOperationMenu __instance = ItemOperationMenu.Instance;
        if (!__instance) return;
        Item targetItem = __instance.TargetItem;
        if (!targetItem) return;
        if (!VTModifiersCoreV2.ItemCanBePatched(targetItem)) return;

        int price = VTModifiersCoreV2.ReforgePrice(targetItem);
        if (!EconomyManager.Pay(new Cost(price))) {
            VT.BubbleUserDebug("Bubble_lack_of_coin".ToPlainText());
            __instance.Close();
            return;
        }

        VTModifiersCoreV2.TryUnpatchItem(targetItem);
        VTModifiersCoreV2.PatchItem(targetItem, VTModifiersCoreV2.Sources.Reforge);
        PostCustomSFX("Terraria_reforging.wav");
        VT.BubbleUserDebug("Bubble_reforge_success".ToPlainText());
        __instance.Close();

        //更新仓库里面的名称
        ItemOperationMenu iom = ItemOperationMenu.Instance;
        if (iom) {
            ItemDisplay itemDisplay = iom.TargetDisplay;
            if (itemDisplay) itemDisplay.nameText.text = itemDisplay.Target.DisplayName;
        }
    }


    //DisplayName patch
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Item), "get_DisplayName")]
    public static void Item_DisplayName_PostFix(Item __instance, ref string __result) {
        if (!VTModifiersCoreV2.IsPatchedItem(__instance)) return;
        string key = __instance.displayName;
        __result = VTModifiersCoreV2.PatchItemDisplayName(__instance, key.ToPlainText());
    }


    //物品价值Patch
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Item), "GetTotalRawValue")]
    public static void Item_GetTotalRawValue_PostFix(Item __instance, ref int __result) {
        // VTMO.Log($"ItemPriceModify: {__instance.DisplayName}");
        __result = Mathf.RoundToInt(
            VTModifiersCoreV2.Modify(__instance, VTModifiersCoreV2.VtmPriceMultiplier, (float)__result)
        );
    }


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

    //拖拽物品色卡
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ItemDisplay), "HandleDirectDrop")]
    public static bool ItemDisplay_HandleDirectDrop_PrePatch(ItemDisplay __instance, PointerEventData eventData) {
        if (__instance.Target == null || eventData.button != PointerEventData.InputButton.Left ||
            __instance.IsStockshopSample)
            return true;
        IItemDragSource component = eventData.pointerDrag.gameObject.GetComponent<IItemDragSource>();
        if (component == null || !component.IsEditable())
            return true;
        Item part = component.GetItem();
        Item main = __instance.Target;
        if (
            part && main
                 && part != main
                 && VTModifiersCoreV2.IsModifiersCard(part)
                 && VTModifiersCoreV2.ItemCanBePatched(main)
        ) {
            VTModifiersCoreV2.PatchByCard(part, main, __instance);
            ItemUIUtilities.NotifyPutItem(part);
            eventData.Use();
            return false;
        }

        return true;
    }

    public static bool loggedIMEColor = false;




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

    void LoadFormulas() {
        AddFormulaSimple(0, new[] { (754, 1), (308, 10), (58, 1) }, ItemUtil.MC_CARD_v1);
        AddFormulaSimple(0, new[] { (755, 1), (309, 10), (58, 1) }, ItemUtil.MC_CARD_v2);
        AddFormulaSimple(0, new[] { (756, 1), (1165, 30), (58, 1) }, ItemUtil.MC_CARD_v3);
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


    void TryInitSetting() {
        if (!ModSettingAPI.IsInit) {
            if (!ModSettingAPI.Init(info)) return;
            ModSettingConnector.Init();
        }
        ModSettingConnector.TryInitSCAV();
    }
    

    protected void RegisterEvents() {
        CraftingManager.OnItemCrafted += OnItemCrafted;
        ItemUtilities.OnItemSentToPlayerInventory += OnItemSentToPlayerInventory;
        ItemTreeData.OnItemLoaded += OnItemLoaded;
    }


    protected void UnregisterEvents() {
        CraftingManager.OnItemCrafted -= OnItemCrafted;
        ItemUtilities.OnItemSentToPlayerInventory -= OnItemSentToPlayerInventory;
        ItemTreeData.OnItemLoaded -= OnItemLoaded;
    }

    private void OnItemSentToPlayerInventory(Item item) {
        // VTMO.Log($"OnItemSentToPlayerInventory: {item.DisplayName}");
        VTModifiersCoreV2.CalcItemModifiers(item);
    }


    //修复
    private void OnItemLoaded(Item item) {
        VTModifiersCoreV2.CalcItemModifiers(item);
    }

    protected void LoadPathCustom() {
        _modifiersDirectoryPersistant = Path.Combine(_resourceDirectory, "modifiers");
        _modifiersDirectoryCustom = Path.Combine(_persistantFilePath, "modifiers");
        Directory.CreateDirectory(_modifiersDirectoryCustom);
    }
}