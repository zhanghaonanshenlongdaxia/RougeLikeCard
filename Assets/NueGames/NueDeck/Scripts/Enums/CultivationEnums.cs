namespace NueGames.NueDeck.Scripts.Enums
{
    /// <summary>五行及特殊属性</summary>
    public enum ElementType
    {
        None,       // 无属性 (神通)
        Metal,      // 金
        Wood,       // 木
        Water,      // 水
        Fire,       // 火
        Earth,      // 土
        Wind,       // 风 (预留)
        Thunder,    // 雷 (预留)
        Ghost,      // 鬼 (预留)
        Sword       // 剑 (预留)
    }

    /// <summary>功法品阶</summary>
    public enum CultivationMethodGrade
    {
        Complete,   // 完整本 — 可修至渡劫
        Fragment    // 残篇 — 只能修到指定境界
    }

    /// <summary>节点解锁方式</summary>
    public enum NodeUnlockType
    {
        Comprehension,     // 消耗参悟点直接解锁
        Minigame,           // 小游戏突破 (技巧+运气)
        Material,           // 需要特殊材料搭配
        CombatTrigger       // 战斗中几率触发突破
    }

    /// <summary>节点奖励类型</summary>
    public enum NodeRewardType
    {
        Card,               // 解锁卡牌 (加入功法基础牌组，可为1~多张)
        Recipe,             // 解锁丹方/图纸
        PassiveStat,        // 被动属性提升 (MaxHP/神识上限/灵力上限)
        CraftBonus          // 炼丹/炼器加成 (成功率/品质，独立计算)
    }

    /// <summary>被动属性类型</summary>
    public enum PassiveStatType
    {
        None,
        MaxHP,              // 生命上限
        ShenShi,            // 神识上限
        MaxMana             // 灵力上限
    }

    /// <summary>炼制加成类型 (独立于属性，单独计算)</summary>
    public enum CraftBonusType
    {
        None,
        AlchemySuccess,     // 炼丹成功率
        AlchemyQuality,     // 炼丹品质
        ForgingSuccess,     // 炼器成功率
        ForgingQuality      // 炼器品质
    }
}
