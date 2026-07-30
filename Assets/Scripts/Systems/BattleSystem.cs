using QFramework;
using NueGames.NueDeck.Scripts.Enums;
using NueGames.NueDeck.Scripts.Managers;

namespace CardGame
{
    public class BattleSystem : AbstractSystem, IBattleSystem
    {
        protected IBattleModel BattleModel => this.GetModel<IBattleModel>();
        protected ICardModel CardModel => this.GetModel<ICardModel>();

        protected override void OnInit()
        {
        }

        public void StartAllyTurn()
        {
            var model = BattleModel;
            model.State.Value = CombatStateType.AllyTurn;
            model.CurrentMana.Value = model.MaxMana.Value;
            model.CanSelectCards.Value = true;
            model.CanUseCards.Value = true;

            if (CollectionManager.Instance)
                CollectionManager.Instance.DrawCards(model.DrawCount.Value);
        }

        public void EndTurn()
        {
            StartEnemyTurn();
        }

        public void StartEnemyTurn()
        {
            var model = BattleModel;
            model.State.Value = CombatStateType.EnemyTurn;
            model.CanSelectCards.Value = false;
            model.CanUseCards.Value = false;

            if (CollectionManager.Instance)
                CollectionManager.Instance.DiscardHand();
        }

        public void WinCombat()
        {
            var model = BattleModel;
            if (model.State.Value == CombatStateType.EndCombat) return;
            model.State.Value = CombatStateType.EndCombat;
            model.CanSelectCards.Value = false;
            model.CanUseCards.Value = false;
        }

        public void LoseCombat()
        {
            var model = BattleModel;
            if (model.State.Value == CombatStateType.EndCombat) return;
            model.State.Value = CombatStateType.EndCombat;
            model.CanSelectCards.Value = false;
            model.CanUseCards.Value = false;
        }
    }
}
