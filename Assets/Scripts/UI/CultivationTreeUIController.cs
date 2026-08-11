using System.Collections.Generic;
using CardGame;
using NueGames.NueDeck.Scripts.Data.Cultivation;
using NueGames.NueDeck.Scripts.Enums;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardGame.UI
{
    public class CultivationTreeUIController : MonoBehaviour
    {
        private ICultivationSystem _cultSystem;
        private ICultivationModel _cultModel;

        private Canvas _canvas;
        private GameObject _panel;
        private Transform _nodeTreeRoot;
        private Transform _detailPanel;
        private TextMeshProUGUI _comprehensionText;
        private TMP_Dropdown _methodDropdown;
        private TextMeshProUGUI _activeMethodText;
        private CultivationMethodData _selectedMethod;
        private CultivationNodeData _selectedNode;

        public void Show()
        {
            _cultSystem = CardGameArchitecture.Interface.GetSystem<ICultivationSystem>();
            _cultModel = CardGameArchitecture.Interface.GetModel<ICultivationModel>();

            bool allValid = _canvas != null && _canvas.gameObject != null
                && _panel != null && _panel.gameObject != null
                && _nodeTreeRoot != null && _nodeTreeRoot.gameObject != null
                && _detailPanel != null && _detailPanel.gameObject != null
                && _comprehensionText != null && _comprehensionText.gameObject != null
                && _methodDropdown != null && _methodDropdown.gameObject != null;

            if (allValid)
            {
                _canvas.gameObject.SetActive(true);
                RefreshAll();
                return;
            }

            if (_canvas != null && _canvas.gameObject != null)
                Destroy(_canvas.gameObject);
            _canvas = null; _panel = null; _nodeTreeRoot = null; _detailPanel = null;
            _comprehensionText = null; _methodDropdown = null; _activeMethodText = null;

            BuildUI();
            RefreshAll();
        }

        public void Hide()
        {
            if (_canvas != null && _canvas.gameObject != null)
                _canvas.gameObject.SetActive(false);
        }

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

            var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

            // Background
            var bg = NewChild("BG", transform);
            bg.AddComponent<Image>().color = new Color(0.04f, 0.04f, 0.08f, 0.97f);
            Stretch(bg);

            // Panel
            _panel = NewChild("Panel", transform);
            var panelRt = _panel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.03f, 0.03f);
            panelRt.anchorMax = new Vector2(0.97f, 0.97f);
            panelRt.offsetMin = Vector2.zero; panelRt.offsetMax = Vector2.zero;
            _panel.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.95f);

            // ===== Top bar: dropdown (left) + title (center) + comprehension (right) =====
            var topBar = NewChild("TopBar", _panel.transform);
            var topRt = topBar.GetComponent<RectTransform>();
            topRt.anchorMin = new Vector2(0, 1); topRt.anchorMax = Vector2.one;
            topRt.pivot = new Vector2(0.5f, 1f);
            topRt.offsetMin = new Vector2(0, -45); topRt.offsetMax = Vector2.zero;

            // Dropdown (left) — only UI element for method selection
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
            if (font) ddLabelText.font = font;
            _methodDropdown.captionText = ddLabelText;

            var ddArrow = NewChild("Arrow", ddGo.transform);
            var ddArrowRt = ddArrow.GetComponent<RectTransform>();
            ddArrowRt.anchorMin = new Vector2(0.82f, 0.3f); ddArrowRt.anchorMax = new Vector2(0.98f, 0.7f);
            ddArrowRt.offsetMin = Vector2.zero; ddArrowRt.offsetMax = Vector2.zero;
            var ddArrowText = ddArrow.AddComponent<TextMeshProUGUI>();
            ddArrowText.text = "V"; ddArrowText.fontSize = 14;
            ddArrowText.alignment = TextAlignmentOptions.Center;
            if (font) ddArrowText.font = font;

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
            ddItemBgRt.anchorMin = new Vector2(0, 0); ddItemBgRt.anchorMax = new Vector2(1, 1);
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
            if (font) ddItemCmText.font = font;
            ddItemToggle.graphic = ddItemCmText;
            ddItemToggle.targetGraphic = ddItemBgImg;

            var ddItemLabel = NewChild("ItemLabel", ddItem.transform);
            var ddItemLabelRt = ddItemLabel.GetComponent<RectTransform>();
            ddItemLabelRt.anchorMin = new Vector2(0, 0); ddItemLabelRt.anchorMax = new Vector2(1, 1);
            ddItemLabelRt.offsetMin = new Vector2(30, 0); ddItemLabelRt.offsetMax = new Vector2(-5, 0);
            var ddItemLabelText = ddItemLabel.AddComponent<TextMeshProUGUI>();
            ddItemLabelText.fontSize = 16; ddItemLabelText.color = new Color(0.85f, 0.85f, 0.9f);
            if (font) ddItemLabelText.font = font;

            _methodDropdown.template = ddTemplateRt;
            _methodDropdown.itemText = ddItemLabelText;
            ddTemplate.SetActive(false);
            _methodDropdown.onValueChanged.AddListener(OnDropdownChanged);

            // Title (center)
            var title = NewText(topBar.transform, "功法修炼", 22, Vector2.zero, TextAnchor.MiddleCenter, font);
            var titleRt = title.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.28f, 0); titleRt.anchorMax = new Vector2(0.7f, 1);
            titleRt.offsetMin = Vector2.zero; titleRt.offsetMax = Vector2.zero;
            title.fontStyle = FontStyles.Bold; title.color = new Color(0.85f, 0.8f, 0.5f);

            // Comprehension points (right)
            var compGo = NewChild("ComprehensionPoints", topBar.transform);
            var compRt = compGo.GetComponent<RectTransform>();
            compRt.anchorMin = new Vector2(0.7f, 0); compRt.anchorMax = Vector2.one;
            compRt.offsetMin = new Vector2(10, 4); compRt.offsetMax = new Vector2(-10, -4);
            _comprehensionText = compGo.AddComponent<TextMeshProUGUI>();
            _comprehensionText.fontSize = 18; _comprehensionText.alignment = TextAlignmentOptions.Right;
            _comprehensionText.color = new Color(0.9f, 0.8f, 0.3f);
            if (font) _comprehensionText.font = font;

            // Active method label (below top bar)
            var activeGo = NewChild("ActiveMethodLabel", _panel.transform);
            var activeRt = activeGo.GetComponent<RectTransform>();
            activeRt.anchorMin = new Vector2(0.01f, 0.91f); activeRt.anchorMax = new Vector2(0.5f, 0.96f);
            activeRt.offsetMin = Vector2.zero; activeRt.offsetMax = Vector2.zero;
            _activeMethodText = activeGo.AddComponent<TextMeshProUGUI>();
            _activeMethodText.fontSize = 15; _activeMethodText.alignment = TextAlignmentOptions.Left;
            _activeMethodText.color = new Color(0.3f, 0.8f, 0.3f);
            if (font) _activeMethodText.font = font;

            // ===== Node tree panel (center, wider) =====
            var treePanel = NewChild("NodeTreePanel", _panel.transform);
            var treeRt = treePanel.GetComponent<RectTransform>();
            treeRt.anchorMin = new Vector2(0.01f, 0.02f); treeRt.anchorMax = new Vector2(0.62f, 0.88f);
            treeRt.offsetMin = Vector2.zero; treeRt.offsetMax = Vector2.zero;
            treePanel.AddComponent<Image>().color = new Color(0.06f, 0.06f, 0.1f, 0.9f);

            var nsObj = NewChild("NodeScroll", treePanel.transform);
            var nsRt = nsObj.GetComponent<RectTransform>();
            nsRt.anchorMin = Vector2.zero; nsRt.anchorMax = Vector2.one;
            nsRt.offsetMin = new Vector2(5, 5); nsRt.offsetMax = new Vector2(-5, -5);
            var nsScroll = nsObj.AddComponent<ScrollRect>();
            nsScroll.horizontal = true; nsScroll.vertical = true;

            var nsViewport = NewChild("NodeViewport", nsObj.transform);
            var nsvpRt = nsViewport.GetComponent<RectTransform>();
            nsvpRt.anchorMin = Vector2.zero; nsvpRt.anchorMax = Vector2.one;
            nsvpRt.offsetMin = Vector2.zero; nsvpRt.offsetMax = Vector2.zero;
            nsViewport.AddComponent<RectMask2D>();
            nsScroll.viewport = nsvpRt;

            var ncGo = NewChild("NodeContent", nsViewport.transform);
            var ncRt = ncGo.GetComponent<RectTransform>();
            _nodeTreeRoot = ncGo.transform;
            ncRt.anchorMin = Vector2.zero; ncRt.anchorMax = Vector2.one;
            ncRt.pivot = new Vector2(0.5f, 1f);
            ncRt.offsetMin = new Vector2(-500, -800); ncRt.offsetMax = new Vector2(500, 0);
            nsScroll.content = ncRt;

            // ===== Detail panel (right) =====
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

            // Close button (bottom center of panel)
            var closeBtn = NewChild("CloseButton", _panel.transform);
            var cbRt = closeBtn.GetComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(0.63f, 0.02f); cbRt.anchorMax = new Vector2(0.98f, 0.02f);
            cbRt.pivot = new Vector2(0.5f, 0f);
            cbRt.offsetMin = new Vector2(0, 5); cbRt.offsetMax = new Vector2(0, 38);
            closeBtn.AddComponent<Image>().color = new Color(0.55f, 0.15f, 0.15f, 0.9f);
            NewText(closeBtn.transform, "关闭", 18, Vector2.zero, TextAnchor.MiddleCenter, font);
            closeBtn.AddComponent<Button>().onClick.AddListener(Hide);
        }

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
            if (_comprehensionText != null && _cultModel != null)
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
            for (int i = _nodeTreeRoot.childCount - 1; i >= 0; i--)
                Destroy(_nodeTreeRoot.GetChild(i).gameObject);

            if (_selectedMethod == null)
            {
                var learned = _cultSystem.GetLearnedMethods();
                if (learned.Count > 0) _selectedMethod = learned[0];
            }
            if (_selectedMethod == null) { RefreshDetail(); return; }

            var realms = _selectedMethod.GetAvailableRealms();
            float layerWidth = 170f;
            float startX = -((realms.Count - 1) * layerWidth) / 2f;
            var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

            for (int r = 0; r < realms.Count; r++)
            {
                var realm = realms[r];
                var nodes = _selectedMethod.GetNodesByRealm(realm);

                var labelGo = NewChild("Realm_" + realm, _nodeTreeRoot);
                var labelRt = labelGo.GetComponent<RectTransform>();
                labelRt.anchorMin = new Vector2(0.5f, 1f); labelRt.anchorMax = new Vector2(0.5f, 1f);
                labelRt.pivot = new Vector2(0.5f, 1f);
                labelRt.anchoredPosition = new Vector2(startX + r * layerWidth, -5);
                labelRt.sizeDelta = new Vector2(150, 25);
                var label = labelGo.AddComponent<TextMeshProUGUI>();
                label.text = RealmText(realm);
                label.fontSize = 17; label.alignment = TextAlignmentOptions.Center;
                label.color = new Color(0.85f, 0.7f, 0.3f);
                if (font) label.font = font;

                for (int i = 0; i < nodes.Count; i++)
                {
                    var node = nodes[i];
                    var isUnlocked = _cultModel.UnlockedNodeIds.Contains(node.NodeId);
                    var canUnlock = _cultSystem.CanUnlockNode(node.NodeId);
                    var isMutexLocked = !string.IsNullOrEmpty(node.MutexGroup) &&
                        _cultModel.SelectedMutexChoices.TryGetValue(node.MutexGroup, out var chosen) && chosen != node.NodeId;

                    var nodeBtn = NewChild("Node_" + node.NodeId, _nodeTreeRoot);
                    var nrt = nodeBtn.GetComponent<RectTransform>();
                    nrt.anchorMin = new Vector2(0.5f, 1f); nrt.anchorMax = new Vector2(0.5f, 1f);
                    nrt.pivot = new Vector2(0.5f, 1f);
                    nrt.anchoredPosition = new Vector2(startX + r * layerWidth, -35 - i * 48);
                    nrt.sizeDelta = new Vector2(150, 40);

                    var img = nodeBtn.AddComponent<Image>();
                    if (isUnlocked) img.color = new Color(0.7f, 0.55f, 0.1f, 0.9f);
                    else if (isMutexLocked) img.color = new Color(0.35f, 0.12f, 0.12f, 0.85f);
                    else if (canUnlock) img.color = new Color(0.08f, 0.45f, 0.12f, 0.9f);
                    else img.color = new Color(0.15f, 0.15f, 0.2f, 0.85f);

                    var nt = NewText(nodeBtn.transform, node.NodeName, 13, Vector2.zero, TextAnchor.MiddleCenter, font);
                    if (isMutexLocked) nt.color = new Color(0.5f, 0.4f, 0.4f);
                    else if (!isUnlocked && !canUnlock) nt.color = new Color(0.5f, 0.5f, 0.55f);

                    var btn = nodeBtn.AddComponent<Button>();
                    var captured = node;
                    btn.onClick.AddListener(() => { _selectedNode = captured; RefreshDetail(); });
                }
            }

            RefreshDetail();
        }

        private void RefreshDetail()
        {
            for (int i = _detailPanel.childCount - 1; i >= 0; i--)
                Destroy(_detailPanel.GetChild(i).gameObject);

            var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

            if (_selectedNode == null)
            {
                AddDetailText("选择一个节点查看详情", 16, new Color(0.7f, 0.7f, 0.7f), font, false);
                return;
            }

            var node = _selectedNode;
            var isUnlocked = _cultModel.UnlockedNodeIds.Contains(node.NodeId);
            var canUnlock = _cultSystem.CanUnlockNode(node.NodeId);
            var isMutexLocked = !string.IsNullOrEmpty(node.MutexGroup) &&
                _cultModel.SelectedMutexChoices.TryGetValue(node.MutexGroup, out var chosen) && chosen != node.NodeId;

            // Node name (bold, gold)
            AddDetailText(node.NodeName, 20, new Color(0.9f, 0.85f, 0.6f), font, true);

            // Description
            AddDetailText(node.Description, 14, new Color(0.8f, 0.8f, 0.85f), font, false);

            // Info line
            var info = "境界: " + RealmText(node.Realm) + "  |  解锁: " + UnlockTypeText(node.UnlockType);
            if (node.ComprehensionCost > 0) info += "  |  参悟: " + node.ComprehensionCost;
            AddDetailText(info, 14, new Color(0.7f, 0.75f, 0.8f), font, false);

            // Reward
            AddDetailText("奖励: " + RewardText(node), 14, new Color(0.6f, 0.8f, 0.6f), font, false);

            // Mutex info
            if (!string.IsNullOrEmpty(node.MutexGroup))
                AddDetailText("互斥组: " + node.MutexGroup + " (同组只能选一个)", 13, new Color(0.8f, 0.4f, 0.4f), font, false);

            // Active indicator
            if (_selectedMethod != null && _selectedMethod.MethodId == _cultModel.ActiveMethodId.Value)
                AddDetailText(">>> 当前装备功法 <<<", 14, new Color(0.3f, 0.8f, 0.3f), font, true);

            // Action button
            string statusText;
            Color btnColor;
            if (isUnlocked) { statusText = "已解锁"; btnColor = new Color(0.3f, 0.4f, 0.3f); }
            else if (isMutexLocked) { statusText = "互斥已锁"; btnColor = new Color(0.35f, 0.15f, 0.15f); }
            else if (canUnlock && node.UnlockType == NodeUnlockType.Comprehension) { statusText = "消耗参悟点解锁"; btnColor = new Color(0.08f, 0.55f, 0.1f); }
            else if (canUnlock && node.UnlockType == NodeUnlockType.MutualExclusion) { statusText = "选择此路线"; btnColor = new Color(0.08f, 0.55f, 0.1f); }
            else if (canUnlock) { statusText = UnlockTypeText(node.UnlockType) + " (可尝试)"; btnColor = new Color(0.15f, 0.4f, 0.7f); }
            else { statusText = "前置未满足"; btnColor = new Color(0.25f, 0.25f, 0.3f); }

            var actionBtn = NewChild("ActionButton", _detailPanel);
            var abLe = actionBtn.AddComponent<LayoutElement>();
            abLe.preferredHeight = 34; abLe.flexibleWidth = 1;
            actionBtn.AddComponent<Image>().color = btnColor;
            var abText = NewText(actionBtn.transform, statusText, 15, Vector2.zero, TextAnchor.MiddleCenter, font);
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

        private TextMeshProUGUI AddDetailText(string text, int fontSize, Color color, TMP_FontAsset font, bool bold)
        {
            var go = NewChild("DetailText", _detailPanel);
            var le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.preferredHeight = 0; // let TMP auto-size via ContentSizeFitter
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = fontSize; tmp.color = color;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            if (bold) tmp.fontStyle = FontStyles.Bold;
            if (font) tmp.font = font;
            // Enable auto-size so ContentSizeFitter can calculate
            tmp.enableAutoSizing = false;
            // Add ContentSizeFitter to the text itself
            var csf = go.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return tmp;
        }

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
            RealmLevel.LianQi => "练气", RealmLevel.ZhuJi => "筑基", RealmLevel.JinDan => "金丹",
            RealmLevel.YuanYing => "元婴", RealmLevel.HuaShen => "化神", RealmLevel.DuJie => "渡劫",
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
