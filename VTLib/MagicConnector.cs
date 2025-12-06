namespace VTModifiers.VTLib; 

public class MagicConnector: Connector {
    public static bool Connected = false;
    public const string MOD_NAME = "VTMagic";
    public static void TryConnect() {
        if (Connected) return;
        if (AssemblyHelper.IsAssemblyLoaded("VTMagic")) {
            Connected = true;
            ModBehaviour.LogStatic("成功连接到[秘法纪元]...");
        }
    }
    public static void OnDeactivated() {
        ModBehaviour.LogStatic("[秘法纪元]已卸载，卸载词条...");
        Connected = false;
    }
    public static void Init() {
        
    }
}