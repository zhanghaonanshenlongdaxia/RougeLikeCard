using System.Collections.Generic;
using QFramework;

namespace CardGame
{
    public interface ICraftSystem : ISystem
    {
        List<RecipeData> GetAvailableRecipes(RecipeType type);
        bool CanCraft(RecipeData recipe);
        bool Craft(RecipeData recipe);
    }
}
