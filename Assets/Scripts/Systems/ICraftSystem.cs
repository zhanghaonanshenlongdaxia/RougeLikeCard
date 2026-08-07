using System.Collections.Generic;
using QFramework;

namespace CardGame
{
    public interface ICraftSystem : ISystem
    {
        List<RecipeData> GetAvailableRecipes(RecipeType type);
        bool CanCraft(RecipeData recipe);
        bool Craft(RecipeData recipe);
        /// <summary>解锁配方（通过掉落获得）</summary>
        void UnlockRecipe(string recipeId);
        /// <summary>检查配方是否已解锁</summary>
        bool IsRecipeUnlocked(string recipeId);
    }
}
