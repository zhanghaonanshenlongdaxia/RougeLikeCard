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
    [CustomEditor(typeof(MonoBehaviour), true), CanEditMultipleObjects]
    public class UIBindInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            // 在每个 MonoBehaviour 的 Inspector 底部加一个"清空绑定"按钮
            if (GUILayout.Button("清空所有 SerializeField 绑定", GUILayout.Height(30)))
            {
                var type = target.GetType();
                var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(f => f.GetCustomAttributes(typeof(SerializeField), false).Length > 0);

                var so = new SerializedObject(target);
                bool dirty = false;
                foreach (var field in fields)
                {
                    if (field.FieldType.IsSubclassOf(typeof(UnityEngine.Object)) || field.FieldType == typeof(UnityEngine.Object))
                    {
                        var prop = so.FindProperty(field.Name);
                        if (prop != null && prop.objectReferenceValue != null)
                        {
                            prop.objectReferenceValue = null;
                            dirty = true;
                        }
                    }
                }
                if (dirty)
                {
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(target);
                    Debug.Log($"[Clear] {type.Name} - all bindings cleared");
                }
            }
        }
    }

    /// <summary>
    /// UI 组件智能绑定工具 v4
    /// - 支持覆盖已有绑定
    /// - 按类型兜底时排除已被其他字段绑定的对象
    /// - 模糊匹配后精确匹配
    /// </summary>
    public class UIBindTool : EditorWindow
    {
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

        static readonly string[] Suffixes = SuffixToType.Keys.ToArray();
        static readonly string[] PrefabSuffixes = { "Prefab", "Template", "ItemPrefab", "ElementPrefab" };

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

            // 收集所有已绑定的对象，避免按类型兜底时重复绑定
            var usedObjects = new HashSet<UnityEngine.Object>();

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
                    var ft = field.FieldType;
                    if (!typeof(UnityEngine.Object).IsAssignableFrom(ft)) continue;

                    // 覆盖模式：不跳过已有值
                    var found = TryMatch(selected.transform, field.Name, ft, usedObjects);
                    if (found != null)
                    {
                        var prop = so.FindProperty(field.Name);
                        if (prop != null)
                        {
                            if (ft == typeof(Transform))
                                prop.objectReferenceValue = found is Transform t ? t : (found as Component)?.transform;
                            else
                                prop.objectReferenceValue = found;
                            usedObjects.Add(found);
                            Debug.Log($"[Bind] ✓ {type.Name}.{field.Name} → {GetObjectName(found)} ({ft.Name})");
                            totalBound++;
                            dirty = true;
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[Bind] ✗ {type.Name}.{field.Name} ({ft.Name}) - 未匹配");
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

        static UnityEngine.Object TryMatch(Transform root, string fieldName, Type fieldType, HashSet<UnityEngine.Object> usedObjects)
        {
            var candidates = GenerateCandidates(fieldName);

            // 第一轮：精准匹配
            foreach (var name in candidates)
            {
                var child = FindChildRecursive(root, name);
                if (child != null)
                {
                    var result = ExtractComponent(child, fieldType);
                    if (result != null && !usedObjects.Contains(result))
                        return result;
                }
            }

            // 第二轮：模糊匹配
            foreach (var name in candidates)
            {
                var child = FindChildFuzzy(root, name);
                if (child != null)
                {
                    var result = ExtractComponent(child, fieldType);
                    if (result != null && !usedObjects.Contains(result))
                    {
                        Debug.Log($"[Bind] Fuzzy: '{fieldName}' → '{child.name}'");
                        return result;
                    }
                }
            }

            // 第三轮：Prefab/Template 字段
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
                    var result = fieldType == typeof(GameObject) ? (UnityEngine.Object)firstChild.gameObject : (UnityEngine.Object)firstChild;
                    if (!usedObjects.Contains(result))
                    {
                        Debug.Log($"[Bind] Prefab '{fieldName}' → '{firstChild.name}' (first child of '{container.name}')");
                        return result;
                    }
                }
            }

            // 第四轮：按类型兜底（排除已绑定的对象）
            if (fieldType == typeof(GameObject) || fieldType == typeof(Transform)) return null;
            if (!typeof(Component).IsAssignableFrom(fieldType)) return null;

            var allChildren = root.GetComponentsInChildren<Transform>(true);
            foreach (var child in allChildren)
            {
                if (child == root) continue;
                var comp = child.GetComponent(fieldType);
                if (comp != null)
                {
                    if (fieldType == typeof(TextMeshProUGUI) && child.name == "Text")
                        continue;
                    if (!usedObjects.Contains(comp))
                        return comp;
                }
            }
            return null;
        }

        static List<string> GenerateCandidates(string fieldName)
        {
            var list = new List<string> { fieldName };
            var pascal = ToPascalCase(fieldName);
            list.Add(pascal);

            foreach (var suffix in Suffixes)
            {
                if (fieldName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    var stripped = fieldName.Substring(0, fieldName.Length - suffix.Length);
                    if (stripped.Length > 0)
                    {
                        list.Add(stripped);
                        list.Add(ToPascalCase(stripped));
                    }
                }
                if (pascal.EndsWith(suffix))
                {
                    list.Add(pascal.Substring(0, pascal.Length - suffix.Length));
                }
            }

            return list.Distinct().ToList();
        }

        static UnityEngine.Object ExtractComponent(Transform child, Type fieldType)
        {
            if (fieldType == typeof(Transform)) return child;
            if (fieldType == typeof(GameObject)) return child.gameObject;
            if (!typeof(Component).IsAssignableFrom(fieldType)) return null;
            var comp = child.GetComponent(fieldType);
            if (comp != null) return comp;
            return child.GetComponentInChildren(fieldType, true);
        }

        static string ToPascalCase(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            if (s.Length == 1) return s.ToUpper();
            return char.ToUpper(s[0]) + s.Substring(1);
        }

        static bool IsPrefabField(string fieldName, Type fieldType)
        {
            if (fieldType != typeof(GameObject) && fieldType != typeof(Transform)) return false;
            foreach (var suffix in PrefabSuffixes)
                if (fieldName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        static string StripPrefabSuffix(string fieldName)
        {
            foreach (var suffix in PrefabSuffixes)
                if (fieldName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    return fieldName.Substring(0, fieldName.Length - suffix.Length);
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

        static Transform FindChildFuzzy(Transform parent, string candidate)
        {
            if (string.IsNullOrEmpty(candidate)) return null;

            var allChildren = parent.GetComponentsInChildren<Transform>(true);
            foreach (var child in allChildren)
            {
                if (child == parent) continue;
                var childName = child.name;

                if (childName.IndexOf(candidate, StringComparison.OrdinalIgnoreCase) >= 0)
                    return child;

                if (childName.Length >= 3 && candidate.IndexOf(childName, StringComparison.OrdinalIgnoreCase) >= 0)
                    return child;
            }
            return null;
        }

        #endregion
    }
}
