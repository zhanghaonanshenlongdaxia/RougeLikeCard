using System.Collections.Generic;
using NueGames.NueDeck.Scripts.Data.Collection;
using NueGames.NueDeck.Scripts.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NueGames.NueDeck.Scripts.UI
{
    /// <summary>
    /// 卡堆查看面板 — 点击抽牌/弃牌/消耗堆时弹出，展示里面的卡牌。
    /// 运行时自动创建，无需场景中预置。
    /// </summary>
    public class PileViewCanvas : MonoBehaviour
    {
        private Canvas _canvas;
        private TextMeshProUGUI _titleText;
        private Transform _contentRoot;
        private GridLayoutGroup _gridLayout;
        private readonly List<GameObject> _spawnedItems = new List<GameObject>();
        private static PileViewCanvas _instance;
        private TMP_FontAsset _font;

        public static PileViewCanvas GetOrCreate()
        {
            if (_instance != null) return _instance;
            var go = new GameObject("PileViewCanvas");
            _instance = go.AddComponent<PileViewCanvas>();
            _instance.BuildUI();
            DontDestroyOnLoad(go);
            return _instance;
        }

        private void BuildUI()
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 200;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            gameObject.AddComponent<GraphicRaycaster>();

            _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

            // Background
            var bgGo = CreateChild("Background", transform);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.02f, 0.03f, 0.06f, 0.85f);
            Stretch(bgGo);

            // Panel
            var panelGo = CreateChild("Panel", transform);
            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.1f, 0.08f);
            panelRect.anchorMax = new Vector2(0.9f, 0.92f);
            var panelImg = panelGo.AddComponent<Image>();
            panelImg.color = new Color(0.06f, 0.09f, 0.14f, 0.98f);

            // Title bar
            var titleBarGo = CreateChild("TitleBar", panelGo.transform);
            var titleBarRect = titleBarGo.GetComponent<RectTransform>();
            titleBarRect.anchorMin = new Vector2(0f, 0.93f);
            titleBarRect.anchorMax = new Vector2(1f, 1f);
            var titleBarImg = titleBarGo.AddComponent<Image>();
            titleBarImg.color = new Color(0.12f, 0.18f, 0.28f, 1f);

            _titleText = CreateText(titleBarGo.transform, "", 28, new Color(1f, 0.85f, 0.3f));
            Stretch(_titleText.gameObject);
            _titleText.alignment = TextAlignmentOptions.Center;

            // Close button
            var closeGo = CreateChild("CloseButton", titleBarGo.transform);
            var closeRect = closeGo.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.93f, 0.1f);
            closeRect.anchorMax = new Vector2(0.99f, 0.9f);
            closeRect.offsetMin = Vector2.zero;
            closeRect.offsetMax = Vector2.zero;
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
            var closeBtn = closeGo.AddComponent<Button>();
            closeBtn.onClick.AddListener(Close);

            // Scroll View
            var scrollGo = CreateChild("ScrollView", panelGo.transform);
            var scrollRect = scrollGo.GetComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.03f, 0.03f);
            scrollRect.anchorMax = new Vector2(0.97f, 0.91f);
            scrollRect.offsetMin = Vector2.zero;
            scrollRect.offsetMax = Vector2.zero;
            var scroll = scrollGo.AddComponent<ScrollRect>();

            // Viewport
            var viewportGo = CreateChild("Viewport", scrollGo.transform);
            Stretch(viewportGo);
            var viewportImg = viewportGo.AddComponent<Image>();
            viewportImg.color = new Color(0, 0, 0, 0.01f);
            viewportGo.AddComponent<RectMask2D>();
            scroll.viewport = viewportGo.GetComponent<RectTransform>();

            // Content
            var contentGo = CreateChild("Content", viewportGo.transform);
            var contentRect = contentGo.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1f);
            _gridLayout = contentGo.AddComponent<GridLayoutGroup>();
            _gridLayout.cellSize = new Vector2(200, 80);
            _gridLayout.spacing = new Vector2(10, 10);
            _gridLayout.padding = new RectOffset(10, 10, 10, 10);
            _gridLayout.childAlignment = TextAnchor.UpperCenter;
            contentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = contentRect;
            _contentRoot = contentGo.transform;

            gameObject.SetActive(false);
        }

        public void Show(List<CardData> cards, string title)
        {
            if (_gridLayout == null) BuildUI();
            if (_gridLayout == null) return;

            gameObject.SetActive(true);
            _titleText.text = $"{title} ({cards.Count})";

            // Clear old
            foreach (var item in _spawnedItems)
                if (item != null) Destroy(item);
            _spawnedItems.Clear();

            // Spawn card info items
            foreach (var cardData in cards)
            {
                var item = CreateCardItem(cardData);
                _spawnedItems.Add(item);
            }
        }

        private GameObject CreateCardItem(CardData cardData)
        {
            var go = new GameObject("Card_" + cardData.CardName);
            go.transform.SetParent(_contentRoot, false);
            go.AddComponent<RectTransform>();

            var bg = go.AddComponent<Image>();
            // Color by rarity
            bg.color = cardData.Rarity switch
            {
    Enums.RarityType.Common => new Color(0.15f, 0.18f, 0.22f, 1f),
    Enums.RarityType.Uncommon => new Color(0.1f, 0.2f, 0.1f, 1f),
    Enums.RarityType.Rare => new Color(0.2f, 0.12f, 0.2f, 1f),
    Enums.RarityType.Legendary => new Color(0.25f, 0.18f, 0.05f, 1f),
    _ => new Color(0.15f, 0.18f, 0.22f, 1f)
            };

            var hlg = go.AddComponent<VerticalLayoutGroup>();
            hlg.spacing = 2;
            hlg.padding = new RectOffset(8, 8, 5, 5);
            hlg.childControlWidth = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = false;

            // Card name
            var nameText = CreateText(go.transform, cardData.CardName, 16, Color.white);
            nameText.alignment = TextAlignmentOptions.Center;

            // Mana cost + description
            var desc = $"{cardData.ManaCost}费  {cardData.MyDescription ?? ""}";
            if (desc.Length > 40) desc = desc.Substring(0, 37) + "...";
            var descText = CreateText(go.transform, desc, 12, new Color(0.7f, 0.75f, 0.8f));
            descText.alignment = TextAlignmentOptions.Center;

            return go;
        }

        private TextMeshProUGUI CreateText(Transform parent, string text, float size, Color color)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            if (_font) tmp.font = _font;
            return tmp;
        }

        public void Close()
        {
            gameObject.SetActive(false);
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

