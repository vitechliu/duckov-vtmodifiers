using Duckov.ItemBuilders;
using Duckov.Utilities;
using ItemStatsSystem;
using UnityEngine;
using Object = UnityEngine.Object;
using VTLib;

namespace VTModifiers.VTLib.Items; 

public class ItemUtil {
    public const int MC_CARD_v1 = 421390900;
    public const int MC_CARD_v2 = 421390901;
    public const int MC_CARD_v3 = 421390902;
    
    public static int[] MC_CARD_IDS = {MC_CARD_v1, MC_CARD_v2, MC_CARD_v3};

    static void CreateMCCard(int id, int lvl) {
        Item item = ItemBuilder.New()
            .TypeID(id)
            .DisableStacking()
            .Icon(VT.LoadSprite($"modifier_card_v{lvl}.png"))
            .Instantiate();
        // modifiersCard.Tags.Add(new Tag());

        item.DisplayNameRaw = "vt_modifiers_card_v" + lvl;
        item.Tags.Add(GetTargetTag("Electric"));
        item.Tags.Add(GetTargetTag("JLab"));
        if (lvl == 1) item.Value = 1000;
        if (lvl == 2) item.Value = 5000;
        if (lvl == 3) item.Value = 10000;
        item.Quality = 2 * lvl;
        ItemAssetsCollection.AddDynamicEntry(item);
        Object.DontDestroyOnLoad(item.gameObject);
        // VT.Log($"成功加载道具:{item.DisplayName}");
    }
    public static void InitItem() {
        if (!VTSettingManager.Setting.EnableModifiersCard) return;

        CreateMCCard(MC_CARD_v1, 1);
        CreateMCCard(MC_CARD_v2, 2);
        CreateMCCard(MC_CARD_v3, 3);
    }

    public static void UnloadItems() {
        foreach (int id in MC_CARD_IDS) {
            Item prefab = ItemAssetsCollection.GetPrefab(id);
            if (prefab) {
                ItemAssetsCollection.RemoveDynamicEntry(prefab);
            }
        }
    }
    
    
    private static Tag GetTargetTag(string tagName) => (Resources.FindObjectsOfTypeAll<Tag>()).FirstOrDefault<Tag>((Func<Tag, bool>) (t => t.name == tagName));

}