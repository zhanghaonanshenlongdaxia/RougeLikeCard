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
    }
}
