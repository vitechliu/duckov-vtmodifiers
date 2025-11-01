using System.Globalization;
using Duckov.UI.DialogueBubbles;
using Duckov.Utilities;
using ItemStatsSystem;

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

    public static string DebugItemTags(Item item) {
        string res = "";
        foreach (Tag tag in item.Tags) {
            string dd = tag.Show ? "(SHOW)" : "";
            res += $"[{tag.DisplayName}({tag.name}){dd}]";
        }

        return item.DisplayName + " " + res;
    }

    public static void BubbleUserDebug(string word) {
        if (!VTModifiersCore.Setting.Debug) return;
        CharacterMainControl c = CharacterMainControl.Main;
        if (c != null) DialogueBubblesManager.Show("Hello world!", c.characterModel.transform);
    }

    public static string RoundToOneDecimalIfNeeded(string input) {
        if (string.IsNullOrWhiteSpace(input))
            return input ?? string.Empty;

        string trimmedInput = input.Trim();

        // 支持多种数字格式（包括负数、科学计数法等）
        if (decimal.TryParse(trimmedInput, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal number)) {
            // 检查是否包含小数点且有足够的小数位数
            if (HasEnoughDecimalPlaces(trimmedInput, 2)) {
                // 四舍五入到1位小数
                decimal rounded = Math.Round(number, 1, MidpointRounding.AwayFromZero);
                return rounded.ToString("0.0", CultureInfo.InvariantCulture);
            }
        }

        return input;
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