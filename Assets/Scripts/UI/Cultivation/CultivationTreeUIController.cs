using System;
using System.Collections.Generic;
using System.Linq;
using CardGame;
using CardGame.Audio;
using NueGames.NueDeck.Scripts.Data.Cultivation;
using NueGames.NueDeck.Scripts.Enums;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardGame.UI.Cultivation
{
    /// <summary>
    /// 功法修炼树UI：按境界分行的节点树，绘制节点之间的连接线和星星进度。
    /// 每一行显示该境界下的节点，带节点互斥组合并。
    /// 同一互斥组的多层节点合并为1个item，解锁层数决定星星数量。
    /// 面板布局由 Prefab 提供，运行时只负责填充数据与刷新。
    /// </summary>
    public class CultivationTreeUIController : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject _abilityItemPrefab;

        [Header("Top Bar")]
        [SerializeField] private TMP_Dropdown _methodDropdown;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _comprehensionText;

        [Header("Active Info")]
        [SerializeField] private TextMeshProUGUI _activeMethodText;

        [Header("Tree")]
        [SerializeField] private RectTransform _treeContent;
        [SerializeField] private RectTransform _linesRoot;
        [SerializeField] private float _itemWidth = 90f;
        [SerializeField] private float _itemHeight = 90f;
        [SerializeField] private float _itemSpacingX = 15f;
        [SerializeField] private float _itemSpacingY = 20f;
        [SerializeField] private float _realmHeaderHeight = 28f;

        [Header("Detail")]
        [SerializeField] private Transform _detailContent;
        [SerializeField] private Button _actionButton;
        [SerializeField] private TextMeshProUGUI _actionButtonText;

        [Header("Buttons")]
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _activateButton; // 运转按钮
        [SerializeField] private TextMeshProUGUI _activateButtonText;

        private ICultivationSystem _cultSystem;
        private ICultivationModel _cultModel;
        private CultivationMethodData _selectedMethod;
        private CultivationNodeData _selectedNode;
        private AbilityItemController _selectedItem;

        private Dictionary<string, RectTransform> _itemRects = new Dictionary<string, RectTransform>();

        private void Awake()
        {
            AutoBindReferences();
            if (_methodDropdown) _methodDropdown.onValueChanged.AddListener(OnDropdownChanged);
            if (_closeButton) _closeButton.onClick.AddListener(Hide);
        }

        private void AutoBindReferences()
        {
            var panel = transform.Find("Panel");
            if (panel == null) return;

            var topBar = panel.Find("TopBar");
            if (topBar != null)
            {
                if (_methodDropdown == null) _methodDropdown = topBar.Find("MethodDropdown")?.GetComponent<TMP_Dropdown>();
                if (_titleText == null) _titleText = topBar.Find("TitleText")?.GetComponent<TextMeshProUGUI>();
                if (_comprehensionText == null) _comprehensionText = topBar.Find("ComprehensionPoints")?.GetComponent<TextMeshProUGUI>();
            }

            if (_activeMethodText == null) _activeMethodText = panel.Find("ActiveMethodLabel")?.GetComponent<TextMeshProUGUI>();

            var treeScroll = panel.Find("TreePanel/TreeScroll");
            if (treeScroll != null)
            {
                if (_treeContent == null) _treeContent = treeScroll.Find("Viewport/TreeContent")?.GetComponent<RectTransform>();
            }

            var detailScroll = panel.Find("DetailPanel/DetailScroll");
            if (detailScroll != null)
            {
                if (_detailContent == null) _detailContent = detailScroll.Find("DetailViewport/DetailContent")?.GetComponent<Transform>();
            }

            if (_actionButton == null) _actionButton = panel.Find("DetailPanel/ActionButton")?.GetComponent<Button>();
            if (_actionButtonText == null && _actionButton != null) _actionButtonText = _actionButton.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
            if (_closeButton == null) _closeButton = panel.Find("CloseButton")?.GetComponent<Button>();

            // 运转按钮
            if (_activateButton == null)
            {
                _activateButton = panel.Find("TopBar/ActivateButton")?.GetComponent<Button>();
                if (_activateButton == null) _activateButton = panel.Find("ActivateButton")?.GetComponent<Button>();
            }
            if (_activateButtonText == null && _activateButton != null)
                _activateButtonText = _activateButton.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
            if (_activateButton != null) _activateButton.onClick.AddListener(OnActivate);
        }

        private void OnActivate()
        {
            if (_selectedMethod == null) return;
            if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick);

            _cultSystem.SetActiveMethod(_selectedMethod.MethodId);
            RefreshActiveLabel();
            RefreshActivateButton();
            FloatingTip.ShowSuccess($"已运转: {_selectedMethod.MethodName}");
        }

        private void RefreshActivateButton()
        {
            if (_activateButton == null) return;
            bool isActive = _selectedMethod != null && _selectedMethod.MethodId == _cultModel.ActiveMethodId.Value;
            if (_activateButtonText != null)
                _activateButtonText.text = isActive ? "已运转中" : "运转";
            _activateButton.interactable = !isActive;
            var img = _activateButton.GetComponent<Image>();
            if (img != null)
                img.color = isActive ? new Color(0.15f, 0.3f, 0.15f) : new Color(0.08f, 0.5f, 0.1f);
        }

        private void OnDestroy()
        {
            if (_methodDropdown) _methodDropdown.onValueChanged.RemoveListener(OnDropdownChanged);
            if (_closeButton) _closeButton.onClick.RemoveListener(Hide);
        }

        public void Show()
        {
            _cultSystem = CardGameArchitecture.Interface.GetSystem<ICultivationSystem>();
            _cultModel = CardGameArchitecture.Interface.GetModel<ICultivationModel>();
            gameObject.SetActive(true);
            RefreshAll();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        #region Refresh
        private void OnDropdownChanged(int index)
        {
            var learned = _cultSystem.GetLearnedMethods();
            if (index >= 0 && index < learned.Count)
            {
                _selectedMethod = learned[index];
                RefreshNodeTree();
                RefreshActivateButton();
            }
        }

        private void RefreshAll()
        {
            RefreshDropdown();
            RefreshComprehension();
            RefreshActiveLabel();
            RefreshActivateButton();
            if (_selectedMethod == null)
            {
                var learned = _cultSystem.GetLearnedMethods();
                if (learned.Count > 0) _selectedMethod = learned[0];
            }
            RefreshNodeTree();
        }

        private void RefreshDropdown()
        {
            var learned = _cultSystem.GetLearnedMethods();
            _methodDropdown.ClearOptions();
            var options = new List<TMP_Dropdown.OptionData>();
            foreach (var m in learned)
                options.Add(new TMP_Dropdown.OptionData(m.MethodName + " [" + ElementText(m.Element) + "]"));
            _methodDropdown.AddOptions(options);

            int selIdx = learned.FindIndex(m => m.MethodId == _cultModel.ActiveMethodId.Value);
            if (selIdx >= 0)
            {
                _methodDropdown.SetValueWithoutNotify(selIdx);
                _selectedMethod = learned[selIdx];
            }
            else if (learned.Count > 0)
            {
                _methodDropdown.SetValueWithoutNotify(0);
                _selectedMethod = learned[0];
            }
        }

        private void RefreshComprehension()
        {
            if (_comprehensionText && _cultModel != null)
                _comprehensionText.text = "参悟点: " + _cultModel.ComprehensionPoints.Value;
        }

        private void RefreshActiveLabel()
        {
            if (_activeMethodText == null || _cultModel == null) return;
            var activeId = _cultModel.ActiveMethodId.Value;
            if (string.IsNullOrEmpty(activeId))
            {
                _activeMethodText.text = "当前装备: 无";
                _activeMethodText.color = new Color(0.7f, 0.7f, 0.75f);
            }
            else
            {
                var m = _cultSystem.GetMethodConfig(activeId);
                if (m != null)
                {
                    _activeMethodText.text = "当前装备: " + m.MethodName + " [" + ElementText(m.Element) + "]";
                    _activeMethodText.color = new Color(0.3f, 0.8f, 0.3f);
                }
            }
        }
        #endregion

        #region Node Merging
        /// <summary>
        /// 互斥组合并，无互斥的单独成组，按 gridIndex.y 排序。
        /// </summary>
        private List<List<CultivationNodeData>> MergeNodes(List<CultivationNodeData> realmNodes)
        {
            var result = new List<List<CultivationNodeData>>();
            var usedIds = new HashSet<string>();

            var mutexGroups = new Dictionary<string, List<CultivationNodeData>>();
            foreach (var node in realmNodes)
            {
                if (string.IsNullOrEmpty(node.MutexGroup)) continue;
                if (usedIds.Contains(node.NodeId)) continue;
                if (!mutexGroups.ContainsKey(node.MutexGroup))
                    mutexGroups[node.MutexGroup] = new List<CultivationNodeData>();
                mutexGroups[node.MutexGroup].Add(node);
                usedIds.Add(node.NodeId);
            }

            foreach (var kvp in mutexGroups)
            {
                kvp.Value.Sort((a, b) => a.GridIndex.y.CompareTo(b.GridIndex.y));
                result.Add(kvp.Value);
            }

            var standalone = new List<CultivationNodeData>();
            foreach (var node in realmNodes)
            {
                if (usedIds.Contains(node.NodeId)) continue;
                standalone.Add(node);
            }
            foreach (var node in standalone)
                result.Add(new List<CultivationNodeData> { node });

            result.Sort((a, b) => a[0].GridIndex.y.CompareTo(b[0].GridIndex.y));
            return result;
        }
        #endregion

        #region Tree
        private void RefreshNodeTree()
        {
            for (int i = _treeContent.childCount - 1; i >= 0; i--)
                Destroy(_treeContent.GetChild(i).gameObject);

            _itemRects.Clear();

            if (_selectedMethod == null)
            {
                var learned = _cultSystem.GetLearnedMethods();
                if (learned.Count > 0) _selectedMethod = learned[0];
            }
            if (_selectedMethod == null) { RefreshDetail(); return; }

            var realms = _selectedMethod.GetAvailableRealms();
            realms.Sort((a, b) => ((int)a).CompareTo((int)b));

            float currentY = 0f;
            float maxRowWidth = 0f;
            float minSpacing = _itemWidth + _itemSpacingX;

            // 记录每个境界的节点位置，供下一境界定位参考
            var realmPositions = new Dictionary<RealmLevel, List<(string nodeId, float x)>>();

            foreach (var realm in realms)
            {
                var nodes = _selectedMethod.GetNodesByRealm(realm);
                if (nodes == null || nodes.Count == 0) continue;

                var mergedGroups = MergeNodes(nodes);

                var headerGo = CreateHeader(_treeContent, realm, currentY);
                currentY += _realmHeaderHeight;

                int count = mergedGroups.Count;

                // === 有属性居中，无属性放两边，整体以 X=0 对称 ===
                float[] xPositions = new float[count];
                {
                    // 按元素属性分组：有显式元素的放中间，None 的放两边
                    var hasElement = new List<int>();
                    var noElement = new List<int>();
                    for (int i = 0; i < count; i++)
                    {
                        var el = mergedGroups[i][0].NodeElement;
                        if (el != ElementType.None)
                            hasElement.Add(i);
                        else
                            noElement.Add(i);
                    }

                    // 合并排列顺序：左无属性 → 中有属性 → 右无属性
                    int leftCount = noElement.Count / 2;
                    int rightCount = noElement.Count - leftCount;
                    var leftIds = noElement.GetRange(0, leftCount);
                    var rightIds = noElement.GetRange(leftCount, rightCount);

                    var orderedIds = new List<int>();
                    orderedIds.AddRange(leftIds);
                    orderedIds.AddRange(hasElement);
                    orderedIds.AddRange(rightIds);

                    // 等距排列，居中
                    float rowWidth = count * _itemWidth + (count - 1) * _itemSpacingX;
                    float startX = -rowWidth / 2f + _itemWidth / 2f;
                    for (int i = 0; i < count; i++)
                        xPositions[orderedIds[i]] = startX + i * minSpacing;
                }

                // === 创建 item ===
                var currentRealmPositions = new List<(string nodeId, float x)>();

                for (int i = 0; i < count; i++)
                {
                    var group = mergedGroups[i];
                    var primaryNode = group[0];

                    bool isUnlocked = _cultModel.UnlockedNodeIds.Contains(primaryNode.NodeId);
                    bool canUnlock = _cultSystem.CanUnlockNode(primaryNode.NodeId);
                    int advLevel = 0;
                    foreach (var n in group)
                        if (_cultModel.UnlockedNodeIds.Contains(n.NodeId))
                            advLevel++;

                    DivineAbilityData ability = null;

                    var element = primaryNode.NodeElement;

                    var itemGo = Instantiate(_abilityItemPrefab, _treeContent, false);
                    itemGo.name = "Node_" + primaryNode.NodeId;
                    var itemRt = itemGo.GetComponent<RectTransform>();
                    itemRt.anchorMin = new Vector2(0.5f, 1f); itemRt.anchorMax = new Vector2(0.5f, 1f);
                    itemRt.pivot = new Vector2(0.5f, 1f);
                    itemRt.anchoredPosition = new Vector2(xPositions[i], -currentY);
                    itemRt.sizeDelta = new Vector2(_itemWidth, _itemHeight);

                    var controller = itemGo.GetComponent<AbilityItemController>();
                    controller.Init(group, ability, element, isUnlocked, canUnlock, advLevel, OnNodeClicked);

                    foreach (var n in group)
                    {
                        _itemRects[n.NodeId] = itemRt;
                        currentRealmPositions.Add((n.NodeId, xPositions[i]));
                    }
                }

                realmPositions[realm] = currentRealmPositions;

                // 计算行宽
                if (count > 0)
                {
                    float minX = float.MaxValue, maxX = float.MinValue;
                    for (int i = 0; i < count; i++)
                    {
                        if (xPositions[i] < minX) minX = xPositions[i];
                        if (xPositions[i] > maxX) maxX = xPositions[i];
                    }
                    float rowWidth = maxX - minX + _itemWidth;
                    if (rowWidth > maxRowWidth) maxRowWidth = rowWidth;
                }

                currentY += _itemHeight + _itemSpacingY;
            }

            float contentWidth = Mathf.Max(maxRowWidth + 100, 800);
            float contentHeight = currentY + 50;
            _treeContent.sizeDelta = new Vector2(contentWidth, contentHeight);

            // Re-create lines root after items so it can be set as first sibling
            if (_linesRoot != null)
            {
                _linesRoot.SetParent(null);
                Destroy(_linesRoot.gameObject);
            }
            var linesGo = new GameObject("LinesRoot");
            linesGo.transform.SetParent(_treeContent, false);
            _linesRoot = linesGo.GetComponent<RectTransform>();
            if (_linesRoot == null) _linesRoot = linesGo.AddComponent<RectTransform>();
            _linesRoot.anchorMin = Vector2.zero; _linesRoot.anchorMax = Vector2.one;
            _linesRoot.offsetMin = Vector2.zero; _linesRoot.offsetMax = Vector2.zero;
            linesGo.transform.SetAsFirstSibling();

            StartCoroutine(DrawLinesNextFrame());
            RefreshDetail();
        }

        private GameObject CreateHeader(RectTransform parent, RealmLevel realm, float y)
        {
            var headerGo = new GameObject("Header_" + realm);
            headerGo.transform.SetParent(parent, false);
            var headerRt = headerGo.AddComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0.5f, 1f); headerRt.anchorMax = new Vector2(0.5f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.anchoredPosition = new Vector2(0, -y);
            headerRt.sizeDelta = new Vector2(600, _realmHeaderHeight);
            var headerTmp = headerGo.AddComponent<TextMeshProUGUI>();
            headerTmp.text = "═══ " + RealmText(realm) + " ═══";
            headerTmp.fontSize = 18; headerTmp.alignment = TextAlignmentOptions.Center;
            headerTmp.color = new Color(0.85f, 0.7f, 0.3f);
            return headerGo;
        }

        private System.Collections.IEnumerator DrawLinesNextFrame()
        {
            yield return null;
            DrawConnectionLines();
        }

        private void DrawConnectionLines()
        {
            if (_selectedMethod?.Nodes == null) return;

            foreach (var node in _selectedMethod.Nodes)
            {
                if (node.Prerequisites == null) continue;

                foreach (var preId in node.Prerequisites)
                {
                    if (!_itemRects.TryGetValue(preId, out var parentRt)) continue;
                    if (!_itemRects.TryGetValue(node.NodeId, out var childRt)) continue;
                    if (parentRt == childRt) continue;

                    DrawLine(parentRt, childRt, _cultModel.UnlockedNodeIds.Contains(preId));
                }
            }
        }

        private ElementType GetNodeElement(CultivationNodeData node)
        {
            return node.NodeElement;
        }

        private void DrawLine(RectTransform from, RectTransform to, bool isUnlocked)
        {
            var fromPos = from.anchoredPosition;
            var toPos = to.anchoredPosition;
            float fromBottomY = fromPos.y - _itemHeight;
            float toTopY = toPos.y;
            float midY = (fromBottomY + toTopY) / 2f;

            CreateLineSegment(fromPos.x, fromBottomY, fromPos.x, midY, isUnlocked);
            if (!Mathf.Approximately(fromPos.x, toPos.x))
                CreateLineSegment(fromPos.x, midY, toPos.x, midY, isUnlocked);
            CreateLineSegment(toPos.x, midY, toPos.x, toTopY, isUnlocked);
        }

        private void CreateLineSegment(float x1, float y1, float x2, float y2, bool isUnlocked)
        {
            float dx = x2 - x1;
            float dy = y2 - y1;
            float length = Mathf.Sqrt(dx * dx + dy * dy);
            if (length < 0.5f) return;

            float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
            float midX = (x1 + x2) / 2f;
            float midY = (y1 + y2) / 2f;

            var lineGo = new GameObject("Line");
            lineGo.transform.SetParent(_linesRoot, false);
            var lineRt = lineGo.AddComponent<RectTransform>();
            lineRt.anchorMin = new Vector2(0.5f, 1f); lineRt.anchorMax = new Vector2(0.5f, 1f);
            lineRt.pivot = new Vector2(0.5f, 0.5f);
            lineRt.anchoredPosition = new Vector2(midX, midY);
            lineRt.sizeDelta = new Vector2(length, 2f);
            lineRt.localRotation = Quaternion.Euler(0, 0, angle);
            var lineImg = lineGo.AddComponent<Image>();
            lineImg.color = isUnlocked ? new Color(0.7f, 0.55f, 0.1f, 0.8f) : new Color(0.3f, 0.3f, 0.35f, 0.5f);
        }

        private void OnNodeClicked(AbilityItemController item)
        {
            _selectedItem = item;
            _selectedNode = item.PrimaryNode;
            RefreshDetail();
        }
        #endregion

        #region Detail
        private void RefreshDetail()
        {
            // Destroy all detail text children
            for (int i = _detailContent.childCount - 1; i >= 0; i--)
                Destroy(_detailContent.GetChild(i).gameObject);

            if (_selectedNode == null)
            {
                AddDetailText("选择一个节点查看详情", 16, new Color(0.7f, 0.7f, 0.7f), false);
                return;
            }

            var node = _selectedNode;
            var mergedNodes = _selectedItem?.MergedNodes ?? new List<CultivationNodeData> { node };
            var isUnlocked = _cultModel.UnlockedNodeIds.Contains(node.NodeId);
            var canUnlock = _cultSystem.CanUnlockNode(node.NodeId);

            string displayName = node.NodeName;
            if (displayName.Length > 2 && (displayName.StartsWith("壹·") || displayName.StartsWith("贰·") || displayName.StartsWith("叁·")))
                displayName = displayName.Substring(2);
            AddDetailText("[" + displayName + "]", 20, new Color(0.9f, 0.85f, 0.6f), true);

            AddDetailText("分类: 修行  |  境界: " + RealmText(node.Realm), 14, new Color(0.7f, 0.75f, 0.8f), false);

            string buffText = GetNodeBuffText(node);
            if (!string.IsNullOrEmpty(buffText))
                AddDetailText(buffText, 14, new Color(0.9f, 0.6f, 0.2f), false);

            if (!string.IsNullOrEmpty(node.Description))
                AddDetailText(node.Description, 14, new Color(0.8f, 0.8f, 0.85f), false);

            string unlockInfo = "解锁方式: " + UnlockTypeText(node.UnlockType);
            if (node.ComprehensionCost > 0) unlockInfo += "  |  参悟消耗: " + node.ComprehensionCost;
            AddDetailText(unlockInfo, 13, new Color(0.6f, 0.65f, 0.7f), false);

            if (mergedNodes.Count > 1)
            {
                AddDetailText("", 8, Color.clear, false);
                AddDetailText("──── 功法进阶 ────", 15, new Color(0.7f, 0.6f, 0.3f), true);

                string[] cnNums = { "壹", "贰", "叁", "肆", "伍" };
                for (int i = 0; i < mergedNodes.Count; i++)
                {
                    var layerNode = mergedNodes[i];
                    bool layerUnlocked = _cultModel.UnlockedNodeIds.Contains(layerNode.NodeId);
                    string num = i < cnNums.Length ? cnNums[i] : (i + 1).ToString();

                    string layerName = layerNode.NodeName;
                    if (layerName.Length > 2 && (layerName.StartsWith("壹·") || layerName.StartsWith("贰·") || layerName.StartsWith("叁·")))
                        layerName = layerName.Substring(2);
                    Color titleColor = layerUnlocked ? new Color(0.9f, 0.8f, 0.3f) : new Color(0.4f, 0.4f, 0.45f);
                    AddDetailText(num + ". [" + layerName + "]" + (layerUnlocked ? " ✓" : ""), 14, titleColor, true);

                    if (!string.IsNullOrEmpty(layerNode.Description))
                        AddDetailText(layerNode.Description, 13, layerUnlocked ? new Color(0.75f, 0.75f, 0.8f) : new Color(0.4f, 0.4f, 0.45f), false);

                    string layerBuff = GetNodeBuffText(layerNode);
                    if (!string.IsNullOrEmpty(layerBuff))
                        AddDetailText(layerBuff, 13, layerUnlocked ? new Color(0.9f, 0.6f, 0.2f) : new Color(0.4f, 0.35f, 0.2f), false);
                }
            }

            if (_selectedMethod != null && _selectedMethod.MethodId == _cultModel.ActiveMethodId.Value)
                AddDetailText(">>> 当前装备功法 <<<", 14, new Color(0.3f, 0.8f, 0.3f), true);

            string statusText;
            Color btnColor;
            if (isUnlocked) { statusText = "已解锁"; btnColor = new Color(0.3f, 0.4f, 0.3f); }
            else if (canUnlock && node.UnlockType == NodeUnlockType.Comprehension) { statusText = "消耗参悟点解锁"; btnColor = new Color(0.08f, 0.55f, 0.1f); }
            else if (canUnlock && node.UnlockType == NodeUnlockType.Comprehension) { statusText = "消耗参悟点解锁"; btnColor = new Color(0.08f, 0.55f, 0.1f); }
            else if (canUnlock) { statusText = UnlockTypeText(node.UnlockType) + " (可尝试)"; btnColor = new Color(0.15f, 0.4f, 0.7f); }
            else { statusText = "前置未满足"; btnColor = new Color(0.25f, 0.25f, 0.3f); }

            if (_actionButton)
            {
                _actionButton.gameObject.SetActive(true);
                _actionButton.image.color = btnColor;
                if (_actionButtonText) _actionButtonText.text = statusText;
                _actionButton.onClick.RemoveAllListeners();
                if (canUnlock && node.UnlockType == NodeUnlockType.Comprehension)
                {
                    var captured = node;
                    _actionButton.onClick.AddListener(() =>
                    {
                        _cultSystem.UnlockNode(captured.NodeId);
                        RefreshComprehension();
                        RefreshNodeTree();
                    });
                }
            }
        }

        private static string GetNodeBuffText(CultivationNodeData node)
        {
            var parts = new List<string>();
            if (node.RewardType == NodeRewardType.PassiveStat && node.PassiveStat != PassiveStatType.None)
                parts.Add(PassiveStatText(node.PassiveStat) + " +" + node.PassiveValue);
            if (node.RewardType == NodeRewardType.CraftBonus && node.CraftBonusType != CraftBonusType.None)
                parts.Add(CraftBonusText(node.CraftBonusType) + " +" + node.CraftBonusValue);
            if (node.RewardType == NodeRewardType.Card && node.RewardIds != null && node.RewardIds.Count > 0)
            {
                // 按卡牌ID分组，显示"中文名×数量"，不同卡牌换行
                var grouped = node.RewardIds
                    .GroupBy(id => id)
                    .Select(g => $"{GetCardDisplayName(g.Key)}×{g.Count()}");
                parts.Add("卡牌:\n" + string.Join("\n", grouped));
            }
            if (node.RewardType == NodeRewardType.Recipe && node.RewardIds != null && node.RewardIds.Count > 0)
            {
                var grouped = node.RewardIds
                    .GroupBy(id => id)
                    .Select(g => $"{GetRecipeDisplayName(g.Key)}×{g.Count()}");
                parts.Add("丹方:\n" + string.Join("\n", grouped));
            }
            return parts.Count > 0 ? string.Join("\n", parts) : "";
        }

        private static string GetCardDisplayName(string cardId)
        {
            var cards = ResourceCache.GetCardsFromAllList();
            if (cards == null) return "未知";
            var card = cards.Find(c => c.Id == cardId);
            return card != null ? card.CardName : "未知";
        }

        private static string GetRecipeDisplayName(string recipeId)
        {
            var recipes = ResourceCache.GetRecipes();
            if (recipes == null) return "未知";
            var recipe = recipes.Find(r => r.recipeId == recipeId);
            return recipe != null ? recipe.name : "未知";
        }

        private static string PassiveStatText(PassiveStatType t) => t switch
        {
            PassiveStatType.MaxHP => "生命上限", PassiveStatType.ShenShi => "神识上限",
            PassiveStatType.MaxMana => "灵力上限", _ => "?"
        };

        private static string CraftBonusText(CraftBonusType t) => t switch
        {
            CraftBonusType.AlchemySuccess => "炼丹成功率", CraftBonusType.AlchemyQuality => "炼丹品质",
            CraftBonusType.ForgingSuccess => "炼器成功率", CraftBonusType.ForgingQuality => "炼器品质",
            _ => "?"
        };

        private void AddDetailText(string text, int fontSize, Color color, bool bold)
        {
            var go = new GameObject("DetailText");
            go.transform.SetParent(_detailContent, false);
            go.AddComponent<LayoutElement>().flexibleWidth = 1;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = fontSize; tmp.color = color;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            if (bold) tmp.fontStyle = FontStyles.Bold;
            var csf = go.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
        #endregion

        #region Helpers
        private static string ElementText(ElementType el) => el switch
        {
            ElementType.Metal => "金", ElementType.Wood => "木", ElementType.Water => "水",
            ElementType.Fire => "火", ElementType.Earth => "土", ElementType.Sword => "剑",
            ElementType.Wind => "风", ElementType.Thunder => "雷", ElementType.Ghost => "鬼",
            _ => "无"
        };

        private static string RealmText(RealmLevel r) => r switch
        {
            RealmLevel.LianQi => "练气期", RealmLevel.ZhuJi => "筑基期", RealmLevel.JinDan => "结丹期",
            RealmLevel.YuanYing => "元婴期", RealmLevel.HuaShen => "化神期", RealmLevel.DuJie => "渡劫期",
            _ => "?"
        };

        private static string UnlockTypeText(NodeUnlockType t) => t switch
        {
            NodeUnlockType.Comprehension => "参悟", NodeUnlockType.Minigame => "小游戏",
            NodeUnlockType.Material => "材料", NodeUnlockType.CombatTrigger => "战斗触发",
            _ => "?"
        };
        #endregion
    }
}
