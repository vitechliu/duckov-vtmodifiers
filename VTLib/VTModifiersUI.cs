using System.Text;
using Duckov.Economy;
using Duckov.UI;
using Duckov.Utilities;
using HarmonyLib;
using ItemStatsSystem;
using TMPro;
using UnityEngine;

namespace VTModifiers.VTLib;

public class VTModifiersUI : MonoBehaviour {
    private ModBehaviour mod;
    
    private bool show = false;

    private void Start() {
        if (this.mod == null) {
            this.mod = ModBehaviour.Instance;
        }
    }

    
    void Update() {
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) {
            if (Input.GetKeyDown(KeyCode.F3)) {
                show = !show;
            }
        }
    }
    
    private bool toggleDebug = true;
    private void OnGUI() {
        if (show)
            windowRect = GUILayout.Window(711451401, windowRect, WindowFunc, "VTModifier Mod Setting");
    }

    private Vector2 vt;
    private Rect windowRect = new Rect(50, 50, 400, 300);
    void WindowFunc(int id) {
        
        vt = GUILayout.BeginScrollView(vt);
        GUILayout.BeginHorizontal();
        
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("x", GUILayout.Width(20))) {
            show = false;
        }
        GUILayout.EndHorizontal();
        GUILayout.Label("游戏设定");
        
        
        
        toggleDebug = GUILayout.Toggle (VTModifiersCore.Setting.Debug, "Debug 调试模式");
        VTModifiersCore.Setting.Debug = toggleDebug;

        
        //重铸设定
        GUILayout.BeginHorizontal();
        VTModifiersCore.Setting.allowReforge = GUILayout.Toggle (VTModifiersCore.Setting.allowReforge, "右键重铸");
        GUILayout.FlexibleSpace();
        VTModifiersCore.Setting.allowForge = GUILayout.Toggle (VTModifiersCore.Setting.allowForge, "右键词缀化");
        GUILayout.EndHorizontal();
        
        
        GUILayout.Label("重铸价格倍率(0.1倍到10倍)");
        VTModifiersCore.Setting.reforgePriceFactor = GUILayout.HorizontalScrollbar(
            VTModifiersCore.Setting.reforgePriceFactor,
            0.01f,
            0.1f,
            10f
        );
        
        //概率
        GUILayout.Label("敌人生成词缀概率");
        VTModifiersCore.Setting.enemyPatchedPercentage = GUILayout.HorizontalScrollbar(
            VTModifiersCore.Setting.enemyPatchedPercentage,
            0.01f,
            0f,
            1f
        );
        
        GUILayout.Label("物资箱生成词缀概率");
        VTModifiersCore.Setting.lootBoxPatchedPercentage = GUILayout.HorizontalScrollbar(
            VTModifiersCore.Setting.lootBoxPatchedPercentage,
            0.01f,
            0f,
            1f
        );
        
        GUILayout.Label("合成道具附带词缀概率");
        VTModifiersCore.Setting.craftPatchedPercentage = GUILayout.HorizontalScrollbar(
            VTModifiersCore.Setting.craftPatchedPercentage,
            0.01f,
            0f,
            1f
        );
        
        
        if (toggleDebug) {
            //词缀操作
            GUILayout.Label("测试功能(会极大影响游戏体验，慎重使用！）");
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+随机词缀", GUILayout.Width(80))) {
                Item item = ItemUIUtilities.SelectedItem;
                if (item != null) {
                    VTModifiersCore.TryUnpatchItem(item);
                    VTModifiersCore.PatchItem(item, VTModifiersCore.Sources.Debug);
                }
                else {
                    VT.BubbleUserDebug("未选中道具");
                }
            }
            if (GUILayout.Button("+Debug词缀", GUILayout.Width(80))) {
                Item item = ItemUIUtilities.SelectedItem;
                if (item != null) {
                    VTModifiersCore.TryUnpatchItem(item);
                    VTModifiersCore.PatchItem(item, VTModifiersCore.Sources.Debug, "Debug");
                }
                else {
                    VT.BubbleUserDebug("未选中道具");
                }
            }
            if (GUILayout.Button("-词缀", GUILayout.Width(80))) {
                Item item = ItemUIUtilities.SelectedItem;
                if (item != null) {
                    VTModifiersCore.TryUnpatchItem(item);
                }
                else {
                    VT.BubbleUserDebug("未选中道具");
                }
            }
            GUILayout.EndHorizontal();
            
            
            //调试操作
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("输出物品信息", GUILayout.Width(80))) {
                Item item = ItemUIUtilities.SelectedItem;
                if (item != null) {
                    StringBuilder stringBuilder = new StringBuilder();
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
                            ModifierTarget mt = Traverse.Create(modifier).Field("target").GetValue<ModifierTarget>();
                            string mts = mt.ToString();
                            stringBuilder.AppendLine("Modifier:" + modifier.Key + "\t" + modifier.DisplayName + "\t" +
                                                     "MT:" + mts + "\t" + modifier.GetDisplayValueString());
                        }
                    }

                    Log($"ItemSelecting:{item.DisplayName}");
                    Log(stringBuilder.ToString());
                }
            }
            if (GUILayout.Button("$10000", GUILayout.Width(80))) {
                EconomyManager.Add(10000);
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
        }
        
        GUILayout.EndScrollView();
        GUI.DragWindow();
    }
    
    
    public static void Log(string message, bool isError = false) {
        ModBehaviour.LogStatic(message, isError);
    }
}