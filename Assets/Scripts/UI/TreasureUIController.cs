using TMPro;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace CardGame.UI
{
    public class TreasureUIController : MonoBehaviour, IController
    {
        [SerializeField] private Image rewardImage;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Transform rewardSlotsRoot;
        [SerializeField] private Button confirmButton;

        public IArchitecture GetArchitecture() => CardGameArchitecture.Interface;

        public void ShowTreasure(string desc)
        {
            if (descriptionText) descriptionText.text = desc;
            gameObject.SetActive(true);
        }

        public void OnConfirm()
        {
            gameObject.SetActive(false);
        }
    }
}
