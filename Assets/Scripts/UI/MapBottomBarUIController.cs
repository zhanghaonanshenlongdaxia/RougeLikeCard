using TMPro;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace CardGame.UI
{
    public class MapBottomBarUIController : MonoBehaviour, IController
    {
        [SerializeField] private Button inventoryButton;
        [SerializeField] private TextMeshProUGUI checkpointText;
        [SerializeField] private Image progressBarFill;
        [SerializeField] private Button evacuateButton;

        private IMapModel _mapModel;

        public IArchitecture GetArchitecture() => CardGameArchitecture.Interface;

        private void Awake()
        {
            _mapModel = this.GetModel<IMapModel>();
        }

        private void OnEnable()
        {
            UpdateProgress();
        }

        private void UpdateProgress()
        {
            if (_mapModel?.CurrentMap == null) return;
            var map = _mapModel.CurrentMap;
            int totalFloors = map.totalFloors;
            int currentFloor = map.currentFloor;

            if (checkpointText) checkpointText.text = $"澶у叧鍗? {Mathf.CeilToInt(currentFloor / 3f)}/{Mathf.CeilToInt(totalFloors / 3f)}";
            if (progressBarFill) progressBarFill.fillAmount = (float)currentFloor / totalFloors;
        }

        public void OnInventoryButton()
        {
            // 鎵撳紑鑳屽寘鐣岄潰
            Debug.Log("[MapBottomBar] Open inventory");
        }

        public void OnEvacuateButton()
        {
            this.GetSystem<IEvacuateSystem>().Evacuate();
            Debug.Log("[MapBottomBar] Evacuate triggered");
        }
    }
}
