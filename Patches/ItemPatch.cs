using Duckov.UI;
using HarmonyLib;
using ItemStatsSystem;
using SodaCraft.Localizations;
using UnityEngine;
using UnityEngine.EventSystems;
using VTModifiers.VTLib;

namespace VTModifiers.Patches; 

[HarmonyPatch]
public static class ItemPatch {
    //重量Patch
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Item), "get_SelfWeight")]
    public static void Item_SelfWeight_PostFix(Item __instance, ref float __result) {
        __result = VTModifiersCoreV2.Modify(__instance, VTModifiersCoreV2.VtmWeight, __result);
    }
    
    //DisplayName patch
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Item), "get_DisplayName")]
    public static void Item_DisplayName_PostFix(Item __instance, ref string __result) {
        if (!VTModifiersCoreV2.IsPatchedItem(__instance)) return;
        string key = __instance.displayName;
        __result = VTModifiersCoreV2.PatchItemDisplayName(__instance, key.ToPlainText());
    }
    
    
    //物品价值Patch
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Item), "GetTotalRawValue")]
    public static void Item_GetTotalRawValue_PostFix(Item __instance, ref int __result) {
        // VTMO.Log($"ItemPriceModify: {__instance.DisplayName}");
        __result = Mathf.RoundToInt(
            VTModifiersCoreV2.Modify(__instance, VTModifiersCoreV2.VtmPriceMultiplier, (float)__result)
        );
    }
    
    //MD:DisplayName patch
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ModifierDescription), "get_DisplayName")]
    public static void ModifierDescription_DisplayName_PostFix(ModifierDescription __instance, ref string __result) {
        if (
            VTModifiersCoreV2.IsModMD(__instance)
            && !__result.StartsWith("VTMC_")
            && !__result.StartsWith("VTM_")
        ) {
            __result = "VTM_" + __result;
        }
    }
    
    
    //拖拽物品色卡
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ItemDisplay), "HandleDirectDrop")]
    public static bool ItemDisplay_HandleDirectDrop_PrePatch(ItemDisplay __instance, PointerEventData eventData) {
        if (__instance.Target == null || eventData.button != PointerEventData.InputButton.Left ||
            __instance.IsStockshopSample)
            return true;
        IItemDragSource component = eventData.pointerDrag.gameObject.GetComponent<IItemDragSource>();
        if (component == null || !component.IsEditable())
            return true;
        Item part = component.GetItem();
        Item main = __instance.Target;
        if (
            part && main
                 && part != main
                 && VTModifiersCoreV2.IsModifiersCard(part)
                 && VTModifiersCoreV2.ItemCanBePatched(main)
        ) {
            VTModifiersCoreV2.PatchByCard(part, main, __instance);
            ItemUIUtilities.NotifyPutItem(part);
            eventData.Use();
            return false;
        }

        return true;
    }

}