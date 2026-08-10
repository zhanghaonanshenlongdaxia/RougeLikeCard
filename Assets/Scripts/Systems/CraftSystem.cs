using System.Collections.Generic;
using System.Linq;
using QFramework;
using UnityEngine;

namespace CardGame
{
    public class CraftSystem : AbstractSystem, ICraftSystem
    {
        protected IInventorySystem InventorySystem => this.GetSystem<IInventorySystem>();
        protected IInventoryModel InventoryModel => this.GetModel<IInventoryModel>();
        protected IRelicSystem RelicSystem => this.GetSystem<IRelicSystem>();
        protected IPotionSystem PotionSystem => this.GetSystem<IPotionSystem>();

        private List<RecipeData> _allRecipes;
        private HashSet<string> _unlockedRecipeIds = new HashSet<string>();

        protected override void OnInit()
        {
        }

        public void UnlockRecipe(string recipeId)
        {
            if (_unlockedRecipeIds.Add(recipeId))
                Debug.Log($"[CraftSystem] 解锁配方: {recipeId}");
        }

        public bool IsRecipeUnlocked(string recipeId)
        {
            // 默认解锁的配方直接返回true
            LoadRecipes();
            var recipe = _allRecipes.FirstOrDefault(r => r.recipeId == recipeId);
            if (recipe != null && recipe.unlockByDefault) return true;
            return _unlockedRecipeIds.Contains(recipeId);
        }

        private void LoadRecipes()
        {
            if (_allRecipes != null) return;
            _allRecipes = ResourceCache.GetRecipes();
            Debug.Log($"[CraftSystem] Loaded {_allRecipes.Count} recipes from cache");
        }

        public List<RecipeData> GetAvailableRecipes(RecipeType type)
        {
            LoadRecipes();
            return _allRecipes.Where(r => r.recipeType == type && (r.unlockByDefault || _unlockedRecipeIds.Contains(r.recipeId))).ToList();
        }

        public System.Collections.Generic.HashSet<string> GetUnlockedRecipeIds()
        {
            return _unlockedRecipeIds;
        }

        public bool CanCraft(RecipeData recipe)
        {
            if (recipe == null) return false;
            foreach (var ingredient in recipe.ingredients)
            {
                if (!InventorySystem.HasItem(ingredient.materialId, ingredient.count))
                    return false;
            }
            return true;
        }

        public bool Craft(RecipeData recipe)
        {
            if (!CanCraft(recipe))
            {
                Debug.Log($"[Craft] 材料不足: {recipe.name}");
                return false;
            }

            // 消耗材料
            foreach (var ingredient in recipe.ingredients)
                InventorySystem.RemoveItem(ingredient.materialId, ingredient.count);

            // 成功率判定
            bool success = Random.value <= recipe.successRate;

            if (success)
            {
                Debug.Log($"[Craft] 炼制成功! 产出: {recipe.outputItemId}");
                GrantOutput(recipe);
            }
            else
            {
                Debug.Log($"[Craft] 炼制失败... 材料已损耗");
            }

            return success;
        }

        private void GrantOutput(RecipeData recipe)
        {
            var gm = NueGames.NueDeck.Scripts.Managers.GameManager.Instance;
            if (gm == null) return;

            switch (recipe.outputType)
            {
                case RecipeOutputType.Card:
                    var card = gm.GameplayData.AllCardsList.Find(c => c.Id == recipe.outputItemId);
                    if (card != null)
                    {
                        for (int i = 0; i < recipe.outputCount; i++)
                            gm.PersistentGameplayData.CurrentCardsList.Add(card);
                        Debug.Log($"[Craft] 获得卡牌: {card.CardName} ×{recipe.outputCount}");
                    }
                    break;

                case RecipeOutputType.Relic:
                    {
                        var relic = ResourceCache.GetRelics().Find(r => r.relicId == recipe.outputItemId);
                        if (relic != null)
                        {
                            RelicSystem.AddRelic(relic);
                            Debug.Log($"[Craft] 获得法宝: {relic.name}");
                        }
                    }
                    break;

                case RecipeOutputType.Potion:
                    {
                        var potion = ResourceCache.GetPotions().Find(p => p.potionId == recipe.outputItemId);
                        if (potion != null)
                        {
                            PotionSystem.ObtainPotion(potion);
                            Debug.Log($"[Craft] 获得丹药: {potion.name}");
                        }
                    }
                    break;

                case RecipeOutputType.Material:
                    {
                        var mat = ResourceCache.GetMaterials().Find(m => m.materialId == recipe.outputItemId);
                        if (mat != null)
                        {
                            InventorySystem.AddItem(mat, recipe.outputCount);
                            Debug.Log($"[Craft] 获得材料: {mat.name} ×{recipe.outputCount}");
                        }
                    }
                    break;
            }
        }
    }
}
