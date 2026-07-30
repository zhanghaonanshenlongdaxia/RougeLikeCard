using System.Collections.Generic;
using QFramework;
using NueGames.NueDeck.Scripts.Data.Collection;

namespace CardGame
{
    /// <summary>
    /// 卡牌数据模型 — 管理牌堆（抽牌/弃牌/手牌/消耗），直接使用 NueDeck 的 CardData
    /// </summary>
    public interface ICardModel : IModel
    {
        List<CardData> DrawPile { get; }
        List<CardData> HandPile { get; }
        List<CardData> DiscardPile { get; }
        List<CardData> ExhaustPile { get; }
        List<CardData> CurrentCardsList { get; }

        BindableProperty<int> DrawPileCount { get; }
        BindableProperty<int> HandCount { get; }
        BindableProperty<int> DiscardPileCount { get; }
        BindableProperty<int> ExhaustPileCount { get; }
    }
}
