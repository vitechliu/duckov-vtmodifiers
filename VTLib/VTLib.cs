using Duckov.Utilities;
using ItemStatsSystem;

namespace VTModifiers.VTLib;

public static class VTLib {
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
            res += $"[{tag.DisplayName}{dd}]";
        }
        return res;
    }
}