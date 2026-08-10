using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using CardGame.Audio;

namespace CardGame.UI
{
    /// <summary>
    /// 祭祀抽奖UI：选材料献祭 → 抽奖获得随机物品。
    /// 材料类型决定产出类型，材料品阶决定产出品阶。
    /// </summary>
    public class RitualUIController : MonoBehaviour, IController
    {
        private TMP_FontAsset _font;
        private IRitualSystem _ritualSystem;
        private IInventoryModel _inventoryModel;
        private IInventorySystem _inventorySystem;

        private Transform _materialListRoot;
        private Transform _offeringRoot;
        private TextMeshProUGUI _previewText;
        private TextMeshProUGUI _resultText;
        private Button _sacrificeBtn;

        private List<(MaterialData material, int count)> _selectedMaterials = new List<(MaterialData, int)>();

        public IArchitecture GetArchitecture() => CardGameArchitecture.Interface;

        private void Awake()
        {
            _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            _ritualSystem = this.GetSystem<IRitualSystem>();
            _inventoryModel = this.GetModel<IInventoryModel>();
            _inventorySystem = this.GetSystem<IInventorySystem>();
            BuildUI();
            UIHelper.EnsureCloseButton(this, () => gameObject.SetActive(false));
        }

        private void OnEnable()
        {
            RefreshMaterialList();
        }

        private void BuildUI()
        {
            var canvas = gameObject.GetComponent<Canvas>();
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

            // 清理 prefab 子物体
            var panel = transform.Find("Panel");
            if (panel != null)
            {
                for (int i = panel.childCount - 1; i >= 0; i--)
                    DestroyImmediate(panel.GetChild(i).gameObject);
                var vlg = panel.GetComponent<VerticalLayoutGroup>();
                if (vlg) DestroyImmediate(vlg);
                var csf = panel.GetComponent<ContentSizeFitter>();
                if (csf) DestroyImmediate(csf);
                if (panel.GetComponent<Image>() == null)
                    panel.gameObject.AddComponent<Image>().color = new Color(0.06f, 0.04f, 0.1f, 0.97f);
            }
            else
            {
                var panelObj = new GameObject("Panel");
                panelObj.transform.SetParent(transform, false);
                var rt = panelObj.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.05f, 0.05f); rt.anchorMax = new Vector2(0.95f, 0.95f);
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                panelObj.AddComponent<Image>().color = new Color(0.06f, 0.04f, 0.1f, 0.97f);
                panel = panelObj.transform;
            }

            // 标题
            var titleObj = CreateText(panel, "祭祀", 26, new Color(0.9f, 0.5f, 1f));
            titleObj.GetComponent<RectTransform>().anchorMin = new Vector2(0.02f, 0.93f);
            titleObj.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 1f);

            // 说明
            var descObj = CreateText(panel, "献祭材料抽奖：灵草→丹药 矿石→法宝 妖丹→卡牌 魂石→配方", 14, new Color(0.6f, 0.5f, 0.7f));
            descObj.GetComponent<RectTransform>().anchorMin = new Vector2(0.02f, 0.88f);
            descObj.GetComponent<RectTransform>().anchorMax = new Vector2(0.98f, 0.93f);

            // 左侧材料列表
            var leftPanel = new GameObject("MaterialList");
            leftPanel.transform.SetParent(panel, false);
            var lRt = leftPanel.AddComponent<RectTransform>();
            lRt.anchorMin = new Vector2(0.02f, 0.05f); lRt.anchorMax = new Vector2(0.48f, 0.86f);
            lRt.offsetMin = Vector2.zero; lRt.offsetMax = Vector2.zero;
            leftPanel.AddComponent<Image>().color = new Color(0.05f, 0.03f, 0.08f, 0.8f);

