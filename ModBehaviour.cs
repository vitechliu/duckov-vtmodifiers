using ItemStatsSystem;
using UnityEngine;
using System.Reflection;
using Duckov.Economy;
using Duckov.Modding;
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
using VTModifiers.ThirdParty;
using VTModifiers.VTLib;
using VTModifiers.VTLib.Items;

// ReSharper disable Unity.PerformanceCriticalCodeInvocation

namespace VTModifiers;

[HarmonyPatch]
public class ModBehaviour : Duckov.Modding.ModBehaviour {
    // public string _logFilePath;
    public string _logFilePathNew;
    public string _dllDirectory;
    public string _cfgDirectoryNew;
    public string _resourceDirectory;
    public string _modifiersDirectoryPersistant;
    public string _modifiersDirectoryCustom;

    public static string _modName = "VTModifiers";
    public static string _version = "0.7.1";
    
    protected bool _isInitialized = false;

    private static ModBehaviour _instance;
    public static ModBehaviour Instance => _instance;

    public VTModifiersUI modUI;

    private Harmony _harmony;

    private void Update() {
        if (!_isInitialized) return;
        if (Input.GetKeyDown(VTSettingManager.Setting.ReforgeKey)) {
            if (LevelManager.Instance) {
                if (LevelManager.Instance.IsBaseLevel) KeyReforge();
            }
        }
    }

    protected virtual void Awake() {
        if (_instance == null) {
            _instance = this;
            DontDestroyOnLoad(gameObject); // 确保单例在场景切换时不会销毁
        }
        else if (_instance != this) {
            Destroy(gameObject); // 销毁多余的实例
        }
    }

