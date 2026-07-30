using QFramework;

namespace CardGame
{
    /// <summary>
    /// 卡牌牌堆系统 — 抽牌、弃牌、消耗、洗牌
    /// </summary>
    public interface ICardSystem : ISystem
    {
        void SetGameDeck();
        void DrawCards(int count);
        void DiscardHand();
        void OnCardPlayed(NueGames.NueDeck.Scripts.Card.CardBase card);
        void OnCardDiscarded(NueGames.NueDeck.Scripts.Card.CardBase card);
        void OnCardExhausted(NueGames.NueDeck.Scripts.Card.CardBase card);
        void ClearPiles();
    }
}
