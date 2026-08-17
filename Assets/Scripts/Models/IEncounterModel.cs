using System.Collections.Generic;
using QFramework;

namespace CardGame
{
    /// <summary>
    /// 遭遇管理模型：记录本次冒险中已使用的遭遇，避免重复；
    /// 记录战斗次数用于敌人强度递增。
    /// </summary>
    public interface IEncounterModel : IModel
    {
        /// <summary>已用普通遭遇（key = encounter索引）</summary>
        HashSet<int> UsedNormalEncounters { get; }
        /// <summary>已用精英遭遇</summary>
        HashSet<int> UsedEliteEncounters { get; }
        /// <summary>本次冒险已战斗次数</summary>
        int CombatCount { get; set; }
        /// <summary>重置（新冒险开始时调用）</summary>
        void Reset();
    }

    public class EncounterModel : AbstractModel, IEncounterModel
    {
        public HashSet<int> UsedNormalEncounters { get; } = new HashSet<int>();
        public HashSet<int> UsedEliteEncounters { get; } = new HashSet<int>();
        public int CombatCount { get; set; } = 0;

        public void Reset()
        {
            UsedNormalEncounters.Clear();
            UsedEliteEncounters.Clear();
            CombatCount = 0;
        }

        protected override void OnInit()
        {
        }
    }
}
