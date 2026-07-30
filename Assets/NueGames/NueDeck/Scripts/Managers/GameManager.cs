using CardGame;
using NueGames.NueDeck.Scripts.Card;
using NueGames.NueDeck.Scripts.Data.Collection;
using NueGames.NueDeck.Scripts.Data.Containers;
using NueGames.NueDeck.Scripts.Data.Settings;
using NueGames.NueDeck.Scripts.EnemyBehaviour;
using NueGames.NueDeck.Scripts.NueExtentions;
using QFramework;
using UnityEngine;
using Random = UnityEngine.Random;

namespace NueGames.NueDeck.Scripts.Managers
{
    [DefaultExecutionOrder(-10)]
    public class GameManager : MonoBehaviour, IController
    { 
        public GameManager(){}
        public static GameManager Instance { get; private set; }
        
        public IArchitecture GetArchitecture() => CardGameArchitecture.Interface;
        
        [Header("Settings")]
        [SerializeField] private GameplayData gameplayData;
        [SerializeField] private EncounterData encounterData;
        [SerializeField] private SceneData sceneData;


        #region Cache
        public SceneData SceneData => sceneData;
        public EncounterData EncounterData => encounterData;
        public GameplayData GameplayData => gameplayData;
        public PersistentGameplayData PersistentGameplayData { get; private set; }
        protected UIManager UIManager => UIManager.Instance;
        #endregion
        
        #region Setup
        private void Awake()
        {
            if (Instance)
            {
                Destroy(gameObject);
                return;
               
            }
            else
            {
                transform.parent = null;
                Instance = this;
                DontDestroyOnLoad(gameObject);
                CardActionProcessor.Initialize();
                EnemyActionProcessor.Initialize();
                RelicProcessor.Initialize();
                PotionProcessor.Initialize();
                InitGameplayData();
                SyncBattleModel();
                SetInitalHand();
            }
        }
        #endregion
        
        #region Public Methods
        public void InitGameplayData()
        { 
            PersistentGameplayData = new PersistentGameplayData(gameplayData);
            if (UIManager)
                UIManager.InformationCanvas.ResetCanvas();
           
        } 
        public CardBase BuildAndGetCard(CardData targetData, Transform parent)
        {
            var clone = Instantiate(GameplayData.CardPrefab, parent);
            clone.SetCard(targetData);
            return clone;
        }
        public void SetInitalHand()
        {
            PersistentGameplayData.CurrentCardsList.Clear();
            
            if (PersistentGameplayData.IsRandomHand)
                for (var i = 0; i < GameplayData.RandomCardCount; i++)
                    PersistentGameplayData.CurrentCardsList.Add(GameplayData.AllCardsList.RandomItem());
            else
                foreach (var cardData in GameplayData.InitalDeck.CardList)
                    PersistentGameplayData.CurrentCardsList.Add(cardData);
        }
        public void NextEncounter()
        {
            PersistentGameplayData.CurrentEncounterId++;
            if (PersistentGameplayData.CurrentEncounterId>=EncounterData.EnemyEncounterList[PersistentGameplayData.CurrentStageId].EnemyEncounterList.Count)
            {
                PersistentGameplayData.CurrentEncounterId = Random.Range(0,
                    EncounterData.EnemyEncounterList[PersistentGameplayData.CurrentStageId].EnemyEncounterList.Count);
            }
        }
        public void OnExitApp()
        {
            
        }

        private void SyncBattleModel()
        {
            var model = this.GetModel<IBattleModel>();
            model.MaxMana.Value = PersistentGameplayData.MaxMana;
            model.CurrentMana.Value = PersistentGameplayData.CurrentMana;
            model.DrawCount.Value = PersistentGameplayData.DrawCount;
            model.CanSelectCards.Value = PersistentGameplayData.CanSelectCards;
            model.CanUseCards.Value = PersistentGameplayData.CanUseCards;
            model.CurrentStageId = PersistentGameplayData.CurrentStageId;
            model.CurrentEncounterId = PersistentGameplayData.CurrentEncounterId;
            model.IsFinalEncounter = PersistentGameplayData.IsFinalEncounter;
            model.MaxCardOnHand = GameplayData.MaxCardOnHand;
        }
        #endregion
      

    }
}
