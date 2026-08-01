using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CardGame.Editor
{
    /// <summary>
    /// UI 组件智能绑定工具
    /// 匹配规则（按优先级）：
    /// 1. 精确匹配字段名
    /// 2. 去掉常见后缀后匹配 (Text/Root/Prefab/Button/Field/Image/Obj)
    /// 3. 驼峰转帕斯卡后匹配 (descriptionText → DescriptionText → Description)
    /// 4. 按类型匹配（找第一个有该组件的子物体）
    /// 5. prefab 字段：找容器下的第一个子物体作为模板
    /// </summary>
    public class UIBindTool : EditorWindow
    {
        // 常见后缀，匹配时自动去除
        static readonly string[] Suffixes = { "Text", "Root", "Prefab", "Button", "Field", "Image", "Obj", "Slot", "Panel", "Area", "Go" };

        [MenuItem("Tools/Auto Bind UI Components")]
        static void AutoBind()
        {
            var selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("Auto Bind", "请先选中一个 UI Canvas 对象", "OK");
                return;
            }

            var components = selected.GetComponents<MonoBehaviour>();
            int totalBound = 0;
            int totalSkipped = 0;

            foreach (var comp in components)
            {
                if (comp == null) continue;
                var type = comp.GetType();
                if (type == typeof(Canvas) || type == typeof(GraphicRaycaster) || type == typeof(CanvasScaler))
                    continue;

                var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(f => f.GetCustomAttributes(typeof(SerializeField), false).Length > 0);

                var so = new SerializedObject(comp);
                bool dirty = false;

                foreach (var field in fields)
                {
                    var fieldName = field.Name;
                    var fieldType = field.FieldType;

                    // 已有值跳过
                    var currentValue = field.GetValue(comp);
                    if (currentValue is UnityEngine.Object obj && obj != null)
                        continue;
                    if (currentValue is string s && !string.IsNullOrEmpty(s))
                        continue;

                    // 尝试多种方式匹配
                    var found = TryMatch(selected.transform, fieldName, fieldType);
                    if (found != null)
                    {
                        var prop = so.FindProperty(fieldName);
                        if (prop != null)
                        {
                            // 如果是 Transform 字段但找到的是 GameObject，取 transform
                            if (fieldType == typeof(Transform))
                                prop.objectReferenceValue = found is Transform t ? t : (found as Component)?.transform;
                            else
                                prop.objectReferenceValue = found;
                            Debug.Log($"[Bind] {type.Name}.{fieldName} -> {(found as Component)?.gameObject?.name ?? (found as UnityEngine.Object)?.name} ({fieldType.Name})");
                            totalBound++;
                            dirty = true;
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[Bind] 未匹配: {type.Name}.{fieldName} ({fieldType.Name})");
                        totalSkipped++;
                    }
                }

                if (dirty)
                {
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(comp);
                }
            }

            if (totalBound > 0)
            {
                var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(selected);
                if (!string.IsNullOrEmpty(prefabPath))
                {
                    PrefabUtility.SaveAsPrefabAsset(selected, prefabPath);
                    Debug.Log($"[Bind] Saved prefab: {prefabPath}, bound={totalBound}, skipped={totalSkipped}");
                }
                else
                {
                    EditorUtility.SetDirty(selected);
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(selected.scene);
                    Debug.Log($"[Bind] Scene dirty, bound={totalBound}, skipped={totalSkipped}");
                }
            }

            EditorUtility.DisplayDialog("Auto Bind",
                totalBound > 0
                    ? $"成功绑定 {totalBound} 个，未匹配 {totalSkipped} 个。\n详见 Console 日志。"
                    : "未找到需要绑定的组件。详见 Console 日志。",
                "OK");
        }

        [MenuItem("Tools/Auto Bind UI Components", true)]
        static bool ValidateAutoBind() => Selection.activeGameObject != null;

        /// <summary>
        /// 尝试用多种规则匹配
        /// </summary>
        static UnityEngine.Object TryMatch(Transform root, string fieldName, Type fieldType)
        {
            // 1. 生成候选名称列表
            var candidates = GenerateCandidates(fieldName);

            // 2. 按候选名称查找子物体
            foreach (var name in candidates)
            {
                var child = FindChildRecursive(root, name);
                if (child != null)
                {
                    var result = ExtractComponent(child, fieldName, fieldType);
                    if (result != null) return result;
                }
            }

            // 3. 按类型查找（找第一个有该类型组件的非自身子物体）
            var byType = FindByType(root, fieldType, fieldName);
            if (byType != null) return byType;

            return null;
        }

        /// <summary>
        /// 从字段名生成候选匹配名称列表
        /// </summary>
        static List<string> GenerateCandidates(string fieldName)
        {
            var list = new List<string> { fieldName };

            // 去后缀
            foreach (var suffix in Suffixes)
            {
                if (fieldName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    var stripped = fieldName.Substring(0, fieldName.Length - suffix.Length);
                    list.Add(stripped);
                    list.Add(stripped + suffix); // 也加回来以防大小写不同
                }
            }

            // 驼峰转帕斯卡 (descriptionText → DescriptionText)
            if (fieldName.Length > 0 && char.IsLower(fieldName[0]))
            {
                var pascal = char.ToUpper(fieldName[0]) + fieldName.Substring(1);
                list.Add(pascal);

                // 去后缀的帕斯卡版本
                foreach (var suffix in Suffixes)
                {
                    if (pascal.EndsWith(suffix))
                    {
                        var stripped = pascal.Substring(0, pascal.Length - suffix.Length);
                        list.Add(stripped);
                    }
                }
            }

            // 去重
            return list.Distinct().ToList();
        }

        /// <summary>
        /// 从找到的子物体中提取正确的组件
        /// </summary>
        static UnityEngine.Object ExtractComponent(Transform child, string fieldName, Type fieldType)
        {
            // Transform 类型 → 直接返回 transform
            if (fieldType == typeof(Transform))
                return child;

            // GameObject 类型 → 返回 gameObject
            if (fieldType == typeof(GameObject))
                return child.gameObject;

            // 其他组件类型 → GetComponent
            var comp = child.GetComponent(fieldType);
            if (comp != null) return comp;

            // 尝试在子物体中找（比如 Button 可能在子物体上）
            comp = child.GetComponentInChildren(fieldType, true);
            return comp;
        }

        /// <summary>
        /// 按类型查找子物体
        /// </summary>
        static UnityEngine.Object FindByType(Transform root, Type fieldType, string fieldName)
        {
            // 如果是 prefab/GameObject 字段，且字段名包含 "Prefab" 或 "Template"
            // 尝试在父容器下找第一个子物体
            if (fieldType == typeof(GameObject) || fieldType == typeof(Transform))
            {
                // 找到可能的容器（去掉 Prefab/Template 后缀后的名字）
                var containerName = fieldName;
                foreach (var suffix in Suffixes)
                {
                    if (containerName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    {
                        containerName = containerName.Substring(0, containerName.Length - suffix.Length);
                        break;
                    }
                }

                // 找容器
                var container = FindChildRecursive(root, containerName);
                if (container != null && container.childCount > 0)
                {
                    var firstChild = container.GetChild(0);
                    Debug.Log($"[Bind] Prefab field '{fieldName}' → first child '{firstChild.name}' of '{container.name}'");
                    return fieldType == typeof(GameObject) ? (UnityEngine.Object)firstChild.gameObject : (UnityEngine.Object)firstChild;
                }
            }

            // 按类型扫描所有子物体
            var allChildren = root.GetComponentsInChildren<Transform>(true);
            foreach (var child in allChildren)
            {
                if (child == root) continue;

                // 跳过 Canvas/CanvasScaler/GraphicRaycaster 自身的组件
                var comp = child.GetComponent(fieldType);
                if (comp != null)
                {
                    // 不要匹配到按钮的文字标签上
                    if (fieldType == typeof(TextMeshProUGUI) && child.name == "Text")
                        continue;
                    return comp;
                }
            }

            return null;
        }

        static Transform FindChildRecursive(Transform parent, string name)
        {
            // 精确匹配
            var exact = parent.Find(name);
            if (exact != null) return exact;

            // 忽略大小写递归匹配
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return child;

                var found = FindChildRecursive(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
