using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CardGame.Editor
{
    /// <summary>
    /// UI 组件一键绑定工具
    /// 用法：选中带有 UI Controller 的 GameObject，点击菜单 "Tools/Auto Bind UI Components"
    /// 它会自动查找所有 [SerializeField] 字段，按字段名匹配子物体并赋值
    /// </summary>
    public class UIBindTool : EditorWindow
    {
        [MenuItem("Tools/Auto Bind UI Components")]
        static void AutoBind()
        {
            var selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("Auto Bind", "请先选中一个 UI Canvas 对象", "OK");
                return;
            }

            // 获取所有 MonoBehaviour 组件
            var components = selected.GetComponents<MonoBehaviour>();
            int totalBound = 0;

            foreach (var comp in components)
            {
                if (comp == null) continue;
                var type = comp.GetType();

                // 获取所有 [SerializeField] 私有字段
                var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(f => f.GetCustomAttributes(typeof(SerializeField), false).Length > 0);

                var so = new SerializedObject(comp);
                bool dirty = false;

                foreach (var field in fields)
                {
                    var fieldName = field.Name;
                    var fieldType = field.FieldType;

                    // 如果已经有值，跳过
                    var currentValue = field.GetValue(comp);
                    if (currentValue != null && !currentValue.Equals(null))
                    {
                        // 但如果是 string 类型且为空，还是尝试
                        if (fieldType == typeof(string) && !string.IsNullOrEmpty((string)currentValue))
                            continue;
                        if (fieldType != typeof(string))
                            continue;
                    }

                    // 按字段名查找子物体
                    var child = FindChildRecursive(selected.transform, fieldName);
                    if (child != null)
                    {
                        var component = child.GetComponent(fieldType);
                        if (component != null)
                        {
                            var prop = so.FindProperty(fieldName);
                            if (prop != null)
                            {
                                prop.objectReferenceValue = component;
                                Debug.Log($"[Bind] {type.Name}.{fieldName} -> {child.name} ({fieldType.Name})");
                                totalBound++;
                                dirty = true;
                            }
                        }
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
                // 保存 Prefab
                var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(selected);
                if (!string.IsNullOrEmpty(prefabPath))
                {
                    PrefabUtility.SaveAsPrefabAsset(selected, prefabPath);
                    Debug.Log($"[Bind] Saved prefab: {prefabPath}, bound {totalBound} fields");
                }
                else
                {
                    EditorUtility.SetDirty(selected);
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(selected.scene);
                    Debug.Log($"[Bind] Marked scene dirty, bound {totalBound} fields");
                }

                EditorUtility.DisplayDialog("Auto Bind", $"成功绑定 {totalBound} 个组件！", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Auto Bind", "未找到需要绑定的组件。\n请确保字段名与子物体名一致。", "OK");
            }
        }

        [MenuItem("Tools/Auto Bind UI Components", true)]
        static bool ValidateAutoBind()
        {
            return Selection.activeGameObject != null;
        }

        static Transform FindChildRecursive(Transform parent, string name)
        {
            // 先精确匹配
            var exact = parent.Find(name);
            if (exact != null) return exact;

            // 模糊匹配（忽略大小写）
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return child;

                // 递归查找
                var found = FindChildRecursive(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