    //枪械Patch
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ItemAgent_Gun), "ShootOneBullet")]
    public static void ItemAgentGun_ShootOneBullet_PostFix(ItemAgent_Gun __instance) {
        if (!__instance) return;
        try {
            Projectile temp = Traverse.Create(__instance).Field("projInst").GetValue<Projectile>();
            if (!temp) return;
            float instantDeathRate = VTModifiersCoreV2.Modify(__instance.Item,
                VTModifiersCoreV2.VtmDeathRate);
            if (VT.Probability(instantDeathRate)) {
                CharacterMainControl c = __instance.Holder;
                //只有玩家才能应用即死
                if (c && c.IsMainCharacter)
                    temp.context.damage = 999999f;
            }
            temp.context.element_Electricity = VTModifiersCoreV2.Modify(__instance.Item,
                VTModifiersCoreV2.VtmElementElectricity, temp.context.element_Electricity);
            temp.context.element_Ice = VTModifiersCoreV2.Modify(__instance.Item,
                VTModifiersCoreV2.VtmElementIce, temp.context.element_Ice);
            temp.context.element_Fire =
                VTModifiersCoreV2.Modify(__instance.Item, VTModifiersCoreV2.VtmElementFire, temp.context.element_Fire);
            temp.context.element_Poison = VTModifiersCoreV2.Modify(__instance.Item, VTModifiersCoreV2.VtmElementPoison,
                temp.context.element_Poison);
            temp.context.element_Space = VTModifiersCoreV2.Modify(__instance.Item, VTModifiersCoreV2.VtmElementSpace,
                temp.context.element_Space);
            temp.context.element_Ghost = VTModifiersCoreV2.Modify(__instance.Item, VTModifiersCoreV2.VtmElementGhost,
                temp.context.element_Ghost);
            
            temp.context.bleedChance =
                VTModifiersCoreV2.Modify(__instance.Item, VTModifiersCoreV2.VtmBleedChance, temp.context.bleedChance);
        
            // if (VTSettingManager.Setting.Debug) {
            //     LogStatic($"Projectile:CritDamageFactor:{temp.context.critDamageFactor}, " +
            //               $"ArmorPiercing:{temp.context.armorPiercing}, " +
            //               $"ArmorBreak:{temp.context.armorBreak}");
            // }
        } catch (Exception ex) {
            LogStatic($"PatchFailed: {ex.Message}\n{ex.StackTrace}");
        }
    }
    //扩大刀光
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CA_Attack), "OnStart")]
    public static void CAAttack_OnStart_PostFix(CA_Attack __instance) {
        if (!__instance.characterController.IsMainCharacter) return;
        ItemAgent_MeleeWeapon weapon = Traverse.Create(__instance).Field("meleeWeapon").GetValue<ItemAgent_MeleeWeapon>();
        if (!weapon) return;
        if (!VTModifiersCoreV2.IsPatchedItem(weapon.Item)) return;
        float? length = VTModifiersCoreV2.GetItemVtmKey(weapon.Item, VTModifiersCoreV2.VtmShootDistanceMultiplier);
        if (!length.HasValue) return;
        GameObject sfx = Traverse.Create(weapon).Field("slashFx").GetValue<GameObject>();
        sfx.transform.localScale *= (1f + (float)length);
    }
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CA_Attack), "OnStop")]
    public static void CAAttack_OnStop_PostFix(CA_Attack __instance) {
        if (!__instance.characterController.IsMainCharacter) return;
        ItemAgent_MeleeWeapon weapon = Traverse.Create(__instance).Field("meleeWeapon").GetValue<ItemAgent_MeleeWeapon>();
        if (!weapon) return;
        GameObject sfx = Traverse.Create(weapon).Field("slashFx").GetValue<GameObject>();
        sfx.transform.localScale = new Vector3(1.92f, 1.92f, 1.92f);
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
                    LogStatic($"闪避触发！");
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
                    LogStatic($"耐久触发！");
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
                //     LogStatic($"LifeSteal:{lifeStealAmount}");
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
            Button btnSample = Traverse.Create(__instance).Field("btn_Equip").GetValue<Button>();
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
                Item targetItem = Traverse.Create(__instance).Property("TargetItem").GetValue<Item>();
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
                Item targetItem = Traverse.Create(__instance).Property("TargetItem").GetValue<Item>();
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
            VT.BubbleUserDebug("Bubble_no_item_select".ToPlainText(), false);
            return;
        }
        if (!VTModifiersCoreV2.ItemCanBePatched(targetItem)) return;
        int price = VTModifiersCoreV2.ReforgePrice(targetItem);
        if (!EconomyManager.Pay(new Cost(price))) {
            VT.BubbleUserDebug("Bubble_lack_of_coin".ToPlainText(), false);
            return;
        }
        VTModifiersCoreV2.TryUnpatchItem(targetItem);
        VTModifiersCoreV2.PatchItem(targetItem, VTModifiersCoreV2.Sources.Reforge);
        VT.PostCustomSFX("Terraria_reforging.wav");
        VT.BubbleUserDebug("Bubble_reforge_success".ToPlainText(), false);

        //更新仓库里面的名称
        VT.ForceUpdateItemDisplayName(display);
    }
    
    public static void OnReforge() {
        ItemOperationMenu __instance = ItemOperationMenu.Instance;
        if (!__instance) return;
        Item targetItem = Traverse.Create(__instance).Property("TargetItem").GetValue<Item>();
        if (!targetItem) return;
        if (!VTModifiersCoreV2.ItemCanBePatched(targetItem)) return;
        
        int price = VTModifiersCoreV2.ReforgePrice(targetItem);
        if (!EconomyManager.Pay(new Cost(price))) {
            VT.BubbleUserDebug("Bubble_lack_of_coin".ToPlainText(), false);
            __instance.Close();
            return;
        }
        VTModifiersCoreV2.TryUnpatchItem(targetItem);
        VTModifiersCoreV2.PatchItem(targetItem, VTModifiersCoreV2.Sources.Reforge);
        VT.PostCustomSFX("Terraria_reforging.wav");
        VT.BubbleUserDebug("Bubble_reforge_success".ToPlainText(), false);
        __instance.Close();

        //更新仓库里面的名称
        ItemOperationMenu iom = ItemOperationMenu.Instance;
        if (iom) {
            ItemDisplay itemDisplay = Traverse.Create(iom).Field("TargetDisplay").GetValue<ItemDisplay>();
            if (itemDisplay) VT.ForceUpdateItemDisplayName(itemDisplay);
        }
    }
    
    
    //DisplayName patch
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Item), "get_DisplayName")]
    public static void Item_DisplayName_PostFix(Item __instance, ref string __result) {
        if (!VTModifiersCoreV2.IsPatchedItem(__instance)) return;
        string key = Traverse.Create(__instance).Field("displayName").GetValue<string>();
        __result = VTModifiersCoreV2.PatchItemDisplayName(__instance, key.ToPlainText());
    }

    //弹药节省Patch
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ItemSetting_Gun), "UseABullet")]
    public static bool ItemSettingGun_UseABullet_PreFix(ItemSetting_Gun __instance) {
        float? ammoSaveChance = VTModifiersCoreV2.GetItemVtmKey(__instance.Item, VTModifiersCoreV2.VtmAmmoSave);
        if (ammoSaveChance.HasValue) {
            bool prob = !VT.Probability(ammoSaveChance.Value);
            // if (VTSettingManager.Setting.Debug) LogStatic("UseABullet:" + prob);
            return prob;
        }
        return true;
    }

    //物品价值Patch
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Item), "GetTotalRawValue")]
    public static void Item_GetTotalRawValue_PostFix(Item __instance, ref int __result) {
        // LogStatic($"ItemPriceModify: {__instance.DisplayName}");
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
            // LogStatic($"CSRSetup:{csrInstanceId}, itemCount:{itemCount}");
        }
        else {
            // LogStatic($"CSRSetupFailed:{csrInstanceId}, cannot find inventory");
        }
    }

    //词缀化来源：物资箱
    [HarmonyPostfix]
    [HarmonyPatch(typeof(LootBoxLoader), "Setup")]
    public static void LootBoxLoader_Setup_PostFix(LootBoxLoader __instance) {
        int lootBoxLoaderId = __instance.GetInstanceID();
        InteractableLootbox lootbox =
            Traverse.Create(__instance).Field("_lootBox").GetValue<InteractableLootbox>();

        if (lootbox != null) {
            string lootBoxName = lootbox.InteractName;
            Inventory inventory = lootbox.Inventory;
            if (inventory != null) {
                int inventoryCount = inventory.Count();
                // LogStatic($"LBLSetup:{lootBoxLoaderId}, name:{lootBoxName}, count:{inventoryCount}");
                foreach (Item item in inventory) {
                    VTModifiersCoreV2.PatchItem(item, VTModifiersCoreV2.Sources.LootBox);
                }
                // Traverse.Create(lootbox).Field("inventoryReference").SetValue(inventory);
                // Traverse.Create(__instance).Field("_lootBox").SetValue(lootbox);
            }
            else {
                // LogStatic($"LBLSetupFailed:{lootBoxLoaderId}, name:{lootBoxName},, nullInventory");
            }
        }
        else {
            // LogStatic($"LBLSetupFailed:{lootBoxLoaderId}, nullLootBox");
        }
    }

    //词缀化来源：合成
    private void OnItemCrafted(CraftingFormula formula, Item item) {
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
    
    
    
    TextMeshProUGUI _text;

    TextMeshProUGUI Text {
        get {
            if (_text == null) {
                _text = Instantiate(GameplayDataSettings.UIStyle.TemplateTextUGUI);
            }

            return _text;
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
        Traverse t = Traverse.Create(__instance);
        TextMeshProUGUI labelGUI = t.Field("displayName").GetValue<TextMeshProUGUI>();
        TextMeshProUGUI valueGUI = t.Field("value").GetValue<TextMeshProUGUI>();
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
        Traverse t = Traverse.Create(__instance);
        TextMeshProUGUI labelGUI = t.Field("labelText").GetValue<TextMeshProUGUI>();
        TextMeshProUGUI valueGUI = t.Field("valueText").GetValue<TextMeshProUGUI>();
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
        if ( __instance.Target == null || eventData.button != PointerEventData.InputButton.Left || __instance.IsStockshopSample)
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


    private LootBoxEventListener SCAV_Listener = null!;
    protected override void OnAfterSetup() {
        if (!_isInitialized) {
            LoadPath();
            VTSettingManager.LoadSetting();
            LocalizationUtil.ReadLang();
            VTModifiersCoreV2.InitData();
            
            ItemUtil.InitItem();

            RegisterEvents();
            _isInitialized = true;
            _harmony = new Harmony("com.vitech.duckov_vt_modifiers_patch");
            _harmony.PatchAll();
            GameObject uiObject = new GameObject("VTModifier_ModUI_Instance");
            modUI = uiObject.AddComponent<VTModifiersUI>();
            DontDestroyOnLoad(uiObject);
            TryConnect();
            TryInitSetting();
            SCAV_Listener = LootBoxEventListener.Instance;
        }
    }

    protected override void OnBeforeDeactivate() {
        if (_isInitialized) {
            _isInitialized = false;
            _harmony.UnpatchAll(_harmony.Id);
            ItemUtil.UnloadItems();
            UnregisterEvents();

            if (_text != null) Destroy(_text);
            if (btn_Reforge != null) Destroy(btn_Reforge);
            
            if (modUI != null && modUI.gameObject != null) {
                Destroy(modUI.gameObject);
            }

            if (SCAV_Listener) {
                Destroy(SCAV_Listener);
            }
            Log("模组已卸载");
        }
    }
    static void TryConnect() {
        MagicConnector.TryConnect();
        DisplayConnector.TryConnect();
    }
    private void ModManager_OnModActivated(ModInfo arg1, Duckov.Modding.ModBehaviour arg2) {
        if (arg1.name == MagicConnector.MOD_NAME) MagicConnector.TryConnect();
        if (arg1.name == DisplayConnector.MOD_NAME) DisplayConnector.TryConnect();
        if (ModSettingAPI.IsInit) return;
        if (arg1.name != ModSettingAPI.MOD_NAME || !ModSettingAPI.Init(info)) return;
        ModSettingConnector.Init();
    }
    private void ModManager_OnModWillBeDeactivated(ModInfo arg1, Duckov.Modding.ModBehaviour arg2) {
        LogStatic("ModWillBeDeactivated:" + arg1.name);
        if (arg1.name == MagicConnector.MOD_NAME) MagicConnector.OnDeactivated();
        if (arg1.name == DisplayConnector.MOD_NAME) DisplayConnector.OnDeactivated();
        // if (arg1.name != ModSettingAPI.MOD_NAME || !ModSettingAPI.Init(info)) return;
        // // //禁用ModSetting的时候移除监听
        // // Setting.OnSlider1ValueChanged -= Setting_OnSlider1ValueChanged;
    }
    void TryInitSetting() {
        if (!ModSettingAPI.IsInit) {
            if (!ModSettingAPI.Init(info)) return;
            ModSettingConnector.Init();
        }
    }
    private void Start() {
        if (_isInitialized) {
            // VTModSettingConnector.Init();
        }
    }


    protected void RegisterEvents() {
        // LevelManager.OnLevelInitialized += OnLevelInitialized;
        // ItemHoveringUI.onSetupItem += OnSetupItemHoveringUI;
        // ItemHoveringUI.onSetupMeta += OnSetupMeta;
        CraftingManager.OnItemCrafted += OnItemCrafted;
        ItemUtilities.OnItemSentToPlayerInventory += OnItemSentToPlayerInventory;
        // ItemUIUtilities.OnSelectionChanged += OnSelectionChanged;
        ItemTreeData.OnItemLoaded += OnItemLoaded;
        ModManager.OnModActivated += ModManager_OnModActivated;
        ModManager.OnModWillBeDeactivated += ModManager_OnModWillBeDeactivated;

        
    }
    
    

    protected void UnregisterEvents() {
        // LevelManager.OnLevelInitialized -= OnLevelInitialized;
        // ItemHoveringUI.onSetupItem -= OnSetupItemHoveringUI;
        // ItemHoveringUI.onSetupMeta -= OnSetupMeta;
        CraftingManager.OnItemCrafted -= OnItemCrafted;
        ItemUtilities.OnItemSentToPlayerInventory -= OnItemSentToPlayerInventory;
        // ItemUIUtilities.OnSelectionChanged -= OnSelectionChanged;
        ItemTreeData.OnItemLoaded -= OnItemLoaded;
        ModManager.OnModActivated -= ModManager_OnModActivated;
        ModManager.OnModWillBeDeactivated -= ModManager_OnModWillBeDeactivated;

    }

    private void OnItemSentToPlayerInventory(Item item) {
        // LogStatic($"OnItemSentToPlayerInventory: {item.DisplayName}");
        VTModifiersCoreV2.CalcItemModifiers(item);
    }


    //修复
    
    //从存档等地方加载Item后，需要更新Modifier
    private void OnItemLoaded(Item item) {
        // LogStatic($"OnItemLoaded: {item.DisplayName}");
        VTModifiersCoreV2.CalcItemModifiers(item);
    }
    void LoadPath() {
        _dllDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        _resourceDirectory = Path.Combine(this._dllDirectory, "resources");
        string mainDirectory = Path.Combine(Application.persistentDataPath, _modName);
        Directory.CreateDirectory(mainDirectory);
        string str = Path.Combine(mainDirectory, "logs");
        Directory.CreateDirectory(str);
        _logFilePathNew = Path.Combine(str,
            string.Format("{0}_log_{1:yyyyMMdd}.txt", _modName, (object)DateTime.Now));
        _cfgDirectoryNew = Path.Combine(mainDirectory, "cfg");
        Directory.CreateDirectory(_cfgDirectoryNew);
        _modifiersDirectoryPersistant = Path.Combine(_resourceDirectory, "modifiers");
        _modifiersDirectoryCustom = Path.Combine(mainDirectory, "modifiers");
        Directory.CreateDirectory(_modifiersDirectoryCustom);
        Log("模组启动，开始初始化，版本:" + _version);
        Log("日志路径: " + _logFilePathNew);
    }
    public static void LogStatic(string message, bool isError = false) {
        if (ModBehaviour.Instance) {
            ModBehaviour.Instance.Log(message, isError);
        }
    }

    protected void Log(string message, bool isError = false) {
        try {
            File.AppendAllText(this._logFilePathNew,
                string.Format("[{0:HH:mm:ss}] {1}\n", (object)DateTime.Now, message));
            if (isError) Debug.LogError(("[" + _modName + "]" + message));
            else Debug.Log(("[" + _modName + "]" + message));
        }
        catch (Exception ex) {
            Debug.LogError((object)("日志写入失败: " + ex.Message));
        }
    }
}