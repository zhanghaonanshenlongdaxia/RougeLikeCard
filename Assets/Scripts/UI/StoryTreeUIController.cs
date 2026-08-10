using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using CardGame.Audio;

namespace CardGame.UI
{
    /// <summary>
    /// 故事线UI面板（重做版）：
    /// - 底部一条从左到右可滑动的横线，线上有节点
    /// - 分支从线往上延伸，分支之间有长方形剧情框
    /// - 锁定节点灰色遮挡，有新剧情的节点闪烁，解锁后可点击放大
    /// - 通过主界面"剧情树"按钮打开，不自动弹出
    /// </summary>
    public class StoryTreeUIController : MonoBehaviour
    {
        private TMP_FontAsset _font;
        private StoryTreeConfig _config;
        private Transform _contentRoot;
        private StoryNodeData _selectedNode;
        private GameObject _detailPanel;
        private TextMeshProUGUI _detailTitle;
        private TextMeshProUGUI _detailDesc;
        private TextMeshProUGUI _detailReward;
        private Button _unlockBtn;
        private Button _closeDetailBtn;

        public IArchitecture GetArchitecture() => CardGameArchitecture.Interface;

        private void Awake()
        {
            _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            LoadConfig();
            BuildUI();
            PopulateTree();
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
            // Canvas
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

            // 半透明背景（点击关闭）
            var bg = new GameObject("BG");
            bg.transform.SetParent(transform, false);
            SetFullStretch(bg.AddComponent<RectTransform>());
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.02f, 0.03f, 0.06f, 0.85f);
            var bgBtn = bg.AddComponent<Button>();
            bgBtn.onClick.AddListener(() => { if (_detailPanel) _detailPanel.SetActive(false); else Close(); });

            // 主容器
            var main = new GameObject("Main");
            main.transform.SetParent(transform, false);
            SetFullStretch(main.AddComponent<RectTransform>());

            // 标题栏
            var header = new GameObject("Header");
            header.transform.SetParent(main.transform, false);
            var hRt = header.AddComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0f, 0.93f); hRt.anchorMax = new Vector2(1f, 1f);
            hRt.offsetMin = Vector2.zero; hRt.offsetMax = Vector2.zero;
            var hTmp = header.AddComponent<TextMeshProUGUI>();
            hTmp.text = "剧情线"; hTmp.fontSize = 28; hTmp.color = new Color(0.9f, 0.8f, 0.3f);
            hTmp.alignment = TextAlignmentOptions.Center;
            if (_font) hTmp.font = _font;

