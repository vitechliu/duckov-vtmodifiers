using System.Globalization;
using System.Text;
using Duckov.Economy;
using Duckov.UI;
using Duckov.Utilities;
using HarmonyLib;
using ItemStatsSystem;
using SodaCraft.Localizations;
using TMPro;
using UnityEngine;
using VTModifiers.ThirdParty;
using VTModifiers.VTLib.Items;
using VTLib;

namespace VTModifiers.VTLib;

public class VTModifiersUI : MonoBehaviour {

    private bool show = false;

    public static List<string> modifiers = new();

    void Update() {
        if (
            Input.GetKey(KeyCode.LeftControl) 
            || Input.GetKey(KeyCode.RightControl)
            || Input.GetKey(KeyCode.LeftCommand)
            || Input.GetKey(KeyCode.RightCommand)
            ) {
            if (Input.GetKeyDown(KeyCode.F3)) {
                show = !show;
            }
        }
        // if (Input.GetKeyDown(KeyCode.F7)) {
        //     Log("F7 Pressed");
        //     show = !show;
        // }
    }

    private bool toggleDebug = true;

    private void OnGUI() {
        if (show)
            windowRect = GUILayout.Window(711451401, windowRect, WindowFunc, "VTModifier Mod Setting");
    }

    private Vector2 vt;
    private Rect windowRect = new Rect(50, 50, 400, 300);

    static string float2Percentage(float value) {
        return $"x{(float)(value * 100.0):0.##}%";
    }

