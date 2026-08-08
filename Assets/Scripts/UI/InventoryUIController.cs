using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using Alchemy.Inspector;
using CardGame.Audio;

namespace CardGame.UI
{
    public class InventoryUIController : MonoBehaviour, IController
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

        public IArchitecture GetArchitecture() => CardGameArchitecture.Interface;

        private void Awake()
        {
            _model = this.GetModel<IInventoryModel>();
            _system = this.GetSystem<IInventorySystem>();
            _font = UnityEngine.Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            BuildUI();
            UIHelper.EnsureCloseButton(this, OnBackButton);
        }

        private void BuildUI()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 60;
                var scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                gameObject.AddComponent<GraphicRaycaster>();
            }
            Transform panel = transform.Find("Panel");
            if (panel == null)
            {
                var panelObj = new GameObject("Panel");
                panelObj.transform.SetParent(transform, false);
                var panelRt = panelObj.AddComponent<RectTransform>();
                panelRt.anchorMin = new Vector2(0.05f, 0.05f); panelRt.anchorMax = new Vector2(0.95f, 0.95f);
                panelRt.offsetMin = Vector2.zero; panelRt.offsetMax = Vector2.zero;
                panelObj.AddComponent<Image>().color = new Color(0.08f, 0.1f, 0.15f, 0.95f);
                panel = panelObj.transform;
            }

            // 标题
            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(panel, false);
            var titleRt = titleObj.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.93f); titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.offsetMin = Vector2.zero; titleRt.offsetMax = Vector2.zero;
            var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "储物袋"; titleTmp.fontSize = 28; titleTmp.color = new Color(0.9f, 0.8f, 0.3f);
            titleTmp.alignment = TextAlignmentOptions.Center;
            if (_font) titleTmp.font = _font;

            // 负重显示
            if (weightText == null)
            {
                var wObj = new GameObject("WeightText");
                wObj.transform.SetParent(panel, false);
                var wRt = wObj.AddComponent<RectTransform>();
                wRt.anchorMin = new Vector2(0.02f, 0.88f); wRt.anchorMax = new Vector2(0.3f, 0.93f);
                wRt.offsetMin = Vector2.zero; wRt.offsetMax = Vector2.zero;
                weightText = wObj.AddComponent<TextMeshProUGUI>();
                weightText.fontSize = 16; weightText.color = new Color(0.6f, 0.8f, 1f);
                weightText.alignment = TextAlignmentOptions.Left;
                if (_font) weightText.font = _font;
            }

            // 分类Tab
            if (tabButtons == null || tabButtons.Length == 0)
            {
                var tabContainer = new GameObject("TabContainer");
                tabContainer.transform.SetParent(panel, false);
                var tabRt = tabContainer.AddComponent<RectTransform>();
                tabRt.anchorMin = new Vector2(0.02f, 0.83f); tabRt.anchorMax = new Vector2(0.98f, 0.88f);
                tabRt.offsetMin = Vector2.zero; tabRt.offsetMax = Vector2.zero;

                var hlg = tabContainer.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = 3; hlg.childControlWidth = true; hlg.childControlHeight = true;
                hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;

                string[] tabNames = { "全部", "灵草", "矿石", "妖丹", "魂石", "灵水", "残片", "灵木", "兽骨", "天宝" };
                var btns = new Transform[tabNames.Length];
                for (int i = 0; i < tabNames.Length; i++)
                {
                    var go = new GameObject($"Tab_{i}");
                    go.transform.SetParent(tabContainer.transform, false);
                    go.AddComponent<RectTransform>();
                    var img = go.AddComponent<Image>();
                    img.color = i == 0 ? new Color(0.2f, 0.5f, 0.3f, 1f) : new Color(0.2f, 0.2f, 0.28f, 1f);

                    var txtObj = new GameObject("Text");
                    txtObj.transform.SetParent(go.transform, false);
                    var txtRt = txtObj.AddComponent<RectTransform>();
                    txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
                    txtRt.offsetMin = Vector2.zero; txtRt.offsetMax = Vector2.zero;
                    var tmp = txtObj.AddComponent<TextMeshProUGUI>();
                    tmp.text = tabNames[i]; tmp.fontSize = 14; tmp.color = Color.white;
                    tmp.alignment = TextAlignmentOptions.Center;
                    if (_font) tmp.font = _font;

                    var btn = go.AddComponent<Button>();
                    int idx = i;
                    btn.onClick.AddListener(() => {
                        if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick);
                        OnTabSelected(idx);
                        // 更新tab颜色
                        for (int j = 0; j < btns.Length; j++)
                        {
                            var bImg = btns[j]?.GetComponent<Image>();
                            if (bImg) bImg.color = j == idx ? new Color(0.2f, 0.5f, 0.3f, 1f) : new Color(0.2f, 0.2f, 0.28f, 1f);
                        }
                    });
                    btns[i] = go.transform;
                }
                tabButtons = btns;
            }

            // 物品网格（滚动）
            if (itemGridRoot == null)
            {
                var gridPanel = new GameObject("GridPanel");
                gridPanel.transform.SetParent(panel, false);
                var gpRt = gridPanel.AddComponent<RectTransform>();
                gpRt.anchorMin = new Vector2(0.02f, 0.05f); gpRt.anchorMax = new Vector2(0.98f, 0.83f);
                gpRt.offsetMin = Vector2.zero; gpRt.offsetMax = Vector2.zero;
                gridPanel.AddComponent<Image>().color = new Color(0.05f, 0.08f, 0.12f, 0.8f);

                var scroll = gridPanel.AddComponent<ScrollRect>();
                scroll.horizontal = false;

                var viewport = new GameObject("Viewport");
                viewport.transform.SetParent(gridPanel.transform, false);
                var vpRt = viewport.AddComponent<RectTransform>();
                vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
                vpRt.pivot = new Vector2(0, 1);
                vpRt.offsetMin = new Vector2(5, 5); vpRt.offsetMax = new Vector2(-5, -5);
                var vpImg = viewport.AddComponent<Image>();
                vpImg.color = new Color(0, 0, 0, 0.01f);
                scroll.viewport = vpRt;

                var content = new GameObject("Content");
                content.transform.SetParent(viewport.transform, false);
                var contentRt = content.AddComponent<RectTransform>();
                contentRt.anchorMin = new Vector2(0, 1); contentRt.anchorMax = new Vector2(1, 1);
                contentRt.pivot = new Vector2(0.5f, 1);
                contentRt.offsetMin = Vector2.zero; contentRt.offsetMax = Vector2.zero;

                var grid = content.AddComponent<GridLayoutGroup>();
                grid.spacing = new Vector2(5, 5);
                grid.padding = new RectOffset(5, 5, 5, 5);
                grid.cellSize = new Vector2(130, 130);
                grid.constraint = GridLayoutGroup.Constraint.Flexible;
                var csf = content.AddComponent<ContentSizeFitter>();
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                scroll.content = contentRt;
                itemGridRoot = content.transform;
            }
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
            if (itemGridRoot == null) return;

            foreach (Transform child in itemGridRoot)
                Destroy(child.gameObject);

            foreach (var slot in _model.Slots)
            {
                if (slot == null || slot.IsEmpty) continue;

                if (_currentFilter.HasValue && slot.item is MaterialData mat)
                {
                    if (mat.materialType != _currentFilter.Value) continue;
                }

                var slotGo = CreateSimpleSlot(itemGridRoot);
                var tmps = slotGo.GetComponentsInChildren<TextMeshProUGUI>();
                if (tmps.Length > 0)
                {
                    string rarityColor = "";
                    if (slot.item is MaterialData m)
                    {
                        rarityColor = m.rarity switch
                        {
                            MaterialRarity.FanPin => "<color=#AAAAAA>",
                            MaterialRarity.LingPin => "<color=#4FC3F7>",
                            MaterialRarity.XuanPin => "<color=#CE93D8>",
                            MaterialRarity.XianPin => "<color=#FFD54F>",
                            _ => ""
                        };
                    }
                    tmps[0].text = $"{rarityColor}{slot.item.ItemName}</color>\n×{slot.count}";
                }

                var btn = slotGo.GetComponentInChildren<Button>();
                if (btn == null) btn = slotGo.AddComponent<Button>();
            }
        }

        private GameObject CreateSimpleSlot(Transform parent)
        {
            var go = new GameObject("ItemSlot");
            go.transform.SetParent(parent);
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
    }
}
