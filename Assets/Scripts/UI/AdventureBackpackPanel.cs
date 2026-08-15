using System.Collections.Generic;
using QFramework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NueGames.NueDeck.Scripts.Enums;
using CardGame.UI.Common;

namespace CardGame.UI
{
    /// <summary>
    /// 冒险背包面板 — 展示本次冒险获得的所有物品（不含卡牌）。
    /// 使用通用 ItemView 组件展示。点击物品触发飞行特效飞向背包按钮。
    /// </summary>
    public class AdventureBackpackPanel : MonoBehaviour
    {
        public static AdventureBackpackPanel Instance { get; private set; }

        [SerializeField] private Transform contentRoot;
        [SerializeField] private TextMeshProUGUI titleText;
        private Canvas _canvas;
        private Transform _contentRoot;
        private TextMeshProUGUI _titleText;
        private readonly List<GameObject> _spawnedItems = new List<GameObject>();
        private TMP_FontAsset _font;
        private RectTransform _flyTarget;
        private static GameObject _prefab;

        public static AdventureBackpackPanel GetOrCreate(RectTransform flyTarget)
        {
            if (Instance != null)
            {
                Instance._flyTarget = flyTarget;
                return Instance;
            }

            // Load prefab
            if (_prefab == null)
                _prefab = Resources.Load<GameObject>("Prefabs/Common/AdventureBackpackPanel");
            
            GameObject go;
            if (_prefab != null)
            {
                go = Instantiate(_prefab);
                Instance = go.GetComponent<AdventureBackpackPanel>();
                if (Instance == null) Instance = go.AddComponent<AdventureBackpackPanel>();
            }
            else
            {
                go = new GameObject("AdventureBackpackPanel");
                Instance = go.AddComponent<AdventureBackpackPanel>();
            }
            
            Instance._flyTarget = flyTarget;
            
            // Wire serialized fields or build at runtime
            if (Instance.contentRoot != null)
            {
                Instance._contentRoot = Instance.contentRoot;
                Instance._titleText = Instance.titleText;
                Instance._canvas = go.GetComponent<Canvas>();
            }
            else
            {
                // Fallback: build UI at runtime
                var buildMethod = typeof(AdventureBackpackPanel).GetMethod("BuildUI", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                buildMethod?.Invoke(Instance, null);
            }
            
            DontDestroyOnLoad(go);
            return Instance;
        }

        private void BuildUI()
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 150;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            gameObject.AddComponent<GraphicRaycaster>();

            _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

            // Background
            var bgGo = CreateChild("BG", transform);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.02f, 0.03f, 0.06f, 0.85f);
            Stretch(bgGo);

            // Panel
            var panelGo = CreateChild("Panel", transform);
            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.2f, 0.1f);
            panelRect.anchorMax = new Vector2(0.8f, 0.9f);
            var panelImg = panelGo.AddComponent<Image>();
            panelImg.color = new Color(0.06f, 0.09f, 0.14f, 0.98f);

            // Title bar
            var titleBar = CreateChild("TitleBar", panelGo.transform);
            var titleRect = titleBar.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.93f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            var titleBarImg = titleBar.AddComponent<Image>();
            titleBarImg.color = new Color(0.12f, 0.18f, 0.28f, 1f);

            _titleText = CreateText(titleBar.transform, "冒险收获", 28, new Color(1f, 0.85f, 0.3f));
            Stretch(_titleText.gameObject);
            _titleText.alignment = TextAlignmentOptions.Center;

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

            // Scroll View
            var scrollGo = CreateChild("ScrollView", panelGo.transform);
            var scrollRect = scrollGo.GetComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.03f, 0.03f);
            scrollRect.anchorMax = new Vector2(0.97f, 0.91f);
            var scroll = scrollGo.AddComponent<ScrollRect>();

            // Viewport
            var viewport = CreateChild("Viewport", scrollGo.transform);
            Stretch(viewport);
            var vpImg = viewport.AddComponent<Image>();
            vpImg.color = new Color(0, 0, 0, 0.01f);
            viewport.AddComponent<RectMask2D>();
            scroll.viewport = viewport.GetComponent<RectTransform>();

            // Content — VerticalLayout (each item is a row)
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

        public void Show()
        {
            gameObject.SetActive(true);
            RefreshItems();
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        private void RefreshItems()
        {
            // Clear old
            foreach (var item in _spawnedItems)
                if (item != null) Destroy(item);
            _spawnedItems.Clear();

            var invModel = CardGameArchitecture.Interface.GetModel<IInventoryModel>();
            if (invModel?.Slots == null) return;

            int totalItems = 0;
            foreach (var slot in invModel.Slots)
            {
                if (slot == null || slot.IsEmpty) continue;
                var item = slot.item;
                if (item == null) continue;

                // Use universal ItemView
                var itemView = ItemView.Create(_contentRoot, "Item_" + item.ItemName);
                itemView.SetData(
                    icon: item.ItemIcon,
                    name: item.ItemName,
                    desc: item.ItemDescription,
                    quality: (item as MaterialData)?.quality ?? ItemQuality.LianQi_T1,
                    count: slot.count,
                    isExhaust: false
                );

                // Add click → fly animation
                var btn = itemView.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                var capturedIcon = item.ItemIcon;
                btn.onClick.AddListener(() =>
                {
                    if (capturedIcon == null || _flyTarget == null) return;
                    var fly = LootFlyAnimation.Instance;
                    if (fly == null)
                    {
                        var flyGo = new GameObject("LootFlyAnimation");
                        fly = flyGo.AddComponent<LootFlyAnimation>();
                    }
                    var screenPos = Input.mousePosition;
                    var targetScreenPos = RectTransformUtility.WorldToScreenPoint(null, _flyTarget.position);
                    fly.SpawnAndFly(capturedIcon, screenPos, targetScreenPos);
                });

                _spawnedItems.Add(itemView.gameObject);
                totalItems += slot.count;
            }

            _titleText.text = $"冒险收获 ({totalItems}件)";
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
            rect.anchorMax = Vector3.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
