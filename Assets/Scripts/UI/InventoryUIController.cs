using System.Collections.Generic;
using NueGames.NueDeck.Scripts.Enums;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using Alchemy.Inspector;
using CardGame.Audio;

namespace CardGame.UI
{
    public class InventoryUIController : MonoBehaviour, IController, LoopScrollDataSource
    {
        [FoldoutGroup("References")]
        [SerializeField] private Transform itemGridRoot;
        [SerializeField] private GameObject itemSlotPrefab;
        [SerializeField] private TextMeshProUGUI weightText;
        [SerializeField] private Button backButton;
        [SerializeField] private Transform[] tabButtons;

        private IInventoryModel _model;
        private IInventorySystem _system;
        private MaterialType? _currentFilter;
        private TMP_FontAsset _font;
        private LoopVerticalScrollRect _loopScroll;
        private LoopScrollPrefabSourceImpl _prefabSource;
        private GameObject _itemTemplate;
        private List<InventorySlot> _filteredSlots = new List<InventorySlot>();

        public IArchitecture GetArchitecture() => CardGameArchitecture.Interface;

        private void Awake()
        {
            _model = this.GetModel<IInventoryModel>();
            _system = this.GetSystem<IInventorySystem>();
            _font = UnityEngine.Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            AutoBindReferences();
            UIHelper.EnsureCloseButton(this, OnBackButton);
        }


        private void OnEnable()
        {
            if (_model != null)
                _model.CurrentWeight.RegisterWithInitValue(UpdateWeight).UnRegisterWhenGameObjectDestroyed(gameObject);
            Refresh();
        }

        private void UpdateWeight(int weight)
        {
            if (weightText) weightText.text = $"负重: {weight}/{_model.MaxWeight.Value}";
        }

        public void Refresh()
        {
            if (itemGridRoot == null || _loopScroll == null) return;

            _filteredSlots.Clear();
            foreach (var slot in _model.Slots)
            {
                if (slot == null || slot.IsEmpty) continue;
                if (_currentFilter.HasValue && slot.item is MaterialData mat)
                {
                    if (mat.materialType != _currentFilter.Value) continue;
                }
                _filteredSlots.Add(slot);
            }

            _loopScroll.totalCount = _filteredSlots.Count;
            if (_loopScroll != null && _loopScroll.prefabSource != null) if (_loopScroll != null && _loopScroll.prefabSource != null) _loopScroll.RefillCells();
        }

        public void ProvideData(Transform transform, int idx)
        {
            if (idx < 0 || idx >= _filteredSlots.Count) return;
            var slot = _filteredSlots[idx];
            var tmps = transform.GetComponentsInChildren<TextMeshProUGUI>();
            if (tmps.Length > 0)
            {
                string rarityColor = "";
                if (slot.item is MaterialData m)
                {
                    var c = ItemQualityHelper.GetColor(m.quality);
                    rarityColor = $"<color=#{ColorUtility.ToHtmlStringRGB(c)}>";
                }
                tmps[0].text = $"{rarityColor}{slot.item.ItemName}</color>\n×{slot.count}";
            }
            var img = transform.GetComponent<Image>();
            if (img) img.color = new Color(0.12f, 0.15f, 0.22f, 1f);
        }

        private GameObject CreateSimpleSlot(Transform parent)
        {
            var go = new GameObject("ItemSlot");
            if (parent != null) go.transform.SetParent(parent);
            go.AddComponent<RectTransform>();
            go.AddComponent<Image>().color = new Color(0.12f, 0.15f, 0.22f, 1f);

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(go.transform);
            var rt = textObj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 14; tmp.color = Color.white;
            tmp.richText = true;
            if (_font) tmp.font = _font;
            return go;
        }

        public void OnTabSelected(int tabIndex)
        {
            _currentFilter = tabIndex switch
            {
                0 => null,
                1 => MaterialType.SpiritHerb,
                2 => MaterialType.Ore,
                3 => MaterialType.DemonCore,
                4 => MaterialType.SoulStone,
                5 => MaterialType.SpiritWater,
                6 => MaterialType.Fragment,
                7 => MaterialType.SpiritWood,
                8 => MaterialType.BeastBone,
                9 => MaterialType.HeavenlyTreasure,
                _ => null
            };
            Refresh();
        }

        public void OnBackButton()
        {
            gameObject.SetActive(false);
        }
        private void AutoBindReferences()
        {
            var panel = transform.Find("Panel");
            if (panel == null) return;
            if (itemGridRoot == null) itemGridRoot = panel.Find("GridPanel/ScrollView/Viewport/Content");
            if (weightText == null) weightText = panel.Find("WeightText")?.GetComponent<TMPro.TextMeshProUGUI>();
            var tabContainer = panel.Find("TabContainer");
            if (tabButtons == null || tabButtons.Length == 0)
            {
                var tabs = new List<Transform>();
                for (int i = 0; i < 10; i++)
                {
                    var tab = tabContainer?.Find($"Tab_{i}");
                    if (tab != null) tabs.Add(tab);
                }
                tabButtons = tabs.ToArray();
            }
            // Wire tab buttons
            for (int i = 0; i < tabButtons.Length; i++)
            {
                var btn = tabButtons[i].GetComponent<Button>();
                if (btn != null)
                {
                    int idx = i;
                    btn.onClick.AddListener(() => OnTabSelected(idx));
                }
            }
            // Setup LoopScrollRect on GridPanel ScrollView
            var scrollObj = panel.Find("GridPanel/ScrollView");
            if (scrollObj != null && _loopScroll == null)
            {
                scrollObj.gameObject.SetActive(false);
                // Get LoopScrollRect from prefab (pre-configured)
                _loopScroll = scrollObj.GetComponent<LoopVerticalScrollRect>();
                _loopScroll.dataSource = this;
                _itemTemplate = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Base/Inventory/InventoryItem.prefab");
                _prefabSource = new LoopScrollPrefabSourceImpl(_itemTemplate, scrollObj);
                _loopScroll.prefabSource = _prefabSource;
                _loopScroll.dataSource = this;
                _loopScroll.viewport = scrollObj.Find("Viewport")?.GetComponent<RectTransform>();
                _loopScroll.content = itemGridRoot?.GetComponent<RectTransform>();

                // Create item template
                _itemTemplate = new GameObject("ItemSlot");
                _itemTemplate.transform.SetParent(transform, false);
                _itemTemplate.SetActive(false);
                var slotRt = _itemTemplate.AddComponent<RectTransform>();
                slotRt.sizeDelta = new Vector2(160, 50);
                var slotImg = _itemTemplate.AddComponent<Image>();
                slotImg.color = new Color(0.12f, 0.15f, 0.2f, 1f);
                var slotText = new GameObject("Text");
                slotText.transform.SetParent(_itemTemplate.transform, false);
                var stRt = slotText.AddComponent<RectTransform>();
                stRt.anchorMin = Vector2.zero; stRt.anchorMax = Vector2.one;
                stRt.offsetMin = new Vector2(5, 2); stRt.offsetMax = new Vector2(-5, -2);
                var stTmp = slotText.AddComponent<TextMeshProUGUI>();
                stTmp.fontSize = 14; stTmp.color = Color.white; stTmp.alignment = TextAlignmentOptions.Center;
                _prefabSource = new LoopScrollPrefabSourceImpl(_itemTemplate, scrollObj);
                _loopScroll.prefabSource = _prefabSource;
            }
        }

    }

}
