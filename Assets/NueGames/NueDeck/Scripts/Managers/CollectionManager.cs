using System.Collections;
using System.Collections.Generic;
using CardGame;
using NueGames.NueDeck.Scripts.Card;
using NueGames.NueDeck.Scripts.Collection;
using NueGames.NueDeck.Scripts.Data.Collection;
using QFramework;
using UnityEngine;

namespace NueGames.NueDeck.Scripts.Managers
{
    public class CollectionManager : MonoBehaviour, IController
    {
        public CollectionManager(){}
      
        public static CollectionManager Instance { get; private set; }

        [Header("Controllers")] 
        [SerializeField] private HandController handController;


        #region Cache

        public List<CardData> DrawPile => this.GetModel<ICardModel>().DrawPile;
        public List<CardData> HandPile => this.GetModel<ICardModel>().HandPile;
        public List<CardData> DiscardPile => this.GetModel<ICardModel>().DiscardPile;
        public List<CardData> ExhaustPile => this.GetModel<ICardModel>().ExhaustPile;
        
        public HandController HandController => handController;
        protected FxManager FxManager => FxManager.Instance;
        protected AudioManager AudioManager => AudioManager.Instance;
        protected GameManager GameManager => GameManager.Instance;
        protected CombatManager CombatManager => CombatManager.Instance;

        protected UIManager UIManager => UIManager.Instance;

        public IArchitecture GetArchitecture() => CardGameArchitecture.Interface;

        #endregion
       
        #region Setup
        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            else
            {
                Instance = this;
            }
        }

        #endregion

        #region Public Methods
        public void DrawCards(int targetDrawCount)
        {
            this.GetSystem<ICardSystem>().DrawCards(targetDrawCount);
            UIManager.CombatCanvas.SetPileTexts();
        }
        public void DiscardHand()
        {
            this.GetSystem<ICardSystem>().DiscardHand();
            UIManager.CombatCanvas.SetPileTexts();
        }
        
        public void OnCardDiscarded(CardBase targetCard)
        {
            this.GetSystem<ICardSystem>().OnCardDiscarded(targetCard);
            UIManager.CombatCanvas.SetPileTexts();
        }
        
        public void OnCardExhausted(CardBase targetCard)
        {
            this.GetSystem<ICardSystem>().OnCardExhausted(targetCard);
            UIManager.CombatCanvas.SetPileTexts();
        }
        public void OnCardPlayed(CardBase targetCard)
        {
            this.GetSystem<ICardSystem>().OnCardPlayed(targetCard);
            UIManager.CombatCanvas.SetPileTexts();
        }
        public void SetGameDeck()
        {
            this.GetSystem<ICardSystem>().SetGameDeck();
        }

        public void ClearPiles()
        {
            this.GetSystem<ICardSystem>().ClearPiles();
        }
        #endregion
    }
}