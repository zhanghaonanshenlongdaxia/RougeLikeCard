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
            var type = target.GetType();
            var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => f.GetCustomAttributes(typeof(SerializeField), false).Length > 0)
                .Where(f => typeof(UnityEngine.Object).IsAssignableFrom(f.FieldType))
                .ToList();

            if (fields.Count == 0)
            {
                DrawDefaultInspector();
                return;
            }

            // 为每个 Object 类型字段绘制带叉号的一行
            var so = new SerializedObject(target);
            bool dirty = false;

            foreach (var field in fields)
            {
                var prop = so.FindProperty(field.Name);
                if (prop == null || prop.propertyType != SerializedPropertyType.ObjectReference)
                {
                    // 非对象引用类型，用默认绘制
                    EditorGUILayout.PropertyField(prop, true);
                    continue;
                }

                // 一行：Label + ObjectField + X按钮
                var rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
                var labelWidth = 120;
                var buttonWidth = 22;
                var fieldWidth = rect.width - labelWidth - buttonWidth - 4;

                // Label
                var labelRect = new Rect(rect.x, rect.y, labelWidth, rect.height);
                EditorGUI.LabelField(labelRect, ObjectNames.NicifyVariableName(field.Name));

                // Object Field
                var fieldRect = new Rect(rect.x + labelWidth + 2, rect.y, fieldWidth, rect.height);
                var newValue = EditorGUI.ObjectField(fieldRect, prop.objectReferenceValue, field.FieldType, true);

                if (newValue != prop.objectReferenceValue)
                {
                    prop.objectReferenceValue = newValue;
                    dirty = true;
                }

                // X 按钮
                var buttonRect = new Rect(rect.x + labelWidth + fieldWidth + 4, rect.y, buttonWidth, rect.height);
                var oldColor = GUI.color;
                if (prop.objectReferenceValue == null)
                    GUI.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);

                if (GUI.Button(buttonRect, "×"))
                {
                    prop.objectReferenceValue = null;
                    dirty = true;
                }
                GUI.color = oldColor;
            }

            // 绘制非 Object 类型的 SerializeField（int/string/bool等）
            var nonObjectFields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => f.GetCustomAttributes(typeof(SerializeField), false).Length > 0)
                .Where(f => !typeof(UnityEngine.Object).IsAssignableFrom(f.FieldType))
                .ToList();

            foreach (var field in nonObjectFields)
            {
                var prop = so.FindProperty(field.Name);
                if (prop != null)
                    EditorGUILayout.PropertyField(prop, true);
            }

            if (dirty)
            {
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            }
        }
    }
}
