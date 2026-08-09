using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using CardGame.Audio;

namespace CardGame.UI
{
    /// <summary>
    /// 故事线树状图UI面板。
    /// 撤离返回基地后弹出，显示可解锁节点。
    /// </summary>
    public class StoryTreeUIController : MonoBehaviour
    {
        private TMP_FontAsset _font;
        private StoryTreeConfig _config;
        private Transform _nodesRoot;
        private TextMeshProUGUI _detailText;
        private Button _unlockBtn;
        private Button _closeBtn;
        private StoryNodeData _selectedNode;

        public IArchitecture GetArchitecture() => CardGameArchitecture.Interface;

        private void Awake()
        {
            _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            LoadConfig();
            BuildUI();
        }

        void LoadConfig()
        {
#if UNITY_EDITOR
            _config = UnityEditor.AssetDatabase.LoadAssetAtPath<StoryTreeConfig>(
                "Assets/NueGames/NueDeck/Data/StoryTree/StoryTreeConfig.asset");
#else
            _config = Resources.Load<StoryTreeConfig>("StoryTreeConfig");
#endif
        }

        void BuildUI()
        {
            var canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 80;
                var scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                gameObject.AddComponent<GraphicRaycaster>();
            }

            // 背景
            var bg = new GameObject("BG");
            bg.transform.SetParent(transform, false);
            SetFullStretch(bg.AddComponent<RectTransform>());
            bg.AddComponent<Image>().color = new Color(0.03f, 0.03f, 0.08f, 0.97f);

            // 主面板
            var panel = new GameObject("Panel");
            panel.transform.SetParent(transform, false);
            var pRt = panel.AddComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0.05f, 0.05f); pRt.anchorMax = new Vector2(0.95f, 0.95f);
            pRt.offsetMin = Vector2.zero; pRt.offsetMax = Vector2.zero;
            panel.AddComponent<Image>().color = new Color(0.06f, 0.08f, 0.12f, 0.98f);

            var hlg = panel.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 15; hlg.padding = new RectOffset(15, 15, 15, 15);
            hlg.childControlWidth = true; hlg.childForceExpandWidth = true;

            // === 左侧：树状图区域 ===
            var treeArea = new GameObject("TreeArea");
            treeArea.transform.SetParent(panel.transform, false);
            treeArea.AddComponent<RectTransform>();
            var treeLe = treeArea.AddComponent<LayoutElement>();
            treeLe.flexibleWidth = 2;

            // 标题
            var title = CreateText(treeArea.transform, "\u6545\u4E8B\u7EBF", 26, new Color(0.9f, 0.8f, 0.3f));
            title.AddComponent<LayoutElement>().preferredHeight = 35;

            // 滚动区域
            var scroll = new GameObject("Scroll");
            scroll.transform.SetParent(treeArea.transform, false);
            var scrollLe = scroll.AddComponent<LayoutElement>();
            scrollLe.flexibleHeight = 1;
            var scrollRect = scroll.AddComponent<ScrollRect>();
            scrollRect.horizontal = true; scrollRect.vertical = true;

            var vp = new GameObject("Viewport");
            vp.transform.SetParent(scroll.transform, false);
            SetFullStretch(vp.AddComponent<RectTransform>());
            vp.AddComponent<Image>().color = new Color(0.04f, 0.06f, 0.1f, 1f);
            vp.AddComponent<RectMask2D>();
            scrollRect.viewport = vp.GetComponent<RectTransform>();

            var content = new GameObject("Content");
            content.transform.SetParent(vp.transform, false);
            var cRt = content.AddComponent<RectTransform>();
            cRt.anchorMin = Vector2.zero; cRt.anchorMax = Vector2.one;
            cRt.pivot = new Vector2(0.5f, 1f);
            cRt.offsetMin = Vector2.zero; cRt.offsetMax = Vector2.zero;
            cRt.sizeDelta = new Vector2(1600, 1400); // 固定大小画布
            scrollRect.content = cRt;
            _nodesRoot = content.transform;

            // === 右侧：详情面板 ===
            var detailArea = new GameObject("DetailArea");
            detailArea.transform.SetParent(panel.transform, false);
            detailArea.AddComponent<RectTransform>();
            var detailLe = detailArea.AddComponent<LayoutElement>();
            detailLe.preferredWidth = 400;

            var dVLG = detailArea.AddComponent<VerticalLayoutGroup>();
            dVLG.spacing = 10; dVLG.padding = new RectOffset(10, 10, 10, 10);
            dVLG.childControlWidth = true; dVLG.childAlignment = TextAnchor.UpperCenter;

            // 详情标题
            var dTitle = CreateText(detailArea.transform, "\u8282\u70B9\u8BE6\u60C5", 22, new Color(0.9f, 0.8f, 0.3f));
            dTitle.AddComponent<LayoutElement>().preferredHeight = 30;

