using System.Reflection;
using ItemStatsSystem;
using SodaCraft.Localizations;
using VTLib;

namespace VTModifiers.VTLib;

//更好的
public class DisplayConnector {
    public static bool Connected => VTMO.IsModConnected(VTMO.MOD_CILV);
    public static void PatchItem(Item item, VTModifiersCoreV2.VtModifierV2 modifier) {
        //能走到这里，说明modifier一定能找到
        string modifierName = modifier.key.ToPlainText();
        item.SetString(VTModifiersCoreV2.VariableVtModifierDisplayHashCode, modifierName);
        string modifierAuthor = VTModifiersCoreV2.AuthorData.GetValueOrDefault(modifier.key, "") ?? throw new ArgumentNullException("VTModifiersCoreV2.AuthorData.GetValueOrDefault(modifier, \"\")");

        if (modifierAuthor != "" && modifierAuthor != "Official") {
            if (modifierAuthor == "Community") modifierAuthor = "群友";
            item.SetString(VTModifiersCoreV2.VariableVtAuthorDisplayHashCode, modifierAuthor);
        }
        item.SetString(VTModifiersCoreV2.VariableVtLevelDisplayHashCode, modifier.quality.ToString());
    }

    public static void TryRefresh(Item item) {
        if (!Connected) return;
        try {
            var type = Type.GetType("CustomItemLevelValue.Core.ModExtensionsManager, CustomItemLevelValue");
            if (type != null) {
                // 获取Instance属性
                var instanceProperty = type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                if (instanceProperty == null) return;
                var instance = instanceProperty.GetValue(null);
                // 获取双参数的方法
                var method = type.GetMethod("RefreshItemCache", 
                    new Type[] { typeof(Item), typeof(bool) });
                if (method != null) {
                    // 调用方法，refreshUI设置为true
                    method.Invoke(instance, new object[] { item, true });
                    // VTMO.Log("invoke success");
                }

                
            }
        }
        catch (Exception e) {
            VTMO.Log("[更丰富的信息显示]刷新失败:" + e.Message);
        }
    }
}