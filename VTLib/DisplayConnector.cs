using ItemStatsSystem;
using SodaCraft.Localizations;

namespace VTModifiers.VTLib; 

//更好的
public class DisplayConnector: Connector {
    public static bool Connected = false;
    public const string MOD_NAME = "CustomItemLevelValue";
    public static void TryConnect() {
        // VT.Log("tryConnect1");
        if (Connected) return;
        if (AssemblyHelper.IsAssemblyLoaded("CustomItemLevelValue")) {
            Connected = true;
            ModBehaviour.LogStatic("成功连接到[更丰富的信息显示]...");
        }
    }
    public static void OnDeactivated() {
        ModBehaviour.LogStatic("[更丰富的信息显示]已卸载");
        Connected = false;
    }
    public static void PatchItem(Item item, string modifier) {
        //能走到这里，说明modifier一定能找到
        item.SetString(VTModifiersCoreV2.VariableVtModifierDisplayHashCode, modifier.ToPlainText());
    }
}