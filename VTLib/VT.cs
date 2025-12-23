using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using Duckov;
using Duckov.UI;
using Duckov.UI.DialogueBubbles;
using Duckov.Utilities;
using HarmonyLib;
using ItemStatsSystem;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;
using Object = System.Object;

namespace VTModifiers.VTLib;

public static class VT {
    // public static Dictionary<string, object> loadConfig(string path) {
    //     if (File.Exists(path)) {
    //         
    //     }
    // }
    public static bool Probability(float probability) {
        return UnityEngine.Random.value < probability;
    }

    public static bool RemoveItemVariable(CustomDataCollection variables, string variableKey) {
        CustomData cd = variables.GetEntry(variableKey);
        if (cd == null) return false;
        variables.Remove(cd);
        return true;
    }

    public static void Log(string message, bool isError = false) {
        VTModifiersUI.Log(message, isError);
    }

    public static void SetButtonText(Button button, string text) {
        foreach (TextMeshProUGUI componentsInChild in button.GetComponentsInChildren<TextMeshProUGUI>()) {
            if (componentsInChild) ((TMP_Text)componentsInChild).text = text;
        }
        //
        // foreach (Text componentsInChild in button.GetComponentsInChildren<Text>()) {
        //     if (componentsInChild) componentsInChild.text = text;
        // }
    }

    public static void SetButtonColor(Button button, Color color) {
        foreach (ProceduralImage image in button.GetComponentsInChildren<ProceduralImage>()) {
            image.color = color;
        }
    }

    public static void ForceUpdateItemDisplayName(ItemDisplay itemDisplay) {
        Traverse.Create(itemDisplay).Field("nameText").GetValue<TextMeshProUGUI>().text
            = itemDisplay.Target.DisplayName;
    }

    public static void PostCustomSFX(string sfxName, GameObject gameObject = null, bool loop = false) {
        string path = Path.Combine(ModBehaviour.Instance._resourceDirectory, "sfx", sfxName);
        AudioManager.PostCustomSFX(path, gameObject, loop);
    }

    public static void HookItemOperationMenuAdd() { }

    public static string DebugItemTags(Item item) {
        string res = "";
        foreach (Tag tag in item.Tags) {
            string dd = tag.Show ? "(SHOW)" : "";
            res += $"[{tag.DisplayName}({tag.name}){dd}]";
        }

        return item.DisplayName + " " + res;
    }

    public static void BubbleUserDebug(string word, bool debug = true) {
        if (debug && !VTSettingManager.Setting.Debug) return;
        CharacterMainControl c = CharacterMainControl.Main;
        if (c != null) DialogueBubblesManager.Show(word, c.transform);
        NotificationText.Push(word);
    }

    public static string RoundToOneDecimalIfNeeded(string input) {
        if (string.IsNullOrWhiteSpace(input))
            return input ?? string.Empty;

        string trimmedInput = input.Trim();

        // 支持多种数字格式（包括负数、科学计数法等）
        if (decimal.TryParse(trimmedInput, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal number)) {
            // 检查是否包含小数点且有足够的小数位数
            if (HasEnoughDecimalPlaces(trimmedInput, 3)) {
                // 四舍五入到1位小数
                decimal rounded = Math.Round(number, 1, MidpointRounding.AwayFromZero);
                return rounded.ToString("0.0", CultureInfo.InvariantCulture);
            }
        }

        return input;
    }

    public static void OpenFolderInExplorer(string folderPath) {
        if (!Directory.Exists(folderPath)) {
            throw new DirectoryNotFoundException($"目录不存在: {folderPath}");
        }
        folderPath = folderPath.Replace('/', Path.DirectorySeparatorChar);
        try {
            if (Application.platform == RuntimePlatform.WindowsPlayer) {
                // Windows
                VT.Log("openPath:" + folderPath);
                Process.Start("explorer.exe", folderPath);
            }
            else if (Application.platform == RuntimePlatform.OSXPlayer) {
                // macOS
                Process.Start("open", folderPath);
            }
            else if (Application.platform == RuntimePlatform.LinuxPlayer) {
                // Linux
                Process.Start("xdg-open", folderPath);
            }
        }
        catch (Exception ex) {
            VT.Log($"打开文件浏览器时出错: {ex.Message}", true);
        }
    }
    
    [CanBeNull]
    public static Sprite LoadSprite(string spriteName) {
        string path = Path.Combine(ModBehaviour.Instance._resourceDirectory, "sprites", spriteName);
        try {
            Sprite iconSprite = null!;
            Texture2D texture = null!;
            byte[] iconData = File.ReadAllBytes(path);
            texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(texture, iconData)) {
                return null;
            }

            texture.filterMode = FilterMode.Bilinear;
            texture.Apply();
            iconSprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f
            );
            return iconSprite;
        }
        catch (Exception ex) {
        }

        return null;
    }

    private static bool HasEnoughDecimalPlaces(string numberString, int minDecimalPlaces) {
        // 移除可能的千分位分隔符
        string cleanString = numberString.Replace(",", "");

        int decimalPointIndex = cleanString.IndexOf('.');

        if (decimalPointIndex == -1)
            return false; // 没有小数点

        // 计算实际的小数位数
        int actualDecimalPlaces = cleanString.Length - decimalPointIndex - 1;

        return actualDecimalPlaces >= minDecimalPlaces;
    }
}