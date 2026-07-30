using System.Collections.Generic;
using QFramework;
using NueGames.NueDeck.Scripts.Data.Collection;

namespace CardGame
{
    public class CardModel : AbstractModel, ICardModel
    {
        public List<CardData> DrawPile { get; } = new List<CardData>();
        public List<CardData> HandPile { get; } = new List<CardData>();
        public List<CardData> DiscardPile { get; } = new List<CardData>();
        public List<CardData> ExhaustPile { get; } = new List<CardData>();
        public List<CardData> CurrentCardsList { get; } = new List<CardData>();

        public BindableProperty<int> DrawPileCount { get; } = new BindableProperty<int>(0);
        public BindableProperty<int> HandCount { get; } = new BindableProperty<int>(0);
        public BindableProperty<int> DiscardPileCount { get; } = new BindableProperty<int>(0);
        public BindableProperty<int> ExhaustPileCount { get; } = new BindableProperty<int>(0);

        protected override void OnInit()
        {
        }
    }
}
