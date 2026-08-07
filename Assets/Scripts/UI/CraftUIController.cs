using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using Alchemy.Inspector;

namespace CardGame.UI
{
    public class CraftUIController : MonoBehaviour, IController
    {
        [FoldoutGroup("References")]
        [SerializeField] private Transform recipeListRoot;
        [SerializeField] private TextMeshProUGUI detailTitle;
        [SerializeField] private TextMeshProUGUI detailMats;
        [SerializeField] private Button craftButton;
        [SerializeField] private Button[] tabButtons;
        [SerializeField] private Button backButton;

        private ICraftSystem _system;
        private RecipeType _currentType = RecipeType.Alchemy;
        private RecipeData _selectedRecipe;

        public IArchitecture GetArchitecture() => CardGameArchitecture.Interface;

        private void Awake()
        {
            _system = this.GetSystem<ICraftSystem>();
            UIHelper.EnsureCloseButton(this, OnBackButton);
        }

        private void OnEnable()
        {
            ShowRecipes(RecipeType.Alchemy);
        }

        public void OnTabSelected(int index)
        {
            _currentType = (RecipeType)index;
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
                go.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.2f, 1);

                var layout = go.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 3;
                layout.padding = new RectOffset(5, 5, 5, 5);

                bool canCraft = _system.CanCraft(recipe);
                CreateText(go, recipe.name, 18, canCraft ? new Color(0.4f, 0.8f, 0.4f) : new Color(0.8f, 0.3f, 0.3f));

                var matsStr = "";
                foreach (var ing in recipe.ingredients)
                    matsStr += $"{ing.materialId}x{ing.count} ";
                CreateText(go, matsStr, 14, new Color(0.7f, 0.7f, 0.7f));

                var btn = go.AddComponent<Button>();
                var captured = recipe;
                btn.onClick.AddListener(() => SelectRecipe(captured));
            }
        }

        private void SelectRecipe(RecipeData recipe)
        {
            _selectedRecipe = recipe;
            if (detailTitle) detailTitle.text = $"产出: {recipe.outputItemId}  成功率: {recipe.successRate * 100:F0}%";

            var matsStr = "";
            var invSys = this.GetSystem<IInventorySystem>();
            foreach (var ing in recipe.ingredients)
            {
                int have = invSys.GetItemCount(ing.materialId);
                bool enough = have >= ing.count;
                matsStr += $"{(enough ? "OK" : "X")} {ing.materialId} x{ing.count} (have:{have})  ";
            }
            if (detailMats) detailMats.text = matsStr;

            if (craftButton)
            {
                craftButton.interactable = _system.CanCraft(recipe);
                craftButton.onClick.RemoveAllListeners();
                craftButton.onClick.AddListener(OnCraft);
            }
        }

        public void OnCraft()
        {
            if (_selectedRecipe == null) return;
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
            var libSans = UnityEngine.Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (libSans) tmp.font = libSans;
        }
    }
}
