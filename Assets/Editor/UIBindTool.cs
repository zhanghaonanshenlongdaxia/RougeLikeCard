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
    /// 
    /// 命名约定常量：
    /// - 子物体名应使用这些常量后缀来标识类型
    /// - 自动绑定按字段名→候选名→子物体名匹配
    /// 
    /// 字段名 → 子物体名 匹配规则（按优先级）：
    /// 1. 精确匹配
    /// 2. 去后缀 (descriptionText → description → Description)
    /// 3. 驼峰转帕斯卡 (backButton → BackButton → Back)
    /// 4. 按类型查找
    /// 5. Prefab/Template 字段 → 找容器名 → 取第一个子物体
    /// </summary>
    public class UIBindTool : EditorWindow
    {
        #region 命名约定常量

        /// <summary>字段名后缀 → 表示该字段是哪类组件</summary>
        static readonly Dictionary<string, Type> SuffixToType = new Dictionary<string, Type>
        {
            { "Button", typeof(Button) },
            { "Text", typeof(TextMeshProUGUI) },
            { "Image", typeof(Image) },
            { "Root", typeof(Transform) },
            { "Slot", typeof(Transform) },
            { "Panel", typeof(GameObject) },
            { "Area", typeof(Transform) },
            { "Field", typeof(TextMeshProUGUI) },
        };

        /// <summary>常见后缀列表（用于去后缀匹配）</summary>
        static readonly string[] Suffixes = SuffixToType.Keys.ToArray();

        /// <summary>Prefab/Template 类字段后缀</summary>
        static readonly string[] PrefabSuffixes = { "Prefab", "Template", "ItemPrefab", "ElementPrefab" };

        #endregion

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

                    var found = TryMatch(selected.transform, fieldName, fieldType);
                    if (found != null)
                    {
                        var prop = so.FindProperty(fieldName);
                        if (prop != null)
                        {
                            if (fieldType == typeof(Transform))
                                prop.objectReferenceValue = found is Transform t ? t : (found as Component)?.transform;
                            else
                                prop.objectReferenceValue = found;
                            Debug.Log($"[Bind] ✓ {type.Name}.{fieldName} → {GetObjectName(found)} ({fieldType.Name})");
                            totalBound++;
                            dirty = true;
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[Bind] ✗ {type.Name}.{fieldName} ({fieldType.Name}) - 未匹配");
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
                    Debug.Log($"[Bind] Prefab saved: {prefabPath} | bound={totalBound} skipped={totalSkipped}");
                }
                else
                {
                    EditorUtility.SetDirty(selected);
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(selected.scene);
                    Debug.Log($"[Bind] Scene dirty | bound={totalBound} skipped={totalSkipped}");
                }
            }

            EditorUtility.DisplayDialog("Auto Bind",
                $"绑定 {totalBound} 个，未匹配 {totalSkipped} 个。\n详见 Console 日志。",
                "OK");
        }

        [MenuItem("Tools/Auto Bind UI Components", true)]
        static bool ValidateAutoBind() => Selection.activeGameObject != null;

        #region 匹配逻辑

        static UnityEngine.Object TryMatch(Transform root, string fieldName, Type fieldType)
        {
            var candidates = GenerateCandidates(fieldName);

            // 第一轮：精准匹配（候选名 → 子物体名 完全匹配，忽略大小写）
            foreach (var name in candidates)
            {
                var child = FindChildRecursive(root, name);
                if (child != null)
                {
                    var result = ExtractComponent(child, fieldType);
                    if (result != null) return result;
                }
            }

            // 第二轮：模糊匹配（子物体名包含候选名，或候选名包含子物体名）
            foreach (var name in candidates)
            {
                var child = FindChildFuzzy(root, name);
                if (child != null)
                {
                    var result = ExtractComponent(child, fieldType);
                    if (result != null)
                    {
                        Debug.Log($"[Bind] Fuzzy match: '{fieldName}' → '{child.name}' (contains '{name}')");
                        return result;
                    }
                }
            }

            // 第三轮：Prefab/Template 字段特殊处理
            if (IsPrefabField(fieldName, fieldType))
            {
                var containerName = StripPrefabSuffix(fieldName);
                var container = FindChildRecursive(root, containerName);
                if (container == null)
                    container = FindChildRecursive(root, ToPascalCase(containerName));
                if (container == null)
                    container = FindChildFuzzy(root, containerName);
                if (container != null && container.childCount > 0)
                {
                    var firstChild = container.GetChild(0);
                    Debug.Log($"[Bind] Prefab '{fieldName}' → '{firstChild.name}' (first child of '{container.name}')");
                    return fieldType == typeof(GameObject) ? (UnityEngine.Object)firstChild.gameObject : (UnityEngine.Object)firstChild;
                }
            }

            // 第四轮：按类型兜底
            return FindByType(root, fieldType, fieldName);
        }

        /// <summary>
        /// 生成候选匹配名称（按优先级排序）
        /// </summary>
        static List<string> GenerateCandidates(string fieldName)
        {
            var list = new List<string>();
            
            // 1. 原名
            list.Add(fieldName);

            // 2. 驼峰转帕斯卡
            var pascal = ToPascalCase(fieldName);
            list.Add(pascal);

            // 3. 去后缀（每个后缀都试）
            foreach (var suffix in Suffixes)
            {
                // 原名去后缀
                if (fieldName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    var stripped = fieldName.Substring(0, fieldName.Length - suffix.Length);
                    list.Add(stripped);
                    list.Add(ToPascalCase(stripped));
                }
                // 帕斯卡去后缀
                if (pascal.EndsWith(suffix))
                {
                    var stripped = pascal.Substring(0, pascal.Length - suffix.Length);
                    list.Add(stripped);
                }
            }

            return list.Distinct().ToList();
        }

        static UnityEngine.Object ExtractComponent(Transform child, Type fieldType)
        {
            if (fieldType == typeof(Transform)) return child;
            if (fieldType == typeof(GameObject)) return child.gameObject;
            
            var comp = child.GetComponent(fieldType);
            if (comp != null) return comp;
            
            // 在子物体中找
            return child.GetComponentInChildren(fieldType, true);
        }

        static UnityEngine.Object FindByType(Transform root, Type fieldType, string fieldName)
        {
            // 跳过非组件类型
            if (fieldType == typeof(string)) return null;

            var allChildren = root.GetComponentsInChildren<Transform>(true);
            foreach (var child in allChildren)
            {
                if (child == root) continue;

                var comp = child.GetComponent(fieldType);
                if (comp != null)
                {
                    // 跳过按钮下的文字标签
                    if (fieldType == typeof(TextMeshProUGUI) && child.name == "Text")
                        continue;
                    return comp;
                }
            }
            return null;
        }

        #endregion

        #region 工具方法

        static string ToPascalCase(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            if (s.Length == 1) return s.ToUpper();
            return char.ToUpper(s[0]) + s.Substring(1);
        }

        static bool IsPrefabField(string fieldName, Type fieldType)
        {
            if (fieldType != typeof(GameObject) && fieldType != typeof(Transform))
                return false;
            foreach (var suffix in PrefabSuffixes)
            {
                if (fieldName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        static string StripPrefabSuffix(string fieldName)
        {
            foreach (var suffix in PrefabSuffixes)
            {
                if (fieldName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    return fieldName.Substring(0, fieldName.Length - suffix.Length);
            }
            return fieldName;
        }

        static string GetObjectName(UnityEngine.Object obj)
        {
            if (obj is Component c) return c.gameObject.name;
            if (obj is GameObject g) return g.name;
            return obj?.name ?? "null";
        }

        static Transform FindChildRecursive(Transform parent, string name)
        {
            var exact = parent.Find(name);
            if (exact != null) return exact;

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

        /// <summary>
        /// 模糊匹配：子物体名包含候选名，或候选名包含子物体名（忽略大小写）
        /// 例：候选 "Description" 能匹配子物体 "EventDescription" 或 "Desc"
        /// </summary>
        static Transform FindChildFuzzy(Transform parent, string candidate)
        {
            if (string.IsNullOrEmpty(candidate)) return null;

            var allChildren = parent.GetComponentsInChildren<Transform>(true);
            foreach (var child in allChildren)
            {
                if (child == parent) continue;
                var childName = child.name;

                // 子物体名包含候选名
                if (childName.IndexOf(candidate, StringComparison.OrdinalIgnoreCase) >= 0)
                    return child;

                // 候选名包含子物体名（子物体名至少3个字符才有效）
                if (childName.Length >= 3 && candidate.IndexOf(childName, StringComparison.OrdinalIgnoreCase) >= 0)
                    return child;
            }
            return null;
        }

        #endregion
    }
}
