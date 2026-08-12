using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using Alchemy.Inspector;
using CardGame.Audio;
using CardGame.UI;

namespace CardGame.UI
{
    public class CraftUIController : MonoBehaviour, IController, LoopScrollDataSource
    {
        [FoldoutGroup("References")]
        [SerializeField] private Transform recipeListRoot;
        [SerializeField] private TextMeshProUGUI detailTitle;
        [SerializeField] private TextMeshProUGUI detailMats;
        [SerializeField] private TextMeshProUGUI detailDesc;
        [SerializeField] private TextMeshProUGUI detailResult;
        [SerializeField] private Button craftButton;
        [SerializeField] private Button[] tabButtons;
        [SerializeField] private Button backButton;
        [SerializeField] private ScrollRect recipeScroll;
        private LoopVerticalScrollRect _loopScroll;
        private LoopScrollPrefabSourceImpl _prefabSource;
        private GameObject _itemTemplate;
        private List<RecipeData> _currentRecipes = new List<RecipeData>();

        private ICraftSystem _system;
        private IInventorySystem _invSystem;
        private RecipeType _currentType = RecipeType.Alchemy;
        private RecipeData _selectedRecipe;
        private TMP_FontAsset _font;

        public IArchitecture GetArchitecture() => CardGameArchitecture.Interface;

        private void Awake()
        {
            _system = this.GetSystem<ICraftSystem>();
            _invSystem = this.GetSystem<IInventorySystem>();
            _font = UnityEngine.Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            AutoBindReferences();
            UIHelper.EnsureCloseButton(this, OnBackButton);
        }

        private void OnEnable()
        {
            if (tabButtons != null && tabButtons.Length > 0)
            {
                // 绑定tab按钮
                for (int i = 0; i < tabButtons.Length; i++)
                {
                    var btn = tabButtons[i];
                    if (btn == null) continue;
                    int idx = i;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => { OnTabSelected(idx); });
                }
            }
            ShowRecipes(_currentType);
        }


