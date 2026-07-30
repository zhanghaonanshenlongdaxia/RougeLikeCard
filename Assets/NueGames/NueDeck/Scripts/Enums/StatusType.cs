namespace NueGames.NueDeck.Scripts.Enums
{
    public enum StatusType
    {
        None = 0,
        Block,
        Poison,
        Strength,
        Dexterity,
        Stun,
        Weak,           // 虚弱：攻击伤害减少25%
        Frail,          // 脆弱：获得的格挡减少25%
        Vulnerable      // 易伤：受到的伤害增加50%
    }
}