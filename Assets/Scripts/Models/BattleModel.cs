using QFramework;
using NueGames.NueDeck.Scripts.Enums;

namespace CardGame
{
    public class BattleModel : AbstractModel, IBattleModel
    {
        public BindableProperty<CombatStateType> State { get; } = new BindableProperty<CombatStateType>(CombatStateType.PrepareCombat);
        public BindableProperty<int> CurrentMana { get; } = new BindableProperty<int>(0);
        public BindableProperty<int> MaxMana { get; } = new BindableProperty<int>(3);
        public BindableProperty<int> DrawCount { get; } = new BindableProperty<int>(4);
        public BindableProperty<bool> CanSelectCards { get; } = new BindableProperty<bool>(true);
        public BindableProperty<bool> CanUseCards { get; } = new BindableProperty<bool>(true);
        public BindableProperty<int> CurrentGold { get; } = new BindableProperty<int>(0);

        public int CurrentStageId { get; set; } = 0;
        public int CurrentEncounterId { get; set; } = 0;
        public bool IsFinalEncounter { get; set; } = false;
        public int MaxCardOnHand { get; set; } = 10;

        protected override void OnInit()
        {
        }
    }
}
