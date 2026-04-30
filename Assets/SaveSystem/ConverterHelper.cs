using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class ConverterHelper
{
    public static List<JsonConverter> GetConverterInstances()
    {
        List<JsonConverter> instances = new List<JsonConverter>();
        List<Type> types = GetLeafTypes(typeof(SaveJsonConverterBase<>));

        foreach (Type type in types)
        {
            try
            {
                // 使用无参数构造函数创建实例
                JsonConverter instance = Activator.CreateInstance(type) as JsonConverter;
                if (instance != null)
                {
                    instances.Add(instance);
                    Debug.Log($"成功创建转换器: {type.Name}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"创建类型 {type.Name} 的实例失败: {ex.Message}");
            }
        }

        return instances;
    }

    /// <summary>
    /// 获取所有直接或间接继承自 base_type 的叶子类型
    /// </summary>
    /// <param name="base_type">基类类型（支持泛型基类如 SaveJsonConverterBase<>）</param>
    /// <returns>叶子类型列表</returns>
    private static List<Type> GetLeafTypes(Type base_type)
    {
        // 获取所有已加载的程序集
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var all_types = new HashSet<Type>();
        var leaf_types = new List<Type>();

        foreach (var assembly in assemblies)
        {
            // 跳过系统程序集提升性能
            if (assembly.FullName.StartsWith("System") ||
                assembly.FullName.StartsWith("Unity") ||
                assembly.FullName.StartsWith("mscorlib") ||
                assembly.FullName.StartsWith("netstandard"))
                continue;

            try
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (IsSubclassOfGenericBase(type, base_type))
                    {
                        all_types.Add(type);
                    }
                }
            }
            catch (ReflectionTypeLoadException)
            {
                // 忽略无法加载的类型
                continue;
            }
        }

        // 找出叶子类型（没有子类的类型）
        foreach (var type in all_types)
        {
            bool is_leaf = true;

            foreach (var other in all_types)
            {
                if (type != other && type.IsAssignableFrom(other))
                {
                    is_leaf = false;
                    break;
                }
            }

            if (is_leaf)
            {
                leaf_types.Add(type);
            }
        }

        return leaf_types;
    }

    /// <summary>
    /// 判断类型是否继承自泛型基类
    /// </summary>
    private static bool IsSubclassOfGenericBase(Type type, Type generic_base_type)
    {
        if (type == null || !type.IsClass || type.IsAbstract || type.IsGenericTypeDefinition)
            return false;

        Type current_type = type;
        while (current_type != null && current_type != typeof(object))
        {
            if (current_type.IsGenericType &&
                current_type.GetGenericTypeDefinition() == generic_base_type)
            {
                return true;
            }
            current_type = current_type.BaseType;
        }

        return false;
    }
}