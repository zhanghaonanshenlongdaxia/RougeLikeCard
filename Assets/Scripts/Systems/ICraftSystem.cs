using System.Collections.Generic;
using QFramework;

namespace CardGame
{
    public interface ICraftSystem : ISystem
    {
        List<RecipeData> GetAvailableRecipes(RecipeType type);
        bool CanCraft(RecipeData recipe);
        bool Craft(RecipeData recipe);
        void UnlockRecipe(string recipeId);
        bool IsRecipeUnlocked(string recipeId);
        /// <summary>获取所有已解锁的非默认配方ID（用于存档）</summary>
        System.Collections.Generic.HashSet<string> GetUnlockedRecipeIds();
    }
}
