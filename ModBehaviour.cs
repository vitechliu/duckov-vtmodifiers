using ItemStatsSystem;
using UnityEngine;
using System.Reflection;
using Duckov.Economy;
using HarmonyLib;
using Duckov.UI;
using Duckov.Utilities;
using ItemStatsSystem.Data;
using ItemStatsSystem.Items;
using SodaCraft.Localizations;
using TMPro;
using UnityEngine.UI;
using VTModifiers.VTLib;
// ReSharper disable Unity.PerformanceCriticalCodeInvocation

namespace VTModifiers;

[HarmonyPatch]
public class ModBehaviour : Duckov.Modding.ModBehaviour {
    public string _logFilePath;
    public string _dllDirectory;
    public string _cfgDirectory;
    public string _sfxDirectory;

    public static string _modName = "VTModifiers";
    public static string _version = "0.5.0";
    
    protected bool _isInitialized = false;

    private static ModBehaviour _instance;
    public static ModBehaviour Instance => _instance;

    public VTModifiersUI modUI;

    private Harmony _harmony;

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
            temp.context.element_Electricity = VTModifiersCore.Modify(__instance.Item,
                VTModifiersCore.VtmElementElectricity, temp.context.element_Electricity);
            temp.context.element_Fire =
                VTModifiersCore.Modify(__instance.Item, VTModifiersCore.VtmElementFire, temp.context.element_Fire);
            temp.context.element_Poison = VTModifiersCore.Modify(__instance.Item, VTModifiersCore.VtmElementPoison,
                temp.context.element_Poison);
            temp.context.element_Space = VTModifiersCore.Modify(__instance.Item, VTModifiersCore.VtmElementSpace,
                temp.context.element_Space);
        
            temp.context.bleedChance =
                VTModifiersCore.Modify(__instance.Item, VTModifiersCore.VtmBleedChance, temp.context.bleedChance);
        