    private static string itemId = "123";
    void WindowFunc(int id) {
        vt = GUILayout.BeginScrollView(vt);
        GUILayout.BeginHorizontal();

        GUILayout.FlexibleSpace();
        if (GUILayout.Button("x", GUILayout.Width(20))) {
            show = false;
        }

        GUILayout.EndHorizontal();
        GUILayout.Label("游戏设定");


        toggleDebug = GUILayout.Toggle(VTSettingManager.Setting.Debug, "Debug 调试模式");
        VTSettingManager.Setting.Debug = toggleDebug;

        GUILayout.BeginHorizontal();
        GUILayout.Label("护甲属性倍率(重启游戏生效)");
        GUILayout.FlexibleSpace();
        GUILayout.Label(VTSettingManager.Setting.ArmorThreshold.ToString(CultureInfo.InvariantCulture));
        GUILayout.EndHorizontal();
        VTSettingManager.Setting.ArmorThreshold = GUILayout.HorizontalScrollbar(
            VTSettingManager.Setting.ArmorThreshold,
            0.1f,
            0.1f,
            4f
        );
        
        GUILayout.BeginHorizontal();
        GUILayout.Label("伤害属性倍率(重启游戏生效)");
        GUILayout.FlexibleSpace();
        GUILayout.Label(VTSettingManager.Setting.DamageThreshold.ToString(CultureInfo.InvariantCulture));
        GUILayout.EndHorizontal();
        VTSettingManager.Setting.DamageThreshold = GUILayout.HorizontalScrollbar(
            VTSettingManager.Setting.DamageThreshold,
            0.1f,
            0.1f,
            4f
        );
        
        VTSettingManager.Setting.FixMode = GUILayout.Toggle(VTSettingManager.Setting.FixMode, "词缀属性是否固定");
        
        //重铸设定
        GUILayout.BeginHorizontal();
        VTSettingManager.Setting.AllowReforge = GUILayout.Toggle(VTSettingManager.Setting.AllowReforge, "右键重铸");
        VTSettingManager.Setting.AllowForge = GUILayout.Toggle(VTSettingManager.Setting.AllowForge, "右键词缀化");
        GUILayout.EndHorizontal();


        if (VTSettingManager.Setting.AllowReforge) {
            GUILayout.BeginHorizontal();
            GUILayout.Label("重铸价格倍率");
            GUILayout.FlexibleSpace();
            GUILayout.Label(VTSettingManager.Setting.ReforgePriceFactor.ToString(CultureInfo.InvariantCulture));
            GUILayout.EndHorizontal();
            VTSettingManager.Setting.ReforgePriceFactor = GUILayout.HorizontalScrollbar(
                VTSettingManager.Setting.ReforgePriceFactor,
                0.1f,
                0.1f,
                10f
            );
        }

        if (VTSettingManager.Setting.AllowForge) {
            GUILayout.BeginHorizontal();
            GUILayout.Label("词缀附加价格倍率");
            GUILayout.FlexibleSpace();
            GUILayout.Label(VTSettingManager.Setting.ForgePriceFactor.ToString(CultureInfo.InvariantCulture));
            GUILayout.EndHorizontal();
            VTSettingManager.Setting.ForgePriceFactor = GUILayout.HorizontalScrollbar(
                VTSettingManager.Setting.ForgePriceFactor,
                0.1f,
                0.1f,
                20f
            );
        }


        //概率
        GUILayout.BeginHorizontal();
        GUILayout.Label("敌人生成词缀概率");
        GUILayout.FlexibleSpace();
        GUILayout.Label(float2Percentage(VTSettingManager.Setting.EnemyPatchedPercentage));
        GUILayout.EndHorizontal();
        VTSettingManager.Setting.EnemyPatchedPercentage = GUILayout.HorizontalScrollbar(
            VTSettingManager.Setting.EnemyPatchedPercentage,
            0.01f,
            0f,
            1.01f
        );

        GUILayout.BeginHorizontal();
        GUILayout.Label("物资箱生成词缀概率");
        GUILayout.FlexibleSpace();
        GUILayout.Label(float2Percentage(VTSettingManager.Setting.LootBoxPatchedPercentage));
        GUILayout.EndHorizontal();
        VTSettingManager.Setting.LootBoxPatchedPercentage = GUILayout.HorizontalScrollbar(
            VTSettingManager.Setting.LootBoxPatchedPercentage,
            0.01f,
            0f,
            1.01f
        );

        GUILayout.BeginHorizontal();
        GUILayout.Label("合成道具附带词缀概率");
        GUILayout.FlexibleSpace();
        GUILayout.Label(float2Percentage(VTSettingManager.Setting.CraftPatchedPercentage));
        GUILayout.EndHorizontal();
        VTSettingManager.Setting.CraftPatchedPercentage = GUILayout.HorizontalScrollbar(
            VTSettingManager.Setting.CraftPatchedPercentage,
            0.01f,
            0f,
            1.01f
        );

        if (VT.IsModConnected(ModifiersModBehaviour.MOD_SCAV)) {
            GUILayout.BeginHorizontal();
            GUILayout.Label("SCAV附带词缀概率");
            GUILayout.FlexibleSpace();
            GUILayout.Label(float2Percentage(VTSettingManager.Setting.SCAVPercentage));
            GUILayout.EndHorizontal();
            VTSettingManager.Setting.SCAVPercentage = GUILayout.HorizontalScrollbar(
                VTSettingManager.Setting.SCAVPercentage,
                0.01f,
                0f,
                1.01f
            );
        }

        if (GUI.changed) {
            SettingUtil.OnSettingChangedDebounce();
        }

        
        if (toggleDebug) {
            GUILayout.Label("");
            GUILayout.Label("");
            //词缀操作
            GUILayout.Label("测试功能(会极大影响游戏体验，慎重使用！）");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+随机词缀", GUILayout.Width(80))) {
                Item item = ItemUIUtilities.SelectedItem;
                if (item != null) {
                    VTModifiersCoreV2.TryUnpatchItem(item);
                    VTModifiersCoreV2.PatchItem(item, VTModifiersCoreV2.Sources.Debug);
                }
                else {
                    VT.BubbleUserDebug("Bubble_no_item_select".ToPlainText());
                }
            }
            if (GUILayout.Button("-词缀", GUILayout.Width(80))) {
                Item item = ItemUIUtilities.SelectedItem;
                if (item != null) {
                    VTModifiersCoreV2.TryUnpatchItem(item);
                }
                else {
                    VT.BubbleUserDebug("Bubble_no_item_select".ToPlainText());
                }
            }
            GUILayout.EndHorizontal();
            
            //将modifiers按一行四个去chunk
            int chunkSize = 4;
            var chunks = modifiers.Select((x, i) => new { x, i })
                .GroupBy(x => x.i / chunkSize)
                .Select(g => g.Select(x => x.x).ToList())
                .ToList();
            foreach (var chunk in chunks) {
                GUILayout.BeginHorizontal();
                foreach (var modifier in chunk) {
                    if (GUILayout.Button(modifier.ToPlainText(), GUILayout.Width(80))) {
                        Item item = ItemUIUtilities.SelectedItem;
                        if (item != null) {
                            VTModifiersCoreV2.TryUnpatchItem(item);
                            VTModifiersCoreV2.PatchItem(item, VTModifiersCoreV2.Sources.Debug, modifier);
                        }
                        else {
                            VT.BubbleUserDebug("未选中道具");
                        }
                    }
                }
                GUILayout.EndHorizontal();
            }
            
            //自定义添加道具
            GUILayout.BeginHorizontal();
            itemId = GUILayout.TextField(itemId);
            if (GUILayout.Button("发送到玩家", GUILayout.Width(80))) {
                Item obj = ItemAssetsCollection.InstantiateSync(int.Parse(itemId));
                if (obj) ItemUtilities.SendToPlayer(obj);
            }
            GUILayout.EndHorizontal();
            
            //调试操作
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("输出物品信息", GUILayout.Width(80))) {
                Item item = ItemUIUtilities.SelectedItem;
                if (item != null) {
                    StringBuilder stringBuilder = new StringBuilder();
                    stringBuilder.AppendLine($"输出物品信息:{item.DisplayName}");
                    if (item.Variables != null) {
                        foreach (CustomData variable in item.Variables) {
                            stringBuilder.AppendLine("Variable:" + variable.Key + "\t" + variable.DisplayName + "\t" +
                                                     variable.GetValueDisplayString());
                        }
                    }

                    if (item.Constants != null) {
                        foreach (CustomData constant in item.Constants) {
                            stringBuilder.AppendLine("Constant:" + constant.Key + "\t" + constant.DisplayName + "\t" +
                                                     constant.GetValueDisplayString());
                        }
                    }

                    if ((UnityEngine.Object)item.Stats != (UnityEngine.Object)null) {
                        foreach (Stat stat in item.Stats) {
                            stringBuilder.AppendLine("Stat:" + stat.Key + "\t" +
                                                     string.Format("{0}\t{1}", (object)stat.DisplayName,
                                                         (object)stat.Value));
                        }
                    }

                    if ((UnityEngine.Object)item.Modifiers != (UnityEngine.Object)null) {
                        foreach (ModifierDescription modifier in item.Modifiers) {
                            ModifierTarget mt = modifier.target;
                            string mts = mt.ToString();
                            stringBuilder.AppendLine("Modifier:" + modifier.Key + "\t" + modifier.DisplayName + "\t" +
                                                     "MT:" + mts + "\t" + modifier.GetDisplayValueString());
                        }
                    }


                    string tags = VT.DebugItemTags(item);
                    stringBuilder.AppendLine("Tags:" + tags);
                    Log(stringBuilder.ToString());
                }
            }

