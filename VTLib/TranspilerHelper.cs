using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace VTModifiers.VTLib;

public static class TranspilerHelper {
//     private static int FindDamageInfoLocalIndex(CodeMatcher matcher) {
//         // 保存当前位置
//         int startPos = matcher.Pos;
//
//         // 重置到开始
//         matcher.Start();
//
//         int damageInfoLocalIndex = -1;
//
//         // 查找创建 DamageInfo 的 newobj 指令
//         while (!matcher.IsEnd) {
//             // 查找 newobj DamageInfo
//             if (matcher.Opcode == OpCodes.Newobj) {
//                 var ctor = matcher.Operand as ConstructorInfo;
//                 if (ctor != null && ctor.DeclaringType == typeof(DamageInfo)) {
//                     // 继续查找下一个 stloc 指令，以获取本地变量索引
//                     matcher.Advance(1);
//
//                     if (!matcher.IsEnd) {
//                         damageInfoLocalIndex = GetLocalIndexFromStloc(matcher.Instruction);
//                         break;
//                     }
//                 }
//             }
//
//             matcher.Advance(1);
//         }
//
//         // 恢复到原来的位置
//         matcher.Start();
//
//         return damageInfoLocalIndex;
//     }

    // 从 stloc 指令获取本地变量索引
    public static int GetLocalIndexFromStloc(CodeInstruction instruction) {
        if (instruction.opcode == OpCodes.Stloc_0) return 0;
        if (instruction.opcode == OpCodes.Stloc_1) return 1;
        if (instruction.opcode == OpCodes.Stloc_2) return 2;
        if (instruction.opcode == OpCodes.Stloc_3) return 3;
        if (instruction.opcode == OpCodes.Stloc_S && instruction.operand is int i) return i;
        if (instruction.opcode == OpCodes.Stloc && instruction.operand is int j) return j;

        // 如果是byte类型（ldloc.s通常使用byte）
        if (instruction.opcode == OpCodes.Stloc_S && instruction.operand is byte b) return b;

        return -1;
    }

    // 检查是否为加载指定索引的本地变量
    public static bool IsLdlocForIndex(CodeInstruction instruction, int index) {
        // 处理标准加载指令
        if (index == 0 && instruction.opcode == OpCodes.Ldloc_0) return true;
        if (index == 1 && instruction.opcode == OpCodes.Ldloc_1) return true;
        if (index == 2 && instruction.opcode == OpCodes.Ldloc_2) return true;
        if (index == 3 && instruction.opcode == OpCodes.Ldloc_3) return true;

        // 处理带参数的加载指令
        if (instruction.opcode == OpCodes.Ldloc_S) {
            if (instruction.operand is int i && i == index) return true;
            if (instruction.operand is byte b && b == index) return true;
        }

        if (instruction.opcode == OpCodes.Ldloc && instruction.operand is int j && j == index) return true;

        return false;
    }

    // 获取加载本地变量的指令
    public static CodeInstruction GetLdlocInstruction(int index) {
        switch (index) {
            case 0: return new CodeInstruction(OpCodes.Ldloc_0);
            case 1: return new CodeInstruction(OpCodes.Ldloc_1);
            case 2: return new CodeInstruction(OpCodes.Ldloc_2);
            case 3: return new CodeInstruction(OpCodes.Ldloc_3);
            default:
                if (index <= 255)
                    return new CodeInstruction(OpCodes.Ldloc_S, index);
                else
                    return new CodeInstruction(OpCodes.Ldloc, index);
        }
    }

    // 获取存储到本地变量的指令
    public static CodeInstruction GetStlocInstruction(int index) {
        switch (index) {
            case 0: return new CodeInstruction(OpCodes.Stloc_0);
            case 1: return new CodeInstruction(OpCodes.Stloc_1);
            case 2: return new CodeInstruction(OpCodes.Stloc_2);
            case 3: return new CodeInstruction(OpCodes.Stloc_3);
            default:
                if (index <= 255)
                    return new CodeInstruction(OpCodes.Stloc_S, index);
                else
                    return new CodeInstruction(OpCodes.Stloc, index);
        }
    }

}