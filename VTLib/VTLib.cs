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
}