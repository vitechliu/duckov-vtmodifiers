using ItemStatsSystem;

namespace VTModifiers.VTLib;

public class VTModifiers {
    //通用
    
    
    //伤害
    public static int VtDmgHashCode = "VT_MODIFIERS_DAMAGE".GetHashCode();
    //重量
    public static int VtWeightHashCode = "VT_MODIFIERS_WEIGHT".GetHashCode();
    
    
    //仅限Gun
    
    //弹药节省
    public static int VtAmmoSaveHashCode2 = "VT_MODIFIERS_AMMO_SAVE".GetHashCode();

    public static void PatchItem(Item item, Sources source) {
        
    }

    //生成来源
    public enum Sources {
        LootBox, //物资
        Enemy //敌人AI
    }
    
}