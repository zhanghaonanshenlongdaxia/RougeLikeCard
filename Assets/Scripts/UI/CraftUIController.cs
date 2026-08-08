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
    public class CraftUIController : MonoBehaviour, IController
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
            BuildUI();
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

        private void BuildUI()
        {
            // 确保有Canvas
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

            // 创建全屏遮罩背景
            // 创建主面板
            Transform panel = transform.Find("Panel");
            if (panel == null)
            {
                var panelObj = new GameObject("Panel");
                panelObj.transform.SetParent(transform, false);
                var panelRt = panelObj.AddComponent<RectTransform>();
                panelRt.anchorMin = new Vector2(0.05f, 0.05f);
                panelRt.anchorMax = new Vector2(0.95f, 0.95f);
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
            titleTmp.text = "炼制"; titleTmp.fontSize = 28; titleTmp.color = new Color(0.9f, 0.8f, 0.3f);
            titleTmp.alignment = TextAlignmentOptions.Center;
            if (_font) titleTmp.font = _font;

            // Tab按钮（丹道/器道/祭道）
            if (tabButtons == null || tabButtons.Length == 0)
            {
                var tabContainer = new GameObject("TabContainer");
                tabContainer.transform.SetParent(panel, false);
                var tabRt = tabContainer.AddComponent<RectTransform>();
                tabRt.anchorMin = new Vector2(0.02f, 0.88f);
                tabRt.anchorMax = new Vector2(0.98f, 0.93f);
                tabRt.offsetMin = Vector2.zero; tabRt.offsetMax = Vector2.zero;

                var tabLayout = tabContainer.AddComponent<HorizontalLayoutGroup>();
                tabLayout.spacing = 5;
                tabLayout.childControlWidth = true;
                tabLayout.childControlHeight = true;
                tabLayout.childForceExpandWidth = true;
                tabLayout.childForceExpandHeight = true;

                string[] tabNames = { "丹道", "器道", "祭道" };
                var btns = new Button[3];
                for (int i = 0; i < 3; i++)
                {
                    var go = new GameObject($"Tab_{i}");
                    go.transform.SetParent(tabContainer.transform, false);
                    go.AddComponent<RectTransform>();
                    var img = go.AddComponent<Image>();
                    img.color = i == 0 ? new Color(0.15f, 0.45f, 0.25f, 1f) : new Color(0.2f, 0.2f, 0.28f, 1f);

                    var txtObj = new GameObject("Text");
                    txtObj.transform.SetParent(go.transform, false);
                    var txtRt = txtObj.AddComponent<RectTransform>();
                    txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
                    txtRt.offsetMin = Vector2.zero; txtRt.offsetMax = Vector2.zero;
                    var tmp = txtObj.AddComponent<TextMeshProUGUI>();
                    tmp.text = tabNames[i]; tmp.alignment = TextAlignmentOptions.Center;
                    tmp.fontSize = 20; tmp.color = Color.white;
                    if (_font) tmp.font = _font;

                    btns[i] = go.AddComponent<Button>();
                    int idx = i;
                    btns[i].onClick.AddListener(() => { OnTabSelected(idx); });
                }
                tabButtons = btns;
            }

            // 左侧配方列表（滚动）
            if (recipeListRoot == null)
            {
                var listPanel = new GameObject("RecipeListPanel");
                listPanel.transform.SetParent(panel, false);
                var listRt = listPanel.AddComponent<RectTransform>();
                listRt.anchorMin = new Vector2(0.02f, 0.05f);
                listRt.anchorMax = new Vector2(0.45f, 0.85f);
                listRt.offsetMin = Vector2.zero; listRt.offsetMax = Vector2.zero;
                listPanel.AddComponent<Image>().color = new Color(0.05f, 0.08f, 0.12f, 0.8f);

                recipeScroll = listPanel.AddComponent<ScrollRect>();
                recipeScroll.horizontal = false;

                var viewport = new GameObject("Viewport");
                viewport.transform.SetParent(listPanel.transform, false);
                var vpRt = viewport.AddComponent<RectTransform>();
                vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
                vpRt.pivot = new Vector2(0, 1);
                vpRt.offsetMin = new Vector2(5, 5); vpRt.offsetMax = new Vector2(-5, -5);
                var vpImg = viewport.AddComponent<Image>();
                vpImg.color = new Color(0, 0, 0, 0.01f);
                viewport.AddComponent<RectMask2D>();
                recipeScroll.viewport = vpRt;

                var content = new GameObject("Content");
                content.transform.SetParent(viewport.transform, false);
                var contentRt = content.AddComponent<RectTransform>();
                contentRt.anchorMin = new Vector2(0, 1); contentRt.anchorMax = new Vector2(1, 1);
                contentRt.pivot = new Vector2(0, 1);
                contentRt.offsetMin = Vector2.zero; contentRt.offsetMax = Vector2.zero;
                var fitter = content.AddComponent<VerticalLayoutGroup>();
                fitter.spacing = 3; fitter.childControlWidth = true; fitter.childForceExpandHeight = false;
                var csf = content.AddComponent<ContentSizeFitter>();
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                recipeScroll.content = contentRt;
                recipeListRoot = content.transform;
            }

            // 右侧详情面板
            if (detailTitle == null)
            {
                var detailPanel = new GameObject("DetailPanel");
                detailPanel.transform.SetParent(panel, false);
                var dpRt = detailPanel.AddComponent<RectTransform>();
                dpRt.anchorMin = new Vector2(0.47f, 0.05f);
                dpRt.anchorMax = new Vector2(0.98f, 0.85f);
                dpRt.offsetMin = Vector2.zero; dpRt.offsetMax = Vector2.zero;
                detailPanel.AddComponent<Image>().color = new Color(0.05f, 0.08f, 0.12f, 0.8f);

                // 标题
                var dtObj = new GameObject("DetailTitle");
                dtObj.transform.SetParent(detailPanel.transform, false);
                var dtRt = dtObj.AddComponent<RectTransform>();
                dtRt.anchorMin = new Vector2(0.05f, 0.82f); dtRt.anchorMax = new Vector2(0.95f, 0.95f);
                dtRt.offsetMin = Vector2.zero; dtRt.offsetMax = Vector2.zero;
                detailTitle = dtObj.AddComponent<TextMeshProUGUI>();
                detailTitle.fontSize = 22; detailTitle.color = new Color(0.9f, 0.8f, 0.3f);
                detailTitle.alignment = TextAlignmentOptions.Center;
                if (_font) detailTitle.font = _font;

                // 描述
                var ddObj = new GameObject("DetailDesc");
                ddObj.transform.SetParent(detailPanel.transform, false);
                var ddRt = ddObj.AddComponent<RectTransform>();
                ddRt.anchorMin = new Vector2(0.05f, 0.68f); ddRt.anchorMax = new Vector2(0.95f, 0.82f);
                ddRt.offsetMin = Vector2.zero; ddRt.offsetMax = Vector2.zero;
                detailDesc = ddObj.AddComponent<TextMeshProUGUI>();
                detailDesc.fontSize = 14; detailDesc.color = new Color(0.7f, 0.75f, 0.8f);
                detailDesc.alignment = TextAlignmentOptions.Center;
                if (_font) detailDesc.font = _font;

                // 材料
                var dmObj = new GameObject("DetailMats");
                dmObj.transform.SetParent(detailPanel.transform, false);
                var dmRt = dmObj.AddComponent<RectTransform>();
                dmRt.anchorMin = new Vector2(0.05f, 0.3f); dmRt.anchorMax = new Vector2(0.95f, 0.68f);
                dmRt.offsetMin = Vector2.zero; dmRt.offsetMax = Vector2.zero;
                detailMats = dmObj.AddComponent<TextMeshProUGUI>();
                detailMats.fontSize = 14; detailMats.color = Color.white;
                detailMats.alignment = TextAlignmentOptions.Left;
                if (_font) detailMats.font = _font;

                // 产出
                var drObj = new GameObject("DetailResult");
                drObj.transform.SetParent(detailPanel.transform, false);
                var drRt = drObj.AddComponent<RectTransform>();
                drRt.anchorMin = new Vector2(0.05f, 0.2f); drRt.anchorMax = new Vector2(0.95f, 0.3f);
                drRt.offsetMin = Vector2.zero; drRt.offsetMax = Vector2.zero;
                detailResult = drObj.AddComponent<TextMeshProUGUI>();
                detailResult.fontSize = 16; detailResult.color = new Color(0.4f, 0.8f, 0.4f);
                detailResult.alignment = TextAlignmentOptions.Center;
                if (_font) detailResult.font = _font;

                // 炼制按钮
                var cbObj = new GameObject("CraftButton");
                cbObj.transform.SetParent(detailPanel.transform, false);
                var cbRt = cbObj.AddComponent<RectTransform>();
                cbRt.anchorMin = new Vector2(0.3f, 0.03f); cbRt.anchorMax = new Vector2(0.7f, 0.18f);
                cbRt.offsetMin = Vector2.zero; cbRt.offsetMax = Vector2.zero;
                cbObj.AddComponent<Image>().color = new Color(0.2f, 0.5f, 0.3f, 1f);

                var cbtObj = new GameObject("Text");
                cbtObj.transform.SetParent(cbObj.transform, false);
                var cbtRt = cbtObj.AddComponent<RectTransform>();
                cbtRt.anchorMin = Vector2.zero; cbtRt.anchorMax = Vector2.one;
                cbtRt.offsetMin = Vector2.zero; cbtRt.offsetMax = Vector2.zero;
                var cbtTmp = cbtObj.AddComponent<TextMeshProUGUI>();
                cbtTmp.text = "炼制"; cbtTmp.fontSize = 20; cbtTmp.color = Color.white;
                cbtTmp.alignment = TextAlignmentOptions.Center;
                if (_font) cbtTmp.font = _font;

                craftButton = cbObj.AddComponent<Button>();
                craftButton.onClick.AddListener(OnCraft);
            }
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
            if (recipeListRoot == null) return;

            for (int i = recipeListRoot.childCount - 1; i >= 0; i--)
                Destroy(recipeListRoot.GetChild(i).gameObject);

            var recipes = _system.GetAvailableRecipes(type);
            foreach (var recipe in recipes)
            {
                var go = new GameObject("Recipe_" + recipe.recipeId);
                go.transform.SetParent(recipeListRoot);
                go.AddComponent<RectTransform>();
                bool canCraft = _system.CanCraft(recipe);
                go.AddComponent<Image>().color = canCraft ? new Color(0.1f, 0.15f, 0.25f, 1f) : new Color(0.08f, 0.08f, 0.1f, 0.8f);

                var layout = go.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 2; layout.padding = new RectOffset(5, 5, 3, 3);
                layout.childControlWidth = true; layout.childForceExpandHeight = false;
                var le = go.AddComponent<LayoutElement>();
                le.preferredHeight = 50;

                // 配方名
                CreateText(go, recipe.name, 16, canCraft ? new Color(0.4f, 0.8f, 0.4f) : new Color(0.6f, 0.3f, 0.3f));
                // 成功率
                CreateText(go, $"成功率 {recipe.successRate * 100:F0}%", 12, new Color(0.7f, 0.7f, 0.7f));

                var btn = go.AddComponent<Button>();
                var captured = recipe;
                btn.onClick.AddListener(() => {
                    if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick);
                    SelectRecipe(captured);
                });
            }

            if (detailTitle) detailTitle.text = $"请选择配方";
            if (detailDesc) detailDesc.text = "";
            if (detailMats) detailMats.text = "";
            if (detailResult) detailResult.text = "";
            if (craftButton) { craftButton.interactable = false; craftButton.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 1f); }
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
    }
}
