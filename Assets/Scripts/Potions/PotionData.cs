using UnityEngine;

namespace CardGame
{
    /// <summary>
    /// 药水静态数据（ScriptableObject）
    /// </summary>
    [CreateAssetMenu(fileName = "NewPotion", menuName = "CardGame/Potion")]
    public class PotionData : ScriptableObject
    {
        [Header("基础信息")]
        public string potionId;
        public new string name;
        [TextArea] public string description;
        public Sprite potionIcon;

        [Header("效果参数")]
        [Tooltip("效果数值，含义由具体药水逻辑决定")]
        public int effectValue;

        [Header("稀有度")]
        public PotionRarity rarity = PotionRarity.Common;

        [Header("目标类型")]
        public PotionTargetType targetType = PotionTargetType.None;

        [Header("解锁")]
        [Tooltip("解锁章节，0=初始可用，1+=需冒险解锁")]
        public int unlockChapter = 0;
    }

    public enum PotionRarity
    {
        Common,
        Uncommon,
        Rare
    }

    public enum PotionTargetType
    {
        None,       // 无目标（自身回血等）
        Enemy,      // 指定敌人
        AllEnemies  // 全体敌人
    }
}
