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
        public MapNodeType CurrentCombatNodeType { get; set; } = MapNodeType.Combat;
        public int PendingStrengthBonus { get; set; } = 0;
        public int PendingDexterityBonus { get; set; } = 0;
        public int PendingEnemyWeak { get; set; } = 0;
        public int PendingEnemyFrail { get; set; } = 0;
        public int PendingEnemyVulnerable { get; set; } = 0;
        public int PendingThorn { get; set; } = 0;
        public int PendingBlockStart { get; set; } = 0;
        public int PendingEnemyHpReduce { get; set; } = 0;

        protected override void OnInit()
        {
        }
    }
}
