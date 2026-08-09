using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NueGames.NueDeck.Scripts.Data.Containers;
using NueGames.NueDeck.Scripts.Enums;
using NueGames.NueDeck.Scripts.NueExtentions;
using UnityEditor;
using UnityEngine;

namespace NueGames.NueDeck.Editor
{
    public class BuffEditorWindow : ExtendedEditorWindow
    {
#if UNITY_EDITOR

        private static BuffEditorWindow CurrentWindow { get; set; }

        private const string BuffDataDefaultPath = "Assets/NueGames/NueDeck/Data/Buffs/";

        #region Cache
        private List<BuffData> AllBuffDataList { get; set; }
        private BuffData SelectedBuffData { get; set; }

        // Cached field values
        private StatusType BuffStatusType { get; set; }
        private string DisplayName { get; set; }
        private string Description { get; set; }
        private Sprite Icon { get; set; }
        private bool DecreaseOverTurn { get; set; }
        private bool IsPermanent { get; set; }
        private bool CanNegativeStack { get; set; }
        private bool ClearAtNextTurn { get; set; }
        private float DamageTakenMult { get; set; } = 1f;
        private float DamageDealtMult { get; set; } = 1f;
        private float BlockMult { get; set; } = 1f;
        private BuffSpecialEffect SpecialEffect { get; set; }

        private void CacheBuffData()
        {
            BuffStatusType = SelectedBuffData.StatusType;
            DisplayName = SelectedBuffData.DisplayName;
            Description = SelectedBuffData.Description;
            Icon = SelectedBuffData.Icon;
            DecreaseOverTurn = SelectedBuffData.DecreaseOverTurn;
            IsPermanent = SelectedBuffData.IsPermanent;
            CanNegativeStack = SelectedBuffData.CanNegativeStack;
            ClearAtNextTurn = SelectedBuffData.ClearAtNextTurn;
            DamageTakenMult = SelectedBuffData.DamageTakenMult;
            DamageDealtMult = SelectedBuffData.DamageDealtMult;
            BlockMult = SelectedBuffData.BlockMult;
            SpecialEffect = SelectedBuffData.SpecialEffect;
        }

        private void ClearCachedBuffData()
        {
            BuffStatusType = StatusType.None;
            DisplayName = "";
            Description = "";
            Icon = null;
            DecreaseOverTurn = false;
            IsPermanent = false;
            CanNegativeStack = false;
            ClearAtNextTurn = false;
            DamageTakenMult = 1f;
            DamageDealtMult = 1f;
            BlockMult = 1f;
            SpecialEffect = BuffSpecialEffect.None;
        }
        #endregion

        #region Setup
        [MenuItem("Tools/NueDeck/Buff Editor")]
        public static void OpenBuffEditor() => CurrentWindow = GetWindow<BuffEditorWindow>("Buff Editor");

        private void OnEnable()
        {
            AllBuffDataList?.Clear();
            AllBuffDataList = ListExtentions.GetAllInstances<BuffData>().ToList();
            Selection.selectionChanged += Repaint;
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= Repaint;
            SelectedBuffData = null;
        }
        #endregion

        #region Layout
        void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            DrawAllBuffButtons();
            EditorGUILayout.Space();
            DrawSelectedBuff();
            EditorGUILayout.EndHorizontal();
        }

