using TMPro;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

using NueUIManager = NueGames.NueDeck.Scripts.Managers.UIManager;

namespace CardGame.UI
{
    public class BaseUIController : MonoBehaviour, IController
    {
        [Header("Buttons")]
        [SerializeField] private Button alchemyButton;
        [SerializeField] private Button forgingButton;
        [SerializeField] private Button ritualButton;
        [SerializeField] private Button inventoryButton;
        [SerializeField] private Button loadoutButton;
        [SerializeField] private Button adventureButton;

        [Header("Panels")]
        [SerializeField] private GameObject craftPanel;
        [SerializeField] private GameObject inventoryPanel;
        [SerializeField] private GameObject loadoutPanel;

        [Header("Status")]
        [SerializeField] private TextMeshProUGUI realmText;
        [SerializeField] private TextMeshProUGUI shenShiText;
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI hpText;

        public IArchitecture GetArchitecture() => CardGameArchitecture.Interface;

        private void Awake()
        {
            // 运行时绑定按钮（因为 Prefab 创建时 SerializeField 未赋值）
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                var name = btn.gameObject.name;
                if (name == "Btn_0") btn.onClick.AddListener(OnAlchemy);
                else if (name == "Btn_1") btn.onClick.AddListener(OnForging);
                else if (name == "Btn_2") btn.onClick.AddListener(OnRitual);
                else if (name == "Btn_3") btn.onClick.AddListener(OnInventory);
                else if (name == "Btn_4") btn.onClick.AddListener(OnLoadout);
                else if (name == "AdventureButton") btn.onClick.AddListener(OnAdventure);
            }

            // 查找子面板
            var canvas = GetComponent<Canvas>() ?? GetComponentInChildren<Canvas>();
            if (canvas == null)
            {
                // 在场景中查找
                craftPanel = GameObject.Find("CraftCanvas");
                inventoryPanel = GameObject.Find("InventoryCanvas");
                loadoutPanel = GameObject.Find("LoadoutCanvas");
            }
        }

        private void OnEnable()
        {
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            var battleModel = this.GetModel<IBattleModel>();
            var loadoutModel = this.GetModel<ILoadoutModel>();
            var gm = NueGames.NueDeck.Scripts.Managers.GameManager.Instance;

            if (realmText) realmText.text = "境界: 练气期";
            if (shenShiText) shenShiText.text = $"神识: {loadoutModel.CurrentShenShi.Value}/{loadoutModel.MaxShenShi.Value}";
            if (goldText) goldText.text = $"灵石: {battleModel.CurrentGold.Value}";
            if (hpText && gm != null && gm.PersistentGameplayData.AllyList.Count > 0)
                hpText.text = $"HP: {gm.PersistentGameplayData.AllyList[0].CharacterStats.CurrentHealth}/{gm.PersistentGameplayData.AllyList[0].CharacterStats.MaxHealth}";
        }

        public void OnAlchemy()
        {
            if (craftPanel) { craftPanel.SetActive(true); var ctrl = craftPanel.GetComponent<CraftUIController>(); }
        }

        public void OnForging()
        {
            if (craftPanel) craftPanel.SetActive(true);
        }

        public void OnRitual()
        {
            if (craftPanel) craftPanel.SetActive(true);
        }

        public void OnInventory()
        {
            if (inventoryPanel) inventoryPanel.SetActive(true);
        }

        public void OnLoadout()
        {
            if (loadoutPanel) loadoutPanel.SetActive(true);
        }

        public void OnAdventure()
        {
            this.GetSystem<ILoadoutSystem>().StartAdventure();
            var uiManager = NueUIManager.Instance;
            if (uiManager != null)
            {
                uiManager.ChangeScene(NueGames.NueDeck.Scripts.Managers.GameManager.Instance.SceneData.mapSceneIndex);
            }
        }
    }
}
