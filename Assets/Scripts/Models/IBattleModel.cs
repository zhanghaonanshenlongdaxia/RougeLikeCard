using QFramework;
using NueGames.NueDeck.Scripts.Enums;

namespace CardGame
{
    /// <summary>
    /// 战斗数据模型 — 管理战斗全局状态，与 NueDeck 对齐
    /// </summary>
    public interface IBattleModel : IModel
    {
        BindableProperty<CombatStateType> State { get; }
        BindableProperty<int> CurrentMana { get; }
        BindableProperty<int> MaxMana { get; }
        BindableProperty<int> DrawCount { get; }
        BindableProperty<bool> CanSelectCards { get; }
        BindableProperty<bool> CanUseCards { get; }
        BindableProperty<int> CurrentGold { get; }

        int CurrentStageId { get; set; }
        int CurrentEncounterId { get; set; }
        bool IsFinalEncounter { get; set; }
        int MaxCardOnHand { get; set; }

        /// <summary>事件预存的力量加成（下场战斗生效后清零）</summary>
        int PendingStrengthBonus { get; set; }
        /// <summary>事件预存的敏捷加成（下场战斗生效后清零）</summary>
        int PendingDexterityBonus { get; set; }
        /// <summary>下场给敌人施虚弱层数</summary>
        int PendingEnemyWeak { get; set; }
        /// <summary>下场给敌人施脆弱层数</summary>
        int PendingEnemyFrail { get; set; }
        /// <summary>下场给敌人施易伤层数</summary>
        int PendingEnemyVulnerable { get; set; }
        /// <summary>下场自身反伤层数</summary>
        int PendingThorn { get; set; }
        /// <summary>下场开局格挡值</summary>
        int PendingBlockStart { get; set; }
        /// <summary>下场敌人HP降低百分比</summary>
        int PendingEnemyHpReduce { get; set; }
    }
}