            // 详情内容
            _detailText = CreateText(detailArea.transform, "\u9009\u62E9\u5DE6\u4FA7\u7684\u8282\u70B9\u67E5\u770B\u8BE6\u60C5", 16, Color.white).GetComponent<TextMeshProUGUI>();
            _detailText.alignment = TextAlignmentOptions.Left;
            _detailText.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 0);
            var dtLe = _detailText.gameObject.AddComponent<LayoutElement>();
            dtLe.flexibleHeight = 1;

            // 解锁按钮
            _unlockBtn = CreateButton(detailArea.transform, "\u89E3\u9501", new Color(0.2f, 0.5f, 0.3f, 1f));
            _unlockBtn.interactable = false;
            _unlockBtn.onClick.AddListener(OnUnlockClick);

            // 关闭按钮
            _closeBtn = CreateButton(detailArea.transform, "\u5173\u95ED", new Color(0.3f, 0.2f, 0.1f, 1f));
            _closeBtn.onClick.AddListener(() =>
            {
                if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick);
                gameObject.SetActive(false);
            });

            PopulateTree();
        }

        void PopulateTree()
        {
            if (_config == null) return;

            var storySystem = CardGameArchitecture.Interface.GetSystem<IStorySystem>();

            // 先画连接线（在节点下方层）
            foreach (var node in _config.nodes)
            {
                foreach (var preId in node.prerequisites)
                {
                    var preNode = _config.GetNode(preId);
                    if (preNode == null) continue;
                    DrawConnectionLine(preNode.position, node.position, preId + "_to_" + node.nodeId);
                }
            }

            // 再画节点
            foreach (var node in _config.nodes)
            {
                bool unlocked = storySystem.IsNodeUnlocked(node.nodeId);
                bool available = !unlocked && node.prerequisites.TrueForAll(p => storySystem.IsNodeUnlocked(p));

                // 节点容器（圆形节点+名称在下方）
                var nodeContainer = new GameObject($"Node_{node.nodeId}");
                nodeContainer.transform.SetParent(_nodesRoot, false);
                var containerRt = nodeContainer.AddComponent<RectTransform>();
                containerRt.anchoredPosition = new Vector2(node.position.x, -node.position.y);
                containerRt.sizeDelta = new Vector2(140, 100);
                containerRt.pivot = new Vector2(0.5f, 0.5f);

                // 圆形节点图标
                var iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(nodeContainer.transform, false);
                var iconRt = iconObj.AddComponent<RectTransform>();
                iconRt.anchorMin = new Vector2(0.5f, 0.5f); iconRt.anchorMax = new Vector2(0.5f, 0.5f);
                iconRt.pivot = new Vector2(0.5f, 0.5f);
                iconRt.anchoredPosition = new Vector2(0, 15);
                iconRt.sizeDelta = new Vector2(70, 70);
                var iconImg = iconObj.AddComponent<Image>();
                
                Color nodeColor;
                if (unlocked) nodeColor = HexToColor(node.colorHex);
                else if (available) nodeColor = new Color(0.15f, 0.45f, 0.2f, 1f);
                else nodeColor = new Color(0.15f, 0.15f, 0.18f, 0.7f);
                iconImg.color = nodeColor;

                // 边框（已解锁/可解锁时有发光边框）
                if (unlocked || available)
                {
                    var borderObj = new GameObject("Border");
                    borderObj.transform.SetParent(nodeContainer.transform, false);
                    var borderRt = borderObj.AddComponent<RectTransform>();
                    borderRt.anchorMin = new Vector2(0.5f, 0.5f); borderRt.anchorMax = new Vector2(0.5f, 0.5f);
                    borderRt.pivot = new Vector2(0.5f, 0.5f);
                    borderRt.anchoredPosition = new Vector2(0, 15);
                    borderRt.sizeDelta = new Vector2(76, 76);
                    var borderImg = borderObj.AddComponent<Image>();
                    borderImg.color = unlocked ? new Color(1f, 0.85f, 0.3f, 0.8f) : new Color(0.2f, 0.8f, 0.3f, 0.6f);
                }

                // 图标文字
                var iconTxt = CreateText(iconObj.transform, node.iconText ?? "", 28, 
                    unlocked ? new Color(0.1f, 0.1f, 0.2f) : Color.white);
                iconTxt.GetComponent<RectTransform>().anchorMin = Vector2.zero;
                iconTxt.GetComponent<RectTransform>().anchorMax = Vector2.one;
                iconTxt.GetComponent<RectTransform>().offsetMin = Vector2.zero;
                iconTxt.GetComponent<RectTransform>().offsetMax = Vector2.zero;

                // 节点名称（图标下方）
                var nameObj = CreateText(nodeContainer.transform, node.nodeName, 13, 
                    unlocked ? new Color(0.9f, 0.8f, 0.3f) : available ? new Color(0.3f, 0.9f, 0.4f) : new Color(0.5f, 0.5f, 0.55f));
                nameObj.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 0f);
                nameObj.GetComponent<RectTransform>().anchorMax = new Vector2(1f, 0.3f);
                nameObj.GetComponent<RectTransform>().offsetMin = Vector2.zero;
                nameObj.GetComponent<RectTransform>().offsetMax = Vector2.zero;

                // 点击按钮（覆盖在图标上）
                var btn = iconObj.AddComponent<Button>();
                btn.targetGraphic = iconImg;
                btn.interactable = available || unlocked;

                var captured = node;
                var capturedAvailable = available;
                btn.onClick.AddListener(() =>
                {
                    if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick);
                    OnNodeSelected(captured, capturedAvailable);
                });
            }
        }

        void DrawConnectionLine(Vector2 from, Vector2 to, string name)
        {
            // 转换为画布坐标（Y翻转）
            var fromCanvas = new Vector2(from.x, -from.y);
            var toCanvas = new Vector2(to.x, -to.y);

            var lineObj = new GameObject($"Line_{name}");
            lineObj.transform.SetParent(_nodesRoot, false);
            var rt = lineObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            var mid = (fromCanvas + toCanvas) * 0.5f;
            var diff = toCanvas - fromCanvas;
            float length = diff.magnitude;
            float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

            rt.anchoredPosition = mid;
            rt.sizeDelta = new Vector2(length, 3f);
            rt.localRotation = Quaternion.Euler(0, 0, angle);

            var img = lineObj.AddComponent<Image>();
            img.color = new Color(0.6f, 0.5f, 0.2f, 0.4f);
            // 确保线在节点下方
            lineObj.transform.SetAsFirstSibling();
        }

        void OnNodeSelected(StoryNodeData node, bool canUnlock)
        {
            _selectedNode = canUnlock ? node : null;

            var storySystem = CardGameArchitecture.Interface.GetSystem<IStorySystem>();
            bool unlocked = storySystem.IsNodeUnlocked(node.nodeId);

            string status = unlocked ? "\u2713 \u5DF2\u89E3\u9501" : canUnlock ? "\u53EF\u89E3\u9501" : "\u9501\u5B9A";
            string prereq = "";
            if (!unlocked && !canUnlock)
            {
                var missing = new List<string>();
                foreach (var p in node.prerequisites)
                    if (!storySystem.IsNodeUnlocked(p))
                    {
                        var preNode = _config.GetNode(p);
                        missing.Add(preNode?.nodeName ?? p);
                    }
                prereq = $"\n\u524D\u7F6E\u6761\u4EF6: {string.Join(", ", missing)}";
            }

            string rewardStr = "";
            if (node.rewardIds.Count > 0)
                rewardStr = $"\n\u5956\u52B1: {node.rewardIds.Count}\u9879";
            if (node.goldReward > 0)
                rewardStr += $"\n\u7075\u77F3: +{node.goldReward}";

            _detailText.text = $"{node.nodeName}\n\u72B6\u6001: {status}{prereq}\n\n{node.description}{rewardStr}";

            _unlockBtn.interactable = canUnlock;
            var unlockTmp = _unlockBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (unlockTmp != null)
                unlockTmp.text = unlocked ? "\u5DF2\u89E3\u9501" : canUnlock ? "\u89E3\u9501" : "\u65E0\u6CD5\u89E3\u9501";
        }

        void OnUnlockClick()
        {
            if (_selectedNode == null) return;
            if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick);

            CardGameArchitecture.Interface.GetSystem<IStorySystem>().UnlockNode(_selectedNode.nodeId);
            FloatingTip.ShowSuccess($"\u89E3\u9501: {_selectedNode.nodeName}");

            // 刷新UI
            // 简单方案：重建所有节点
            for (int i = _nodesRoot.childCount - 1; i >= 0; i--)
                Destroy(_nodesRoot.GetChild(i).gameObject);
            PopulateTree();

            _selectedNode = null;
            _unlockBtn.interactable = false;
            _detailText.text = "\u89E3\u9501\u6210\u529F\uFF01\u9009\u62E9\u5176\u4ED6\u8282\u70B9\u7EE7\u7EED\u3002";
        }

        Color HexToColor(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return Color.gray;
            hex = hex.Replace("#", "");
            if (hex.Length != 6) return Color.gray;
            byte r = System.Convert.ToByte(hex.Substring(0, 2), 16);
            byte g = System.Convert.ToByte(hex.Substring(2, 2), 16);
            byte b = System.Convert.ToByte(hex.Substring(4, 2), 16);
            return new Color32(r, g, b, 255);
        }

        void SetFullStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        GameObject CreateText(Transform parent, string text, float size, Color color)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = size; tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            if (_font) tmp.font = _font;
            return go;
        }

        Button CreateButton(Transform parent, string label, Color color)
        {
            var go = new GameObject($"Btn_{label}");
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = color;
            go.AddComponent<LayoutElement>().preferredHeight = 45;
            var txt = CreateText(go.transform, label, 20, new Color(0.95f, 0.85f, 0.4f));
            txt.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            txt.GetComponent<RectTransform>().anchorMax = Vector2.one;
            txt.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            txt.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            return go.AddComponent<Button>();
        }
    }
}
