using QFramework;
using NueGames.NueDeck.Scripts.Enums;

namespace CardGame
{
    /// <summary>
    /// 战斗流程系统 — 回合管理、胜负判定
    /// </summary>
    public interface IBattleSystem : ISystem
    {
        void StartAllyTurn();
        void EndTurn();
        void StartEnemyTurn();
        void WinCombat();
        void LoseCombat();
    }
}
