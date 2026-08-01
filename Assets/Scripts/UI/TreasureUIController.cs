using TMPro;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using Alchemy.Inspector;

namespace CardGame.UI
{
    public class TreasureUIController : MonoBehaviour, IController
    {
        [SerializeField] private Image rewardImage;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Transform rewardSlotsRoot;
        [SerializeField] private Button confirmButton;

        public IArchitecture GetArchitecture() => CardGameArchitecture.Interface;

        private void Awake()
        {
            // 自动绑定确认按钮
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                if (btn.gameObject.name.Contains("Confirm"))
                {
                    confirmButton = btn;
                    btn.onClick.AddListener(OnConfirm);
                    break;
                }
            }
        }

        public void ShowTreasure(string desc)
        {
            if (descriptionText) descriptionText.text = desc;
            gameObject.SetActive(true);
        }

        public void OnConfirm()
        {
            gameObject.SetActive(false);
            var mapMgr = FindObjectOfType<NueGames.NueDeck.Scripts.Managers.MapManager>();
            if (mapMgr != null) mapMgr.SendMessage("BuildMap");
        }
    }
}
