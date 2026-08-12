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
            _selectedMethod.EditElement((ElementType)EditorGUILayout.EnumPopup("五行属性", _selectedMethod.Element));
            _selectedMethod.EditGrade((CultivationMethodGrade)EditorGUILayout.EnumPopup("品阶", _selectedMethod.Grade));
            _selectedMethod.EditMaxRealm((RealmLevel)EditorGUILayout.EnumPopup("最高境界", _selectedMethod.MaxRealm));
            _selectedMethod.EditQuality((ItemQuality)EditorGUILayout.EnumPopup("品质", _selectedMethod.Quality));

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
                string title = $"{RealmText(realm)} ({realmNodes.Count} 个节点)";
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

                    // 该境界下的节点，按下标排序
                    var sortedNodes = realmNodes
                        .OrderBy(n => GetNodeIndexInRealm(n))
                        .ToList();
                    foreach (var node in sortedNodes)
                    {
                        DrawNodeListItem(node);
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
            var ability = GetAbilityOfNode(node);
            bool isSelected = _selectedNode == node;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(20);

            GUI.backgroundColor = isSelected ? new Color(0.3f, 0.5f, 0.7f, 0.9f) : new Color(0.2f, 0.2f, 0.25f, 0.9f);
            string displayName = !string.IsNullOrEmpty(ability?.AbilityName) ? ability.AbilityName : node.NodeName;
            string info = displayName;
            if (GUILayout.Button(info, GUILayout.ExpandWidth(true), GUILayout.Height(28)))
            {
                _selectedNode = _selectedNode == node ? null : node;
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
            node.EditRealm((RealmLevel)EditorGUILayout.EnumPopup((RealmLevel)node.Realm));
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
            node.EditUnlockType((NodeUnlockType)EditorGUILayout.EnumPopup((NodeUnlockType)node.UnlockType));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("参悟消耗", GUILayout.Width(70));
            node.EditComprehensionCost(EditorGUILayout.IntField(node.ComprehensionCost));
            EditorGUILayout.EndHorizontal();

            // 五行属性：节点自身属性；若是神通节点则同步到神通资源
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("五行属性", GUILayout.Width(70));

            EditorGUI.BeginChangeCheck();
            var newElement = (ElementType)EditorGUILayout.EnumPopup((ElementType)node.NodeElement);
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
            var newRewardType = (NodeRewardType)EditorGUILayout.EnumPopup((NodeRewardType)node.RewardType);
            if (EditorGUI.EndChangeCheck())
            {
                node.EditRewardType(newRewardType);
                MarkDirty(_selectedMethod);
                if (newRewardType == NodeRewardType.DivineAbility)
                {
                    EnsureNodeHasAbility(node);
                }
            }
            EditorGUILayout.EndHorizontal();

            switch (node.RewardType)
            {
                case NodeRewardType.DivineAbility:
                    DrawAbilityPicker(node);
                    break;
                case NodeRewardType.Card:
                case NodeRewardType.Recipe:
                case NodeRewardType.SpecialSkill:
                    DrawStringListEditor("奖励ID", node.RewardIds, list => { node.EditRewardIds(list); MarkDirty(_selectedMethod); });
                    break;
                case NodeRewardType.PassiveStat:
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("属性类型", GUILayout.Width(70));
                    node.EditPassiveStat((PassiveStatType)EditorGUILayout.EnumPopup((PassiveStatType)node.PassiveStat));
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("数值", GUILayout.Width(70));
                    node.EditPassiveValue(EditorGUILayout.IntField(node.PassiveValue));
                    EditorGUILayout.EndHorizontal();
                    break;
                case NodeRewardType.CraftBonus:
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("加成类型", GUILayout.Width(70));
                    node.EditCraftBonusType((CraftBonusType)EditorGUILayout.EnumPopup((CraftBonusType)node.CraftBonusType));
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
            else if (node.RewardType == NodeRewardType.DivineAbility)
            {
                EditorGUILayout.HelpBox("请在“奖励类型”上方选择一个神通资源或点击“新建神通并关联”。", MessageType.Info);
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
            var currentAbility = GetAbilityOfNode(node);
            var newAbility = (DivineAbilityData)EditorGUILayout.ObjectField("神通资源", currentAbility, typeof(DivineAbilityData), false);

            if (newAbility != currentAbility)
            {
                if (newAbility != null) node.EditRewardIds(new List<string> { newAbility.AbilityId });
                else node.EditRewardIds(new List<string>());
                MarkDirty(_selectedMethod);
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("新建时属性", GUILayout.Width(70));
            var newAbilityElement = (ElementType)EditorGUILayout.EnumPopup((ElementType)_selectedMethod.Element, GUILayout.Width(100));
            if (GUILayout.Button("新建神通并关联", GUILayout.Width(140)))
            {
                var created = CreateNewAbilityAsset();
                if (created != null)
                {
                    created.EditElement(newAbilityElement);
                    node.EditRewardIds(new List<string> { created.AbilityId });
                    MarkDirty(_selectedMethod);
                    MarkDirty(created);
                    AssetDatabase.SaveAssets();
                    RefreshAll();
                }
            }
            EditorGUILayout.EndHorizontal();
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
            ability.EditQuality((ItemQuality)EditorGUILayout.EnumPopup((ItemQuality)ability.Quality));
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
            var allNodeIds = _selectedMethod.Nodes?.Select(n => n.NodeId).ToList() ?? new List<string>();
            allNodeIds.Remove(_selectedNode.NodeId);

            if (prereqs.Count == 0)
            {
                EditorGUILayout.HelpBox("无前置节点", MessageType.Info);
            }

            for (int i = prereqs.Count - 1; i >= 0; i--)
            {
                EditorGUILayout.BeginHorizontal();
                int idx = allNodeIds.IndexOf(prereqs[i]);
                int newIdx = EditorGUILayout.Popup("前置 " + (i + 1), idx, allNodeIds.ToArray(), GUILayout.Width(220));
                if (newIdx >= 0 && newIdx != idx)
                {
                    prereqs[i] = allNodeIds[newIdx];
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
                var available = allNodeIds.Find(id => !prereqs.Contains(id));
                if (!string.IsNullOrEmpty(available))
                {
                    prereqs.Add(available);
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
                    EditorGUILayout.LabelField("→ " + sub.NodeName + " [" + sub.NodeId + "]", GUILayout.ExpandWidth(true));
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
                .Select(n => n.NodeName + " [" + n.NodeId + "]")
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
            newNode.EditRewardType(NodeRewardType.DivineAbility);

            var newAbility = CreateNewAbilityAsset();
            if (newAbility != null)
            {
                newAbility.EditElement(_selectedMethod.Element);
                newAbility.EditAbilityName(newNode.NodeName);
                MarkDirty(newAbility);
                newNode.EditRewardIds(new List<string> { newAbility.AbilityId });
            }
            else
            {
                newNode.EditRewardIds(new List<string>());
            }

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
            var realmNodes = _selectedMethod.Nodes
                .Where(n => n.Realm == node.Realm && n != node)
                .OrderBy(n => GetNodeIndexInRealm(n))
                .ToList();

            if (realmNodes.Count == 0)
            {
                EditorGUILayout.HelpBox("当前境界下没有其他节点", MessageType.Info);
                return;
            }

            string currentGroup = node.MutexGroup ?? "";
            var groupMembers = _selectedMethod.Nodes
                .Where(n => n != node && !string.IsNullOrEmpty(n.MutexGroup) && n.MutexGroup == currentGroup)
                .ToList();

            EditorGUILayout.HelpBox("勾选与当前神通互斥的节点（同一境界下），勾选后会自动建立互斥组。", MessageType.Info);

            bool anyChange = false;
            var newSelectedIds = new List<string>();
            foreach (var other in realmNodes)
            {
                bool isInGroup = !string.IsNullOrEmpty(currentGroup) && other.MutexGroup == currentGroup;
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                bool newValue = EditorGUILayout.Toggle($"{GetNodeDisplayName(other)}", isInGroup);
                EditorGUILayout.EndHorizontal();

                if (newValue)
                {
                    newSelectedIds.Add(other.NodeId);
                }

                if (newValue != isInGroup)
                {
                    anyChange = true;
                }
            }

            if (anyChange)
            {
                // Clear old group
                foreach (var member in groupMembers)
                {
                    member.EditMutexGroup("");
                }
                node.EditMutexGroup("");

                // Create new group if any selected
                if (newSelectedIds.Count > 0)
                {
                    string groupId = $"{_selectedMethod.MethodId}_{node.Realm}_mutex_{node.NodeId}";
                    node.EditMutexGroup(groupId);
                    foreach (var id in newSelectedIds)
                    {
                        var other = _selectedMethod.Nodes.Find(n => n.NodeId == id);
                        if (other != null) other.EditMutexGroup(groupId);
                    }
                }
                MarkDirty(_selectedMethod);
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
            ability.EditElement((ElementType)EditorGUILayout.EnumPopup((ElementType)ability.Element));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("品质", GUILayout.Width(70));
            ability.EditQuality((ItemQuality)EditorGUILayout.EnumPopup((ItemQuality)ability.Quality));
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
            if (node == null || node.RewardType != NodeRewardType.DivineAbility) return;
            if (node.RewardIds != null && node.RewardIds.Count > 0 && GetAbilityOfNode(node) != null) return;

            var ability = CreateNewAbilityAsset();
            if (ability == null) return;

            ability.EditElement(node.NodeElement != ElementType.None ? node.NodeElement : _selectedMethod.Element);
            ability.EditAbilityName(node.NodeName);
            node.EditRewardIds(new List<string> { ability.AbilityId });
            MarkDirty(ability);
            MarkDirty(_selectedMethod);
            AssetDatabase.SaveAssets();
            RefreshAll();
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
            if (node == null || _allAbilities == null) return null;
            if (node.RewardType != NodeRewardType.DivineAbility || node.RewardIds == null || node.RewardIds.Count == 0)
                return null;
            return _allAbilities.Find(a => a != null && a.AbilityId == node.RewardIds[0]);
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