        public void OnTabSelected(int index)
        {
            _currentType = (RecipeType)index;
            // 更新tab颜色
            for (int i = 0; i < tabButtons.Length; i++)
            {
                if (tabButtons[i] == null) continue;
                var img = tabButtons[i].GetComponent<Image>();
                if (img) img.color = i == index ? new Color(0.2f, 0.5f, 0.3f, 1f) : new Color(0.15f, 0.15f, 0.2f, 1f);
            }
            if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick);
            ShowRecipes(_currentType);
        }

        private void ShowRecipes(RecipeType type)
        {
            _currentRecipes = _system.GetAvailableRecipes(type);

            if (_loopScroll != null)
            {
                _loopScroll.totalCount = _currentRecipes.Count;
                _loopScroll.RefillCells();
            }

            if (detailTitle) detailTitle.text = $"请选择配方";
            if (detailDesc) detailDesc.text = "";
            if (detailMats) detailMats.text = "";
            if (detailResult) detailResult.text = "";
            if (craftButton) { craftButton.interactable = false; craftButton.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 1f); }
        }

        public void ProvideData(Transform transform, int idx)
        {
            if (idx < 0 || idx >= _currentRecipes.Count) return;
            var recipe = _currentRecipes[idx];
            bool canCraft = _system.CanCraft(recipe);

            var img = transform.GetComponent<Image>();
            if (img) img.color = canCraft ? new Color(0.1f, 0.15f, 0.25f, 1f) : new Color(0.08f, 0.08f, 0.1f, 0.8f);

            var tmps = transform.GetComponentsInChildren<TextMeshProUGUI>();
            if (tmps.Length >= 1) tmps[0].text = recipe.name;
            if (tmps.Length >= 1) tmps[0].color = canCraft ? new Color(0.4f, 0.8f, 0.4f) : new Color(0.6f, 0.3f, 0.3f);
            if (tmps.Length >= 2) tmps[1].text = $"成功率 {recipe.successRate * 100:F0}%";

            var btn = transform.GetComponent<Button>();
            if (btn == null) btn = transform.gameObject.AddComponent<Button>();
            btn.onClick.RemoveAllListeners();
            var captured = recipe;
            btn.onClick.AddListener(() => {
                if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick);
                SelectRecipe(captured);
            });
        }

        private GameObject CreateRecipeItemTemplate()
        {
            var go = new GameObject("RecipeItem");
            go.AddComponent<RectTransform>();
            go.AddComponent<Image>().color = new Color(0.1f, 0.15f, 0.25f, 1f);

            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 2; layout.padding = new RectOffset(5, 5, 3, 3);
            layout.childControlWidth = true; layout.childForceExpandHeight = false;
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 50;

            // 配方名
            var nameObj = new GameObject("NameText");
            nameObj.transform.SetParent(go.transform, false);
            nameObj.AddComponent<RectTransform>();
            var nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
            nameTmp.alignment = TextAlignmentOptions.Center;
            nameTmp.fontSize = 16; nameTmp.color = new Color(0.4f, 0.8f, 0.4f);
            nameTmp.richText = true;
            if (_font) nameTmp.font = _font;

            // 成功率
            var rateObj = new GameObject("RateText");
            rateObj.transform.SetParent(go.transform, false);
            rateObj.AddComponent<RectTransform>();
            var rateTmp = rateObj.AddComponent<TextMeshProUGUI>();
            rateTmp.alignment = TextAlignmentOptions.Center;
            rateTmp.fontSize = 12; rateTmp.color = new Color(0.7f, 0.7f, 0.7f);
            if (_font) rateTmp.font = _font;

            go.AddComponent<Button>();
            return go;
        }

        private void SelectRecipe(RecipeData recipe)
        {
            _selectedRecipe = recipe;

            // 标题：配方名 + 成功率
            if (detailTitle) detailTitle.text = $"{recipe.name}  (成功率 {recipe.successRate * 100:F0}%)";

            // 描述
            if (detailDesc) detailDesc.text = recipe.description ?? "";

            // 材料列表（显示拥有量）
            var matsStr = "所需材料:\n";
            foreach (var ing in recipe.ingredients)
            {
                int have = _invSystem.GetItemCount(ing.materialId);
                bool enough = have >= ing.count;
                string status = enough ? "<color=#4CAF50>✓</color>" : "<color=#F44336>✗</color>";
                matsStr += $"  {status} {ing.materialId} ×{ing.count} (持有:{have})\n";
            }
            if (detailMats) detailMats.text = matsStr;

            // 产出信息
            string outputStr = recipe.outputType switch {
                RecipeOutputType.Card => $"产出: 卡牌 ×{recipe.outputCount}",
                RecipeOutputType.Relic => $"产出: 法宝 ×{recipe.outputCount}",
                RecipeOutputType.Potion => $"产出: 丹药 ×{recipe.outputCount}",
                RecipeOutputType.Material => $"产出: 材料 ×{recipe.outputCount}",
                _ => $"产出: ×{recipe.outputCount}"
            };
            if (detailResult) detailResult.text = outputStr;

            // 炼制按钮
            if (craftButton)
            {
                craftButton.interactable = _system.CanCraft(recipe);
                craftButton.GetComponent<Image>().color = craftButton.interactable ? new Color(0.2f, 0.5f, 0.3f, 1f) : new Color(0.3f, 0.15f, 0.15f, 1f);
                craftButton.onClick.RemoveAllListeners();
                craftButton.onClick.AddListener(OnCraft);
            }
        }

        public void OnCraft()
        {
            if (_selectedRecipe == null) return;
            if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick);
            bool success = _system.Craft(_selectedRecipe);
            ShowRecipes(_currentType);
            if (_selectedRecipe != null) SelectRecipe(_selectedRecipe);
        }

        public void OnBackButton()
        {
            gameObject.SetActive(false);
        }

        private void CreateText(GameObject parent, string text, float size, Color color)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent.transform);
            go.AddComponent<RectTransform>();
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.richText = true;
            if (_font) tmp.font = _font;
        }
        private void AutoBindReferences()
        {
            var panel = transform.Find("Panel");
            if (panel == null) return;
            if (recipeListRoot == null) recipeListRoot = panel.Find("RecipeListPanel/ScrollView/Viewport/Content");
            if (detailTitle == null) detailTitle = panel.Find("DetailPanel/DetailTitle")?.GetComponent<TMPro.TextMeshProUGUI>();
            if (detailDesc == null) detailDesc = panel.Find("DetailPanel/DetailDesc")?.GetComponent<TMPro.TextMeshProUGUI>();
            if (detailMats == null) detailMats = panel.Find("DetailPanel/DetailMats")?.GetComponent<TMPro.TextMeshProUGUI>();
            if (detailResult == null) detailResult = panel.Find("DetailPanel/DetailResult")?.GetComponent<TMPro.TextMeshProUGUI>();
            if (craftButton == null) craftButton = panel.Find("DetailPanel/CraftButton")?.GetComponent<UnityEngine.UI.Button>();
            var tabContainer = panel.Find("TabContainer");
            if (tabButtons == null || tabButtons.Length == 0)
            {
                var tabs = new List<UnityEngine.UI.Button>();
                for (int i = 0; i < 3; i++)
                {
                    var tab = tabContainer?.Find($"Tab_{i}")?.GetComponent<UnityEngine.UI.Button>();
                    if (tab != null) tabs.Add(tab);
                }
                tabButtons = tabs.ToArray();
            }
        
            // Setup LoopScrollRect on RecipeListPanel ScrollView
            var scrollObj = panel.Find("RecipeListPanel/ScrollView");
            if (scrollObj != null && _loopScroll == null)
            {
                scrollObj.gameObject.SetActive(false);
                var oldSR = scrollObj.GetComponent<ScrollRect>(); if (oldSR != null) DestroyImmediate(oldSR);
                _loopScroll = scrollObj.gameObject.AddComponent<LoopVerticalScrollRect>();
                // Fix m_Horizontal/m_Vertical via reflection (avoid Awake assertion)
                _loopScroll.horizontal = false;
                _loopScroll.vertical = true;
                scrollObj.gameObject.SetActive(true);
                _loopScroll.dataSource = this;
                _loopScroll.viewport = scrollObj.Find("Viewport")?.GetComponent<RectTransform>();
                _loopScroll.content = recipeListRoot?.GetComponent<RectTransform>();
                _itemTemplate = new GameObject("RecipeItem");
                _itemTemplate.transform.SetParent(transform, false);
                _itemTemplate.SetActive(false);
                var rt = _itemTemplate.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(0, 60);
                _itemTemplate.AddComponent<Image>().color = new Color(0.1f, 0.15f, 0.25f, 1f);
                var nameTmp = new GameObject("NameText"); nameTmp.transform.SetParent(_itemTemplate.transform, false);
                nameTmp.AddComponent<RectTransform>();
                var nt = nameTmp.AddComponent<TMPro.TextMeshProUGUI>(); nt.fontSize = 16; nt.color = Color.white; nt.alignment = TextAlignmentOptions.Left;
                var rateTmp = new GameObject("RateText"); rateTmp.transform.SetParent(_itemTemplate.transform, false);
                rateTmp.AddComponent<RectTransform>();
                var rt2 = rateTmp.AddComponent<TMPro.TextMeshProUGUI>(); rt2.fontSize = 14; rt2.color = new Color(0.9f, 0.7f, 0.2f); rt2.alignment = TextAlignmentOptions.Left;
                _prefabSource = new LoopScrollPrefabSourceImpl(_itemTemplate, scrollObj);
                _loopScroll.prefabSource = _prefabSource;
            }

}

    }

}
