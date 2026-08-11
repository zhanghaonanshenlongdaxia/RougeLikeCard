using System.Collections.Generic;
using NueGames.NueDeck.Scripts.Enums;
using UnityEngine;

namespace CardGame
{
    public enum RecipeType
    {
        Alchemy,    // 丹道（产出卡牌）
        Forging,    // 器道（产出遗物）
        Ritual      // 祭道（产出特殊物品）
    }

    public enum RecipeOutputType
    {
        Card,       // 卡牌
        Relic,      // 遗物
        Potion,     // 药水
        Material    // 材料
    }

    [System.Serializable]
    public struct RecipeIngredient
    {
        public string materialId;
        public int count;
    }

    /// <summary>
    /// 炼制配方（ScriptableObject）
    /// </summary>
    [CreateAssetMenu(fileName = "NewRecipe", menuName = "CardGame/Recipe")]
    public class RecipeData : ScriptableObject
    {
        [Header("基础信息")]
        public string recipeId;
        public new string name;
        [TextArea] public string description;
        public RecipeType recipeType;

        [Header("产出")]
        public RecipeOutputType outputType;
        public string outputItemId;
        public int outputCount = 1;

        [Header("材料")]
        public List<RecipeIngredient> ingredients = new List<RecipeIngredient>();

        [Header("属性")]
        [Range(0f, 1f)] public float successRate = 0.8f;
        public bool unlockByDefault = true;

        [Header("品质")]
        [Tooltip("配方产出物品的品质")]
        public ItemQuality quality = ItemQuality.LianQi_T1;
    }
}
