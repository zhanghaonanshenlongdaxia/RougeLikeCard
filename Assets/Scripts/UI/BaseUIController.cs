using TMPro;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using System.Collections.Generic;

using NueUIManager = NueGames.NueDeck.Scripts.Managers.UIManager;
using Alchemy.Inspector;

namespace CardGame.UI
{
    /// <summary>
    /// 基地场景启动器 — 自动从 Prefab 实例化所有 UI
    /// 挂在基地场景的一个空 GameObject 上
    /// </summary>
    public class BaseSceneInitializer : MonoBehaviour
    {
        private void Awake()
        {
            // 实例化 BaseCanvas
            InstantiateCanvas("BaseCanvas");
            // 子面板默认隐藏，需要时才实例化/显示
        }

        /// <summary>
        /// 从 Prefab 路径实例化 Canvas，返回 GameObject
        /// </summary>
        public static GameObject InstantiateCanvas(string canvasName)
        {
            // 先检查场景中是否已存在
            var existing = GameObject.Find(canvasName);
            if (existing != null)
            {
                existing.SetActive(true);
                return existing;
            }

            // 从 Prefab 加载
            GameObject prefab = null;
#if UNITY_EDITOR
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/NueGames/NueDeck/Prefabs/UI/{canvasName}.prefab");
#endif
            if (prefab == null)
                prefab = Resources.Load<GameObject>($"UI/{canvasName}");

            if (prefab != null)
            {
                var instance = Instantiate(prefab);
                instance.name = canvasName;
                Debug.Log($"[BaseScene] Instantiated {canvasName}");
                return instance;
            }

            Debug.LogWarning($"[BaseScene] Prefab not found: {canvasName}");
            return null;
        }
    }

    public class BaseUIController : MonoBehaviour, IController
    {
        [FoldoutGroup("Buttons")]
        [SerializeField] private Button alchemyButton;
        [SerializeField] private Button forgingButton;
        [SerializeField] private Button ritualButton;
        [SerializeField] private Button inventoryButton;
        [SerializeField] private Button loadoutButton;
        [SerializeField] private Button adventureButton;

        [FoldoutGroup("Status")]
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
                if (name == "AlchemyButton") { alchemyButton = btn; btn.onClick.AddListener(OnAlchemy); }
                else if (name == "ForgingButton") { forgingButton = btn; btn.onClick.AddListener(OnForging); }
                else if (name == "RitualButton") { ritualButton = btn; btn.onClick.AddListener(OnRitual); }
                else if (name == "InventoryButton") { inventoryButton = btn; btn.onClick.AddListener(OnInventory); }
                else if (name == "LoadoutButton") { loadoutButton = btn; btn.onClick.AddListener(OnLoadout); }
                else if (name == "AdventureButton") { adventureButton = btn; btn.onClick.AddListener(OnAdventure); }
            }
        }

        private void OnEnable()
        {
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            try
            {
                var battleModel = this.GetModel<IBattleModel>();
                var loadoutModel = this.GetModel<ILoadoutModel>();
                var gm = NueGames.NueDeck.Scripts.Managers.GameManager.Instance;

                if (realmText) realmText.text = "境界: 练气期";
                if (loadoutModel != null && shenShiText) shenShiText.text = $"神识: {loadoutModel.CurrentShenShi.Value}/{loadoutModel.MaxShenShi.Value}";
                if (battleModel != null && goldText) goldText.text = $"灵石: {battleModel.CurrentGold.Value}";
                if (hpText && gm != null && gm.PersistentGameplayData != null && gm.PersistentGameplayData.AllyList != null && gm.PersistentGameplayData.AllyList.Count > 0)
                    hpText.text = $"HP: {gm.PersistentGameplayData.AllyList[0].CharacterStats.CurrentHealth}/{gm.PersistentGameplayData.AllyList[0].CharacterStats.MaxHealth}";
            }
            catch { /* QFramework not initialized yet */ }
        }

        public void OnAlchemy()
        {
            TogglePanel("CraftCanvas");
        }

        public void OnForging()
        {
            TogglePanel("CraftCanvas");
        }

        public void OnRitual()
        {
            TogglePanel("CraftCanvas");
        }

        public void OnInventory()
        {
            TogglePanel("InventoryCanvas");
        }

        public void OnLoadout()
        {
            TogglePanel("LoadoutCanvas");
        }

        /// <summary>
        /// 打开/关闭子面板，不存在则从 Prefab 实例化
        /// </summary>
        private void TogglePanel(string canvasName)
        {
            // 先找场景中是否已有
            var existing = GameObject.Find(canvasName);
            if (existing != null)
            {
                existing.SetActive(!existing.activeSelf);
                return;
            }

            // 从 Prefab 实例化
            var go = BaseSceneInitializer.InstantiateCanvas(canvasName);
            if (go != null)
                go.SetActive(true);
        }

        public void OnAdventure()
        {
            this.GetSystem<ILoadoutSystem>().StartAdventure();
            var uiManager = NueUIManager.Instance;
            if (uiManager != null && NueGames.NueDeck.Scripts.Managers.GameManager.Instance != null)
            {
                uiManager.ChangeScene(NueGames.NueDeck.Scripts.Managers.GameManager.Instance.SceneData.mapSceneIndex);
            }
        }
    }
}
