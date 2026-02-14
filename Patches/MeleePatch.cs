using HarmonyLib;
using UnityEngine;
using VTLib;
using VTModifiers.VTLib;

namespace VTModifiers.Patches;

[HarmonyPatch]
public static class MeleePatch {
    // ReSharper disable once UnusedMethodReturnValue.Global
    public static bool MyHurt(DamageReceiver damageReceiver, DamageInfo damageInfo) {
        CharacterMainControl character = damageInfo.fromCharacter;
        if (character) {
            ItemAgent_MeleeWeapon agent = character.GetMeleeWeapon();
            if (agent && agent.Item && VTModifiersCoreV2.IsPatchedItem(agent.Item)) {
                Dictionary<int, float> toModify = new();
                List<ElementFactor> newFactors = new();
                foreach (ElementTypes elementType in VTModifiersCoreV2.ElementMapping.Keys) {
                    float original = 0f;
                    float editted = 0f;
                    foreach (var ef in damageInfo.elementFactors) {
                        if (ef.elementType == elementType) {
                            original = ef.factor;
                            break;
                        }
                    }
                    if (VTModifiersCoreV2.ElementMapping.ContainsKey(elementType)) {
                        editted = VTModifiersCoreV2.Modify(agent.Item, VTModifiersCoreV2.ElementMapping[elementType], original);
                    }
                    if (editted > 0.0f) {
                        ElementFactor newFactor = new () {
                            elementType = elementType,
                            factor = editted
                        };
                        newFactors.Add(newFactor);
                        VTMO.Log($"修改了近战武器的: {elementType}为{editted}");
                    }
                }
                damageInfo.elementFactors = newFactors;
                if (character.IsMainCharacter) {
                    float instantDeathRate = VTModifiersCoreV2.Modify(agent.Item,
                        VTModifiersCoreV2.VtmDeathRate);
                    if (VT.Probability(instantDeathRate)) {
                        damageInfo.damageValue = 999999f;
                    }
                }
            }
        }
        return damageReceiver.Hurt(damageInfo);
    }
    
    [HarmonyPatch(typeof(ItemAgent_MeleeWeapon), nameof(ItemAgent_MeleeWeapon.CheckCollidersInRange))]
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
        var codeMatcher = new CodeMatcher(instructions /*, ILGenerator generator*/);
        codeMatcher.MatchStartForward(
                CodeMatch.Calls(() => default(DamageReceiver)!.Hurt(default))
            )
            .ThrowIfInvalid("Could not find call to DamageReceiver.Hurt")
            .RemoveInstruction()
            .InsertAndAdvance(
                CodeInstruction.Call(() => MyHurt(default!, default))
            );
        return codeMatcher.Instructions();
    }
    
    //扩大刀光
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CA_Attack), "OnStart")]
    public static void CAAttack_OnStart_PostFix(CA_Attack __instance) {
        if (!__instance.characterController.IsMainCharacter) return;
        ItemAgent_MeleeWeapon weapon = __instance.meleeWeapon;
        if (!weapon) return;
        if (!VTModifiersCoreV2.IsPatchedItem(weapon.Item)) return;
        float length = VTModifiersCoreV2.Modify(weapon.Item, VTModifiersCoreV2.VtmShootDistanceMultiplier);
        if (length <= 0.0) return;
        GameObject sfx = weapon.slashFx;
        sfx.transform.localScale *= (1f + length);
    }
    
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CA_Attack), "OnStop")]
    public static void CAAttack_OnStop_PostFix(CA_Attack __instance) {
        if (!__instance.characterController.IsMainCharacter) return;
        ItemAgent_MeleeWeapon weapon =
            __instance.meleeWeapon;
        if (!weapon) return;
        GameObject sfx = weapon.slashFx;
        sfx.transform.localScale = new Vector3(1.92f, 1.92f, 1.92f);
    }
}