using System.Collections.Generic;
using QFramework;

namespace CardGame
{
    /// <summary>
    /// 祭祀抽奖结果
    /// </summary>
    public class RitualResult
    {
        public string itemTypeName; // "丹药"/"法宝"/"卡牌"/"配方"/"材料"
        public string itemName;
        public int rarityLevel;     // 0=凡品/Common, 1=灵品/Uncommon, 2=玄品/Rare, 3=仙品/Legendary
        public bool isLuckyUp;      // 是否触发了品阶提升
        public int luckyUpLevels;   // 提升了几阶
    }

    /// <summary>
    /// 祭祀系统接口 — 献祭材料抽奖
    /// </summary>
    public interface IRitualSystem : ISystem
    {
        /// <summary>执行祭祀：献祭材料列表，返回抽奖结果</summary>
        RitualResult Sacrifice(List<(MaterialData material, int count)> offerings);

        /// <summary>预览献祭的品阶等级（不实际消耗）</summary>
        int PreviewRarityLevel(List<(MaterialData material, int count)> offerings);

        /// <summary>预览产出类型</summary>
        string PreviewOutputType(List<(MaterialData material, int count)> offerings);
    }
}
