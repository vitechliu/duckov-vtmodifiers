using System.Globalization;
using Duckov;
using Duckov.UI;
using Duckov.UI.DialogueBubbles;
using Duckov.Utilities;
using HarmonyLib;
using ItemStatsSystem;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;
using Object = System.Object;

namespace VTModifiers.VTLib;

public static class VTSettingManager {
    public static string SettingFilePath => Path.Combine(ModBehaviour.Instance._cfgDirectory, "config.json");

    public static void LoadSetting() {
        try {
            if (File.Exists(SettingFilePath)) {
                // 读取文件内容
                string json = File.ReadAllText(SettingFilePath);

                // 解析保存的JSON
                var savedJson = Newtonsoft.Json.Linq.JObject.Parse(json);

                // 1. 先获取默认设置（确保新增字段有默认值）
                var defaultSetting = new VtModifierSetting();
                var defaultJson = Newtonsoft.Json.Linq.JObject.FromObject(defaultSetting);

                // 2. 用保存的字段覆盖默认设置（删除的字段会被忽略）
                foreach (var prop in savedJson.Properties()) {
                    defaultJson[prop.Name] = prop.Value;
                }

                // 3. 转换回设置对象
                Setting = defaultJson.ToObject<VtModifierSetting>();
                ModBehaviour.LogStatic("设置加载成功");
            }
            else {
                // 文件不存在，使用默认设置
                Setting = new VtModifierSetting();
                ModBehaviour.LogStatic("未找到设置文件，使用默认设置");
                OnSettingChanged();
            }
        }
        catch (System.Exception ex) {
            ModBehaviour.LogStatic($"加载设置失败: {ex.Message}\n{ex.StackTrace}");
            // 加载失败时强制使用默认设置
            Setting = new VtModifierSetting();
        }
    }

    public static void OnSettingChanged() {
        try {
            // 序列化当前设置（格式化便于查看）
            string json = JsonConvert.SerializeObject(Setting, Formatting.Indented);

            // 写入文件
            File.WriteAllText(SettingFilePath, json);
            if (Setting.Debug) ModBehaviour.LogStatic($"设置已保存到: {SettingFilePath}");
        }
        catch (System.Exception ex) {
            if (Setting.Debug) ModBehaviour.LogStatic($"保存设置失败: {ex.Message}\n{ex.StackTrace}");
        }
    }

    public static VtModifierSetting Setting = new VtModifierSetting();

    public struct VtModifierSetting {
        public bool Debug = false;

        public bool RemoveAllBadModifiers = false;

        public bool AllowReforge = true; //实装+UI
        public bool AllowForge = true; //实装+UI

        public float ReforgePriceFactor = 2f; //实装+UI
        public float ForgePriceFactor = 10f; //实装+UI

        public float EnemyPatchedPercentage = 0.4f; //实装+UI
        public float LootBoxPatchedPercentage = 0.75f; //实装+UI
        public float CraftPatchedPercentage = 0.75f; //实装+UI


        public VtModifierSetting() { }
    }
}