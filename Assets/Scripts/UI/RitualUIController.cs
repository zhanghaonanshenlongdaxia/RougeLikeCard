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
            AutoBindReferences();
            UIHelper.EnsureCloseButton(this, () => gameObject.SetActive(false));
        }

        private void OnEnable()
        {
            RefreshMaterialList();
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
                string levelName = level switch { 0 => "黄", 1 => "玄", 2 => "地", 3 => "天", _ => "黄" };
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
                string levelName = result.rarityLevel switch { 0 => "黄", 1 => "玄", 2 => "地", 3 => "天", _ => "?" };
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
        private void AutoBindReferences()
        {
            var panel = transform.Find("Panel");
            if (panel == null) return;
            var matList = panel.Find("MaterialList");
            if (matList != null)
            {
                var content = matList.Find("Scroll/Viewport/Content");
                _materialListRoot = content;
            }
            var offering = panel.Find("OfferingArea");
            if (offering != null)
            {
                _offeringRoot = offering.Find("OfferingScroll/Viewport/Content");
                _previewText = offering.Find("PreviewText")?.GetComponent<TMPro.TextMeshProUGUI>();
                _resultText = offering.Find("ResultText")?.GetComponent<TMPro.TextMeshProUGUI>();
                _sacrificeBtn = offering.Find("SacrificeButton")?.GetComponent<UnityEngine.UI.Button>();
            }
        }

    }

}
