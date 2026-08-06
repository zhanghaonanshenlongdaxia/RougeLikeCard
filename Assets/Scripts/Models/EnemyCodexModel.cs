using System.Collections.Generic;
using QFramework;

namespace CardGame
{
    /// <summary>
    /// 敌人图鉴数据模型：记录已解锁的敌人
    /// </summary>
    public interface IEnemyCodexModel : IModel
    {
        /// <summary>已解锁的敌人Id集合</summary>
        HashSet<string> UnlockedEnemyIds { get; }

        /// <summary>记录一次遭遇（解锁敌人）</summary>
        void UnlockEnemy(string enemyId);
    }

    public class EnemyCodexModel : AbstractModel, IEnemyCodexModel
    {
        public HashSet<string> UnlockedEnemyIds { get; } = new HashSet<string>();

        protected override void OnInit()
        {
        }

        public void UnlockEnemy(string enemyId)
        {
            if (string.IsNullOrEmpty(enemyId)) return;
            if (UnlockedEnemyIds.Add(enemyId))
                UnityEngine.Debug.Log($"[图鉴] 解锁敌人: {enemyId}");
        }
    }
}