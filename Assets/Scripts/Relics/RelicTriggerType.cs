namespace CardGame
{
    /// <summary>
    /// 遗物触发时机
    /// </summary>
    public enum RelicTriggerType
    {
        OnCombatStart,      // 战斗开始时
        OnCombatEnd,        // 战斗结束时
        OnTurnStart,        // 每回合开始时
        OnTurnEnd,          // 每回合结束时
        OnCardPlayed,       // 打出卡牌时
        OnAttackPlayed,     // 打出攻击牌时
        OnSkillPlayed,      // 打出技能牌时
        OnCardDrawn,        // 抽牌时
        OnPlayerDamaged,    // 玩家受伤时
        OnEnemyDamaged,     // 敌人受伤时
        OnEnemyDeath,       // 敌人死亡时
        OnUsePotion,        // 使用药水时
        OnGainGold,         // 获得金币时
        Passive              // 被动效果（持续生效）
    }
}
