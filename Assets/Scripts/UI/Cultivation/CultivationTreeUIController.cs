using System;
using System.Collections.Generic;
using CardGame;
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
    /// </summary>
    public class CultivationTreeUIController : MonoBehaviour
    {
        private ICultivationSystem _cultSystem;
        private ICultivationModel _cultModel;
        private TMP_FontAsset _font;

        private Canvas _canvas;
        private GameObject _panel;
        private RectTransform _treeContent;
        private RectTransform _linesRoot;
        private Transform _detailPanel;
        private TextMeshProUGUI _comprehensionText;
        private TMP_Dropdown _methodDropdown;
        private TextMeshProUGUI _activeMethodText;
        private CultivationMethodData _selectedMethod;
        private CultivationNodeData _selectedNode;
        private AbilityItemController _selectedItem;

        private Dictionary<string, RectTransform> _itemRects = new Dictionary<string, RectTransform>();
        private float _itemWidth = 90f;
        private float _itemHeight = 90f;
        private float _itemSpacingX = 15f;
        private float _itemSpacingY = 20f;
        private float _realmHeaderHeight = 28f;

        public void Show()
        {
            _cultSystem = CardGameArchitecture.Interface.GetSystem<ICultivationSystem>();
            _cultModel = CardGameArchitecture.Interface.GetModel<ICultivationModel>();
            _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

            if (_canvas != null && _canvas.gameObject != null)
            {
                _canvas.gameObject.SetActive(true);
                RefreshAll();
                return;
            }
            BuildUI();
            RefreshAll();
        }

        public void Hide()
        {
            if (_canvas != null) _canvas.gameObject.SetActive(false);
        }

        #region Build UI
        private void BuildUI()
        {
            _canvas = gameObject.GetComponent<Canvas>();
            if (_canvas == null)
            {
                _canvas = gameObject.AddComponent<Canvas>();
                var scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                gameObject.AddComponent<GraphicRaycaster>();
            }
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 80;

            var bg = NewChild("BG", transform);
            bg.AddComponent<Image>().color = new Color(0.04f, 0.04f, 0.08f, 0.97f);
            Stretch(bg);

            _panel = NewChild("Panel", transform);
            var panelRt = _panel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.03f, 0.03f);
            panelRt.anchorMax = new Vector2(0.97f, 0.97f);
            panelRt.offsetMin = Vector2.zero; panelRt.offsetMax = Vector2.zero;
            _panel.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.95f);

            BuildTopBar();
            BuildTreePanel();
            BuildDetailPanel();
            BuildCloseButton();
        }

        private void BuildTopBar()
        {
            var topBar = NewChild("TopBar", _panel.transform);
            var topRt = topBar.GetComponent<RectTransform>();
            topRt.anchorMin = new Vector2(0, 1); topRt.anchorMax = Vector2.one;
            topRt.pivot = new Vector2(0.5f, 1f);
            topRt.offsetMin = new Vector2(0, -45); topRt.offsetMax = Vector2.zero;

            // Dropdown
            var ddGo = NewChild("MethodDropdown", topBar.transform);
            var ddRt = ddGo.GetComponent<RectTransform>();
            ddRt.anchorMin = new Vector2(0, 0); ddRt.anchorMax = new Vector2(0.28f, 1);
            ddRt.offsetMin = new Vector2(10, 4); ddRt.offsetMax = new Vector2(-5, -4);
            ddGo.AddComponent<Image>().color = new Color(0.12f, 0.15f, 0.2f, 0.95f);
            _methodDropdown = ddGo.AddComponent<TMP_Dropdown>();

            var ddLabel = NewChild("Label", ddGo.transform);
            var ddLabelRt = ddLabel.GetComponent<RectTransform>();
            ddLabelRt.anchorMin = new Vector2(0, 0); ddLabelRt.anchorMax = new Vector2(0.8f, 1);
            ddLabelRt.offsetMin = new Vector2(10, 0); ddLabelRt.offsetMax = new Vector2(-25, 0);
            var ddLabelText = ddLabel.AddComponent<TextMeshProUGUI>();
            ddLabelText.fontSize = 16; ddLabelText.alignment = TextAlignmentOptions.Left;
            ddLabelText.color = new Color(0.85f, 0.8f, 0.5f);
            if (_font) ddLabelText.font = _font;
            _methodDropdown.captionText = ddLabelText;

            var ddArrow = NewChild("Arrow", ddGo.transform);
            var ddArrowRt = ddArrow.GetComponent<RectTransform>();
            ddArrowRt.anchorMin = new Vector2(0.82f, 0.3f); ddArrowRt.anchorMax = new Vector2(0.98f, 0.7f);
            ddArrowRt.offsetMin = Vector2.zero; ddArrowRt.offsetMax = Vector2.zero;
            var ddArrowText = ddArrow.AddComponent<TextMeshProUGUI>();
            ddArrowText.text = "V"; ddArrowText.fontSize = 14;
            ddArrowText.alignment = TextAlignmentOptions.Center;
            if (_font) ddArrowText.font = _font;

            var ddTemplate = NewChild("Template", ddGo.transform);
            var ddTemplateRt = ddTemplate.GetComponent<RectTransform>();
            ddTemplateRt.anchorMin = new Vector2(0, 0); ddTemplateRt.anchorMax = new Vector2(1, 0);
            ddTemplateRt.pivot = new Vector2(0.5f, 1f);
            ddTemplateRt.offsetMin = new Vector2(0, -200); ddTemplateRt.offsetMax = Vector2.zero;
            ddTemplate.AddComponent<Image>().color = new Color(0.1f, 0.12f, 0.18f, 0.98f);
            var ddScroll = ddTemplate.AddComponent<ScrollRect>();
            ddScroll.horizontal = false; ddScroll.vertical = true;

            var ddViewport = NewChild("Viewport", ddTemplate.transform);
            var ddvpRt = ddViewport.GetComponent<RectTransform>();
            ddvpRt.anchorMin = Vector2.zero; ddvpRt.anchorMax = Vector2.one;
            ddvpRt.offsetMin = Vector2.zero; ddvpRt.offsetMax = Vector2.zero;
            ddViewport.AddComponent<RectMask2D>();
            ddScroll.viewport = ddvpRt;

            var ddContent = NewChild("Content", ddViewport.transform);
            var ddceRt = ddContent.GetComponent<RectTransform>();
            ddceRt.anchorMin = new Vector2(0, 1); ddceRt.anchorMax = Vector2.one;
            ddceRt.pivot = new Vector2(0.5f, 1f);
            ddceRt.offsetMin = Vector2.zero; ddceRt.offsetMax = Vector2.zero;
            ddScroll.content = ddceRt;

            var ddItem = NewChild("Item", ddContent.transform);
            var ddItemRt = ddItem.GetComponent<RectTransform>();
            ddItemRt.anchorMin = new Vector2(0, 0.5f); ddItemRt.anchorMax = new Vector2(1, 0.5f);
            ddItemRt.pivot = new Vector2(0.5f, 0.5f);
            ddItemRt.offsetMin = new Vector2(0, -15); ddItemRt.offsetMax = new Vector2(0, 15);
            var ddItemToggle = ddItem.AddComponent<Toggle>();

            var ddItemBg = NewChild("ItemBackground", ddItem.transform);
            var ddItemBgRt = ddItemBg.GetComponent<RectTransform>();
            ddItemBgRt.anchorMin = Vector2.zero; ddItemBgRt.anchorMax = Vector2.one;
            ddItemBgRt.offsetMin = new Vector2(5, 0); ddItemBgRt.offsetMax = new Vector2(-5, 0);
            var ddItemBgImg = ddItemBg.AddComponent<Image>();
            ddItemBgImg.color = new Color(0.15f, 0.18f, 0.25f, 0.8f);

            var ddItemCm = NewChild("ItemCheckmark", ddItem.transform);
            var ddItemCmRt = ddItemCm.GetComponent<RectTransform>();
            ddItemCmRt.anchorMin = new Vector2(0, 0); ddItemCmRt.anchorMax = new Vector2(0, 1);
            ddItemCmRt.pivot = new Vector2(0f, 0.5f);
            ddItemCmRt.offsetMin = new Vector2(5, 0); ddItemCmRt.offsetMax = new Vector2(25, 0);
            var ddItemCmText = ddItemCm.AddComponent<TextMeshProUGUI>();
            ddItemCmText.text = ">"; ddItemCmText.fontSize = 14; ddItemCmText.color = new Color(0.8f, 0.7f, 0.2f);
            if (_font) ddItemCmText.font = _font;
            ddItemToggle.graphic = ddItemCmText;
            ddItemToggle.targetGraphic = ddItemBgImg;

            var ddItemLabel = NewChild("ItemLabel", ddItem.transform);
            var ddItemLabelRt = ddItemLabel.GetComponent<RectTransform>();
            ddItemLabelRt.anchorMin = Vector2.zero; ddItemLabelRt.anchorMax = Vector2.one;
            ddItemLabelRt.offsetMin = new Vector2(30, 0); ddItemLabelRt.offsetMax = new Vector2(-5, 0);
            var ddItemLabelText = ddItemLabel.AddComponent<TextMeshProUGUI>();
            ddItemLabelText.fontSize = 16; ddItemLabelText.color = new Color(0.85f, 0.85f, 0.9f);
            if (_font) ddItemLabelText.font = _font;

            _methodDropdown.template = ddTemplateRt;
            _methodDropdown.itemText = ddItemLabelText;
            ddTemplate.SetActive(false);
            _methodDropdown.onValueChanged.AddListener(OnDropdownChanged);

            // Title
            var title = NewText(topBar.transform, "功法修炼", 22, Vector2.zero, TextAnchor.MiddleCenter, _font);
            var titleRt = title.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.28f, 0); titleRt.anchorMax = new Vector2(0.7f, 1);
            titleRt.offsetMin = Vector2.zero; titleRt.offsetMax = Vector2.zero;
            title.fontStyle = FontStyles.Bold; title.color = new Color(0.85f, 0.8f, 0.5f);

            // Comprehension
            var compGo = NewChild("ComprehensionPoints", topBar.transform);
            var compRt = compGo.GetComponent<RectTransform>();
            compRt.anchorMin = new Vector2(0.7f, 0); compRt.anchorMax = Vector2.one;
            compRt.offsetMin = new Vector2(10, 4); compRt.offsetMax = new Vector2(-10, -4);
            _comprehensionText = compGo.AddComponent<TextMeshProUGUI>();
            _comprehensionText.fontSize = 18; _comprehensionText.alignment = TextAlignmentOptions.Right;
            _comprehensionText.color = new Color(0.9f, 0.8f, 0.3f);
            if (_font) _comprehensionText.font = _font;

            // Active label
            var activeGo = NewChild("ActiveMethodLabel", _panel.transform);
            var activeRt = activeGo.GetComponent<RectTransform>();
            activeRt.anchorMin = new Vector2(0.01f, 0.91f); activeRt.anchorMax = new Vector2(0.5f, 0.96f);
            activeRt.offsetMin = Vector2.zero; activeRt.offsetMax = Vector2.zero;
            _activeMethodText = activeGo.AddComponent<TextMeshProUGUI>();
            _activeMethodText.fontSize = 15; _activeMethodText.alignment = TextAlignmentOptions.Left;
            _activeMethodText.color = new Color(0.3f, 0.8f, 0.3f);
            if (_font) _activeMethodText.font = _font;
        }

        private void BuildTreePanel()
        {
            var treePanel = NewChild("TreePanel", _panel.transform);
            var treeRt = treePanel.GetComponent<RectTransform>();
            treeRt.anchorMin = new Vector2(0.01f, 0.02f); treeRt.anchorMax = new Vector2(0.62f, 0.88f);
            treeRt.offsetMin = Vector2.zero; treeRt.offsetMax = Vector2.zero;
            treePanel.AddComponent<Image>().color = new Color(0.06f, 0.06f, 0.1f, 0.9f);

            var scrollObj = NewChild("TreeScroll", treePanel.transform);
            var scrollRt = scrollObj.GetComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero; scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(5, 5); scrollRt.offsetMax = new Vector2(-5, -5);
            var scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.horizontal = true; scroll.vertical = true;

            var viewport = NewChild("Viewport", scrollObj.transform);
            var vpRt = viewport.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero; vpRt.offsetMax = Vector2.zero;
            viewport.AddComponent<RectMask2D>();
            scroll.viewport = vpRt;

            var content = NewChild("TreeContent", viewport.transform);
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0.5f, 1f); contentRt.anchorMax = new Vector2(0.5f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.offsetMin = Vector2.zero; contentRt.offsetMax = Vector2.zero;
            scroll.content = contentRt;
            _treeContent = contentRt;
        }

        private void BuildDetailPanel()
        {
            var dpGo = NewChild("DetailPanel", _panel.transform);
            var dpRt = dpGo.GetComponent<RectTransform>();
            dpRt.anchorMin = new Vector2(0.63f, 0.02f); dpRt.anchorMax = new Vector2(0.98f, 0.88f);
            dpRt.offsetMin = Vector2.zero; dpRt.offsetMax = Vector2.zero;
            dpGo.AddComponent<Image>().color = new Color(0.08f, 0.1f, 0.14f, 0.95f);

            var dpScrollObj = NewChild("DetailScroll", dpGo.transform);
            var dpsRt = dpScrollObj.GetComponent<RectTransform>();
            dpsRt.anchorMin = Vector2.zero; dpsRt.anchorMax = Vector2.one;
            dpsRt.offsetMin = new Vector2(5, 45); dpsRt.offsetMax = new Vector2(-5, -5);
            var dpScroll = dpScrollObj.AddComponent<ScrollRect>();
            dpScroll.horizontal = false; dpScroll.vertical = true;

            var dpViewport = NewChild("DetailViewport", dpScrollObj.transform);
            var dpvpRt = dpViewport.GetComponent<RectTransform>();
            dpvpRt.anchorMin = Vector2.zero; dpvpRt.anchorMax = Vector2.one;
            dpvpRt.offsetMin = Vector2.zero; dpvpRt.offsetMax = Vector2.zero;
            dpViewport.AddComponent<RectMask2D>();
            dpScroll.viewport = dpvpRt;

            var dpContent = NewChild("DetailContent", dpViewport.transform);
            var dpceRt = dpContent.GetComponent<RectTransform>();
            _detailPanel = dpContent.transform;
            dpceRt.anchorMin = new Vector2(0, 1); dpceRt.anchorMax = Vector2.one;
            dpceRt.pivot = new Vector2(0.5f, 1f);
            dpceRt.offsetMin = Vector2.zero; dpceRt.offsetMax = Vector2.zero;
            var dpVlg = dpContent.AddComponent<VerticalLayoutGroup>();
            dpVlg.spacing = 6; dpVlg.childControlHeight = true; dpVlg.childForceExpandWidth = true;
            dpVlg.childForceExpandHeight = false; dpVlg.childAlignment = TextAnchor.UpperLeft;
            dpVlg.padding = new RectOffset(8, 8, 8, 8);
            var dpCsf = dpContent.AddComponent<ContentSizeFitter>();
            dpCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            dpScroll.content = dpceRt;
        }

        private void BuildCloseButton()
        {
            var closeBtn = NewChild("CloseButton", _panel.transform);
            var cbRt = closeBtn.GetComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(0.63f, 0.02f); cbRt.anchorMax = new Vector2(0.98f, 0.02f);
            cbRt.pivot = new Vector2(0.5f, 0f);
            cbRt.offsetMin = new Vector2(0, 5); cbRt.offsetMax = new Vector2(0, 38);
            closeBtn.AddComponent<Image>().color = new Color(0.55f, 0.15f, 0.15f, 0.9f);
            NewText(closeBtn.transform, "关闭", 18, Vector2.zero, TextAnchor.MiddleCenter, _font);
            closeBtn.AddComponent<Button>().onClick.AddListener(Hide);
        }
        #endregion

        #region Node Merging
        /// <summary>
        /// 将同一互斥组的节点合并为一组，每层对应一个星星。
        /// 无互斥组的节点单独成组。
        /// </summary>
        private List<List<CultivationNodeData>> MergeNodes(List<CultivationNodeData> realmNodes)
        {
            var result = new List<List<CultivationNodeData>>();
            var usedIds = new HashSet<string>();

            // 先按互斥组分组
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

            // 按 Position.y 排序互斥组内节点（壹->贰->叁）
            foreach (var kvp in mutexGroups)
            {
                kvp.Value.Sort((a, b) => a.Position.y.CompareTo(b.Position.y));
                result.Add(kvp.Value);
            }

            // 再处理无互斥组节点，按 Position.x 排序
            var standalone = new List<CultivationNodeData>();
            foreach (var node in realmNodes)
            {
                if (usedIds.Contains(node.NodeId)) continue;
                standalone.Add(node);
            }
            standalone.Sort((a, b) => a.Position.x.CompareTo(b.Position.x));
            foreach (var node in standalone)
                result.Add(new List<CultivationNodeData> { node });

            // 按每组首节点的 Position.x 排序整组顺序
            result.Sort((a, b) => a[0].Position.x.CompareTo(b[0].Position.x));

            return result;
        }
        #endregion

        #region Refresh
        private void OnDropdownChanged(int index)
        {
            var learned = _cultSystem.GetLearnedMethods();
            if (index >= 0 && index < learned.Count)
            {
                _selectedMethod = learned[index];
                RefreshNodeTree();
            }
        }

        private void RefreshAll()
        {
            RefreshDropdown();
            RefreshComprehension();
            RefreshActiveLabel();
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
                _activeMethodText.color = new Color(0.6f, 0.3f, 0.3f);
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

        private void RefreshNodeTree()
        {
            for (int i = _treeContent.childCount - 1; i >= 0; i--)
                Destroy(_treeContent.GetChild(i).gameObject);

            // 连线层（先创建以便被节点覆盖）
            var linesGo = NewChild("LinesRoot", _treeContent);
            _linesRoot = linesGo.GetComponent<RectTransform>();
            _linesRoot.anchorMin = Vector2.zero; _linesRoot.anchorMax = Vector2.one;
            _linesRoot.offsetMin = Vector2.zero; _linesRoot.offsetMax = Vector2.zero;
            linesGo.transform.SetAsFirstSibling();

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

            foreach (var realm in realms)
            {
                var nodes = _selectedMethod.GetNodesByRealm(realm);
                if (nodes == null || nodes.Count == 0) continue;

                // 合并节点分组
                var mergedGroups = MergeNodes(nodes);

                // 创建境界标题
                var headerGo = NewChild("Header_" + realm, _treeContent);
                var headerRt = headerGo.GetComponent<RectTransform>();
                headerRt.anchorMin = new Vector2(0.5f, 1f); headerRt.anchorMax = new Vector2(0.5f, 1f);
                headerRt.pivot = new Vector2(0.5f, 1f);
                headerRt.anchoredPosition = new Vector2(0, -currentY);
                headerRt.sizeDelta = new Vector2(600, _realmHeaderHeight);
                var headerTmp = headerGo.AddComponent<TextMeshProUGUI>();
                headerTmp.text = "═══ " + RealmText(realm) + " ═══";
                headerTmp.fontSize = 18; headerTmp.alignment = TextAlignmentOptions.Center;
                headerTmp.color = new Color(0.85f, 0.7f, 0.3f);
                if (_font) headerTmp.font = _font;
                currentY += _realmHeaderHeight;

                // 创建该行所有合并节点 item
                int count = mergedGroups.Count;
                float rowWidth = count * _itemWidth + (count - 1) * _itemSpacingX;
                if (rowWidth > maxRowWidth) maxRowWidth = rowWidth;
                float startX = -rowWidth / 2f + _itemWidth / 2f;

                for (int i = 0; i < count; i++)
                {
                    var group = mergedGroups[i];
                    var primaryNode = group[0];

                    // 判断节点解锁状态
                    bool isUnlocked = _cultModel.UnlockedNodeIds.Contains(primaryNode.NodeId);
                    bool canUnlock = _cultSystem.CanUnlockNode(primaryNode.NodeId);
                    int advLevel = 0;
                    foreach (var n in group)
                        if (_cultModel.UnlockedNodeIds.Contains(n.NodeId))
                            advLevel++;

                    // 神通数据优先，否则为 null
                    DivineAbilityData ability = null;
                    if (primaryNode.RewardType == NodeRewardType.DivineAbility && primaryNode.RewardIds != null && primaryNode.RewardIds.Count > 0)
                        ability = _cultSystem.GetAbilityConfig(primaryNode.RewardIds[0]);

                    // 元素：神通元素优先，否则使用功法主元素
                    ElementType element = ability?.Element ?? ElementType.None;
                    if (element == ElementType.None) element = _selectedMethod.Element;

                    var itemGo = NewChild("Node_" + primaryNode.NodeId, _treeContent);
                    var itemRt = itemGo.GetComponent<RectTransform>();
                    itemRt.anchorMin = new Vector2(0.5f, 1f); itemRt.anchorMax = new Vector2(0.5f, 1f);
                    itemRt.pivot = new Vector2(0.5f, 1f);
                    itemRt.anchoredPosition = new Vector2(startX + i * (_itemWidth + _itemSpacingX), -currentY);
                    itemRt.sizeDelta = new Vector2(_itemWidth, _itemHeight);

                    var controller = itemGo.AddComponent<AbilityItemController>();
                    controller.Init(group, ability, element, isUnlocked, canUnlock,
                        advLevel, _font, OnNodeClicked);

                    // 记录每个节点ID对应的 RectTransform（同一合并组共享），用于绘制连线
                    foreach (var n in group)
                        _itemRects[n.NodeId] = itemRt;
                }

                currentY += _itemHeight + _itemSpacingY;
            }

            var contentRt = _treeContent;
            float contentWidth = Mathf.Max(maxRowWidth + 100, 800);
            float contentHeight = currentY + 50;
            contentRt.sizeDelta = new Vector2(contentWidth, contentHeight);

            StartCoroutine(DrawLinesNextFrame());
            RefreshDetail();
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
                // 无元素节点不绘制连线
                ElementType nodeElement = GetNodeElement(node);
                if (nodeElement == ElementType.None) continue;

                foreach (var preId in node.Prerequisites)
                {
                    if (!_itemRects.TryGetValue(preId, out var parentRt)) continue;
                    if (!_itemRects.TryGetValue(node.NodeId, out var childRt)) continue;

                    // 父节点也必须是有元素属性才绘制连线
                    var parentNode = _selectedMethod.GetNode(preId);
                    if (parentNode == null) continue;
                    ElementType parentElement = GetNodeElement(parentNode);
                    if (parentElement == ElementType.None) continue;

                    // 同一个合并item内部节点不画线
                    if (parentRt == childRt) continue;

                    DrawLine(parentRt, childRt, _cultModel.UnlockedNodeIds.Contains(preId));
                }
            }
        }

        private ElementType GetNodeElement(CultivationNodeData node)
        {
            // 神通节点取神通自身的元素
            if (node.RewardType == NodeRewardType.DivineAbility && node.RewardIds != null && node.RewardIds.Count > 0)
            {
                var ability = _cultSystem.GetAbilityConfig(node.RewardIds[0]);
                if (ability != null) return ability.Element;
            }
            // 否则使用功法主元素
            return _selectedMethod?.Element ?? ElementType.None;
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
            for (int i = _detailPanel.childCount - 1; i >= 0; i--)
                Destroy(_detailPanel.GetChild(i).gameObject);

            if (_selectedNode == null)
            {
                AddDetailText("选择一个节点查看详情", 16, new Color(0.7f, 0.7f, 0.7f), false);
                return;
            }

            var node = _selectedNode;
            var mergedNodes = _selectedItem?.MergedNodes ?? new System.Collections.Generic.List<CultivationNodeData> { node };
            var isUnlocked = _cultModel.UnlockedNodeIds.Contains(node.NodeId);
            var canUnlock = _cultSystem.CanUnlockNode(node.NodeId);

            // 技能名
            string displayName = node.NodeName;
            if (displayName.Length > 2 && (displayName.StartsWith("壹·") || displayName.StartsWith("贰·") || displayName.StartsWith("叁·")))
                displayName = displayName.Substring(2);
            AddDetailText("[" + displayName + "]", 20, new Color(0.9f, 0.85f, 0.6f), true);

            // 分类 + 境界
            AddDetailText("分类: 修行  |  境界: " + RealmText(node.Realm), 14, new Color(0.7f, 0.75f, 0.8f), false);

            // 属性加成
            string buffText = GetNodeBuffText(node);
            if (!string.IsNullOrEmpty(buffText))
                AddDetailText(buffText, 14, new Color(0.9f, 0.6f, 0.2f), false);

            // 描述
            if (!string.IsNullOrEmpty(node.Description))
                AddDetailText(node.Description, 14, new Color(0.8f, 0.8f, 0.85f), false);

            // 解锁信息
            string unlockInfo = "解锁方式: " + UnlockTypeText(node.UnlockType);
            if (node.ComprehensionCost > 0) unlockInfo += "  |  参悟消耗: " + node.ComprehensionCost;
            AddDetailText(unlockInfo, 13, new Color(0.6f, 0.65f, 0.7f), false);

            // 进阶详情
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

            // 当前装备标记
            if (_selectedMethod != null && _selectedMethod.MethodId == _cultModel.ActiveMethodId.Value)
                AddDetailText(">>> 当前装备功法 <<<", 14, new Color(0.3f, 0.8f, 0.3f), true);

            // 操作按钮
            string statusText;
            Color btnColor;
            if (isUnlocked) { statusText = "已解锁"; btnColor = new Color(0.3f, 0.4f, 0.3f); }
            else if (canUnlock && node.UnlockType == NodeUnlockType.Comprehension) { statusText = "消耗参悟点解锁"; btnColor = new Color(0.08f, 0.55f, 0.1f); }
            else if (canUnlock && node.UnlockType == NodeUnlockType.MutualExclusion) { statusText = "选择此路线"; btnColor = new Color(0.08f, 0.55f, 0.1f); }
            else if (canUnlock) { statusText = UnlockTypeText(node.UnlockType) + " (可尝试)"; btnColor = new Color(0.15f, 0.4f, 0.7f); }
            else { statusText = "前置未满足"; btnColor = new Color(0.25f, 0.25f, 0.3f); }

            var actionBtn = NewChild("ActionButton", _detailPanel);
            var abLe = actionBtn.AddComponent<LayoutElement>();
            abLe.preferredHeight = 34; abLe.flexibleWidth = 1;
            actionBtn.AddComponent<Image>().color = btnColor;
            var abText = NewText(actionBtn.transform, statusText, 15, Vector2.zero, TextAnchor.MiddleCenter, _font);
            var abTextRt = abText.GetComponent<RectTransform>();
            abTextRt.anchorMin = Vector2.zero; abTextRt.anchorMax = Vector2.one;
            abTextRt.offsetMin = Vector2.zero; abTextRt.offsetMax = Vector2.zero;

            if (canUnlock && (node.UnlockType == NodeUnlockType.Comprehension || node.UnlockType == NodeUnlockType.MutualExclusion))
            {
                var btn = actionBtn.AddComponent<Button>();
                var captured = node;
                btn.onClick.AddListener(() =>
                {
                    _cultSystem.UnlockNode(captured.NodeId);
                    RefreshComprehension();
                    RefreshNodeTree();
                });
            }
        }

        private static string GetNodeBuffText(CultivationNodeData node)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (node.RewardType == NodeRewardType.PassiveStat && node.PassiveStat != PassiveStatType.None)
                parts.Add(PassiveStatText(node.PassiveStat) + " +" + node.PassiveValue);
            if (node.RewardType == NodeRewardType.CraftBonus && node.CraftBonusType != CraftBonusType.None)
                parts.Add(CraftBonusText(node.CraftBonusType) + " +" + node.CraftBonusValue);
            if (node.RewardType == NodeRewardType.Card && node.RewardIds != null && node.RewardIds.Count > 0)
                parts.Add("卡牌: " + string.Join(",", node.RewardIds));
            if (node.RewardType == NodeRewardType.DivineAbility && node.RewardIds != null && node.RewardIds.Count > 0)
                parts.Add("神通: " + string.Join(",", node.RewardIds));
            if (node.RewardType == NodeRewardType.Recipe && node.RewardIds != null && node.RewardIds.Count > 0)
                parts.Add("丹方: " + string.Join(",", node.RewardIds));
            return parts.Count > 0 ? string.Join("  ", parts) : "";
        }

        private static string PassiveStatText(PassiveStatType t) => t switch
        {
            PassiveStatType.MaxHP => "生命上限", PassiveStatType.ShenShi => "神识",
            PassiveStatType.Strength => "力量", PassiveStatType.Dexterity => "敏捷",
            PassiveStatType.DrawCount => "抽牌数", PassiveStatType.MaxMana => "灵力上限",
            PassiveStatType.BlockStart => "初始格挡", _ => "?"
        };

        private static string CraftBonusText(CraftBonusType t) => t switch
        {
            CraftBonusType.AlchemySuccess => "炼丹成功率", CraftBonusType.AlchemyQuality => "炼丹品质",
            CraftBonusType.ForgingSuccess => "炼器成功率", CraftBonusType.ForgingQuality => "炼器品质",
            _ => "?"
        };

        private void AddDetailText(string text, int fontSize, Color color, bool bold)
        {
            var go = NewChild("DetailText", _detailPanel);
            go.AddComponent<LayoutElement>().flexibleWidth = 1;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = fontSize; tmp.color = color;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            if (bold) tmp.fontStyle = FontStyles.Bold;
            if (_font) tmp.font = _font;
            var csf = go.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
        #endregion

        #region Helpers
        private static GameObject NewChild(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private static void Stretch(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private static TextMeshProUGUI NewText(Transform parent, string text, int fontSize, Vector2 pos, TextAnchor anchor, TMP_FontAsset font)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(pos.x, -pos.y - 30); rt.offsetMax = new Vector2(-pos.x, -pos.y);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = fontSize;
            tmp.alignment = anchor == TextAnchor.UpperLeft ? TextAlignmentOptions.TopLeft :
                            anchor == TextAnchor.MiddleCenter ? TextAlignmentOptions.Center :
                            TextAlignmentOptions.Top;
            if (font) tmp.font = font;
            return tmp;
        }

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
            NodeUnlockType.MutualExclusion => "互斥选择", _ => "?"
        };

        private static string RewardText(CultivationNodeData node) => node.RewardType switch
        {
            NodeRewardType.Card => "卡牌: " + string.Join(",", node.RewardIds),
            NodeRewardType.Recipe => "丹方/图纸: " + string.Join(",", node.RewardIds),
            NodeRewardType.DivineAbility => "神通: " + string.Join(",", node.RewardIds),
            NodeRewardType.PassiveStat => node.PassiveStat + " +" + node.PassiveValue,
            NodeRewardType.CraftBonus => node.CraftBonusType + " +" + node.CraftBonusValue,
            _ => "无"
        };
        #endregion
    }
}
