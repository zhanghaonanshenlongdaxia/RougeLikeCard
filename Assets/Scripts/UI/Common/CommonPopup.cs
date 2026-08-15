using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardGame.UI.Common
{
    /// <summary>
    /// 通用弹窗 — 标题+内容区+关闭按钮。运行时创建，单例。
    /// </summary>
    public class CommonPopup : MonoBehaviour
    {
        public static CommonPopup Instance { get; private set; }

        [SerializeField] private Transform contentRoot;
        [SerializeField] private TextMeshProUGUI titleText;
        private Canvas _canvas;
        private TextMeshProUGUI _titleText;
        private Transform _contentRoot;
        private TMP_FontAsset _font;
        private static GameObject _prefab;

        public static CommonPopup GetOrCreate()
        {
            if (Instance != null) return Instance;

            if (_prefab == null)
                _prefab = Resources.Load<GameObject>("Prefabs/Common/CommonPopup");
            
            GameObject go;
            if (_prefab != null)
            {
                go = Instantiate(_prefab);
                Instance = go.GetComponent<CommonPopup>();
                if (Instance == null) Instance = go.AddComponent<CommonPopup>();
            }
            else
            {
                go = new GameObject("CommonPopup");
                Instance = go.AddComponent<CommonPopup>();
            }

            // Wire serialized fields or build at runtime
            if (Instance.contentRoot != null)
            {
                Instance._contentRoot = Instance.contentRoot;
                Instance._titleText = Instance.titleText;
                Instance._canvas = go.GetComponent<Canvas>();
            }
            else
            {
                var buildMethod = typeof(CommonPopup).GetMethod("BuildUI", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                buildMethod?.Invoke(Instance, null);
            }

            DontDestroyOnLoad(go);
            return Instance;
        }

        private void BuildUI()
        {
            _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 180;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            gameObject.AddComponent<GraphicRaycaster>();

            // Background
            var bgGo = CreateChild("BG", transform);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.02f, 0.03f, 0.06f, 0.85f);
            Stretch(bgGo);

            // Panel
            var panelGo = CreateChild("Panel", transform);
            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.2f, 0.15f);
            panelRect.anchorMax = new Vector2(0.8f, 0.85f);
            var panelImg = panelGo.AddComponent<Image>();
            panelImg.color = new Color(0.06f, 0.09f, 0.14f, 0.98f);

            // Title bar
            var titleBar = CreateChild("TitleBar", panelGo.transform);
            var titleRect = titleBar.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.93f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            var titleBarImg = titleBar.AddComponent<Image>();
            titleBarImg.color = new Color(0.12f, 0.18f, 0.28f, 1f);

            _titleText = CreateText(titleBar.transform, "", 28, new Color(1f, 0.85f, 0.3f), TextAlignmentOptions.Center);
            Stretch(_titleText.gameObject);

            // Close button
            var closeGo = CreateChild("CloseBtn", titleBar.transform);
            var closeRect = closeGo.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.93f, 0.1f);
            closeRect.anchorMax = new Vector2(0.99f, 0.9f);
            var closeImg = closeGo.AddComponent<Image>();
            closeImg.color = new Color(0.5f, 0.15f, 0.15f);
            var closeTextGo = CreateChild("Text", closeGo.transform);
            Stretch(closeTextGo);
            var closeText = closeTextGo.AddComponent<TextMeshProUGUI>();
            closeText.text = "✕";
            closeText.alignment = TextAlignmentOptions.Center;
            closeText.fontSize = 26;
            closeText.color = Color.white;
            closeText.raycastTarget = false;
            if (_font) closeText.font = _font;
            closeGo.AddComponent<Button>().onClick.AddListener(Close);

            // Scroll content
            var scrollGo = CreateChild("ScrollView", panelGo.transform);
            var scrollRect = scrollGo.GetComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.03f, 0.03f);
            scrollRect.anchorMax = new Vector2(0.97f, 0.91f);
            var scroll = scrollGo.AddComponent<ScrollRect>();

            var viewport = CreateChild("Viewport", scrollGo.transform);
            Stretch(viewport);
            var vpImg = viewport.AddComponent<Image>();
            vpImg.color = new Color(0, 0, 0, 0.01f);
            viewport.AddComponent<RectMask2D>();
            scroll.viewport = viewport.GetComponent<RectTransform>();

            var content = CreateChild("Content", viewport.transform);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1f);
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = contentRect;
            _contentRoot = content.transform;

            gameObject.SetActive(false);
        }

        public Transform ContentRoot => _contentRoot;

        public void Show(string title)
        {
            _titleText.text = title;
            gameObject.SetActive(true);
        }

        public void Close()
        {
            gameObject.SetActive(false);
            // Clear children
            for (int i = _contentRoot.childCount - 1; i >= 0; i--)
                Destroy(_contentRoot.GetChild(i).gameObject);
        }

        private TextMeshProUGUI CreateText(Transform parent, string text, float size, Color color, TextAlignmentOptions alignment)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;
            if (_font) tmp.font = _font;
            return tmp;
        }

        private GameObject CreateChild(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private void Stretch(GameObject go)
        {
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
