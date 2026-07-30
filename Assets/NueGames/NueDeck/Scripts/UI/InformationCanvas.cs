using CardGame;
using NueGames.NueDeck.Scripts.Managers;
using QFramework;
using TMPro;
using UnityEngine;

namespace NueGames.NueDeck.Scripts.UI
{
    public class InformationCanvas : CanvasBase, IController
    {
        public IArchitecture GetArchitecture() => CardGameArchitecture.Interface;
        [Header("Settings")] 
        [SerializeField] private GameObject randomizedDeckObject;
        [SerializeField] private TextMeshProUGUI roomTextField;
        [SerializeField] private TextMeshProUGUI goldTextField;
        [SerializeField] private TextMeshProUGUI nameTextField;
        [SerializeField] private TextMeshProUGUI healthTextField;

        public GameObject RandomizedDeckObject => randomizedDeckObject;
        public TextMeshProUGUI RoomTextField => roomTextField;
        public TextMeshProUGUI GoldTextField => goldTextField;
        public TextMeshProUGUI NameTextField => nameTextField;
        public TextMeshProUGUI HealthTextField => healthTextField;
        
        
        #region Setup
        private void Awake()
        {
            ResetCanvas();
        }
        #endregion
        
        #region Public Methods
        public void SetRoomText(int roomNumber,bool useStage = false, int stageNumber = -1) => 
            RoomTextField.text = useStage ? $"Room {stageNumber}/{roomNumber}" : $"Room {roomNumber}";

        public void SetGoldText(int value)
        {
            GoldTextField.text = $"{value}";
            this.GetModel<IBattleModel>().CurrentGold.Value = value;
        }

        public void SetNameText(string name) => NameTextField.text = $"{name}";

        public void SetHealthText(int currentHealth,int maxHealth) => HealthTextField.text = $"{currentHealth}/{maxHealth}";

        public override void ResetCanvas()
        {
            var battleModel = this.GetModel<IBattleModel>();
            RandomizedDeckObject.SetActive(GameManager.PersistentGameplayData.IsRandomHand);
            SetHealthText(GameManager.PersistentGameplayData.AllyList[0].AllyCharacterData.MaxHealth,GameManager.PersistentGameplayData.AllyList[0].AllyCharacterData.MaxHealth);
            SetNameText(GameManager.GameplayData.DefaultName);
            SetRoomText(battleModel.CurrentEncounterId+1,GameManager.GameplayData.UseStageSystem,battleModel.CurrentStageId+1);
            SetGoldText(battleModel.CurrentGold.Value);
        }
        #endregion
        
    }
}