using System.Collections.Generic;
using QFramework;
using NueGames.NueDeck.Scripts.Card;
using NueGames.NueDeck.Scripts.Data.Collection;
using NueGames.NueDeck.Scripts.Managers;
using UnityEngine;

namespace CardGame
{
    public class CardSystem : AbstractSystem, ICardSystem
    {
        protected ICardModel CardModel => this.GetModel<ICardModel>();
        protected IBattleModel BattleModel => this.GetModel<IBattleModel>();
        protected GameManager GameManager => GameManager.Instance;
        protected CollectionManager CollectionManager => CollectionManager.Instance;

        protected override void OnInit()
        {
        }

        public void SetGameDeck()
        {
            var model = CardModel;
            model.DrawPile.Clear();
            model.DiscardPile.Clear();
            model.HandPile.Clear();
            model.ExhaustPile.Clear();

            if (GameManager && GameManager.PersistentGameplayData != null)
            {
                foreach (var card in GameManager.PersistentGameplayData.CurrentCardsList)
                    model.DrawPile.Add(card);
            }

            UpdateCountProperties();
        }

        public void DrawCards(int count)
        {
            var model = CardModel;
            var maxOnHand = BattleModel.MaxCardOnHand;
            var currentDrawCount = 0;

            for (var i = 0; i < count; i++)
            {
                if (maxOnHand <= model.HandPile.Count) break;

                if (model.DrawPile.Count <= 0)
                {
                    var nDrawCount = count - currentDrawCount;
                    if (nDrawCount >= model.DiscardPile.Count)
                        nDrawCount = model.DiscardPile.Count;
                    ReshuffleDiscardPile();
                    DrawCards(nDrawCount);
                    break;
                }

                var randomCard = model.DrawPile[Random.Range(0, model.DrawPile.Count)];
                var clone = GameManager.BuildAndGetCard(randomCard, CollectionManager.HandController.drawTransform);
                CollectionManager.HandController.AddCardToHand(clone);
                model.HandPile.Add(randomCard);
                model.DrawPile.Remove(randomCard);
                currentDrawCount++;
            }

            foreach (var cardObject in CollectionManager.HandController.hand)
                cardObject.UpdateCardText();

            UpdateCountProperties();
        }

        public void DiscardHand()
        {
            var model = CardModel;
            foreach (var cardBase in CollectionManager.HandController.hand)
                cardBase.Discard();
            CollectionManager.HandController.hand.Clear();
            UpdateCountProperties();
        }

        public void OnCardPlayed(CardBase targetCard)
        {
            var model = CardModel;
            if (targetCard.CardData.ExhaustAfterPlay)
                targetCard.Exhaust();
            else
                targetCard.Discard();

            foreach (var cardObject in CollectionManager.HandController.hand)
                cardObject.UpdateCardText();
        }

        public void OnCardDiscarded(CardBase targetCard)
        {
            var model = CardModel;
            model.HandPile.Remove(targetCard.CardData);
            model.DiscardPile.Add(targetCard.CardData);
            UpdateCountProperties();
        }

        public void OnCardExhausted(CardBase targetCard)
        {
            var model = CardModel;
            model.HandPile.Remove(targetCard.CardData);
            model.ExhaustPile.Add(targetCard.CardData);
            UpdateCountProperties();
        }

        public void ClearPiles()
        {
            var model = CardModel;
            model.DiscardPile.Clear();
            model.DrawPile.Clear();
            model.HandPile.Clear();
            model.ExhaustPile.Clear();
            if (CollectionManager && CollectionManager.HandController)
                CollectionManager.HandController.hand.Clear();
            UpdateCountProperties();
        }

        private void ReshuffleDiscardPile()
        {
            var model = CardModel;
            foreach (var card in model.DiscardPile)
                model.DrawPile.Add(card);
            model.DiscardPile.Clear();
            UpdateCountProperties();
        }

        private void UpdateCountProperties()
        {
            var model = CardModel;
            model.DrawPileCount.Value = model.DrawPile.Count;
            model.HandCount.Value = model.HandPile.Count;
            model.DiscardPileCount.Value = model.DiscardPile.Count;
            model.ExhaustPileCount.Value = model.ExhaustPile.Count;
        }
    }
}
