using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using VTModifiers.VTLib;

namespace VTModifiers.Patches;
[HarmonyDebug]
[HarmonyPatch(typeof(ItemAgent_MeleeWeapon), nameof(ItemAgent_MeleeWeapon.CheckCollidersInRange))]
public static class MeleePatch {
    public static DamageInfo CustomLogic(DamageInfo damageInfo, ModBehaviour __instance) {
        VTMO.Log("CustomLogicRun");
        return damageInfo;
    }

    public static bool MyHurt(DamageReceiver damageReceiver, DamageInfo damageInfo) {
        VTMO.Log("MyHurt");
        return damageReceiver.Hurt(damageInfo);
    }

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
        var codeMatcher = new CodeMatcher(instructions /*, ILGenerator generator*/);
        codeMatcher.MatchStartForward(
                CodeMatch.Calls(() => default(DamageReceiver).Hurt(default))
            )
            .ThrowIfInvalid("Could not find call to DamageReceiver.Hurt")
            .RemoveInstruction()
            .InsertAndAdvance(
                CodeInstruction.Call(() => MyHurt(default, default))
            );

        return codeMatcher.Instructions();
    }
}