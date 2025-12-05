namespace VTModifiers.VTLib; 

public static class MagicConnector {
    public static bool Connected = false;

    public static void TryConnect() {
        if (Connected) return;
        ModBehaviour.LogStatic("尝试连接到[秘法纪元]...");
        Connected = true;
    }

    public static void OnVtMagicDeactivated() {
        ModBehaviour.LogStatic("[秘法纪元]已卸载，卸载词条...");
        Connected = false;
    }
    public static void Init() {
        
    }
}