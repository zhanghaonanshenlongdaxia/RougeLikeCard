using QFramework;

namespace CardGame
{
    public class EndTurnCommand : AbstractCommand
    {
        protected override void OnExecute()
        {
            this.GetSystem<IBattleSystem>().EndTurn();
        }
    }
}
