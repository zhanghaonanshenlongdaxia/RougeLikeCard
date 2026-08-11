using System;
using System.Collections.Generic;
using System.Linq;
using CardGame;
using NueGames.NueDeck.Scripts.Data.Cultivation;
using NueGames.NueDeck.Scripts.Enums;
using UnityEditor;
using UnityEngine;

namespace NueGames.NueDeck.Editor
{
    public class CultivationEditorWindow : ExtendedEditorWindow
    {
#if UNITY_EDITOR

        private static CultivationEditorWindow CurrentWindow { get; set; }
        private const string MethodPath = "Assets/NueGames/NueDeck/Data/Cultivation/Methods/";
        private const string AbilityPath = "Assets/NueGames/NueDeck/Data/Cultivation/Abilities/";

        private List<CultivationMethodData> _allMethods;
        private List<DivineAbilityData> _allAbilities;
        private CultivationMethodData _selectedMethod;
        private DivineAbilityData _selectedAbility;
        private int _tab;

        [MenuItem("Tools/NueDeck/Cultivation Editor")]
        public static void Open() => CurrentWindow = GetWindow<CultivationEditorWindow>("Cultivation Editor");

        private void OnEnable()
        {
            RefreshAll();
        }

        private void RefreshAll()
        {
            _allMethods = FindAllAssets<CultivationMethodData>();
            _allAbilities = FindAllAssets<DivineAbilityData>();
            _selectedMethod = null;
            _selectedAbility = null;
        }

        void OnGUI()
        {
            _tab = GUILayout.Toolbar(_tab, new[] { "功法", "神通" });
            EditorGUILayout.Space(5);

            if (_tab == 0) DrawMethodEditor();
            else DrawAbilityEditor();
        }

        #region 功法编辑
        private Vector2 _methodListScroll;
        private Vector2 _nodeScroll;