            var leftTitle = CreateText(leftPanel.transform, "背包材料", 18, new Color(0.7f, 0.6f, 0.8f));
            leftTitle.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0.92f);
            leftTitle.GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);

            // 滚动列表
            var scroll = CreateScroll(leftPanel.transform, 0f, 0.85f);
            _materialListRoot = scroll;

            // 右侧献祭区
            var rightPanel = new GameObject("OfferingArea");
            rightPanel.transform.SetParent(panel, false);
            var rRt = rightPanel.AddComponent<RectTransform>();
            rRt.anchorMin = new Vector2(0.52f, 0.05f); rRt.anchorMax = new Vector2(0.98f, 0.86f);
            rRt.offsetMin = Vector2.zero; rRt.offsetMax = Vector2.zero;
            rightPanel.AddComponent<Image>().color = new Color(0.08f, 0.05f, 0.12f, 0.8f);

            var rightTitle = CreateText(rightPanel.transform, "献祭台", 18, new Color(0.7f, 0.6f, 0.8f));
            rightTitle.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0.92f);
            rightTitle.GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);

            // 已选材料区
            var offeringLabel = CreateText(rightPanel.transform, "已选材料:", 15, new Color(0.8f, 0.7f, 0.9f));
            offeringLabel.GetComponent<RectTransform>().anchorMin = new Vector2(0.02f, 0.75f);
            offeringLabel.GetComponent<RectTransform>().anchorMax = new Vector2(0.98f, 0.82f);

            var offeringScroll = CreateScroll(rightPanel.transform, 0.1f, 0.75f, "OfferingScroll");
            _offeringRoot = offeringScroll;

            // 预览信息
            _previewText = CreateText(rightPanel.transform, "", 15, new Color(0.6f, 0.8f, 0.6f)).GetComponent<TextMeshProUGUI>();
            _previewText.GetComponent<RectTransform>().anchorMin = new Vector2(0.02f, 0.4f);
            _previewText.GetComponent<RectTransform>().anchorMax = new Vector2(0.98f, 0.55f);

            // 祭祀按钮
            var sacBtnObj = new GameObject("SacrificeButton");
            sacBtnObj.transform.SetParent(rightPanel.transform, false);
            _sacrificeBtn = sacBtnObj.AddComponent<Button>();
            var sRt = sacBtnObj.AddComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0.2f, 0.05f); sRt.anchorMax = new Vector2(0.8f, 0.15f);
            sRt.offsetMin = Vector2.zero; sRt.offsetMax = Vector2.zero;
            sacBtnObj.AddComponent<Image>().color = new Color(0.4f, 0.15f, 0.5f, 1f);
            var sTxt = CreateText(_sacrificeBtn.transform, "祭祀！", 22, Color.white);
            sTxt.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            sTxt.GetComponent<RectTransform>().anchorMax = Vector2.one;
            sTxt.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            sTxt.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            _sacrificeBtn.onClick.AddListener(OnSacrifice);

            // 结果显示
            _resultText = CreateText(rightPanel.transform, "", 18, new Color(1f, 0.85f, 0.3f)).GetComponent<TextMeshProUGUI>();
            _resultText.GetComponent<RectTransform>().anchorMin = new Vector2(0.02f, 0.18f);
            _resultText.GetComponent<RectTransform>().anchorMax = new Vector2(0.98f, 0.38f);
        }

        private void RefreshMaterialList()
        {
            if (_materialListRoot == null) return;
            for (int i = _materialListRoot.childCount - 1; i >= 0; i--)
                Destroy(_materialListRoot.GetChild(i).gameObject);

            var slots = _inventoryModel.Slots;
            if (slots == null) return;

            foreach (var slot in slots)
            {
                if (slot == null || slot.IsEmpty) continue;
                if (slot.item is not MaterialData mat) continue;

                var entry = new GameObject($"Mat_{mat.materialId}");
                entry.transform.SetParent(_materialListRoot, false);
                entry.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 40);
                entry.AddComponent<Image>().color = new Color(0.1f, 0.08f, 0.15f, 0.9f);
                var le = entry.AddComponent<LayoutElement>();
                le.preferredHeight = 40; le.flexibleWidth = 1;

                var hlg = entry.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = 5; hlg.padding = new RectOffset(5, 5, 2, 2);

                var nameTxt = CreateText(entry.transform, $"{mat.name} [{mat.rarity}] ×{slot.count}", 14, Color.white);
                nameTxt.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
                nameTxt.AddComponent<LayoutElement>().flexibleWidth = 1;

                var addBtnObj = new GameObject("AddBtn");
                addBtnObj.transform.SetParent(entry.transform, false);
                addBtnObj.AddComponent<RectTransform>().sizeDelta = new Vector2(60, 30);
                addBtnObj.AddComponent<Image>().color = new Color(0.2f, 0.15f, 0.35f, 1f);
                var addBtn = addBtnObj.AddComponent<Button>();
                var btnTxt = CreateText(addBtn.transform, "+选择", 13, Color.white);

                var captured = mat;
                var capturedCount = slot.count;
                addBtn.onClick.AddListener(() => AddMaterial(captured, capturedCount));
            }
        }

        private void AddMaterial(MaterialData mat, int availableCount)
        {
            // 检查是否已选过
            var existing = _selectedMaterials.FindIndex(s => s.material.materialId == mat.materialId);
            if (existing >= 0)
            {
                if (_selectedMaterials[existing].count >= availableCount) return;
                _selectedMaterials[existing] = (mat, _selectedMaterials[existing].count + 1);
            }
            else
            {
                _selectedMaterials.Add((mat, 1));
            }
            RefreshOffering();
        }

        private void RemoveMaterial(int index)
        {
            if (index < 0 || index >= _selectedMaterials.Count) return;
            _selectedMaterials.RemoveAt(index);
            RefreshOffering();
        }

        private void RefreshOffering()
        {
            if (_offeringRoot == null) return;
            for (int i = _offeringRoot.childCount - 1; i >= 0; i--)
                Destroy(_offeringRoot.GetChild(i).gameObject);

            for (int i = 0; i < _selectedMaterials.Count; i++)
            {
                var (mat, cnt) = _selectedMaterials[i];
                var entry = new GameObject($"Offering_{i}");
                entry.transform.SetParent(_offeringRoot, false);
                entry.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 35);
                entry.AddComponent<Image>().color = new Color(0.15f, 0.1f, 0.2f, 0.9f);
                entry.AddComponent<LayoutElement>().preferredHeight = 35;

                var hlg = entry.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = 5; hlg.padding = new RectOffset(5, 5, 2, 2);

                var txt = CreateText(entry.transform, $"{mat.name} [{mat.rarity}] ×{cnt}", 14, new Color(0.8f, 0.7f, 0.9f));
                txt.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
                txt.AddComponent<LayoutElement>().flexibleWidth = 1;

                var rmBtnObj = new GameObject("RmBtn");
                rmBtnObj.transform.SetParent(entry.transform, false);
                rmBtnObj.AddComponent<RectTransform>().sizeDelta = new Vector2(40, 28);
                rmBtnObj.AddComponent<Image>().color = new Color(0.3f, 0.15f, 0.15f, 1f);
                var rmBtn = rmBtnObj.AddComponent<Button>();
                CreateText(rmBtn.transform, "✕", 14, Color.white);
                var captured = i;
                rmBtn.onClick.AddListener(() => RemoveMaterial(captured));
            }

            // 更新预览
            if (_selectedMaterials.Count == 0)
            {
                _previewText.text = "";
                _sacrificeBtn.interactable = false;
            }
            else
            {
                int level = _ritualSystem.PreviewRarityLevel(_selectedMaterials);
                string outputType = _ritualSystem.PreviewOutputType(_selectedMaterials);
                string levelName = level switch { 0 => "凡品", 1 => "灵品", 2 => "玄品", 3 => "仙品", _ => "凡品" };
                _previewText.text = $"预估品阶: {levelName}\n产出类型: {outputType}\n(10%概率+1品阶, 5%概率+2品阶)";
                _sacrificeBtn.interactable = true;
            }
        }

        private void OnSacrifice()
        {
            if (_selectedMaterials.Count == 0) return;
            if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick);

            var result = _ritualSystem.Sacrifice(_selectedMaterials);
            _selectedMaterials.Clear();

            if (result != null)
            {
                string levelName = result.rarityLevel switch { 0 => "凡品", 1 => "灵品", 2 => "玄品", 3 => "仙品", _ => "?" };
                string lucky = result.isLuckyUp ? $" ★运气爆棚!+{result.luckyUpLevels}品阶!" : "";
                _resultText.text = $"祭祀结果{lucky}\n获得{result.itemTypeName}: {result.itemName} [{levelName}]";
                FloatingTip.ShowSuccess($"祭祀获得: {result.itemName}");
            }
            else
            {
                _resultText.text = "祭祀失败...";
            }

            RefreshMaterialList();
            RefreshOffering();
        }

        private Transform CreateScroll(Transform parent, float yMin, float yMax, string name = "Scroll")
        {
            var scrollObj = new GameObject(name);
            scrollObj.transform.SetParent(parent, false);
            var sRt = scrollObj.AddComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0.02f, yMin); sRt.anchorMax = new Vector2(0.98f, yMax);
            sRt.offsetMin = Vector2.zero; sRt.offsetMax = Vector2.zero;
            var scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.vertical = true;

            var vp = new GameObject("Viewport");
            vp.transform.SetParent(scrollObj.transform, false);
            var vpRt = vp.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero; vpRt.offsetMax = Vector2.zero;
            vp.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            vp.AddComponent<RectMask2D>();
            scroll.viewport = vpRt;

            var content = new GameObject("Content");
            content.transform.SetParent(vp.transform, false);
            var cRt = content.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0, 1); cRt.anchorMax = new Vector2(1, 1);
            cRt.pivot = new Vector2(0.5f, 1f);
            cRt.offsetMin = Vector2.zero; cRt.offsetMax = Vector2.zero;
            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 3; vlg.childControlWidth = true; vlg.childForceExpandHeight = false;
            scroll.content = cRt;

            return content.transform;
        }

        private GameObject CreateText(Transform parent, string text, float size, Color color)
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
    }
}
