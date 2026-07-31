using TMPro;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace CardGame.UI
{
    public class DeathUIController : MonoBehaviour, IController
    {
        [SerializeField] private TextMeshProUGUI lostItemsText;
        [SerializeField] private Button returnButton;

        private IEvacuateSystem _evacSystem;
        private IInventoryModel _invModel;

        public IArchitecture GetArchitecture() => CardGameArchitecture.Interface;

        private void Awake()
        {
            _evacSystem = this.GetSystem<IEvacuateSystem>();
            _invModel = this.GetModel<IInventoryModel>();
        }

        public void ShowDeath()
        {
            // 璁板綍涓㈠け鐗╁搧
            var lost = "涓㈠け鐨勭墿鍝?\n";
            foreach (var slot in _invModel.Slots)
            {
                if (slot != null && !slot.IsEmpty)
                    lost += $"路 {slot.item.ItemName} 脳{slot.count}\n";
            }
            if (lost == "涓㈠け鐨勭墿鍝?\n") lost += "锛堣儗鍖呬负绌猴級";
            if (lostItemsText) lostItemsText.text = lost;

            // 鎵ц姝讳骸閫昏緫
            _evacSystem.OnDeath();

            gameObject.SetActive(true);
        }

        public void OnReturnToBase()
        {
            gameObject.SetActive(false);
            var uiManager = NueGames.NueDeck.Scripts.Managers.UIManager.Instance;
            if (uiManager != null && NueGames.NueDeck.Scripts.Managers.GameManager.Instance != null)
            {
                uiManager.ChangeScene(NueGames.NueDeck.Scripts.Managers.GameManager.Instance.SceneData.mainMenuSceneIndex);
            }
        }
    }
}
