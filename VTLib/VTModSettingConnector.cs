using System.Globalization;
using System.Text;
using Duckov.Economy;
using Duckov.UI;
using Duckov.Utilities;
using HarmonyLib;
using ItemStatsSystem;
using TMPro;
using UnityEngine;
using VTModifiers.ThirdParty;

namespace VTModifiers.VTLib;

public class VTModSettingConnector {
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
        ModSettingAPI.AddToggle("FixMode", "词缀属性是否固定", VTSettingManager.Setting.FixMode, 
            b => {
                VTSettingManager.Setting.FixMode = b;
                OnSettingChanged();
            });
        ModSettingAPI.AddSlider("ArmorThreshold", "护甲属性倍率", VTSettingManager.Setting.ArmorThreshold, 
            new Vector2(0.1f, 4f), f => {
                VTSettingManager.Setting.ArmorThreshold = f;
                OnSettingChanged();
            });
        ModSettingAPI.AddSlider("DamageThreshold", "伤害属性倍率", VTSettingManager.Setting.DamageThreshold, 
            new Vector2(0.1f, 4f), f => {
                VTSettingManager.Setting.DamageThreshold = f;
                OnSettingChanged();
            });
        ModSettingAPI.AddToggle("Reforge", "允许重铸", VTSettingManager.Setting.AllowReforge, 
            b => {
                VTSettingManager.Setting.AllowReforge = b;
                OnSettingChanged();
            });
        ModSettingAPI.AddSlider("ReforgePriceFactor", "重铸价格倍率", VTSettingManager.Setting.ReforgePriceFactor, 
            new Vector2(0.1f, 10f), f => {
                VTSettingManager.Setting.ReforgePriceFactor = f;
                OnSettingChanged();
            });
        ModSettingAPI.AddToggle("Forge", "允许词缀附加", VTSettingManager.Setting.AllowForge, 
            b => {
                VTSettingManager.Setting.AllowForge = b;
                OnSettingChanged();
            });
        ModSettingAPI.AddSlider("ForgePriceFactor", "词缀附加价格倍率", VTSettingManager.Setting.ForgePriceFactor, 
            new Vector2(0.1f, 20f), f => {
                VTSettingManager.Setting.ForgePriceFactor = f;
                OnSettingChanged();
            });
        ModSettingAPI.AddSlider("EnemyPatchedPercentage", "敌人生成词缀概率", VTSettingManager.Setting.EnemyPatchedPercentage, 
            new Vector2(0f, 1f), f => {
                VTSettingManager.Setting.EnemyPatchedPercentage = f;
                OnSettingChanged();
            });
        ModSettingAPI.AddSlider("LootBoxPatchedPercentage", "物资箱生成词缀概率", VTSettingManager.Setting.LootBoxPatchedPercentage, 
            new Vector2(0f, 1f), f => {
                VTSettingManager.Setting.LootBoxPatchedPercentage = f;
                OnSettingChanged();
            });
        ModSettingAPI.AddSlider("CraftPatchedPercentage", "合成道具附带词缀概率", VTSettingManager.Setting.CraftPatchedPercentage, 
            new Vector2(0f, 1f), f => {
                VTSettingManager.Setting.CraftPatchedPercentage = f;
                OnSettingChanged();
            });
    }

    static void OnSettingChanged() {
        if (VTModifiersUI.debouncer != null) {
            VTModifiersUI.debouncer.Invoke();
        }
    }
}