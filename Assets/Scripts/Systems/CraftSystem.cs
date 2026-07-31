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

        protected override void OnInit()
        {
        }

        private void LoadRecipes()
        {
            if (_allRecipes != null) return;
            _allRecipes = new List<RecipeData>();
            var guids = UnityEditor.AssetDatabase.FindAssets("t:RecipeData", new[] { "Assets/NueGames/NueDeck/Data/Recipes" });
            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var recipe = UnityEditor.AssetDatabase.LoadAssetAtPath<RecipeData>(path);
                if (recipe != null) _allRecipes.Add(recipe);
            }
            Debug.Log($"[CraftSystem] Loaded {_allRecipes.Count} recipes");
        }

        public List<RecipeData> GetAvailableRecipes(RecipeType type)
        {
            LoadRecipes();
            return _allRecipes.Where(r => r.recipeType == type).ToList();
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
                    var relicGuids = UnityEditor.AssetDatabase.FindAssets("t:RelicData", new[] { "Assets/NueGames/NueDeck/Data/Relics" });
                    foreach (var guid in relicGuids)
                    {
                        var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                        var relic = UnityEditor.AssetDatabase.LoadAssetAtPath<RelicData>(path);
                        if (relic != null && relic.relicId == recipe.outputItemId)
                        {
                            RelicSystem.AddRelic(relic);
                            Debug.Log($"[Craft] 获得法宝: {relic.name}");
                            break;
                        }
                    }
                    break;

                case RecipeOutputType.Potion:
                    var potionGuids = UnityEditor.AssetDatabase.FindAssets("t:PotionData", new[] { "Assets/NueGames/NueDeck/Data/Potions" });
                    foreach (var guid in potionGuids)
                    {
                        var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                        var potion = UnityEditor.AssetDatabase.LoadAssetAtPath<PotionData>(path);
                        if (potion != null && potion.potionId == recipe.outputItemId)
                        {
                            PotionSystem.ObtainPotion(potion);
                            Debug.Log($"[Craft] 获得丹药: {potion.name}");
                            break;
                        }
                    }
                    break;

                case RecipeOutputType.Material:
                    var matGuids = UnityEditor.AssetDatabase.FindAssets("t:MaterialData", new[] { "Assets/NueGames/NueDeck/Data/Materials" });
                    foreach (var guid in matGuids)
                    {
                        var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                        var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<MaterialData>(path);
                        if (mat != null && mat.materialId == recipe.outputItemId)
                        {
                            InventorySystem.AddItem(mat, recipe.outputCount);
                            Debug.Log($"[Craft] 获得材料: {mat.name} ×{recipe.outputCount}");
                            break;
                        }
                    }
                    break;
            }
        }
    }
}
