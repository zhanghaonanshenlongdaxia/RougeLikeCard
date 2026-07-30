using CardGame;
using NueGames.NueDeck.Scripts.Enums;
using NueGames.NueDeck.Scripts.Managers;
using QFramework;
using TMPro;
using UnityEngine;

namespace NueGames.NueDeck.Scripts.UI
{
    public class CombatCanvas : CanvasBase, IController
    {
        public IArchitecture GetArchitecture() => CardGameArchitecture.Interface;
        [Header("Texts")]
        [SerializeField] private TextMeshProUGUI drawPileTextField;
        [SerializeField] private TextMeshProUGUI discardPileTextField;
        [SerializeField] private TextMeshProUGUI exhaustPileTextField;
        [SerializeField] private TextMeshProUGUI manaTextTextField;
        
        [Header("Panels")]
        [SerializeField] private GameObject combatWinPanel;
        [SerializeField] private GameObject combatLosePanel;

        public TextMeshProUGUI DrawPileTextField => drawPileTextField;
        public TextMeshProUGUI DiscardPileTextField => discardPileTextField;
        public TextMeshProUGUI ManaTextTextField => manaTextTextField;
        public GameObject CombatWinPanel => combatWinPanel;
        public GameObject CombatLosePanel => combatLosePanel;

        public TextMeshProUGUI ExhaustPileTextField => exhaustPileTextField;

        #region Setup
        private void Awake()
        {
            CombatWinPanel.SetActive(false);
            CombatLosePanel.SetActive(false);
        }
        #endregion

        #region Public Methods
        public void SetPileTexts()
        {
            var cardModel = this.GetModel<ICardModel>();
            var battleModel = this.GetModel<IBattleModel>();
            DrawPileTextField.text = $"{cardModel.DrawPile.Count}";
            DiscardPileTextField.text = $"{cardModel.DiscardPile.Count}";
            ExhaustPileTextField.text =  $"{cardModel.ExhaustPile.Count}";
            ManaTextTextField.text = $"{battleModel.CurrentMana.Value}/{battleModel.MaxMana.Value}";
        }

        public override void ResetCanvas()
        {
            base.ResetCanvas();
            CombatWinPanel.SetActive(false);
            CombatLosePanel.SetActive(false);
        }

        public void EndTurn()
        {
            if (CombatManager.CurrentCombatStateType == CombatStateType.AllyTurn)
                CombatManager.EndTurn();
        }
        #endregion
    }
}