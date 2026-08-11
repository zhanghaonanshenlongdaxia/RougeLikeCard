using NueGames.NueDeck.Scripts.Enums;
using UnityEngine;

namespace CardGame
{
    /// <summary>
    /// 遗物静态数据（ScriptableObject，策划可配置）
    /// </summary>
    [CreateAssetMenu(fileName = "NewRelic", menuName = "CardGame/Relic")]
    public class RelicData : ScriptableObject
    {
        [Header("基础信息")]
        public string relicId;
        public new string name;
        [TextArea] public string description;
        public Sprite relicIcon;

        [Header("触发设置")]
        public RelicTriggerType triggerType = RelicTriggerType.Passive;

        [Header("效果参数")]
        [Tooltip("效果数值，含义由具体遗物逻辑决定")]
        public int effectValue;
        [Tooltip("是否一次性触发（如回血一次）还是每次触发")]
        public bool oneTimeUse = false;

        [Header("品质")]
        [Tooltip("旧稀有度，已废弃，用quality代替")]
        public RelicRarity rarity = RelicRarity.Common;
        [Tooltip("统一品质")]
        public ItemQuality quality = ItemQuality.LianQi_T1;

        [Header("耐久度")]
        [Tooltip("最大耐久度，归零后法宝损坏")]
        public int maxDurability = 5;
        [Tooltip("每次触发扣除的耐久度，强力效果可设2-3")]
        public int durabilityCost = 1;

        [Header("解锁")]
        [Tooltip("解锁章节，0=初始可用，1+=需冒险解锁")]
        public int unlockChapter = 0;
    }

    /// <summary>旧遗物稀有度，保留用于序列化兼容</summary>
    public enum RelicRarity { Common, Uncommon, Rare, Boss, Shop }
}