            if (GUILayout.Button("$10000", GUILayout.Width(80))) {
                EconomyManager.Add(10000);
            }
            if (GUILayout.Button("移除基地回血buff", GUILayout.Width(80))) {
                CharacterMainControl c = CharacterMainControl.Main;
                c.RemoveBuff(GameplayDataSettings.Buffs.BaseBuff.ID, false);
            }
            // if (GUILayout.Button("测试对话", GUILayout.Width(80))) {
            //     string[] dialog = {
            //         "a1", "a2"
            //     };
            //     VTDialog.DialogFlow(dialog);
            // }
            // if (GUILayout.Button("修改ID名称", GUILayout.Width(80))) {
            //     ItemDisplay itemDisplay = ItemUIUtilities.SelectedItemDisplay;
            //     if (itemDisplay) VT.ForceUpdateItemDisplayName(itemDisplay);
            // }
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("词卡1", GUILayout.Width(80))) {
                ItemUtilities.SendToPlayer(ItemAssetsCollection.InstantiateSync(ItemUtil.MC_CARD_v1));
            }
            if (GUILayout.Button("词卡2", GUILayout.Width(80))) {
                ItemUtilities.SendToPlayer(ItemAssetsCollection.InstantiateSync(ItemUtil.MC_CARD_v2));
            }
            if (GUILayout.Button("词卡3", GUILayout.Width(80))) {
                ItemUtilities.SendToPlayer(ItemAssetsCollection.InstantiateSync(ItemUtil.MC_CARD_v3));
            }
            GUILayout.EndHorizontal();

        }

        GUILayout.EndScrollView();
        GUI.DragWindow();
    }


    public static void Log(string message, bool isError = false) {
        VT.Log(message, isError);
    }

    public class Debouncer {
        private Timer _timer;
        private Action _action;
        private int _debounceTime;

        public Debouncer(Action action, int debounceTime) {
            _action = action;
            _debounceTime = debounceTime;
            _timer = null;
        }

        public void Invoke() {
            if (_timer != null) {
                _timer.Change(Timeout.Infinite, Timeout.Infinite); // 取消之前的计时器
            }

            _timer = new Timer(x => {
                _action(); // 执行实际的操作
                _timer?.Dispose(); // 释放资源
                _timer = null;
            }, null, _debounceTime, Timeout.Infinite);
        }
    }
}