namespace NueGames.NueDeck.Scripts.Enums
{
    /// <summary>敌人生成模式</summary>
    public enum EnemySpawnType
    {
        Solo,           // 单独：单个出现
        Multiple,       // 复数：相同敌人出现多个，数量可配置
        Commander,      // 主将：作为主将单独出现，可能有从属
        Subordinate     // 从属：作为主将的附属存在
    }

    /// <summary>从属生成模式</summary>
    public enum SubordinateSpawnMode
    {
        WithCommander,     // 进战斗直接生成主将+从属
        SummonByCommander  // 只有主将进场，主将定期召唤从属
    }
}
