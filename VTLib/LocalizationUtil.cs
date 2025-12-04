namespace VTModifiers.VTLib;


using Newtonsoft.Json;
using SodaCraft.Localizations;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class LocalizationUtil {
    public static void ReadLang() {
        string directoryPath = Path.Combine(ModBehaviour.Instance._resourceDirectory, "lang");
        SystemLanguage currentLang = LocalizationManager.CurrentLanguage;
        string path = Path.Combine(directoryPath, currentLang.ToString() + ".json");
        if (!File.Exists(path)) {
            VT.Log($"not supporting:{currentLang}, fallback to English");
            currentLang = SystemLanguage.English;
            path = Path.Combine(directoryPath, "English.json");
        }
        try {
            string jsonContent = File.ReadAllText(path);
            Lang lang = JsonConvert.DeserializeObject<Lang>(jsonContent);
            foreach (string key in lang.lang.Keys) {
                LocalizationManager.SetOverrideText(key, lang.lang[key]);
            }
            VT.Log($"加载语言{currentLang}成功");
        }
        catch (JsonException jsonEx) {
            VT.Log($"语言JSON解析错误， {path}: {jsonEx.Message}");
        }
        catch (Exception ex) {
            VT.Log($"读取文件 语言JSON错误， {path}: {ex.Message}");
        }
    }

    public struct Lang {
        public string author = "Official";
        public string version = "0.0.1";
        public Dictionary<string, string> lang = new Dictionary<string, string>();
        public Lang() {
        }
    }
}