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

        [Header("稀有度")]
        public RelicRarity rarity = RelicRarity.Common;

        [Header("解锁")]
        [Tooltip("解锁章节，0=初始可用，1+=需冒险解锁")]
        public int unlockChapter = 0;
    }

    public enum RelicRarity
    {
        Common,
        Uncommon,
        Rare,
        Boss,
        Shop
    }
}
