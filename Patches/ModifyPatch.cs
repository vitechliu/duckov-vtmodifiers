using Duckov.Utilities;
using HarmonyLib;
using ItemStatsSystem;
using ItemStatsSystem.Items;
using VTModifiers.VTLib;

namespace VTModifiers.Patches; 

[HarmonyPatch]
public static class ModifyPatch {
    
    //词缀化来源：敌人生成
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CharacterSpawnerRoot), "AddCreatedCharacter")]
    public static void CharacterSpawnerRoot_AddCreatedCharacter_PostFix(
        CharacterSpawnerRoot __instance,
        CharacterMainControl c
    ) {
        if (LevelManager.Instance.IsBaseLevel) return;
        int csrInstanceId = __instance.GetInstanceID();
        if (c.CharacterItem && c.CharacterItem.Inventory) {
            Inventory inventory = c.CharacterItem.Inventory;

            foreach (Item item in inventory) {
                VTModifiersCoreV2.PatchItem(item, VTModifiersCoreV2.Sources.Enemy);
            }

            foreach (Slot slot in c.CharacterItem.Slots) {
                if (slot.Content == null) continue;
                VTModifiersCoreV2.PatchItem(slot.Content, VTModifiersCoreV2.Sources.Enemy);
            }
            // VTMO.Log($"CSRSetup:{csrInstanceId}, itemCount:{itemCount}");
        }
        else {
            // VTMO.Log($"CSRSetupFailed:{csrInstanceId}, cannot find inventory");
        }
    }

    //词缀化来源：物资箱
    [HarmonyPostfix]
    [HarmonyPatch(typeof(LootBoxLoader), "Setup")]
    public static void LootBoxLoader_Setup_PostFix(LootBoxLoader __instance) {
        int lootBoxLoaderId = __instance.GetInstanceID();
        InteractableLootbox lootbox =
            __instance._lootBox;

        if (lootbox != null) {
            string lootBoxName = lootbox.InteractName;
            Inventory inventory = lootbox.Inventory;
            if (inventory != null) {
                int inventoryCount = inventory.Count();
                // VTMO.Log($"LBLSetup:{lootBoxLoaderId}, name:{lootBoxName}, count:{inventoryCount}");
                foreach (Item item in inventory) {
                    VTModifiersCoreV2.PatchItem(item, VTModifiersCoreV2.Sources.LootBox);
                }
            }
            else {
                // VTMO.Log($"LBLSetupFailed:{lootBoxLoaderId}, name:{lootBoxName},, nullInventory");
            }
        }
        else {
            // VTMO.Log($"LBLSetupFailed:{lootBoxLoaderId}, nullLootBox");
        }
    }

    //词缀化来源：合成
    public static void OnItemCrafted(CraftingFormula formula, Item item) {
        VTModifiersCoreV2.PatchItem(item, VTModifiersCoreV2.Sources.Craft);
    }

    //词缀化来源：地面词缀卡
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CharacterItemControl), "PickupItem")]
    public static void CharacterItemControl_PickupItem_PostPatch(
        CharacterItemControl __instance,
        Item item,
        bool __result
    ) {
        if (__result) {
            if (item
                && VTModifiersCoreV2.IsModifiersCard(item)
                && item.FromInfoKey == "Ground"
                && !VTModifiersCoreV2.IsPatchedItem(item)) {
                VTModifiersCoreV2.PatchItem(item, VTModifiersCoreV2.Sources.Card);
            }
        }
    }

}