        private void DrawMethodEditor()
        {
            EditorGUILayout.BeginHorizontal();
            // Left: method list
            _methodListScroll = EditorGUILayout.BeginScrollView(_methodListScroll, GUILayout.Width(200), GUILayout.ExpandHeight(true));
            EditorGUILayout.BeginVertical("box", GUILayout.Width(200), GUILayout.ExpandHeight(true));
            EditorGUILayout.LabelField("功法列表", EditorStyles.boldLabel);
            if (GUILayout.Button("Refresh", GUILayout.Width(180))) RefreshAll();
            foreach (var m in _allMethods)
            {
                if (GUILayout.Button(m.MethodName, GUILayout.MaxWidth(200)))
                {
                    _selectedMethod = m;
                    GUI.FocusControl(null);
                }
            }
            if (GUILayout.Button("+ 新建功法", GUILayout.MaxWidth(200)))
            {
                var path = MethodPath + "new_method.asset";
                if (!System.IO.Directory.Exists(MethodPath)) System.IO.Directory.CreateDirectory(MethodPath);
                var uniquePath = AssetDatabase.GenerateUniqueAssetPath(path);
                var m = CreateInstance<CultivationMethodData>();
                AssetDatabase.CreateAsset(m, uniquePath);
                AssetDatabase.SaveAssets();
                RefreshAll();
                _selectedMethod = _allMethods.Find(x => AssetDatabase.GetAssetPath(x) == uniquePath);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();

            // Right: detail
            EditorGUILayout.BeginVertical("box", GUILayout.ExpandHeight(true));
            if (!_selectedMethod)
            {
                EditorGUILayout.LabelField("选择一本功法或新建");
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
                return;
            }

            // Method properties
            EditorGUILayout.LabelField("功法属性", EditorStyles.boldLabel);
            _selectedMethod.EditMethodId(EditorGUILayout.TextField("Method ID", _selectedMethod.MethodId));
            _selectedMethod.EditMethodName(EditorGUILayout.TextField("名称", _selectedMethod.MethodName));
            _selectedMethod.EditDescription(EditorGUILayout.TextArea(_selectedMethod.Description, GUILayout.MinHeight(40)));
            _selectedMethod.EditIcon((Sprite)EditorGUILayout.ObjectField("图标", _selectedMethod.Icon, typeof(Sprite), false));
            _selectedMethod.EditElement((ElementType)EditorGUILayout.EnumPopup("五行属性", _selectedMethod.Element));
            _selectedMethod.EditGrade((CultivationMethodGrade)EditorGUILayout.EnumPopup("品阶", _selectedMethod.Grade));
            _selectedMethod.EditMaxRealm((RealmLevel)EditorGUILayout.EnumPopup("最高境界", _selectedMethod.MaxRealm));

            EditorGUILayout.Space(10);

            // Nodes
            EditorGUILayout.LabelField("修炼节点", EditorStyles.boldLabel);
            if (_selectedMethod.Nodes == null)
                _selectedMethod.EditNodes(new List<CultivationNodeData>());

            _nodeScroll = EditorGUILayout.BeginScrollView(_nodeScroll, GUILayout.ExpandHeight(true));
            for (int i = 0; i < _selectedMethod.Nodes.Count; i++)
            {
                var node = _selectedMethod.Nodes[i];
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"[{i}] {node.NodeName}", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    _selectedMethod.Nodes.RemoveAt(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();

                // Use SerializedObject for node editing via reflection-friendly approach
                DrawNodeFields(node);
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(3);
            }

            if (GUILayout.Button("+ 添加节点", GUILayout.Width(200)))
                _selectedMethod.Nodes.Add(new CultivationNodeData());

            EditorGUILayout.EndScrollView();

            // Save
            EditorGUILayout.Space(5);
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Save", GUILayout.Width(100), GUILayout.Height(30)))
            {
                EditorUtility.SetDirty(_selectedMethod);
                AssetDatabase.SaveAssets();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawNodeFields(CultivationNodeData node)
        {
            node.EditNodeId(EditorGUILayout.TextField("Node ID", node.NodeId));
            node.EditNodeName(EditorGUILayout.TextField("名称", node.NodeName));
            node.EditDescription(EditorGUILayout.TextArea(node.Description, GUILayout.MinHeight(30)));
            node.EditRealm((RealmLevel)EditorGUILayout.EnumPopup("境界", node.Realm));
            node.EditUnlockType((NodeUnlockType)EditorGUILayout.EnumPopup("解锁方式", node.UnlockType));
            node.EditComprehensionCost(EditorGUILayout.IntField("参悟点消耗", node.ComprehensionCost));
            node.EditMutexGroup(EditorGUILayout.TextField("互斥组 (空=不互斥)", node.MutexGroup));

            // Prerequisites
            EditorGUILayout.LabelField("前置节点ID (逗号分隔):");
            var prereqStr = node.Prerequisites != null ? string.Join(",", node.Prerequisites) : "";
            var newPrereq = EditorGUILayout.TextField(prereqStr);
            var prereqList = string.IsNullOrEmpty(newPrereq) ? new List<string>() :
                new List<string>(newPrereq.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
            for (int j = 0; j < prereqList.Count; j++) prereqList[j] = prereqList[j].Trim();
            node.EditPrerequisites(prereqList);

            node.EditRewardType((NodeRewardType)EditorGUILayout.EnumPopup("奖励类型", node.RewardType));

            if (node.RewardType != NodeRewardType.None && node.RewardType != NodeRewardType.PassiveStat && node.RewardType != NodeRewardType.CraftBonus)
            {
                EditorGUILayout.LabelField("奖励ID (逗号分隔):");
                var rewardStr = node.RewardIds != null ? string.Join(",", node.RewardIds) : "";
                var newReward = EditorGUILayout.TextField(rewardStr);
                var rewardList = string.IsNullOrEmpty(newReward) ? new List<string>() :
                    new List<string>(newReward.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                for (int j = 0; j < rewardList.Count; j++) rewardList[j] = rewardList[j].Trim();
                node.EditRewardIds(rewardList);
            }

            if (node.RewardType == NodeRewardType.PassiveStat)
            {
                node.EditPassiveStat((PassiveStatType)EditorGUILayout.EnumPopup("属性类型", node.PassiveStat));
                node.EditPassiveValue(EditorGUILayout.IntField("数值", node.PassiveValue));
            }

            if (node.RewardType == NodeRewardType.CraftBonus)
            {
                node.EditCraftBonusType((CraftBonusType)EditorGUILayout.EnumPopup("加成类型", node.CraftBonusType));
                node.EditCraftBonusValue(EditorGUILayout.FloatField("数值", node.CraftBonusValue));
            }
        }
        #endregion

        #region 神通编辑
        private Vector2 _abilityScroll;

        private void DrawAbilityEditor()
        {
            EditorGUILayout.BeginHorizontal();
            _abilityScroll = EditorGUILayout.BeginScrollView(_abilityScroll, GUILayout.Width(200), GUILayout.ExpandHeight(true));
            EditorGUILayout.BeginVertical("box", GUILayout.Width(200), GUILayout.ExpandHeight(true));
            EditorGUILayout.LabelField("神通列表", EditorStyles.boldLabel);
            if (GUILayout.Button("Refresh", GUILayout.Width(180))) RefreshAll();
            foreach (var a in _allAbilities)
            {
                if (GUILayout.Button(a.AbilityName, GUILayout.MaxWidth(200)))
                {
                    _selectedAbility = a;
                    GUI.FocusControl(null);
                }
            }
            if (GUILayout.Button("+ 新建神通", GUILayout.MaxWidth(200)))
            {
                var path = AbilityPath + "new_ability.asset";
                if (!System.IO.Directory.Exists(AbilityPath)) System.IO.Directory.CreateDirectory(AbilityPath);
                var uniquePath = AssetDatabase.GenerateUniqueAssetPath(path);
                var a = CreateInstance<DivineAbilityData>();
                AssetDatabase.CreateAsset(a, uniquePath);
                AssetDatabase.SaveAssets();
                RefreshAll();
                _selectedAbility = _allAbilities.Find(x => AssetDatabase.GetAssetPath(x) == uniquePath);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginVertical("box", GUILayout.ExpandHeight(true));
            if (!_selectedAbility)
            {
                EditorGUILayout.LabelField("选择一个神通或新建");
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
                return;
            }

            _selectedAbility.EditAbilityId(EditorGUILayout.TextField("Ability ID", _selectedAbility.AbilityId));
            _selectedAbility.EditAbilityName(EditorGUILayout.TextField("名称", _selectedAbility.AbilityName));
            _selectedAbility.EditDescription(EditorGUILayout.TextArea(_selectedAbility.Description, GUILayout.MinHeight(40)));
            _selectedAbility.EditIcon((Sprite)EditorGUILayout.ObjectField("图标", _selectedAbility.Icon, typeof(Sprite), false));
            _selectedAbility.EditElement((ElementType)EditorGUILayout.EnumPopup("属性", _selectedAbility.Element));
            _selectedAbility.EditCardId(EditorGUILayout.TextField("卡牌ID", _selectedAbility.CardId));
            _selectedAbility.EditEnergyCost(EditorGUILayout.IntField("能量消耗", _selectedAbility.EnergyCost));

            EditorGUILayout.Space(5);
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Save", GUILayout.Width(100), GUILayout.Height(30)))
            {
                EditorUtility.SetDirty(_selectedAbility);
                AssetDatabase.SaveAssets();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }
        #endregion

        private static List<T> FindAllAssets<T>() where T : UnityEngine.Object
        {
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            var list = new List<T>();
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null) list.Add(asset);
            }
            return list;
        }

#endif
    }
}
