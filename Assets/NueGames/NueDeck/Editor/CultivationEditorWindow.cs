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
    /// <summary>
    /// 功法编辑器 — 左侧功法列表，右侧 Inspector 式属性面板。
    /// 上部分显示功法基础属性，下部分按境界折叠列表显示节点，点击节点可展开编辑。
    /// 神通作为节点的奖励类型进行内联编辑。
    /// </summary>
    public class CultivationEditorWindow : ExtendedEditorWindow
    {
#if UNITY_EDITOR

        private static CultivationEditorWindow CurrentWindow { get; set; }
        private const string MethodPath = "Assets/NueGames/NueDeck/Data/Cultivation/Methods/";
        private const string AbilityPath = "Assets/NueGames/NueDeck/Data/Cultivation/Abilities/";

        private List<CultivationMethodData> _allMethods;
        private List<DivineAbilityData> _allAbilities;

        private int _tab;
        private readonly string[] _tabNames = { "功法编辑器", "神通库" };

        #region Method Editor State
        private CultivationMethodData _selectedMethod;
        private CultivationNodeData _selectedNode;
        private List<CultivationNodeData> _selectedGroupNodes; // 同 MutexGroup 的多阶段节点
        private readonly Dictionary<RealmLevel, bool> _realmFoldouts = new Dictionary<RealmLevel, bool>();
        private Vector2 _methodListScroll;
        private Vector2 _methodPropScroll;
        #endregion

        #region Ability Library State
        private DivineAbilityData _selectedLibraryAbility;
        private Vector2 _abilityLibListScroll;
        private Vector2 _abilityLibPropScroll;
        #endregion

        [MenuItem("Tools/NueDeck/Cultivation Editor")]
        public static void Open() => CurrentWindow = GetWindow<CultivationEditorWindow>("Cultivation Editor");

        private void OnEnable() => RefreshAll();

        private void RefreshAll()
        {
            _allMethods = FindAllAssets<CultivationMethodData>();
            _allAbilities = FindAllAssets<DivineAbilityData>();
            if (_selectedMethod != null && !_allMethods.Contains(_selectedMethod))
            {
                _selectedMethod = null;
                _selectedNode = null;
            }
            if (_selectedLibraryAbility != null && !_allAbilities.Contains(_selectedLibraryAbility))
                _selectedLibraryAbility = null;
        }

        void OnGUI()
        {
            if (_allMethods == null || _allAbilities == null) RefreshAll();

            _tab = GUILayout.Toolbar(_tab, _tabNames, GUILayout.Height(28));
            EditorGUILayout.Space(5);

            if (_tab == 0) DrawMethodEditor();
            else DrawAbilityLibrary();
        }

        #region 功法编辑器
        private void DrawMethodEditor()
        {
            EditorGUILayout.BeginHorizontal();

            DrawMethodListSidebar();

            EditorGUILayout.BeginVertical("box", GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            if (_selectedMethod == null)
            {
                EditorGUILayout.LabelField("选择一本功法或新建", EditorStyles.boldLabel);
            }
            else
            {
                DrawMethodPropertiesPanel();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawMethodListSidebar()
        {
            _methodListScroll = EditorGUILayout.BeginScrollView(_methodListScroll, GUILayout.Width(220), GUILayout.ExpandHeight(true));
            EditorGUILayout.BeginVertical("box", GUILayout.Width(220), GUILayout.ExpandHeight(true));
            EditorGUILayout.LabelField("功法列表", EditorStyles.boldLabel);
            if (GUILayout.Button("Refresh", GUILayout.Width(200))) RefreshAll();

            foreach (var m in _allMethods)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(m.MethodName, GUILayout.MaxWidth(170)))
                {
                    _selectedMethod = m;
                    _selectedNode = null;
                    GUI.FocusControl(null);
                }
                GUI.backgroundColor = new Color(0.8f, 0.2f, 0.2f);
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    if (EditorUtility.DisplayDialog("删除功法", $"确认删除功法 [{m.MethodName}] 吗？\n此操作不可撤销。", "删除", "取消"))
                    {
                        var path = AssetDatabase.GetAssetPath(m);
                        AssetDatabase.DeleteAsset(path);
                        AssetDatabase.SaveAssets();
                        if (_selectedMethod == m)
                        {
                            _selectedMethod = null;
                            _selectedNode = null;
                        }
                        RefreshAll();
                    }
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("+ 新建功法", GUILayout.MaxWidth(200)))
            {
                if (!System.IO.Directory.Exists(MethodPath)) System.IO.Directory.CreateDirectory(MethodPath);
                var uniquePath = AssetDatabase.GenerateUniqueAssetPath(MethodPath + "new_method.asset");
                var m = CreateInstance<CultivationMethodData>();
                m.EditMethodId("new_method");
                m.EditMethodName("新功法");
                AssetDatabase.CreateAsset(m, uniquePath);
                AssetDatabase.SaveAssets();
                RefreshAll();
                _selectedMethod = _allMethods.Find(x => AssetDatabase.GetAssetPath(x) == uniquePath);
                _selectedNode = null;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        private void DrawMethodPropertiesPanel()
        {
            _methodPropScroll = EditorGUILayout.BeginScrollView(_methodPropScroll, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            EditorGUILayout.BeginVertical("box", GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));

            // 功法基础属性
            DrawMethodProperties();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space(5);

            // 境界节点列表
            DrawNodeListByRealm();

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        private void DrawMethodProperties()
        {
            EditorGUILayout.LabelField("功法属性", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            _selectedMethod.EditMethodId(EditorGUILayout.TextField("Method ID", _selectedMethod.MethodId));
            _selectedMethod.EditMethodName(EditorGUILayout.TextField("名称", _selectedMethod.MethodName));
            _selectedMethod.EditDescription(EditorGUILayout.TextArea(_selectedMethod.Description, GUILayout.MinHeight(60)));
            _selectedMethod.EditIcon((Sprite)EditorGUILayout.ObjectField("图标", _selectedMethod.Icon, typeof(Sprite), false));
            _selectedMethod.EditElement(EnumPopupCN("五行属性", _selectedMethod.Element, ElementTextCN));
            _selectedMethod.EditGrade(EnumPopupCN("品阶", _selectedMethod.Grade, GradeTextCN));
            _selectedMethod.EditMaxRealm(EnumPopupCN("最高境界", _selectedMethod.MaxRealm, RealmTextCN));
            _selectedMethod.EditQuality(EnumPopupCN("品质", _selectedMethod.Quality, QualityTextCN));

            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("保存功法", GUILayout.Width(120), GUILayout.Height(30)))
            {
                MarkDirty(_selectedMethod);
                AssetDatabase.SaveAssets();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawNodeListByRealm()
        {
            EditorGUILayout.LabelField("境界与节点", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            if (_selectedMethod.Nodes == null) _selectedMethod.EditNodes(new List<CultivationNodeData>());

            var realms = _selectedMethod.GetAvailableRealms();
            realms.Sort((a, b) => ((int)a).CompareTo((int)b));

            foreach (var realm in realms)
            {
                if (!_realmFoldouts.ContainsKey(realm)) _realmFoldouts[realm] = false;

                var realmNodes = _selectedMethod.GetNodesByRealm(realm);
                // 多阶段节点(同MutexGroup)算1个
                int displayCount = realmNodes.Count(n => string.IsNullOrEmpty(n.MutexGroup))
                    + realmNodes.Where(n => !string.IsNullOrEmpty(n.MutexGroup)).Select(n => n.MutexGroup).Distinct().Count();
                string title = $"{RealmText(realm)} ({displayCount} 个节点)";
                _realmFoldouts[realm] = EditorGUILayout.Foldout(_realmFoldouts[realm], title, true);

                if (_realmFoldouts[realm])
                {
                    EditorGUILayout.BeginVertical("box");

                    // 添加节点按钮
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    if (GUILayout.Button("+ 添加节点", GUILayout.Width(120)))
                    {
                        AddNewNode(realm);
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space(3);

                    // 按 MutexGroup 分组：同组的合并为一条，单独的无组照常显示
                    var sortedNodes = realmNodes
                        .OrderBy(n => n.GridIndex.x)
                        .ThenBy(n => n.GridIndex.y)
                        .ThenBy(n => n.NodeId)
                        .ToList();

                    var shownIds = new HashSet<string>();
                    foreach (var node in sortedNodes)
                    {
                        if (shownIds.Contains(node.NodeId)) continue;

                        if (!string.IsNullOrEmpty(node.MutexGroup))
                        {
                            // 找到同组所有节点，合并显示
                            var groupNodes = sortedNodes
                                .Where(n => n.MutexGroup == node.MutexGroup)
                                .OrderBy(n => n.GridIndex.y)
                                .ToList();
                            foreach (var gn in groupNodes) shownIds.Add(gn.NodeId);
                            DrawGroupedNodeItem(groupNodes);
                        }
                        else
                        {
                            shownIds.Add(node.NodeId);
                            DrawNodeListItem(node);
                        }
                    }

                    EditorGUILayout.EndVertical();
                }
            }

            // 如果没有节点，提示添加
            if (_selectedMethod.Nodes.Count == 0)
            {
                EditorGUILayout.HelpBox("当前功法没有节点，点击上方“添加节点”开始创建。", MessageType.Info);
            }
        }

        private void DrawNodeListItem(CultivationNodeData node)
        {
            if (node == null) return;
            bool isSelected = _selectedNode == node && (_selectedGroupNodes == null || _selectedGroupNodes.Count == 0);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(20);

            GUI.backgroundColor = isSelected ? new Color(0.3f, 0.5f, 0.7f, 0.9f) : new Color(0.2f, 0.2f, 0.25f, 0.9f);
            string displayName = node.NodeName;
            if (GUILayout.Button(displayName, GUILayout.ExpandWidth(true), GUILayout.Height(28)))
            {
                _selectedNode = isSelected ? null : node;
                _selectedGroupNodes = null;
                GUI.FocusControl(null);
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            if (isSelected)
            {
                EditorGUILayout.BeginVertical("box");
                GUILayout.Space(5);
                DrawSelectedNodeEditor();
                GUILayout.Space(5);
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawGroupedNodeItem(List<CultivationNodeData> groupNodes)
        {
            if (groupNodes == null || groupNodes.Count == 0) return;

            var primary = groupNodes[0];
            bool isSelected = _selectedGroupNodes != null && _selectedGroupNodes.Count > 0 && _selectedGroupNodes[0] == primary;

            // 显示名：去掉壹/贰/叁前缀
            string displayName = StripStagePrefix(primary.NodeName);
            if (groupNodes.Count > 1) displayName += $" ({groupNodes.Count}阶)";

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(20);

            GUI.backgroundColor = isSelected ? new Color(0.3f, 0.5f, 0.7f, 0.9f) : new Color(0.15f, 0.3f, 0.15f, 0.9f);
            if (GUILayout.Button(displayName, GUILayout.ExpandWidth(true), GUILayout.Height(28)))
            {
                _selectedGroupNodes = isSelected ? null : groupNodes;
                _selectedNode = isSelected ? null : primary;
                GUI.FocusControl(null);
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            if (isSelected && _selectedGroupNodes != null)
            {
                EditorGUILayout.BeginVertical("box");
                GUILayout.Space(5);
                DrawMultiStageEditor();
                GUILayout.Space(5);
                EditorGUILayout.EndVertical();
            }
        }

        private static string StripStagePrefix(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            if (name.Length > 2 && (name.StartsWith("壹·") || name.StartsWith("贰·") || name.StartsWith("叁·") || name.StartsWith("肆·") || name.StartsWith("伍·")))
                return name.Substring(2);
            return name;
        }

        private void DrawMultiStageEditor()
        {
            if (_selectedGroupNodes == null || _selectedGroupNodes.Count == 0) return;

            EditorGUILayout.LabelField("多阶段神通属性", EditorStyles.boldLabel);
            EditorGUILayout.Space(3);

            // 共享属性（只编辑第一阶段，不包含名称和参悟消耗）
            var primary = _selectedGroupNodes[0];
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("境界", GUILayout.Width(70));
            primary.EditRealm(EnumPopupCN(primary.Realm, RealmTextCN));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("解锁方式", GUILayout.Width(70));
            primary.EditUnlockType(EnumPopupCN(primary.UnlockType, UnlockTypeTextCN));
            EditorGUILayout.EndHorizontal();

            // 图标（全阶段共用，只编辑第一阶段）
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("图标", GUILayout.Width(70));
            var newIcon = (Sprite)EditorGUILayout.ObjectField(primary.NodeIcon, typeof(Sprite), false);
            if (newIcon != primary.NodeIcon)
            {
                foreach (var n in _selectedGroupNodes) n.EditNodeIcon(newIcon);
                MarkDirty(_selectedMethod);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("五行属性", GUILayout.Width(70));
            var newEl = EnumPopupCN(primary.NodeElement, ElementTextCN);
            if (newEl != primary.NodeElement)
            {
                foreach (var n in _selectedGroupNodes) n.EditNodeElement(newEl);
                MarkDirty(_selectedMethod);
            }
            EditorGUILayout.EndHorizontal();

            // 前置/后置节点（复用单节点编辑器的完整UI）
            _selectedNode = primary;
            DrawNodePrerequisiteEditor();
            DrawNodeSubsequentEditor();

            // 互斥节点（多阶段神通也需要选择互斥）
            DrawMutexMultiSelect(primary);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("── 阶段列表 ──", EditorStyles.boldLabel);

            string[] cnNums = { "壹", "贰", "叁", "肆", "伍", "陆", "柒", "捌", "玖", "拾" };

            for (int i = 0; i < _selectedGroupNodes.Count; i++)
            {
                var stage = _selectedGroupNodes[i];
                string num = i < cnNums.Length ? cnNums[i] : (i + 1).ToString();

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{num}阶段", EditorStyles.boldLabel, GUILayout.Width(60));
                GUILayout.FlexibleSpace();
                if (_selectedGroupNodes.Count > 1)
                {
                    GUI.backgroundColor = new Color(0.6f, 0.2f, 0.2f);
                    if (GUILayout.Button("删除阶段", GUILayout.Width(70), GUILayout.Height(20)))
                    {
                        RemoveStage(stage);
                        return;
                    }
                    GUI.backgroundColor = Color.white;
                }
                EditorGUILayout.EndHorizontal();

                // 每阶段独立编辑：名称、说明、参悟消耗、奖励
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("名称", GUILayout.Width(50));
                string currentName = stage.NodeName;
                // 去掉前缀显示，编辑时用无前缀名
                string stripped = StripStagePrefix(currentName);
                string newName = EditorGUILayout.TextField(stripped);
                if (newName != stripped)
                {
                    // 保存时自动加上前缀
                    string prefix = i < cnNums.Length ? cnNums[i] + "·" : (i + 1) + "·";
                    stage.EditNodeName(prefix + newName);
                }
                EditorGUILayout.EndHorizontal();

                DrawStageRewardEditor(stage);

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(3);
            }

            // 添加阶段按钮
            if (_selectedGroupNodes.Count < 10)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUI.backgroundColor = new Color(0.2f, 0.5f, 0.2f);
                if (GUILayout.Button("+ 添加阶段", GUILayout.Width(120), GUILayout.Height(25)))
                {
                    AddStage();
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
            }

            // 保存按钮
            EditorGUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("保存功法", GUILayout.Width(120), GUILayout.Height(30)))
            {
                MarkDirty(_selectedMethod);
                AssetDatabase.SaveAssets();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawCardRewardEditor(CultivationNodeData node)
        {
            EditorGUILayout.LabelField($"卡牌奖励 ({node.RewardIds?.Count ?? 0} 张)");

            // 显示已选卡牌列表
            if (node.RewardIds != null)
            {
                for (int i = node.RewardIds.Count - 1; i >= 0; i--)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    var cardId = node.RewardIds[i];
                    var card = ResourceCache.GetCardsFromAllList()?.Find(c => c.Id == cardId);
                    string displayName = card != null ? $"{card.CardName} [{card.Id}]" : cardId;
                    EditorGUILayout.LabelField(displayName, GUILayout.ExpandWidth(true));
                    if (GUILayout.Button("↑", GUILayout.Width(25)) && i > 0)
                    {
                        var list = node.RewardIds;
                        (list[i], list[i - 1]) = (list[i - 1], list[i]);
                        node.EditRewardIds(list);
                        MarkDirty(_selectedMethod);
                    }
                    if (GUILayout.Button("↓", GUILayout.Width(25)) && i < node.RewardIds.Count - 1)
                    {
                        var list = node.RewardIds;
                        (list[i], list[i + 1]) = (list[i + 1], list[i]);
                        node.EditRewardIds(list);
                        MarkDirty(_selectedMethod);
                    }
                    if (GUILayout.Button("-", GUILayout.Width(25)))
                    {
                        var list = node.RewardIds;
                        list.RemoveAt(i);
                        node.EditRewardIds(list);
                        MarkDirty(_selectedMethod);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUI.backgroundColor = new Color(0.2f, 0.5f, 0.2f);
            if (GUILayout.Button("+ 选择卡牌", GUILayout.Width(120), GUILayout.Height(25)))
            {
                var captured = node;
                CardGame.Editor.CardPickerWindow.Open(selectedIds =>
                {
                    captured.EditRewardIds(selectedIds);
                    MarkDirty(_selectedMethod);
                }, captured.RewardIds);
            }
            GUI.backgroundColor = Color.white;
            if (GUILayout.Button("清空", GUILayout.Width(60), GUILayout.Height(25)))
            {
                node.EditRewardIds(new List<string>());
                MarkDirty(_selectedMethod);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawStageRewardEditor(CultivationNodeData stage)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("参悟消耗", GUILayout.Width(70));
            stage.EditComprehensionCost(EditorGUILayout.IntField(stage.ComprehensionCost));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("说明", GUILayout.Width(50));
            stage.EditDescription(EditorGUILayout.TextArea(stage.Description, GUILayout.MinHeight(30)));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("奖励类型", GUILayout.Width(70));
            var newReward = EnumPopupCN(stage.RewardType, RewardTypeTextCN);
            if (newReward != stage.RewardType) { stage.EditRewardType(newReward); MarkDirty(_selectedMethod); }
            EditorGUILayout.EndHorizontal();

            switch (stage.RewardType)
            {
                case NodeRewardType.Card:
                    DrawCardRewardEditor(stage);
                    break;
                case NodeRewardType.Recipe:
                    DrawStringListEditor("奖励ID", stage.RewardIds, list => { stage.EditRewardIds(list); MarkDirty(_selectedMethod); });
                    break;
                case NodeRewardType.PassiveStat:
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("属性", GUILayout.Width(70));
                    stage.EditPassiveStat(EnumPopupCN(stage.PassiveStat, PassiveStatTextCN));
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("数值", GUILayout.Width(70));
                    stage.EditPassiveValue(EditorGUILayout.IntField(stage.PassiveValue));
                    EditorGUILayout.EndHorizontal();
                    break;
                case NodeRewardType.CraftBonus:
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("加成", GUILayout.Width(70));
                    stage.EditCraftBonusType(EnumPopupCN(stage.CraftBonusType, CraftBonusTextCN));
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("数值", GUILayout.Width(70));
                    stage.EditCraftBonusValue(EditorGUILayout.FloatField(stage.CraftBonusValue));
                    EditorGUILayout.EndHorizontal();
                    break;
            }
        }

        private void AddStage()
        {
            if (_selectedGroupNodes == null || _selectedGroupNodes.Count == 0) return;
            var primary = _selectedGroupNodes[0];

            int count = _selectedGroupNodes.Count;
            var newNode = new CultivationNodeData();
            newNode.EditNodeId($"{primary.NodeId}_{count + 1}");
            newNode.EditNodeName($"新阶段{count + 1}");
            newNode.EditRealm(primary.Realm);
            newNode.EditGridIndex(new Vector2(primary.GridIndex.x, count));
            newNode.EditUnlockType(primary.UnlockType);
            newNode.EditComprehensionCost(0);
            newNode.EditMutexGroup(primary.MutexGroup);
            newNode.EditNodeElement(primary.NodeElement);
            newNode.EditRewardType(NodeRewardType.PassiveStat);
            newNode.EditRewardIds(new List<string>());

            var prereqList = primary.Prerequisites != null ? new List<string>(primary.Prerequisites) : new List<string>();
            newNode.EditPrerequisites(prereqList);

            _selectedMethod.Nodes.Add(newNode);
            _selectedGroupNodes.Add(newNode);
            MarkDirty(_selectedMethod);
        }

        private void RemoveStage(CultivationNodeData stage)
        {
            if (_selectedGroupNodes == null) return;

            // 清理其他节点的前置引用
            if (_selectedMethod.Nodes != null)
            {
                foreach (var n in _selectedMethod.Nodes)
                {
                    if (n.Prerequisites != null && n.Prerequisites.Contains(stage.NodeId))
                    {
                        var list = new List<string>(n.Prerequisites);
                        list.Remove(stage.NodeId);
                        n.EditPrerequisites(list);
                    }
                }
            }

            _selectedMethod.Nodes.Remove(stage);
            _selectedGroupNodes.Remove(stage);

            if (_selectedGroupNodes.Count == 0)
            {
                _selectedGroupNodes = null;
                _selectedNode = null;
            }
            else
            {
                _selectedNode = _selectedGroupNodes[0];
            }
            MarkDirty(_selectedMethod);
        }

        private List<string> DrawPrerequisitePopup(CultivationNodeData node)
        {
            var allNodes = _selectedMethod.Nodes.Where(n => n != node && n.Realm <= node.Realm).ToList();
            var current = node.Prerequisites ?? new List<string>();

            if (allNodes.Count == 0)
            {
                EditorGUILayout.LabelField("(无可用前置)");
                return current;
            }

            // 多选弹出
            EditorGUILayout.BeginHorizontal();
            string display = current.Count > 0 ? string.Join(",", current.Select(id =>
            {
                var n = _selectedMethod.GetNode(id);
                return n != null ? StripStagePrefix(n.NodeName) : id;
            })) : "(无)";
            EditorGUILayout.LabelField(display, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                var menu = new GenericMenu();
                foreach (var n in allNodes)
                {
                    bool isActive = current.Contains(n.NodeId);
                    string name = StripStagePrefix(n.NodeName);
                    menu.AddItem(new GUIContent(name), isActive, () =>
                    {
                        var list = new List<string>(current);
                        if (list.Contains(n.NodeId)) list.Remove(n.NodeId);
                        else list.Add(n.NodeId);
                        node.EditPrerequisites(list);
                        MarkDirty(_selectedMethod);
                    });
                }
                menu.ShowAsContext();
            }
            EditorGUILayout.EndHorizontal();
            return current;
        }

        private void DrawSelectedNodeEditor()
        {
            if (_selectedNode == null) return;
            var node = _selectedNode;
            var ability = GetAbilityOfNode(node);

            EditorGUILayout.LabelField("节点属性", EditorStyles.boldLabel);
            EditorGUILayout.Space(3);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Node ID", GUILayout.Width(70));
            node.EditNodeId(EditorGUILayout.TextField(node.NodeId));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("名称", GUILayout.Width(70));
            node.EditNodeName(EditorGUILayout.TextField(node.NodeName));
            EditorGUILayout.EndHorizontal();

            node.EditDescription(EditorGUILayout.TextArea(node.Description, GUILayout.MinHeight(40)));

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("境界", GUILayout.Width(70));
            node.EditRealm(EnumPopupCN(node.Realm, RealmTextCN));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("下标", GUILayout.Width(70));
            int newIndex = EditorGUILayout.IntField(GetNodeIndexInRealm(node), GUILayout.Width(60));
            if (newIndex != GetNodeIndexInRealm(node))
            {
                SetNodeIndexInRealm(node, newIndex);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("解锁方式", GUILayout.Width(70));
            node.EditUnlockType(EnumPopupCN(node.UnlockType, UnlockTypeTextCN));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("参悟消耗", GUILayout.Width(70));
            node.EditComprehensionCost(EditorGUILayout.IntField(node.ComprehensionCost));
            EditorGUILayout.EndHorizontal();

            // 图标
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("图标", GUILayout.Width(70));
            node.EditNodeIcon((Sprite)EditorGUILayout.ObjectField(node.NodeIcon, typeof(Sprite), false));
            EditorGUILayout.EndHorizontal();

            // 五行属性：节点自身属性；若是神通节点则同步到神通资源
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("五行属性", GUILayout.Width(70));

            EditorGUI.BeginChangeCheck();
            var newElement = EnumPopupCN(node.NodeElement, ElementTextCN);
            if (EditorGUI.EndChangeCheck())
            {
                node.EditNodeElement(newElement);
                MarkDirty(_selectedMethod);
                if (ability != null && ability.Element != newElement)
                {
                    ability.EditElement(newElement);
                    MarkDirty(ability);
                }
            }

            EditorGUILayout.EndHorizontal();

            // 互斥组：使用当前境界其他节点的多选
            DrawMutexMultiSelect(node);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("奖励类型", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("类型", GUILayout.Width(70));
            EditorGUI.BeginChangeCheck();
            var newRewardType = EnumPopupCN(node.RewardType, RewardTypeTextCN);
            if (EditorGUI.EndChangeCheck())
            {
                node.EditRewardType(newRewardType);
                MarkDirty(_selectedMethod);
            }
            EditorGUILayout.EndHorizontal();

            switch (node.RewardType)
            {
                case NodeRewardType.Card:
                    DrawCardRewardEditor(node);
                    break;
                case NodeRewardType.Recipe:
                    DrawStringListEditor("奖励ID", node.RewardIds, list => { node.EditRewardIds(list); MarkDirty(_selectedMethod); });
                    break;
                case NodeRewardType.PassiveStat:
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("属性类型", GUILayout.Width(70));
                    node.EditPassiveStat(EnumPopupCN(node.PassiveStat, PassiveStatTextCN));
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("数值", GUILayout.Width(70));
                    node.EditPassiveValue(EditorGUILayout.IntField(node.PassiveValue));
                    EditorGUILayout.EndHorizontal();
                    break;
                case NodeRewardType.CraftBonus:
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("加成类型", GUILayout.Width(70));
                    node.EditCraftBonusType(EnumPopupCN(node.CraftBonusType, CraftBonusTextCN));
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("数值", GUILayout.Width(70));
                    node.EditCraftBonusValue(EditorGUILayout.FloatField(node.CraftBonusValue));
                    EditorGUILayout.EndHorizontal();
                    break;
            }

            // 神通剩余属性（无独立标题，直接跟在奖励类型下方）
            if (ability != null)
            {
                EditorGUILayout.Space(5);
                DrawInlineAbilityProperties(ability);
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("节点关系", EditorStyles.boldLabel);
            DrawNodePrerequisiteEditor();
            DrawNodeSubsequentEditor();

            EditorGUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("删除节点", GUILayout.Width(120), GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("删除节点", $"确认删除节点 [{node.NodeName}] 吗？", "删除", "取消"))
                {
                    RemoveNodeFromMethod(node);
                }
            }
            GUI.backgroundColor = Color.white;

            GUILayout.FlexibleSpace();
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("保存功法", GUILayout.Width(120), GUILayout.Height(30)))
            {
                MarkDirty(_selectedMethod);
                if (ability != null) MarkDirty(ability);
                AssetDatabase.SaveAssets();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawAbilityPicker(CultivationNodeData node)
        {
            // DivineAbility reward type removed — no longer used
        }

        private void DrawInlineAbilityProperties(DivineAbilityData ability)
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("神通名称", GUILayout.Width(70));
            ability.EditAbilityName(EditorGUILayout.TextField(ability.AbilityName));
            EditorGUILayout.EndHorizontal();

            ability.EditDescription(EditorGUILayout.TextArea(ability.Description, GUILayout.MinHeight(40)));

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("图标", GUILayout.Width(70));
            ability.EditIcon((Sprite)EditorGUILayout.ObjectField(ability.Icon, typeof(Sprite), false));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("品质", GUILayout.Width(70));
            ability.EditQuality(EnumPopupCN(ability.Quality, QualityTextCN));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("卡牌ID", GUILayout.Width(70));
            ability.EditCardId(EditorGUILayout.TextField(ability.CardId));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("能量消耗", GUILayout.Width(70));
            ability.EditEnergyCost(EditorGUILayout.IntField(ability.EnergyCost));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("参悟消耗", GUILayout.Width(70));
            ability.EditComprehensionCost(EditorGUILayout.IntField(ability.ComprehensionCost));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(3);
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("保存神通", GUILayout.Width(120), GUILayout.Height(25)))
            {
                MarkDirty(ability);
                AssetDatabase.SaveAssets();
            }
            GUI.backgroundColor = Color.white;
        }

        private void DrawNodePrerequisiteEditor()
        {
            EditorGUILayout.LabelField("前置节点", EditorStyles.boldLabel);
            var prereqs = _selectedNode.Prerequisites ?? new List<string>();
            var allNodes = _selectedMethod.Nodes?.Where(n => n != _selectedNode).ToList() ?? new List<CultivationNodeData>();

            if (prereqs.Count == 0)
            {
                EditorGUILayout.HelpBox("无前置节点", MessageType.Info);
            }

            for (int i = prereqs.Count - 1; i >= 0; i--)
            {
                EditorGUILayout.BeginHorizontal();
                var displayNames = allNodes.Select(n => StripStagePrefix(n.NodeName) + " [" + n.NodeId + "]").ToArray();
                var ids = allNodes.Select(n => n.NodeId).ToList();
                int idx = ids.IndexOf(prereqs[i]);
                int newIdx = EditorGUILayout.Popup("前置 " + (i + 1), idx, displayNames, GUILayout.Width(280));
                if (newIdx >= 0 && newIdx != idx)
                {
                    prereqs[i] = ids[newIdx];
                    _selectedNode.EditPrerequisites(prereqs);
                    MarkDirty(_selectedMethod);
                }
                if (GUILayout.Button("-", GUILayout.Width(25)))
                {
                    prereqs.RemoveAt(i);
                    _selectedNode.EditPrerequisites(prereqs);
                    MarkDirty(_selectedMethod);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+ 添加前置", GUILayout.Width(120)))
            {
                var available = allNodes.FirstOrDefault(n => !prereqs.Contains(n.NodeId));
                if (available != null)
                {
                    prereqs.Add(available.NodeId);
                    _selectedNode.EditPrerequisites(prereqs);
                    MarkDirty(_selectedMethod);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawNodeSubsequentEditor()
        {
            EditorGUILayout.LabelField("后置节点", EditorStyles.boldLabel);
            var subsequents = new List<CultivationNodeData>();
            if (_selectedMethod.Nodes != null)
            {
                foreach (var n in _selectedMethod.Nodes)
                {
                    if (n == _selectedNode) continue;
                    if (n.Prerequisites != null && n.Prerequisites.Contains(_selectedNode.NodeId))
                        subsequents.Add(n);
                }
            }

            if (subsequents.Count == 0)
            {
                EditorGUILayout.HelpBox("暂无后置节点。将其他节点前置设为当前节点即可建立后置关系。", MessageType.Info);
            }
            else
            {
                for (int i = subsequents.Count - 1; i >= 0; i--)
                {
                    var sub = subsequents[i];
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("→ " + StripStagePrefix(sub.NodeName) + " [" + sub.NodeId + "]", GUILayout.ExpandWidth(true));
                    if (GUILayout.Button("-", GUILayout.Width(25)))
                    {
                        var list = new List<string>(sub.Prerequisites);
                        list.Remove(_selectedNode.NodeId);
                        sub.EditPrerequisites(list);
                        MarkDirty(_selectedMethod);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.Space(3);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+ 添加后置", GUILayout.Width(120)))
            {
                AddSubsequentNodePopup();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void AddSubsequentNodePopup()
        {
            if (_selectedMethod.Nodes == null) return;
            var candidates = _selectedMethod.Nodes
                .Where(n => n != _selectedNode && (n.Prerequisites == null || !n.Prerequisites.Contains(_selectedNode.NodeId)))
                .Select(n => StripStagePrefix(n.NodeName) + " [" + n.NodeId + "]")
                .ToArray();
            var ids = _selectedMethod.Nodes
                .Where(n => n != _selectedNode && (n.Prerequisites == null || !n.Prerequisites.Contains(_selectedNode.NodeId)))
                .Select(n => n.NodeId)
                .ToList();

            if (candidates.Length == 0)
            {
                EditorUtility.DisplayDialog("添加后置", "没有可作为后置的节点。", "确定");
                return;
            }

            var menu = new GenericMenu();
            for (int i = 0; i < candidates.Length; i++)
            {
                int captured = i;
                menu.AddItem(new GUIContent(candidates[i]), false, () =>
                {
                    var target = _selectedMethod.Nodes.Find(n => n.NodeId == ids[captured]);
                    if (target != null)
                    {
                        var prereqs = target.Prerequisites ?? new List<string>();
                        prereqs.Add(_selectedNode.NodeId);
                        target.EditPrerequisites(prereqs);
                        MarkDirty(_selectedMethod);
                        Repaint();
                    }
                });
            }
            menu.ShowAsContext();
        }

        private void AddNewNode(RealmLevel realm)
        {
            if (_selectedMethod.Nodes == null) _selectedMethod.EditNodes(new List<CultivationNodeData>());

            int count = _selectedMethod.Nodes.Count(n => n.Realm == realm);
            var newNode = new CultivationNodeData();
            newNode.EditNodeId($"{_selectedMethod.MethodId}_{realm.ToString().ToLowerInvariant()}_{count + 1}");
            newNode.EditNodeName("新节点");
            newNode.EditRealm(realm);
            newNode.EditGridIndex(new Vector2(count, 0));
            newNode.EditNodeElement(_selectedMethod.Element);
            newNode.EditRewardType(NodeRewardType.Card);
            newNode.EditRewardIds(new List<string>());

            _selectedMethod.Nodes.Add(newNode);
            MarkDirty(_selectedMethod);
            _selectedNode = newNode;
            _realmFoldouts[realm] = true;
        }

        private int GetNodeIndexInRealm(CultivationNodeData node)
        {
            if (_selectedMethod?.Nodes == null) return 0;
            var realmNodes = _selectedMethod.Nodes
                .Where(n => n.Realm == node.Realm)
                .OrderBy(n => n.GridIndex.x)
                .ThenBy(n => n.GridIndex.y)
                .ThenBy(n => n.NodeId)
                .ToList();
            return realmNodes.FindIndex(n => n == node);
        }

        private void SetNodeIndexInRealm(CultivationNodeData node, int newIndex)
        {
            if (_selectedMethod?.Nodes == null) return;
            var realmNodes = _selectedMethod.Nodes
                .Where(n => n.Realm == node.Realm)
                .OrderBy(n => n.GridIndex.x)
                .ThenBy(n => n.GridIndex.y)
                .ThenBy(n => n.NodeId)
                .ToList();

            int currentIndex = realmNodes.FindIndex(n => n == node);
            if (currentIndex < 0 || newIndex < 0 || newIndex >= realmNodes.Count) return;

            // Swap positions with target
            var target = realmNodes[newIndex];
            var tempGrid = node.GridIndex;
            node.EditGridIndex(target.GridIndex);
            target.EditGridIndex(tempGrid);
            MarkDirty(_selectedMethod);
        }

        private void DrawMutexMultiSelect(CultivationNodeData node)
        {
            EditorGUILayout.LabelField("互斥节点", EditorStyles.boldLabel);

            // 收集同境界所有其他"神通组"的首节点（多阶段只取第一阶）
            var realmNodes = _selectedMethod.Nodes
                .Where(n => n.Realm == node.Realm && n != node)
                .OrderBy(n => n.GridIndex.x).ThenBy(n => n.GridIndex.y).ThenBy(n => n.NodeId)
                .ToList();

            if (realmNodes.Count == 0)
            {
                EditorGUILayout.HelpBox("当前境界下没有其他节点", MessageType.Info);
                return;
            }

            // 过滤：多阶段节点只显示首阶段，同MutexGroup取GridIndex.y最小的
            var seenIds = new HashSet<string>();
            var candidates = new List<CultivationNodeData>();
            foreach (var n in realmNodes)
            {
                if (seenIds.Contains(n.NodeId)) continue;
                if (!string.IsNullOrEmpty(n.MutexGroup) && n.MutexGroup == node.MutexGroup)
                {
                    // 同组的自己人，跳过
                    seenIds.Add(n.NodeId);
                    continue;
                }
                if (!string.IsNullOrEmpty(n.MutexGroup))
                {
                    var peers = realmNodes.Where(x => x.MutexGroup == n.MutexGroup).OrderBy(x => x.GridIndex.y).ToList();
                    candidates.Add(peers[0]);
                    foreach (var p in peers) seenIds.Add(p.NodeId);
                }
                else
                {
                    candidates.Add(n);
                    seenIds.Add(n.NodeId);
                }
            }

            string currentGroup = node.MutexGroup ?? "";

            EditorGUILayout.HelpBox("勾选与当前神通互斥的节点（同一境界下）。多阶段神通只显示首阶段。", MessageType.Info);

            foreach (var other in candidates)
            {
                // 判断是否与当前节点互斥：同 MutexGroup
                bool isMutex = !string.IsNullOrEmpty(currentGroup) && other.MutexGroup == currentGroup;

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                bool newValue = EditorGUILayout.Toggle($"{GetNodeDisplayName(other)}", isMutex);

                if (newValue && !isMutex)
                {
                    // 新建互斥组
                    string groupId = $"{_selectedMethod.MethodId}_{node.Realm}_mutex_{System.Guid.NewGuid().ToString("N").Substring(0, 8)}";
                    node.EditMutexGroup(groupId);
                    other.EditMutexGroup(groupId);
                    MarkDirty(_selectedMethod);
                }
                else if (!newValue && isMutex)
                {
                    // 取消互斥：清掉当前节点和同组所有成员的 MutexGroup
                    var groupMembers = _selectedMethod.Nodes
                        .Where(x => !string.IsNullOrEmpty(x.MutexGroup) && x.MutexGroup == currentGroup)
                        .ToList();
                    foreach (var m in groupMembers) m.EditMutexGroup("");
                    MarkDirty(_selectedMethod);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private string GetNodeDisplayName(CultivationNodeData node)
        {
            var ability = GetAbilityOfNode(node);
            return !string.IsNullOrEmpty(ability?.AbilityName) ? ability.AbilityName : node.NodeName;
        }

        private void RemoveNodeFromMethod(CultivationNodeData node)
        {
            if (_selectedMethod.Nodes != null)
            {
                foreach (var n in _selectedMethod.Nodes)
                {
                    if (n.Prerequisites != null && n.Prerequisites.Contains(node.NodeId))
                    {
                        var list = new List<string>(n.Prerequisites);
                        list.Remove(node.NodeId);
                        n.EditPrerequisites(list);
                    }
                }
                _selectedMethod.Nodes.Remove(node);
            }
            _selectedNode = null;
            MarkDirty(_selectedMethod);
        }
        #endregion

        #region 神通库
        private void DrawAbilityLibrary()
        {
            EditorGUILayout.BeginHorizontal();

            _abilityLibListScroll = EditorGUILayout.BeginScrollView(_abilityLibListScroll, GUILayout.Width(260), GUILayout.ExpandHeight(true));
            EditorGUILayout.BeginVertical("box", GUILayout.Width(260), GUILayout.ExpandHeight(true));
            EditorGUILayout.LabelField("神通书籍库", EditorStyles.boldLabel);
            if (GUILayout.Button("Refresh", GUILayout.Width(240))) RefreshAll();

            foreach (var a in _allAbilities)
            {
                GUIStyle style = _selectedLibraryAbility == a ? EditorStyles.boldLabel : EditorStyles.label;
                if (GUILayout.Button(a.AbilityName, GUILayout.MaxWidth(240)))
                {
                    _selectedLibraryAbility = a;
                    GUI.FocusControl(null);
                }
            }

            if (GUILayout.Button("+ 新建神通书籍", GUILayout.MaxWidth(240)))
            {
                var created = CreateNewAbilityAsset();
                if (created != null)
                {
                    RefreshAll();
                    _selectedLibraryAbility = created;
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginVertical("box", GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            if (_selectedLibraryAbility == null)
            {
                EditorGUILayout.LabelField("选择一个神通或新建");
            }
            else
            {
                DrawLibraryAbilityProperties(_selectedLibraryAbility);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawLibraryAbilityProperties(DivineAbilityData ability)
        {
            _abilityLibPropScroll = EditorGUILayout.BeginScrollView(_abilityLibPropScroll, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField("神通属性", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Ability ID", GUILayout.Width(70));
            ability.EditAbilityId(EditorGUILayout.TextField(ability.AbilityId));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("名称", GUILayout.Width(70));
            ability.EditAbilityName(EditorGUILayout.TextField(ability.AbilityName));
            EditorGUILayout.EndHorizontal();

            ability.EditDescription(EditorGUILayout.TextArea(ability.Description, GUILayout.MinHeight(60)));

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("图标", GUILayout.Width(70));
            ability.EditIcon((Sprite)EditorGUILayout.ObjectField(ability.Icon, typeof(Sprite), false));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("属性", GUILayout.Width(70));
            ability.EditElement(EnumPopupCN(ability.Element, ElementTextCN));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("品质", GUILayout.Width(70));
            ability.EditQuality(EnumPopupCN(ability.Quality, QualityTextCN));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("卡牌ID", GUILayout.Width(70));
            ability.EditCardId(EditorGUILayout.TextField(ability.CardId));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("能量消耗", GUILayout.Width(70));
            ability.EditEnergyCost(EditorGUILayout.IntField(ability.EnergyCost));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("参悟消耗", GUILayout.Width(70));
            ability.EditComprehensionCost(EditorGUILayout.IntField(ability.ComprehensionCost));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("保存神通", GUILayout.Width(120), GUILayout.Height(30)))
            {
                MarkDirty(ability);
                AssetDatabase.SaveAssets();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        private DivineAbilityData CreateNewAbilityAsset()
        {
            if (!System.IO.Directory.Exists(AbilityPath)) System.IO.Directory.CreateDirectory(AbilityPath);
            var uniquePath = AssetDatabase.GenerateUniqueAssetPath(AbilityPath + "new_ability.asset");
            var a = CreateInstance<DivineAbilityData>();
            a.EditAbilityId("new_ability");
            a.EditAbilityName("新神通");
            AssetDatabase.CreateAsset(a, uniquePath);
            AssetDatabase.SaveAssets();
            return a;
        }

        private void EnsureNodeHasAbility(CultivationNodeData node)
        {
            // DivineAbility reward type removed — nodes now give cards directly
        }
        #endregion

        #region Helpers
        private void DrawStringListEditor(string label, List<string> list, System.Action<List<string>> onChanged)
        {
            bool changed = false;
            if (list == null)
            {
                list = new List<string>();
                changed = true;
            }
            EditorGUILayout.LabelField(label + " (" + list.Count + ")");
            for (int i = list.Count - 1; i >= 0; i--)
            {
                EditorGUILayout.BeginHorizontal();
                var v = EditorGUILayout.TextField(list[i], GUILayout.Width(240));
                if (v != list[i]) { list[i] = v; changed = true; }
                if (GUILayout.Button("-", GUILayout.Width(25)))
                {
                    list.RemoveAt(i);
                    changed = true;
                }
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button("+ 添加 " + label, GUILayout.Width(140)))
            {
                list.Add("");
                changed = true;
            }
            if (changed)
            {
                onChanged(list);
            }
        }

        private void MarkDirty(UnityEngine.Object asset)
        {
            if (asset != null) EditorUtility.SetDirty(asset);
        }

        private DivineAbilityData GetAbilityOfNode(CultivationNodeData node)
        {
            // DivineAbility reward type removed
            return null;
        }

        private static string RealmText(RealmLevel r) => r switch
        {
            RealmLevel.LianQi => "练气期",
            RealmLevel.ZhuJi => "筑基期",
            RealmLevel.JinDan => "结丹期",
            RealmLevel.YuanYing => "元婴期",
            RealmLevel.HuaShen => "化神期",
            RealmLevel.DuJie => "渡劫期",
            _ => "?"
        };

        private static string ElementChar(ElementType el) => el switch
        {
            ElementType.Metal => "金", ElementType.Wood => "木", ElementType.Water => "水",
            ElementType.Fire => "火", ElementType.Earth => "土", ElementType.Sword => "剑",
            ElementType.Wind => "风", ElementType.Thunder => "雷", ElementType.Ghost => "鬼",
            _ => "无"
        };

        #region 中文枚举下拉
        private static T EnumPopupCN<T>(T current, System.Func<T, string> toCn) where T : System.Enum
        {
            var values = System.Enum.GetValues(typeof(T));
            var options = new string[values.Length];
            int selIdx = 0;
            for (int i = 0; i < values.Length; i++)
            {
                var v = (T)values.GetValue(i);
                options[i] = toCn(v);
                if (System.Enum.GetName(typeof(T), v) == System.Enum.GetName(typeof(T), current))
                    selIdx = i;
            }
            int newIdx = EditorGUILayout.Popup(selIdx, options);
            return (T)values.GetValue(newIdx);
        }

        private static T EnumPopupCN<T>(string label, T current, System.Func<T, string> toCn) where T : System.Enum
        {
            var values = System.Enum.GetValues(typeof(T));
            var options = new string[values.Length];
            int selIdx = 0;
            for (int i = 0; i < values.Length; i++)
            {
                var v = (T)values.GetValue(i);
                options[i] = toCn(v);
                if (System.Enum.GetName(typeof(T), v) == System.Enum.GetName(typeof(T), current))
                    selIdx = i;
            }
            int newIdx = EditorGUILayout.Popup(label, selIdx, options);
            return (T)values.GetValue(newIdx);
        }

        private static string RealmTextCN(RealmLevel r) => r switch
        {
            RealmLevel.LianQi => "练气期", RealmLevel.ZhuJi => "筑基期", RealmLevel.JinDan => "结丹期",
            RealmLevel.YuanYing => "元婴期", RealmLevel.HuaShen => "化神期", RealmLevel.DuJie => "渡劫期",
            _ => r.ToString()
        };

        private static string UnlockTypeTextCN(NodeUnlockType t) => t switch
        {
            NodeUnlockType.Comprehension => "参悟", NodeUnlockType.Minigame => "小游戏突破",
            NodeUnlockType.Material => "材料", NodeUnlockType.CombatTrigger => "战斗触发",
            _ => t.ToString()
        };

        private static string ElementTextCN(ElementType el) => el switch
        {
            ElementType.None => "无", ElementType.Metal => "金", ElementType.Wood => "木",
            ElementType.Water => "水", ElementType.Fire => "火", ElementType.Earth => "土",
            ElementType.Sword => "剑", ElementType.Wind => "风", ElementType.Thunder => "雷",
            ElementType.Ghost => "鬼", _ => el.ToString()
        };

        private static string RewardTypeTextCN(NodeRewardType t) => t switch
        {
            NodeRewardType.Card => "卡牌", NodeRewardType.Recipe => "丹方/图纸",
            NodeRewardType.PassiveStat => "被动属性", NodeRewardType.CraftBonus => "炼制加成",
            _ => t.ToString()
        };

        private static string PassiveStatTextCN(PassiveStatType t) => t switch
        {
            PassiveStatType.None => "无", PassiveStatType.MaxHP => "生命上限",
            PassiveStatType.ShenShi => "神识上限", PassiveStatType.MaxMana => "灵力上限",
            _ => t.ToString()
        };

        private static string CraftBonusTextCN(CraftBonusType t) => t switch
        {
            CraftBonusType.None => "无", CraftBonusType.AlchemySuccess => "炼丹成功率",
            CraftBonusType.AlchemyQuality => "炼丹品质", CraftBonusType.ForgingSuccess => "炼器成功率",
            CraftBonusType.ForgingQuality => "炼器品质", _ => t.ToString()
        };

        private static string GradeTextCN(CultivationMethodGrade g) => g switch
        {
            CultivationMethodGrade.Complete => "完整本", CultivationMethodGrade.Fragment => "残篇",
            _ => g.ToString()
        };

        private static string QualityTextCN(ItemQuality q) => ItemQualityHelper.GetDisplayName(q);
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
        #endregion

#endif
    }
}
