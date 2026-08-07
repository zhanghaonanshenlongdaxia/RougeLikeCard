using UnityEngine;

namespace CardGame
{
    /// <summary>
    /// 材料稀有度
    /// </summary>
    public enum MaterialRarity
    {
        FanPin,   // 凡品
        LingPin,  // 灵品
        XuanPin,  // 玄品
        XianPin   // 仙品
    }

    /// <summary>
    /// 材料类型
    /// </summary>
    public enum MaterialType
    {
        SpiritHerb,    // 灵草
        Ore,           // 矿石
        DemonCore,     // 妖丹
        SoulStone,     // 魂石
        SpiritWater,   // 灵水
        Fragment,      // 残片
        SpiritWood,    // 灵木
        BeastBone,     // 灵兽骨
        HeavenlyTreasure // 天材地宝
    }

    /// <summary>
    /// 材料静态数据（ScriptableObject）
    /// </summary>
    [CreateAssetMenu(fileName = "NewMaterial", menuName = "CardGame/Material")]
    public class MaterialData : ScriptableObject, IInventoryItem
    {
        [Header("基础信息")]
        public string materialId;
        public new string name;
        [TextArea] public string description;
        public Sprite icon;

        [Header("分类")]
        public MaterialType materialType;
        public MaterialRarity rarity;
        [Tooltip("产出区域：0=山野荒原 1=幽冥秘境 2=万蛊沼泽 3=天魔裂隙 -1=通用")]
        public int regionId = -1;

        [Header("属性")]
        [Tooltip("单个占用的负重")]
        public int weight = 1;
        [Tooltip("是否可堆叠")]
        public bool stackable = true;
        [Tooltip("最大堆叠数")]
        public int maxStack = 99;

        public string ItemId => materialId;
        public string ItemName => name;
        public string ItemDescription => description;
        public Sprite ItemIcon => icon;
        public int ItemWeight => weight;
        public bool IsStackable => stackable;
        public int MaxStack => maxStack;
    }
}