            // 关闭按钮
            var closeBtn = new GameObject("CloseButton");
            closeBtn.transform.SetParent(main.transform, false);
            var cRt = closeBtn.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.92f, 0.93f); cRt.anchorMax = new Vector2(0.99f, 0.99f);
            cRt.offsetMin = Vector2.zero; cRt.offsetMax = Vector2.zero;
            closeBtn.AddComponent<Image>().color = new Color(0.3f, 0.15f, 0.1f, 1f);
            var cBtn = closeBtn.AddComponent<Button>();
            cBtn.onClick.AddListener(Close);
            var cTxtObj = new GameObject("Text");
            cTxtObj.transform.SetParent(closeBtn.transform, false);
            var cTxtRt = cTxtObj.AddComponent<RectTransform>();
            cTxtRt.anchorMin = Vector2.zero; cTxtRt.anchorMax = Vector2.one;
            cTxtRt.offsetMin = Vector2.zero; cTxtRt.offsetMax = Vector2.zero;
            var cTmp = cTxtObj.AddComponent<TextMeshProUGUI>();
            cTmp.text = "✕"; cTmp.fontSize = 22; cTmp.color = Color.white;
            cTmp.alignment = TextAlignmentOptions.Center;
            if (_font) cTmp.font = _font;

            // 横向滚动区域（故事线）
            var scrollObj = new GameObject("StoryScroll");
            scrollObj.transform.SetParent(main.transform, false);
            var sRt = scrollObj.AddComponent<RectTransform>();
            sRt.anchorMin = Vector2.zero; sRt.anchorMax = Vector2.one;
            sRt.offsetMin = Vector2.zero; sRt.offsetMax = new Vector2(0, -10);
            var scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.horizontal = true; scroll.vertical = false;
            scroll.scrollSensitivity = 30f;

            var vp = new GameObject("Viewport");
            vp.transform.SetParent(scrollObj.transform, false);
            SetFullStretch(vp.AddComponent<RectTransform>());
            var vpMask = vp.AddComponent<RectMask2D>();
            scroll.viewport = vp.GetComponent<RectTransform>();

            // Content — 宽度根据节点数动态计算
            var content = new GameObject("Content");
            content.transform.SetParent(vp.transform, false);
            var cContentRt = content.AddComponent<RectTransform>();
            cContentRt.anchorMin = new Vector2(0, 0); cContentRt.anchorMax = new Vector2(0, 1);
            cContentRt.pivot = new Vector2(0, 0.5f);
            cContentRt.offsetMin = Vector2.zero; cContentRt.offsetMax = Vector2.zero;
            scroll.content = cContentRt;
            _contentRoot = content.transform;

            // 详情弹窗（默认隐藏）
            BuildDetailPanel(main.transform);
        }

        void BuildDetailPanel(Transform parent)
        {
            _detailPanel = new GameObject("DetailPanel");
            _detailPanel.transform.SetParent(parent, false);
            var dRt = _detailPanel.AddComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0.25f, 0.2f); dRt.anchorMax = new Vector2(0.75f, 0.8f);
            dRt.offsetMin = Vector2.zero; dRt.offsetMax = Vector2.zero;
            _detailPanel.AddComponent<Image>().color = new Color(0.06f, 0.08f, 0.12f, 0.98f);

            var vlg = _detailPanel.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10; vlg.padding = new RectOffset(25, 25, 25, 25);
            vlg.childControlWidth = true; vlg.childForceExpandHeight = false;

            _detailTitle = CreateTMP(_detailPanel.transform, "", 24, new Color(0.9f, 0.8f, 0.3f));
            _detailTitle.alignment = TextAlignmentOptions.Center;
            _detailTitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 35;

            _detailDesc = CreateTMP(_detailPanel.transform, "", 16, Color.white);
            _detailDesc.alignment = TextAlignmentOptions.Left;
            _detailDesc.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;

            _detailReward = CreateTMP(_detailPanel.transform, "", 15, new Color(0.5f, 0.8f, 0.5f));
            _detailReward.alignment = TextAlignmentOptions.Left;

            // 按钮行
            var btnRow = new GameObject("BtnRow");
            btnRow.transform.SetParent(_detailPanel.transform, false);
            btnRow.AddComponent<RectTransform>();
            var hlg = btnRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20; hlg.childAlignment = TextAnchor.MiddleCenter;

            _unlockBtn = CreateButtonInRow(btnRow.transform, "解锁", new Color(0.2f, 0.5f, 0.3f, 1f));
            _unlockBtn.onClick.AddListener(OnUnlockClick);

            _closeDetailBtn = CreateButtonInRow(btnRow.transform, "关闭", new Color(0.3f, 0.2f, 0.1f, 1f));
            _closeDetailBtn.onClick.AddListener(() => _detailPanel.SetActive(false));

            _detailPanel.SetActive(false);
        }

        void PopulateTree()
        {
            if (_config == null) return;

            var storySystem = CardGameArchitecture.Interface.GetSystem<IStorySystem>();

            // 按章节排序节点
            var sortedNodes = new List<StoryNodeData>(_config.nodes);
            sortedNodes.Sort((a, b) => a.chapter.CompareTo(b.chapter));

            // 计算总宽度
            float nodeSpacing = 180f;
            float totalWidth = Mathf.Max(sortedNodes.Count * nodeSpacing + 100, 1920);
            var contentRt = _contentRoot.GetComponent<RectTransform>();
            contentRt.sizeDelta = new Vector2(totalWidth, 0);

            // 画底部主线（一条横线）
            var lineObj = new GameObject("MainLine");
            lineObj.transform.SetParent(_contentRoot, false);
            var lRt = lineObj.AddComponent<RectTransform>();
            lRt.anchorMin = new Vector2(0, 0.12f); lRt.anchorMax = new Vector2(1, 0.12f);
            lRt.offsetMin = new Vector2(50, -2); lRt.offsetMax = new Vector2(-50, 2);
            lineObj.AddComponent<Image>().color = new Color(0.7f, 0.6f, 0.3f, 0.6f);

            // 画每个节点
            for (int i = 0; i < sortedNodes.Count; i++)
            {
                var node = sortedNodes[i];
                bool unlocked = storySystem.IsNodeUnlocked(node.nodeId);
                bool available = !unlocked && node.prerequisites.TrueForAll(p => storySystem.IsNodeUnlocked(p));

                float xPos = 80 + i * nodeSpacing;
                CreateStoryNode(node, unlocked, available, xPos);
            }

            // 画连接线（前置 → 后继）
            foreach (var node in sortedNodes)
            {
                foreach (var preId in node.prerequisites)
                {
                    var preNode = _config.GetNode(preId);
                    if (preNode == null) continue;
                    int preIdx = sortedNodes.IndexOf(preNode);
                    int curIdx = sortedNodes.IndexOf(node);
                    if (preIdx < 0 || curIdx < 0) continue;

                    float x1 = 80 + preIdx * nodeSpacing;
                    float x2 = 80 + curIdx * nodeSpacing;
                    DrawBranchLine(x1, x2, preId + "_to_" + node.nodeId);
                }
            }
        }

        void CreateStoryNode(StoryNodeData node, bool unlocked, bool available, float xPos)
        {
            // 节点容器
            var nodeObj = new GameObject($"Node_{node.nodeId}");
            nodeObj.transform.SetParent(_contentRoot, false);
            var nRt = nodeObj.AddComponent<RectTransform>();
            nRt.anchorMin = new Vector2(0, 0); nRt.anchorMax = new Vector2(0, 1);
            nRt.pivot = new Vector2(0.5f, 0.5f);
            nRt.anchoredPosition = new Vector2(xPos, 0);
            nRt.sizeDelta = new Vector2(160, 0);

            // 线上的圆点（主线节点标记）
            var dotObj = new GameObject("Dot");
            dotObj.transform.SetParent(nodeObj.transform, false);
            var dRt = dotObj.AddComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0.5f, 0.12f); dRt.anchorMax = new Vector2(0.5f, 0.12f);
            dRt.pivot = new Vector2(0.5f, 0.5f);
            dRt.sizeDelta = new Vector2(24, 24);
            var dotImg = dotObj.AddComponent<Image>();
            dotImg.color = unlocked ? new Color(1f, 0.85f, 0.3f, 1f) : available ? new Color(0.2f, 0.8f, 0.3f, 1f) : new Color(0.2f, 0.2f, 0.25f, 1f);

            // 剧情框（长方形，在线上方）
            var boxObj = new GameObject("StoryBox");
            boxObj.transform.SetParent(nodeObj.transform, false);
            var bRt = boxObj.AddComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.5f, 0.2f); bRt.anchorMax = new Vector2(0.5f, 0.2f);
            bRt.pivot = new Vector2(0.5f, 0.5f);
            bRt.sizeDelta = new Vector2(140, 80);
            bRt.anchoredPosition = new Vector2(0, 50);
            var boxImg = boxObj.AddComponent<Image>();

            // 状态颜色
            if (unlocked)
                boxImg.color = new Color(0.15f, 0.25f, 0.15f, 0.95f);
            else if (available)
                boxImg.color = new Color(0.1f, 0.2f, 0.35f, 0.95f);
            else
                boxImg.color = new Color(0.08f, 0.08f, 0.1f, 0.8f);

            // 框内文字
            var txt = CreateTMP(boxObj.transform, node.nodeName, 15,
                unlocked ? new Color(0.9f, 0.8f, 0.3f) : available ? new Color(0.3f, 0.9f, 0.4f) : new Color(0.4f, 0.4f, 0.45f));
            txt.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            txt.GetComponent<RectTransform>().anchorMax = Vector2.one;
            txt.GetComponent<RectTransform>().offsetMin = new Vector2(5, 5);
            txt.GetComponent<RectTransform>().offsetMax = new Vector2(-5, -5);
            tmp.alignment = TextAlignmentOptions.Center;

            // 锁定装饰（未解锁时显示锁图标）
            if (!unlocked)
            {
                var lockObj = new GameObject("LockIcon");
                lockObj.transform.SetParent(boxObj.transform, false);
                var lockRt = lockObj.AddComponent<RectTransform>();
                lockRt.anchorMin = new Vector2(1, 1); lockRt.anchorMax = new Vector2(1, 1);
                lockRt.pivot = new Vector2(1, 1);
                lockRt.sizeDelta = new Vector2(20, 20);
                lockRt.anchoredPosition = new Vector2(-3, -3);
                var lockTmp = lockObj.AddComponent<TextMeshProUGUI>();
                lockTmp.text = "🔒"; lockTmp.fontSize = 14;
                lockTmp.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
                lockTmp.alignment = TextAlignmentOptions.Center;
                if (_font) lockTmp.font = _font;
            }

            // 闪烁效果（可解锁时）
            if (available)
            {
                var blinker = boxObj.AddComponent<StoryBoxBlinker>();
                blinker.targetImage = boxImg;
            }

            // 点击事件
            var btn = boxObj.AddComponent<Button>();
            btn.targetGraphic = boxImg;
            btn.interactable = unlocked || available;
            var captured = node;
            var capturedAvailable = available;
            btn.onClick.AddListener(() =>
            {
                if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick);
                ShowDetail(captured, capturedAvailable);
            });
        }

        TextMeshProUGUI tmp;
        TextMeshProUGUI CreateTMP(Transform parent, string text, float size, Color color)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            if (_font) t.font = _font;
            tmp = t;
            return t;
        }

        void DrawBranchLine(float x1, float x2, string name)
        {
            // 从主线往上画的连接线（垂直+水平）
            var lineObj = new GameObject($"Branch_{name}");
            lineObj.transform.SetParent(_contentRoot, false);
            var rt = lineObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0, 0.5f);

            float mid = (x1 + x2) * 0.5f;
            float width = Mathf.Abs(x2 - x1);
            rt.anchoredPosition = new Vector2(Mathf.Min(x1, x2), 0.12f * 1080);
            rt.sizeDelta = new Vector2(width, 3f);

            var img = lineObj.AddComponent<Image>();
            img.color = new Color(0.5f, 0.4f, 0.2f, 0.3f);
        }

        void ShowDetail(StoryNodeData node, bool canUnlock)
        {
            _selectedNode = canUnlock ? node : null;

            var storySystem = CardGameArchitecture.Interface.GetSystem<IStorySystem>();
            bool unlocked = storySystem.IsNodeUnlocked(node.nodeId);

            string status = unlocked ? "✓ 已解锁" : canUnlock ? "可解锁" : "锁定";
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
                prereq = $"\n前置: {string.Join(", ", missing)}";
            }

            string rewardStr = "";
            if (node.rewardIds.Count > 0)
                rewardStr = $"\n奖励: {node.rewardIds.Count}项";
            if (node.goldReward > 0)
                rewardStr += $"\n灵石: +{node.goldReward}";

            _detailTitle.text = $"{node.nodeName}  [{status}]";
            _detailDesc.text = $"{node.description}{prereq}";
            _detailReward.text = rewardStr;

            _unlockBtn.interactable = canUnlock;
            var unlockTmp = _unlockBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (unlockTmp != null)
                unlockTmp.text = unlocked ? "已解锁" : canUnlock ? "解锁" : "无法解锁";

            _detailPanel.SetActive(true);
        }

        void OnUnlockClick()
        {
            if (_selectedNode == null) return;
            if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick);

            CardGameArchitecture.Interface.GetSystem<IStorySystem>().UnlockNode(_selectedNode.nodeId);
            FloatingTip.ShowSuccess($"解锁: {_selectedNode.nodeName}");

            // 重建树
            for (int i = _contentRoot.childCount - 1; i >= 0; i--)
                Destroy(_contentRoot.GetChild(i).gameObject);
            PopulateTree();

            _detailPanel.SetActive(false);
        }

        void Close()
        {
            gameObject.SetActive(false);
        }

        void SetFullStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        Button CreateButtonInRow(Transform parent, string label, Color color)
        {
            var go = new GameObject($"Btn_{label}");
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = color;
            go.AddComponent<LayoutElement>().preferredHeight = 40;
            var t = CreateTMP(go.transform, label, 18, new Color(0.95f, 0.85f, 0.4f));
            t.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            t.GetComponent<RectTransform>().anchorMax = Vector2.one;
            t.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            t.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            return go.AddComponent<Button>();
        }
    }

    /// <summary>可解锁剧情框的闪烁动画组件</summary>
    public class StoryBoxBlinker : MonoBehaviour
    {
        public Image targetImage;
        private float _timer;
        private Color _baseColor;

        void Start()
        {
            if (targetImage != null) _baseColor = targetImage.color;
        }

        void Update()
        {
            if (targetImage == null) return;
            _timer += Time.deltaTime;
            float alpha = 0.6f + Mathf.Sin(_timer * 3f) * 0.35f;
            targetImage.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, alpha);
        }
    }
}
