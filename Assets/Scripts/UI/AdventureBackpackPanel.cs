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
        private RectTransform _flyTarget;
        private static GameObject _prefab;
        private bool _initialized;

        public static AdventureBackpackPanel GetOrCreate(RectTransform flyTarget)
        {
            if (Instance != null)
            {
                Instance._flyTarget = flyTarget;
                return Instance;
            }

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
            DontDestroyOnLoad(go);
            return Instance;
        }

        private void Awake()
        {
            AutoBindReferences();
        }

        private void AutoBindReferences()
        {
            if (_initialized) return;
            _initialized = true;

            var panel = transform.Find("Panel");
            if (panel == null) return;

            var titleBar = panel.Find("TitleBar");
            if (titleBar != null)
            {
                if (_titleText == null) _titleText = titleBar.Find("Text")?.GetComponent<TextMeshProUGUI>();

                // 绑定关闭按钮
                var closeBtn = titleBar.Find("CloseBtn")?.GetComponent<Button>();
                if (closeBtn != null) closeBtn.onClick.AddListener(Close);
            }

            var scroll = panel.Find("ScrollView");
            if (scroll != null)
            {
                var viewport = scroll.Find("Viewport");
                if (viewport != null)
                {
                    var content = viewport.Find("Content");
                    if (content != null) _contentRoot = content;
                }
            }

            _canvas = GetComponent<Canvas>();
        }

        public void Show()
        {
            if (!_initialized) AutoBindReferences();
            gameObject.SetActive(true);
            RefreshItems();
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        private void RefreshItems()
        {
            if (_contentRoot == null) return;

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

                var itemView = ItemView.Create(_contentRoot, "Item_" + item.ItemName);
                itemView.SetData(
                    icon: item.ItemIcon,
                    name: item.ItemName,
                    quality: (item as MaterialData)?.quality ?? ItemQuality.LianQi_T1,
                    count: slot.count
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

            if (_titleText != null)
                _titleText.text = $"冒险收获 ({totalItems}件)";
        }
    }
}
