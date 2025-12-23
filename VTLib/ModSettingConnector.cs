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

namespace VTModifiers.VTLib;

public static class ModSettingConnector {
    public static void Init() {
        bool success = ModSettingAPI.Init(ModBehaviour.Instance.info);
        if (!success) {
            ModBehaviour.LogStatic("接入ModSetting失败");
            return;
        }
        
        ModSettingAPI.AddToggle("Debug", "Debug Mode", VTSettingManager.Setting.Debug, 
            b => {
                VTSettingManager.Setting.Debug = b;
                OnSettingChanged();
            });
        ModSettingAPI.AddToggle("FixMode", "MSText_FixMode".ToPlainText(), VTSettingManager.Setting.FixMode, 
            b => {
                VTSettingManager.Setting.FixMode = b;
                OnSettingChanged();
            });
        ModSettingAPI.AddToggle("EnableModifiersCard", "MSText_EnableModifiersCard".ToPlainText(), VTSettingManager.Setting.EnableModifiersCard, 
            b => {
                VTSettingManager.Setting.EnableModifiersCard = b;
                OnSettingChanged();
            });
        ModSettingAPI.AddSlider("ArmorThreshold", "MSText_ArmorThreshold".ToPlainText(), VTSettingManager.Setting.ArmorThreshold, 
            new Vector2(0.1f, 4f), f => {
                VTSettingManager.Setting.ArmorThreshold = f;
                OnSettingChanged();
            });
        ModSettingAPI.AddSlider("DamageThreshold", "MSText_DamageThreshold".ToPlainText(), VTSettingManager.Setting.DamageThreshold, 
            new Vector2(0.1f, 4f), f => {
                VTSettingManager.Setting.DamageThreshold = f;
                OnSettingChanged();
            });
        ModSettingAPI.AddToggle("Reforge", "MSText_Reforge".ToPlainText(), VTSettingManager.Setting.AllowReforge, 
            b => {
                VTSettingManager.Setting.AllowReforge = b;
                OnSettingChanged();
            });
        ModSettingAPI.AddSlider("ReforgePriceFactor", "MSText_ReforgePriceFactor".ToPlainText(), VTSettingManager.Setting.ReforgePriceFactor, 
            new Vector2(0.1f, 10f), f => {
                VTSettingManager.Setting.ReforgePriceFactor = f;
                OnSettingChanged();
            });
        ModSettingAPI.AddToggle("Forge", "MSText_Forge".ToPlainText(), VTSettingManager.Setting.AllowForge, 
            b => {
                VTSettingManager.Setting.AllowForge = b;
                OnSettingChanged();
            });
        ModSettingAPI.AddSlider("ForgePriceFactor", "MSText_ForgePriceFactor".ToPlainText(), VTSettingManager.Setting.ForgePriceFactor, 
            new Vector2(0.1f, 20f), f => {
                VTSettingManager.Setting.ForgePriceFactor = f;
                OnSettingChanged();
            });
        ModSettingAPI.AddSlider("EnemyPatchedPercentage", "MSText_EnemyPatchedPercentage".ToPlainText(), VTSettingManager.Setting.EnemyPatchedPercentage, 
            new Vector2(0f, 1f), f => {
                VTSettingManager.Setting.EnemyPatchedPercentage = f;
                OnSettingChanged();
            });
        ModSettingAPI.AddSlider("LootBoxPatchedPercentage", "MSText_LootBoxPatchedPercentage".ToPlainText(), VTSettingManager.Setting.LootBoxPatchedPercentage, 
            new Vector2(0f, 1f), f => {
                VTSettingManager.Setting.LootBoxPatchedPercentage = f;
                OnSettingChanged();
            });
        ModSettingAPI.AddSlider("CraftPatchedPercentage", "MSText_CraftPatchedPercentage".ToPlainText(), VTSettingManager.Setting.CraftPatchedPercentage, 
            new Vector2(0f, 1f), f => {
                VTSettingManager.Setting.CraftPatchedPercentage = f;
                OnSettingChanged();
            });
        
        ModSettingAPI.AddToggle("EnableCommunityModifiers", "MSText_EnableCommunityModifiers".ToPlainText(), VTSettingManager.Setting.EnableCommunityModifiers, 
            b => {
                VTSettingManager.Setting.EnableCommunityModifiers = b;
                OnSettingChanged();
            });
        ModSettingAPI.AddToggle("EnableArcaneModifiers", "MSText_EnableArcaneModifiers".ToPlainText(), VTSettingManager.Setting.EnableArcaneModifiers, 
            b => {
                VTSettingManager.Setting.EnableArcaneModifiers = b;
                OnSettingChanged();
            });
        ModSettingAPI.AddButton("CustomModifiersDirectory", "MSText_CustomModifiersDirectory".ToPlainText(), "MSText_Open".ToPlainText(), 
            () => {
                if (Directory.Exists(ModBehaviour.Instance._modifiersDirectoryCustom)) {
                    //打开对应目录
                    VT.OpenFolderInExplorer(ModBehaviour.Instance._modifiersDirectoryCustom);
                }
            });
        ModSettingAPI.AddButton("OfficialModifiersDirectory", "MSText_OfficialModifiersDirectory".ToPlainText(), "MSText_Open".ToPlainText(), 
            () => {
                if (Directory.Exists(ModBehaviour.Instance._modifiersDirectoryPersistant)) {
                    //打开对应目录
                    VT.OpenFolderInExplorer(ModBehaviour.Instance._modifiersDirectoryPersistant);
                }
            });
        ModSettingAPI.AddKeybinding("ReforgeKey", "MSText_ReforgeKey".ToPlainText(), VTSettingManager.Setting.ReforgeKey, 
            b => {
                VTSettingManager.Setting.ReforgeKey = b;
                OnSettingChanged();
            });
    }
    public static void InitSCAV() {
        bool success = ModSettingAPI.Init(ModBehaviour.Instance.info);
        if (!success) {
            return;
        }
        
        ModSettingAPI.AddSlider("SCAVPercentage", "SCAV模式生成词缀概率", VTSettingManager.Setting.SCAVPercentage, 
            new Vector2(0f, 1f), f => {
                VTSettingManager.Setting.SCAVPercentage = f;
                OnSettingChanged();
            });
    }
    static void OnSettingChanged() {
        if (VTModifiersUI.debouncer != null) {
            VTModifiersUI.debouncer.Invoke();
        }
    }
}