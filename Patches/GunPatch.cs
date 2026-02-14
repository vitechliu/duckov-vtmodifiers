using HarmonyLib;
using UnityEngine;
using VTLib;
using VTModifiers.VTLib;

namespace VTModifiers.Patches; 

[HarmonyPatch]
public static class GunPatch {
    
    //枪械Patch
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ItemAgent_Gun), "ShootOneBullet")]
    public static void ItemAgentGun_ShootOneBullet_PostFix(ItemAgent_Gun __instance) {
        if (!__instance || !__instance.Item) return;
        if (!VTModifiersCoreV2.IsPatchedItem(__instance.Item)) return;
        try {
            Projectile temp = __instance.projInst;
            if (!temp) return;
            float instantDeathRate = VTModifiersCoreV2.Modify(__instance.Item,
                VTModifiersCoreV2.VtmDeathRate);
            if (VT.Probability(instantDeathRate)) {
                CharacterMainControl c = __instance.Holder;
                //只有玩家才能应用即死
                if (c && c.IsMainCharacter)
                    temp.context.damage = 999999f;
            }

            temp.context.element_Electricity = VTModifiersCoreV2.Modify(__instance.Item,
                VTModifiersCoreV2.VtmElementElectricity, temp.context.element_Electricity);
            temp.context.element_Ice = VTModifiersCoreV2.Modify(__instance.Item,
                VTModifiersCoreV2.VtmElementIce, temp.context.element_Ice);
            temp.context.element_Fire =
                VTModifiersCoreV2.Modify(__instance.Item, VTModifiersCoreV2.VtmElementFire, temp.context.element_Fire);
            temp.context.element_Poison = VTModifiersCoreV2.Modify(__instance.Item, VTModifiersCoreV2.VtmElementPoison,
                temp.context.element_Poison);
            temp.context.element_Space = VTModifiersCoreV2.Modify(__instance.Item, VTModifiersCoreV2.VtmElementSpace,
                temp.context.element_Space);
            temp.context.element_Ghost = VTModifiersCoreV2.Modify(__instance.Item, VTModifiersCoreV2.VtmElementGhost,
                temp.context.element_Ghost);
            temp.context.bleedChance =
                VTModifiersCoreV2.Modify(__instance.Item, VTModifiersCoreV2.VtmBleedChance, temp.context.bleedChance);


            float controlMindTypeRaw = VTModifiersCoreV2.Modify(__instance.Item, VTModifiersCoreV2.VtmControlMindType, 0f);
            int controlMindType = Mathf.RoundToInt(controlMindTypeRaw);
            VTMO.Log("cmt:" + controlMindType);
            if (controlMindType is > 0 and < 3) {
                temp.context.controlMindType = (ControlMindTypes) controlMindType;
            }
        }
        catch (Exception ex) {
            VTMO.Log($"PatchFailed: {ex.Message}\n{ex.StackTrace}");
        }
    }
    
    //弹药节省Patch
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ItemSetting_Gun), "UseABullet")]
    public static bool ItemSettingGun_UseABullet_PreFix(ItemSetting_Gun __instance) {
        float ammoSaveChance = VTModifiersCoreV2.Modify(__instance.Item, VTModifiersCoreV2.VtmAmmoSave);
        return !VT.Probability(ammoSaveChance);
    }
}