        private Vector2 _allBuffScrollPos;
        private void DrawAllBuffButtons()
        {
            _allBuffScrollPos = EditorGUILayout.BeginScrollView(_allBuffScrollPos, GUILayout.Width(180), GUILayout.ExpandHeight(true));
            EditorGUILayout.BeginVertical("box", GUILayout.Width(180), GUILayout.ExpandHeight(true));

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Buffs", EditorStyles.boldLabel, GUILayout.Width(50), GUILayout.Height(20));
            GUILayout.FlexibleSpace();
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.blue;
            if (GUILayout.Button("Refresh", GUILayout.Width(75), GUILayout.Height(20)))
                RefreshBuffData();
            GUI.backgroundColor = oldColor;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Separator();

            foreach (var data in AllBuffDataList)
            {
                var label = string.IsNullOrEmpty(data.DisplayName) ? data.StatusType.ToString() : data.DisplayName;
                if (GUILayout.Button(label, GUILayout.MaxWidth(180)))
                {
                    SelectedBuffData = data;
                    CacheBuffData();
                    GUI.FocusControl(null);
                }
            }

            if (GUILayout.Button("+", GUILayout.MaxWidth(180)))
                CreateNewBuff();

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        private void CreateNewBuff()
        {
            var clone = CreateInstance<BuffData>();
            var str = new StringBuilder();
            str.Append("new_buff");
            var path = str.Insert(0, BuffDataDefaultPath).Append(".asset").ToString();
            var uniquePath = AssetDatabase.GenerateUniqueAssetPath(path);
            AssetDatabase.CreateAsset(clone, uniquePath);
            AssetDatabase.SaveAssets();
            RefreshBuffData();
            SelectedBuffData = AllBuffDataList.Find(x => x != null && AssetDatabase.GetAssetPath(x) == uniquePath);
            CacheBuffData();
        }
        #endregion

        #region Selected Buff Editor
        private Vector2 _detailScrollPos;
        private void DrawSelectedBuff()
        {
            EditorGUILayout.BeginVertical("box", GUILayout.ExpandHeight(true));
            if (!SelectedBuffData)
            {
                EditorGUILayout.LabelField("Select a buff or create a new one");
                EditorGUILayout.EndVertical();
                return;
            }

            GUILayout.Space(10);
            _detailScrollPos = EditorGUILayout.BeginScrollView(_detailScrollPos, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            // Identity
            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            BuffStatusType = (StatusType)EditorGUILayout.EnumPopup("Status Type:", BuffStatusType);
            DisplayName = EditorGUILayout.TextField("Display Name:", DisplayName);
            Description = EditorGUILayout.TextArea(Description, GUILayout.MinHeight(60));
            EditorGUILayout.BeginHorizontal();
            Icon = (Sprite)EditorGUILayout.ObjectField("Icon:", Icon, typeof(Sprite), false);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Separator();

            // Behavior
            EditorGUILayout.LabelField("Behavior", EditorStyles.boldLabel);
            DecreaseOverTurn = EditorGUILayout.ToggleLeft("Decrease Over Turn (回合递减)", DecreaseOverTurn);
            IsPermanent = EditorGUILayout.ToggleLeft("Is Permanent (永久不可清除)", IsPermanent);
            CanNegativeStack = EditorGUILayout.ToggleLeft("Can Negative Stack (允许负值)", CanNegativeStack);
            ClearAtNextTurn = EditorGUILayout.ToggleLeft("Clear At Next Turn (下回合清除)", ClearAtNextTurn);

            EditorGUILayout.Separator();

            // Multipliers
            EditorGUILayout.LabelField("Multipliers (1 = no effect)", EditorStyles.boldLabel);
            DamageTakenMult = EditorGUILayout.FloatField("Damage Taken Mult (受伤倍率)", DamageTakenMult);
            DamageDealtMult = EditorGUILayout.FloatField("Damage Dealt Mult (攻击倍率)", DamageDealtMult);
            BlockMult = EditorGUILayout.FloatField("Block Mult (格挡倍率)", BlockMult);

            EditorGUILayout.Separator();

            // Special Effect
            EditorGUILayout.LabelField("Special Effect", EditorStyles.boldLabel);
            SpecialEffect = (BuffSpecialEffect)EditorGUILayout.EnumPopup("Special Effect:", SpecialEffect);
            EditorGUILayout.HelpBox(
                SpecialEffect == BuffSpecialEffect.None
                    ? "纯数值修饰，完全数据驱动，无需写代码。"
                    : SpecialEffect == BuffSpecialEffect.Poison
                        ? "回合结束时按层数扣血（无视格挡）。"
                        : SpecialEffect == BuffSpecialEffect.Stun
                            ? "拥有此buff时无法行动。"
                            : "受击时反弹伤害给攻击者。",
                MessageType.Info);

            EditorGUILayout.Separator();

            // Preview
            EditorGUILayout.BeginHorizontal("box");
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"名称: {DisplayName}");
            EditorGUILayout.LabelField($"说明: {Description}");
            EditorGUILayout.LabelField($"类型: {BuffStatusType} | 特殊: {SpecialEffect}");
            EditorGUILayout.LabelField($"倍率: 受伤×{DamageTakenMult} 攻击×{DamageDealtMult} 格挡×{BlockMult}");
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();

            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Save", GUILayout.Width(100), GUILayout.Height(30)))
                SaveBuffData();
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
        #endregion

        #region Save / Refresh
        private void SaveBuffData()
        {
            if (!SelectedBuffData) return;
            SelectedBuffData.EditStatusType(BuffStatusType);
            SelectedBuffData.EditDisplayName(DisplayName);
            SelectedBuffData.EditDescription(Description);
            SelectedBuffData.EditIcon(Icon);
            SelectedBuffData.EditDecreaseOverTurn(DecreaseOverTurn);
            SelectedBuffData.EditIsPermanent(IsPermanent);
            SelectedBuffData.EditCanNegativeStack(CanNegativeStack);
            SelectedBuffData.EditClearAtNextTurn(ClearAtNextTurn);
            SelectedBuffData.EditDamageTakenMult(DamageTakenMult);
            SelectedBuffData.EditDamageDealtMult(DamageDealtMult);
            SelectedBuffData.EditBlockMult(BlockMult);
            SelectedBuffData.EditSpecialEffect(SpecialEffect);
            EditorUtility.SetDirty(SelectedBuffData);
            AssetDatabase.SaveAssets();
        }

        private void RefreshBuffData()
        {
            SelectedBuffData = null;
            ClearCachedBuffData();
            AllBuffDataList?.Clear();
            AllBuffDataList = ListExtentions.GetAllInstances<BuffData>().ToList();
        }
        #endregion

#endif
    }
}