            if (VTSettingManager.Setting.Debug) {
                LogStatic($"Projectile:CritDamageFactor:{temp.context.critDamageFactor}, " +
                          $"ArmorPiercing:{temp.context.armorPiercing}, " +
                          $"ArmorBreak:{temp.context.armorBreak}");
            }
        } catch (Exception ex) {
            LogStatic($"PatchFailed: {ex.Message}\n{ex.StackTrace}");
        }
    }
    //近战Patch
    // [HarmonyPostfix]
    // [HarmonyPatch(typeof(ItemAgent_MeleeWeapon), "CheckCollidersInRange")]
    // public static void ItemAgentMeleeWeapon_CheckCollidersInRange_PostFix(ItemAgent_MeleeWeapon __instance) {
    //     if (!__instance || !__instance.Holder) return;
    //     if (!__instance.Holder.IsMainCharacter) return;
    //     LogStatic("辉及");
    //     CharacterMainControl main = __instance.Holder;
    //     GameObject[] list = main.gameObject.GetComponentsInChildren<GameObject>();
    //     foreach (var gameObj in list) {
    //         LogStatic("找到obj:" + gameObj.name);   
    //     }
    // }
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CA_Attack), "OnStart")]
    public static void CAAttack_OnStart_PostFix(CA_Attack __instance) {
        if (!__instance.characterController.IsMainCharacter) return;
        ItemAgent_MeleeWeapon weapon = Traverse.Create(__instance).Field("meleeWeapon").GetValue<ItemAgent_MeleeWeapon>();
        if (!weapon) return;
        if (!VTModifiersCore.IsPatchedItem(weapon.Item)) return;
        float? length = VTModifiersCore.GetItemVtm(weapon.Item, VTModifiersCore.VtmShootDistanceMultiplier);
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
        __result = VTModifiersCore.Modify(__instance, VTModifiersCore.VtmWeight, __result);
    }

    //MD:DisplayName patch
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ModifierDescription), "get_DisplayName")]
    public static void ModifierDescription_DisplayName_PostFix(ModifierDescription __instance, ref string __result) {
        if (
            VTModifiersCore.IsModMD(__instance)
            && !__result.StartsWith("VTMC_")
            && !__result.StartsWith("VTM_")
        ) {
            if (VTModifiersCore.Vtms.Contains("VTMC_" + __instance.Key)) {
                //是特殊词缀
                __result = "VTMC_" + __result;
            }
            else {
                __result = "VTM_" + __result;
            }
        }
    }

    //ItemOperationMenu 物品操作相关Button
    
    public static Button btn_Reforge = null!;
    
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
                if (targetItem && VTModifiersCore.ItemCanBePatched(targetItem)) {
                    bool patched = VTModifiersCore.IsPatchedItem(targetItem);
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
                if (targetItem && VTModifiersCore.ItemCanBePatched(targetItem)) {
                    bool patched = VTModifiersCore.IsPatchedItem(targetItem);
                    if ((patched && VTSettingManager.Setting.AllowReforge)
                        || (!patched && VTSettingManager.Setting.AllowForge)) {
                        EnsureButtonStyle(targetItem);
                    }
                }
            }
        }
    }
    static void EnsureButtonStyle(Item targetItem) {
        bool patched = VTModifiersCore.IsPatchedItem(targetItem);
        int price = VTModifiersCore.ReforgePrice(targetItem);
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

    
    
    public static void OnReforge() {
        ItemOperationMenu __instance = ItemOperationMenu.Instance;
        if (!__instance) return;
        Item targetItem = Traverse.Create(__instance).Property("TargetItem").GetValue<Item>();
        if (!targetItem) return;
        if (!VTModifiersCore.ItemCanBePatched(targetItem)) return;
        
        int price = VTModifiersCore.ReforgePrice(targetItem);
        if (!EconomyManager.Pay(new Cost(price))) {
            VT.BubbleUserDebug("Bubble_lack_of_coin".ToPlainText(), false);
            __instance.Close();
            return;
        }
        VTModifiersCore.TryUnpatchItem(targetItem);
        VTModifiersCore.PatchItem(targetItem, VTModifiersCore.Sources.Reforge);
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
        if (!VTModifiersCore.IsPatchedItem(__instance)) return;
        string key = Traverse.Create(__instance).Field("displayName").GetValue<string>();
        __result = VTModifiersCore.PatchItemDisplayName(__instance, key.ToPlainText());
    }

    //弹药节省Patch
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ItemSetting_Gun), "UseABullet")]
    public static bool ItemSettingGun_UseABullet_PreFix(ItemSetting_Gun __instance) {
        float? ammoSaveChance = VTModifiersCore.GetItemVtm(__instance.Item, VTModifiersCore.VtmAmmoSave);
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
        int beforePrice = __result;
        __result = Mathf.RoundToInt(
            VTModifiersCore.Modify(__instance, VTModifiersCore.VtmPriceMultiplier, (float)__result)
        );
        // if (__result != beforePrice) {
        //     LogStatic($"ItemPriceModify: {__instance.DisplayName}: {beforePrice} -> {__result}");
        // }
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
            // int itemCount = inventory.Count();
            // if (c.PrimWeaponSlot() != null && c.PrimWeaponSlot().Content != null) {
            //     itemCount++;
            //     VTModifiersCore.PatchItem(c.PrimWeaponSlot().Content, VTModifiersCore.Sources.Enemy);
            // }
            //
            // if (c.MeleeWeaponSlot() != null && c.MeleeWeaponSlot().Content != null) {
            //     itemCount++;
            //     VTModifiersCore.PatchItem(c.MeleeWeaponSlot().Content, VTModifiersCore.Sources.Enemy);
            // }
            //
            // if (c.MeleeWeaponSlot() != null && c.MeleeWeaponSlot().Content != null) {
            //     itemCount++;
            //     VTModifiersCore.PatchItem(c.MeleeWeaponSlot().Content, VTModifiersCore.Sources.Enemy);
            // }

            foreach (Item item in inventory) {
                VTModifiersCore.PatchItem(item, VTModifiersCore.Sources.Enemy);
            }

            foreach (Slot slot in c.CharacterItem.Slots) {
                if (slot.Content == null) continue;
                VTModifiersCore.PatchItem(slot.Content, VTModifiersCore.Sources.Enemy);
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
                    VTModifiersCore.PatchItem(item, VTModifiersCore.Sources.LootBox);
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
        VTModifiersCore.PatchItem(item, VTModifiersCore.Sources.Craft);
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

    private void OnSetupMeta(ItemHoveringUI uI, ItemMetaData data) {
        // Text.gameObject.SetActive(false);
    }

    //物品悬停UI改变物品名
    private void OnSetupItemHoveringUI(ItemHoveringUI uiInstance, Item item) {
        if (item == null) {
            // Text.gameObject.SetActive(false);
            return;
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

    public static bool loggedIMEColor = false;

    // //物品操作菜单Patch
    // [HarmonyPostfix]
    // [HarmonyPatch(typeof(ItemOperationMenu), "Setup")]
    // public static void ItemOperationMenu_Setup_PostPatch(ItemOperationMenu __instance) {
    //     Item item = Traverse.Create(__instance).Field("TargetItem").GetValue<Item>();
    //     if (item == null) return;
    //     TextMeshProUGUI itemNameUGUI = Traverse.Create(__instance).Field("nameText").GetValue<TextMeshProUGUI>();
    //     itemNameUGUI.text = VTModifiersCore.PatchItemDisplayName(item);
    // }

    // //物品自定义菜单Patch
    // [HarmonyPostfix]
    // [HarmonyPatch(typeof(ItemCustomizeSelectionView), "RefreshSelectedItemInfo")]
    // public static void ItemCustomizeSelectionView_RefreshSelectedItemInfo_PostPatch(ItemCustomizeSelectionView __instance) {
    //     Item item = ItemUIUtilities.SelectedItem;
    //     if (item == null) return;
    //     TextMeshProUGUI itemNameUGUI = Traverse.Create(__instance).Field("selectedItemName").GetValue<TextMeshProUGUI>();
    //     itemNameUGUI.text = VTModifiersCore.PatchItemDisplayName(item);
    // }

    protected override void OnAfterSetup() {
        if (!_isInitialized) {
            _dllDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            InitializeLogFile();
            InitializeCfg();
            InitializeSfxFile();
            _harmony = new Harmony("com.vitech.duckov_vt_modifiers_patch");
            _harmony.PatchAll();
            VTModifiersCore.InitData();
            RegisterEvents();
            _isInitialized = true;
            
            GameObject uiObject = new GameObject("VTModifier_ModUI_Instance");
            modUI = uiObject.AddComponent<VTModifiersUI>();
            DontDestroyOnLoad(uiObject);
        }
    }

    private void Start() {
        if (_isInitialized) {
            // VTModSettingConnector.Init();
        }
    }

    protected override void OnBeforeDeactivate() {
        if (_isInitialized) {
            _isInitialized = false;
            _harmony.UnpatchAll();
            UnregisterEvents();
            if (VTModifiersCore.ModifierData.Count > 0) {
                VTModifiersCore.ModifierData.Clear();
            }

            if (_text != null) Destroy(_text);
            if (btn_Reforge != null) Destroy(btn_Reforge);
            
            if (modUI != null && modUI.gameObject != null) {
                Destroy(modUI.gameObject);
            }
            Log("模组已卸载");
        }
    }

    protected void RegisterEvents() {
        // LevelManager.OnLevelInitialized += OnLevelInitialized;
        ItemHoveringUI.onSetupItem += OnSetupItemHoveringUI;
        ItemHoveringUI.onSetupMeta += OnSetupMeta;
        CraftingManager.OnItemCrafted += OnItemCrafted;
        ItemUtilities.OnItemSentToPlayerInventory += OnItemSentToPlayerInventory;
        ItemUIUtilities.OnSelectionChanged += OnSelectionChanged;
        ItemTreeData.OnItemLoaded += OnItemLoaded;
        
        VTModSettingConnector.Init();
    }

    protected void UnregisterEvents() {
        // LevelManager.OnLevelInitialized -= OnLevelInitialized;
        ItemHoveringUI.onSetupItem -= OnSetupItemHoveringUI;
        ItemHoveringUI.onSetupMeta -= OnSetupMeta;
        CraftingManager.OnItemCrafted -= OnItemCrafted;
        ItemUtilities.OnItemSentToPlayerInventory -= OnItemSentToPlayerInventory;
        ItemUIUtilities.OnSelectionChanged -= OnSelectionChanged;
        ItemTreeData.OnItemLoaded -= OnItemLoaded;
    }

    private void OnItemSentToPlayerInventory(Item item) {
        // LogStatic($"OnItemSentToPlayerInventory: {item.DisplayName}");
        VTModifiersCore.CalcItemModifiers(item);
    }

    private async void OnSelectionChanged() {
        // Item selectingItem = ItemUIUtilities.SelectedItem;
        // if (selectingItem && VTModifiersCore.IsPatchedItem(selectingItem)) {
        //     string[] dialog = {
        //         "a1", "a2"
        //     };
        //     await VTDialog.DialogFlow(dialog);
        // }
    }

    //修复
    
    //从存档等地方加载Item后，需要更新Modifier
    private void OnItemLoaded(Item item) {
        // LogStatic($"OnItemLoaded: {item.DisplayName}");
        VTModifiersCore.CalcItemModifiers(item);
    }

    //初始化地图后，扫描该地图的敌人和物资箱，为其中的武器等道具异步加入词缀
    private void OnLevelInitialized() {
        Log("地图已初始化");
    }


    void Update() {
        // if (VTSettingManager.Setting.Debug && _isInitialized) {
        //     //随机附加
        //     if (Input.GetKeyDown(KeyCode.G)) {
        //         KeyDownG();
        //     }
        //     //显示信息
        //     if (Input.GetKeyDown(KeyCode.H)) {
        //         KeyDownH();
        //     }
        // }
    }


    // void Awake() {
    //     
    // }

    protected void InitializeLogFile() {
        string str = Path.Combine(this._dllDirectory, "logs");
        Directory.CreateDirectory(str);
        _logFilePath = Path.Combine(str,
            string.Format("{0}_log_{1:yyyyMMdd}.txt", _modName, (object)DateTime.Now));
        Log("模组启动，开始初始化，版本:" + _version);
        Log("日志路径: " + _logFilePath);
    }
    protected void InitializeSfxFile() {
        _sfxDirectory = Path.Combine(this._dllDirectory, "sfx");
        Directory.CreateDirectory(_sfxDirectory);
    }
    protected void InitializeCfg() {
        _cfgDirectory = Path.Combine(this._dllDirectory, "cfg");
        Directory.CreateDirectory(_cfgDirectory);
        VTSettingManager.LoadSetting();
    }
    public static void LogStatic(string message, bool isError = false) {
        if (ModBehaviour.Instance) {
            ModBehaviour.Instance.Log(message, isError);
        }
    }

    protected void Log(string message, bool isError = false) {
        try {
            File.AppendAllText(this._logFilePath,
                string.Format("[{0:HH:mm:ss}] {1}\n", (object)DateTime.Now, message));
            if (isError) Debug.LogError(("[" + _modName + "]" + message));
            else Debug.Log(("[" + _modName + "]" + message));
        }
        catch (Exception ex) {
            Debug.LogError((object)("日志写入失败: " + ex.Message));
        }
    }
}