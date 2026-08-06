using System.Collections.Generic;
using QFramework;

namespace CardGame
{
    /// <summary>
    /// 敌人图鉴系统：管理敌人解锁、查询
    /// </summary>
    public interface IEnemyCodexSystem : ISystem
    {
        /// <summary>敌人遭遇时调用，解锁图鉴</summary>
        void OnEncounter(string enemyId);

        /// <summary>查询敌人是否已解锁</summary>
        bool IsUnlocked(string enemyId);

        /// <summary>获取所有已解锁的敌人Id</summary>
        List<string> GetAllUnlocked();
    }

    public class EnemyCodexSystem : AbstractSystem, IEnemyCodexSystem
    {
        private IEnemyCodexModel _model;

        protected override void OnInit()
        {
            _model = this.GetModel<IEnemyCodexModel>();
        }

        public void OnEncounter(string enemyId)
        {
            if (_model != null) _model.UnlockEnemy(enemyId);
        }

        public bool IsUnlocked(string enemyId)
        {
            return _model != null && _model.UnlockedEnemyIds.Contains(enemyId);
        }

        public List<string> GetAllUnlocked()
        {
            return _model != null ? new List<string>(_model.UnlockedEnemyIds) : new List<string>();
        }
    }
}