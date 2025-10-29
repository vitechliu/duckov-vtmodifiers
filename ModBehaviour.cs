using ItemStatsSystem;
using UnityEngine;
using System.Reflection;
using System.Text;
using HarmonyLib;
using Duckov.UI;
using Duckov.Utilities;
using ItemStatsSystem.Data;
using SodaCraft.Localizations;
using TMPro;
using VTModifiers.VTLib;
using UnityEngine.UI;
// ReSharper disable Unity.PerformanceCriticalCodeInvocation

namespace VTModifiers;

[HarmonyPatch]
public class ModBehaviour : Duckov.Modding.ModBehaviour {
    protected string _logFilePath;
    protected string _dllDirectory;

    protected string _modName = "VTModifiers";

    protected string _version = "0.0.1";
    protected bool _isInitialized = false;

    private static ModBehaviour _instance;
    public static ModBehaviour Instance => _instance;

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

    
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ItemAgent_Gun), "ShootOneBullet")]
    public static void ItemAgentGun_ShootOneBullet_PostFix(ItemAgent_Gun __instance) {
        Projectile temp = Traverse.Create(__instance).Field("projInst").GetValue<Projectile>();
        temp.context.element_Electricity = VTModifiersCore.Modify(__instance.Item, VTModifiersCore.VtmElementElectricity, temp.context.element_Electricity);
        temp.context.element_Fire = VTModifiersCore.Modify(__instance.Item, VTModifiersCore.VtmElementFire, temp.context.element_Fire);
        temp.context.element_Poison = VTModifiersCore.Modify(__instance.Item, VTModifiersCore.VtmElementPoison, temp.context.element_Poison);
        temp.context.element_Space = VTModifiersCore.Modify(__instance.Item, VTModifiersCore.VtmElementSpace, temp.context.element_Space);
        
        temp.context.bleedChance = VTModifiersCore.Modify(__instance.Item, VTModifiersCore.VtmBleedChance, temp.context.bleedChance);
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
            } else {
                __result = "VTM_" + __result;
            }
        }
    }
    
    //try DisplayName patch
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Item), "get_DisplayName")]
    public static void Item_DisplayName_PostFix(Item __instance, ref string __result) {
        __result = VTModifiersCore.PatchItemDisplayName(__instance, __result);
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

    

    //对随机敌人背包道具词缀化
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CharacterSpawnerRoot), "AddCreatedCharacter")]
    public static void CharacterSpawnerRoot_AddCreatedCharacter_PostFix(
        CharacterSpawnerRoot __instance,
        CharacterMainControl c
    ) {
        int csrInstanceId = __instance.GetInstanceID();
        if (c.CharacterItem && c.CharacterItem.Inventory) {
            Inventory inventory = c.CharacterItem.Inventory;
            int itemCount = inventory.Count();
            if (c.PrimWeaponSlot() != null && c.PrimWeaponSlot().Content != null) {
                itemCount++;
                VTModifiersCore.PatchItem(c.PrimWeaponSlot().Content, VTModifiersCore.Sources.Enemy);
            }
            if (c.MeleeWeaponSlot() != null && c.MeleeWeaponSlot().Content != null) {
                itemCount++;
                VTModifiersCore.PatchItem(c.MeleeWeaponSlot().Content, VTModifiersCore.Sources.Enemy);
            }
            foreach (Item item in inventory) {
                VTModifiersCore.PatchItem(item, VTModifiersCore.Sources.Enemy);
            }
            // LogStatic($"CSRSetup:{csrInstanceId}, itemCount:{itemCount}");
        }
        else {
            // LogStatic($"CSRSetupFailed:{csrInstanceId}, cannot find inventory");
        }
    }

    //对物资箱词缀化
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
                Traverse.Create(lootbox).Field("inventoryReference").SetValue(inventory);
                Traverse.Create(__instance).Field("_lootBox").SetValue(lootbox);
            }
            else {
                // LogStatic($"LBLSetupFailed:{lootBoxLoaderId}, name:{lootBoxName},, nullInventory");
            }
        }
        else {
            // LogStatic($"LBLSetupFailed:{lootBoxLoaderId}, nullLootBox");
        }
    }

    TextMeshProUGUI _text;
    TextMeshProUGUI Text
    {
        get
        {
            if (_text == null)
            {
                _text = Instantiate(GameplayDataSettings.UIStyle.TemplateTextUGUI);
            }
            return _text;
        }
    }
    
    private void OnSetupMeta(ItemHoveringUI uI, ItemMetaData data)
    {
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
    static Color VTLabelColorLight = new Color(247/255, 82/255, 228/255);
    static Color VTLabelColorDefault = Color.white;
    
    
    //物品InventoryView键值对UI改颜色
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ItemModifierEntry), "Refresh")]
    public static void ItemModifierEntry_Refresh_PostFix(ItemModifierEntry __instance) {
        Traverse t = Traverse.Create(__instance);
        TextMeshProUGUI labelGUI = t.Field("displayName").GetValue<TextMeshProUGUI>();
        TextMeshProUGUI valueGUI = t.Field("value").GetValue<TextMeshProUGUI>();
        string label = labelGUI.text;
        if (label.StartsWith("VTM_")) {
            labelGUI.color = VTLabelColorLight;
            labelGUI.text = labelGUI.text.Substring(4);
            valueGUI.color = VTLabelColorLight;
        } else if (label.StartsWith("VTMC_")) {
            labelGUI.color = VTLabelColorLight;
            labelGUI.text = labelGUI.text.Substring(5);
            valueGUI.color = VTLabelColorLight;
        } else {
            labelGUI.color = VTLabelColorDefault;
            valueGUI.color = VTLabelColorDefault;
        }
    }
    
    //物品HoveringUI参数键值对UI改颜色
    [HarmonyPostfix]
    [HarmonyPatch(typeof(LabelAndValue), "Setup")]
    public static void LabelAndValue_Setup_PostFix(LabelAndValue __instance, string label) {
        Traverse t = Traverse.Create(__instance);
        TextMeshProUGUI labelGUI = t.Field("labelText").GetValue<TextMeshProUGUI>();
        TextMeshProUGUI valueGUI = t.Field("valueText").GetValue<TextMeshProUGUI>();
        if (label.StartsWith("VTM_")) {
            labelGUI.color = VTLabelColor;
            labelGUI.text = labelGUI.text.Substring(4);
            valueGUI.color = VTLabelColor;
        } else if (label.StartsWith("VTMC_")) {
            labelGUI.color = VTLabelColor;
            labelGUI.text = labelGUI.text.Substring(5);
            valueGUI.color = VTLabelColor;
        } else{
            labelGUI.color = VTLabelColorDefault;
            valueGUI.color = VTLabelColorDefault;
        }
    }
    
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
            _harmony = new Harmony("com.vitech.duckov_vt_modifiers_patch");
            _harmony.PatchAll();
            VTModifiersCore.InitData();
            RegisterEvents();
            _isInitialized = true;
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
            Log("模组已卸载");
        }
    }

    protected void RegisterEvents() {
        // LevelManager.OnLevelInitialized += OnLevelInitialized;
        ItemHoveringUI.onSetupItem += OnSetupItemHoveringUI;
        ItemHoveringUI.onSetupMeta += OnSetupMeta;
        ItemUtilities.OnItemSentToPlayerInventory += OnItemSentToPlayerInventory;
        ItemTreeData.OnItemLoaded += OnItemLoaded;
    }

    protected void UnregisterEvents() {
        // LevelManager.OnLevelInitialized -= OnLevelInitialized;
        ItemHoveringUI.onSetupItem -= OnSetupItemHoveringUI;
        ItemHoveringUI.onSetupMeta -= OnSetupMeta;
        ItemUtilities.OnItemSentToPlayerInventory -= OnItemSentToPlayerInventory;
        ItemTreeData.OnItemLoaded -= OnItemLoaded;
    }

    private void OnItemSentToPlayerInventory(Item item) {
        // LogStatic($"OnItemSentToPlayerInventory: {item.DisplayName}");
        VTModifiersCore.CalcItemModifiers(item);
    }
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
        if (VTModifiersCore.DEBUG && _isInitialized) {
            if (Input.GetKeyDown(KeyCode.G)) {
                KeyDownG();
            }
            
            if (Input.GetKeyDown(KeyCode.H)) {
                KeyDownH();
            }
        }
    }

    void KeyDownG() {
        Item weapon = MainCharacterWeapon();
        if (weapon != null) {
            VTModifiersCore.TryUnpatchItem(weapon);
            VTModifiersCore.PatchItem(weapon, VTModifiersCore.Sources.Debug);
            Log($"KeyCodeG PatchMainCharacterWeapon: {weapon.DisplayName}");
        }
        
    }

    void KeyDownH() {
        Item item = MainCharacterWeapon();
        if (item != null) {
            StringBuilder stringBuilder = new StringBuilder();
            if (item.Variables != null)
            {
                foreach (CustomData variable in item.Variables)
                {
                    if (variable.Display)
                        stringBuilder.AppendLine(variable.Key + "\t" + variable.DisplayName + "\t" + variable.GetValueDisplayString());
                }
            }
            if (item.Constants != null)
            {
                foreach (CustomData constant in item.Constants)
                {
                    if (constant.Display)
                        stringBuilder.AppendLine(constant.Key + "\t" + constant.DisplayName + "\t" + constant.GetValueDisplayString());
                }
            }
            if ((UnityEngine.Object) item.Stats != (UnityEngine.Object) null)
            {
                foreach (Stat stat in item.Stats)
                {
                    if (stat.Display)
                        stringBuilder.AppendLine(stat.Key + "\t" + string.Format("{0}\t{1}", (object) stat.DisplayName, (object) stat.Value));
                }
            }
            if ((UnityEngine.Object) item.Modifiers != (UnityEngine.Object) null)
            {
                foreach (ModifierDescription modifier in item.Modifiers)
                {
                    if (modifier.Display)
                        stringBuilder.AppendLine(modifier.Key + "\t" + modifier.DisplayName + "\t" + modifier.GetDisplayValueString());
                }
            }
            Log($"ItemPropertiesDisplay:{item.DisplayName}");
            Log(stringBuilder.ToString());
        }
    }
    
    Item MainCharacterWeapon() {
        return CharacterMainControl.Main?.PrimWeaponSlot()?.Content;
    }

    // void Awake() {
    //     Debug.Log("fffff loaded!! v1");
    // }

    protected void InitializeLogFile() {
        string str = Path.Combine(this._dllDirectory, "logs");
        Directory.CreateDirectory(str);
        _logFilePath = Path.Combine(str,
            string.Format("{0}_log_{1:yyyyMMdd}.txt", this._modName, (object)DateTime.Now));
        Log("模组启动，开始初始化，版本:" + _version);
        Log("日志路径: " + _logFilePath);
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
            if (isError) Debug.LogError(("[" + this._modName + "]" + message));
            else Debug.Log(("[" + this._modName + "]" + message));
        }
        catch (Exception ex) {
            Debug.LogError((object)("日志写入失败: " + ex.Message));
        }
    }
}