using TMPro;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using Alchemy.Inspector;
using CardGame.UI;

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

            // Auto-bind inventoryButton if not assigned
            if (inventoryButton == null)
            {
                var btn = transform.Find("InventoryButton");
                if (btn != null) inventoryButton = btn.GetComponent<Button>();
            }
            if (evacuateButton == null)
            {
                var btn = transform.Find("EvacuateButton");
                if (btn != null) evacuateButton = btn.GetComponent<Button>();
            }
            if (checkpointText == null)
            {
                var txt = transform.Find("Progress/Text");
                if (txt != null) checkpointText = txt.GetComponent<TextMeshProUGUI>();
            }
            if (progressBarFill == null)
            {
                var fill = transform.Find("Progress/ProgressBarBg/ProgressBarFill");
                if (fill != null) progressBarFill = fill.GetComponent<Image>();
            }

            if (inventoryButton != null)
                inventoryButton.onClick.AddListener(OnInventoryButton);
            if (evacuateButton != null)
                evacuateButton.onClick.AddListener(OnEvacuateButton);
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

            if (checkpointText) checkpointText.text = $"关卡: {Mathf.CeilToInt(currentFloor / 3f)}/{Mathf.CeilToInt(totalFloors / 3f)}";
            if (progressBarFill) progressBarFill.fillAmount = (float)currentFloor / totalFloors;
        }

        public void OnInventoryButton()
        {
            var flyTarget = inventoryButton != null ? inventoryButton.GetComponent<RectTransform>() : null;
            var panel = AdventureBackpackPanel.GetOrCreate(flyTarget);
            panel.Show();
        }

        public void OnEvacuateButton()
        {
            this.GetSystem<IEvacuateSystem>().Evacuate();
            Debug.Log("[MapBottomBar] Evacuate triggered");
        }
    }
}
