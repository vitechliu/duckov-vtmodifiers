namespace VTModifiers.VTLib;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public static class AssemblyHelper {
    /// <summary>
    /// 判断类型是否已加载
    /// </summary>
    public static bool IsTypeLoaded(string typeFullName) {
        return FindLoadedType(typeFullName) != null;
    }

    /// <summary>
    /// 判断命名空间下的类是否已加载
    /// </summary>
    public static bool IsTypeLoaded(string namespaceName, string className) {
        return IsTypeLoaded($"{namespaceName}.{className}");
    }

    /// <summary>
    /// 查找已加载的类型
    /// </summary>
    public static Type FindLoadedType(string typeFullName) {
        if (string.IsNullOrWhiteSpace(typeFullName))
            return null;

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
            try {
                // 跳过动态程序集
                if (assembly.IsDynamic)
                    continue;

                var type = assembly.GetType(typeFullName, false);
                if (type != null)
                    return type;
            }
            catch {
                // 忽略异常，继续检查其他程序集
                continue;
            }
        }

        return null;
    }

    /// <summary>
    /// 获取所有已加载的程序集名称
    /// </summary>
    public static List<string> GetLoadedAssemblyNames() {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic)
            .Select(a => a.GetName().Name)
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// 获取指定命名空间下所有已加载的类型
    /// </summary>
    public static List<Type> GetTypesInNamespace(string namespaceName) {
        var result = new List<Type>();

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
            try {
                if (assembly.IsDynamic)
                    continue;

                var types = assembly.GetTypes()
                    .Where(t => t.Namespace == namespaceName)
                    .ToArray();

                result.AddRange(types);
            }
            catch (ReflectionTypeLoadException ex) {
                // 处理部分类型加载失败的情况
                var loadedTypes = ex.Types.Where(t => t != null && t.Namespace == namespaceName);
                result.AddRange(loadedTypes);
            }
            catch {
                continue;
            }
        }

        return result;
    }

    /// <summary>
    /// 检查程序集是否已加载
    /// </summary>
    public static bool IsAssemblyLoaded(string assemblyName) {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Any(a => !a.IsDynamic &&
                      a.GetName().Name.Equals(assemblyName, StringComparison.OrdinalIgnoreCase));
    }
}