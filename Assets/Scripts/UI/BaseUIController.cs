using TMPro;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using System.Collections.Generic;
using System.Linq;

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
                else if (name == "CodexButton") { btn.onClick.AddListener(OnCodex); }
                else if (name == "AdventureButton") { adventureButton = btn; btn.onClick.AddListener(OnAdventure); }
            }

            // Create CodexButton if not found
            if (GetComponentsInChildren<Button>(true).Length <= 6)
            {
                var codexBtnObj = new GameObject("CodexButton");
                codexBtnObj.transform.SetParent(transform, false);
                var rt = codexBtnObj.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.35f, 0.03f); rt.anchorMax = new Vector2(0.5f, 0.12f);
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                var img = codexBtnObj.AddComponent<Image>(); img.color = new Color(0.15f, 0.3f, 0.5f, 1f);
                var txtObj = new GameObject("Text");
                txtObj.transform.SetParent(codexBtnObj.transform, false);
                var txtRt = txtObj.AddComponent<RectTransform>();
                txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
                txtRt.offsetMin = Vector2.zero; txtRt.offsetMax = Vector2.zero;
                var tmp = txtObj.AddComponent<TextMeshProUGUI>();
                tmp.text = "图鉴"; tmp.fontSize = 20; tmp.color = Color.white;
                tmp.alignment = TextAlignmentOptions.Center;
                var libSans = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                if (libSans) tmp.font = libSans;
                var codexBtn = codexBtnObj.AddComponent<Button>();
                codexBtn.onClick.AddListener(OnCodex);
                Debug.Log("[BaseUI] CodexButton created at runtime");
            }

            // Create BreakthroughButton if not found
            if (GetComponentsInChildren<Button>(true).All(b => b.gameObject.name != "BreakthroughButton"))
            {
                var btBtnObj = new GameObject("BreakthroughButton");
                btBtnObj.transform.SetParent(transform, false);
                var rt = btBtnObj.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.51f, 0.03f); rt.anchorMax = new Vector2(0.66f, 0.12f);
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                btBtnObj.AddComponent<Image>().color = new Color(0.3f, 0.2f, 0.5f, 1f);
                var txtObj = new GameObject("Text");
                txtObj.transform.SetParent(btBtnObj.transform, false);
                var txtRt = txtObj.AddComponent<RectTransform>();
                txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
                txtRt.offsetMin = Vector2.zero; txtRt.offsetMax = Vector2.zero;
                var tmp = txtObj.AddComponent<TextMeshProUGUI>();
                tmp.text = "突破"; tmp.fontSize = 16; tmp.color = new Color(0.9f, 0.8f, 0.3f);
                tmp.alignment = TextAlignmentOptions.Center;
                var libSans = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                if (libSans) tmp.font = libSans;
                var btBtn = btBtnObj.AddComponent<Button>();
                btBtn.onClick.AddListener(OnBreakthrough);
                Debug.Log("[BaseUI] BreakthroughButton created at runtime");
            }

            // Create StoryTreeButton if not found
            if (GetComponentsInChildren<Button>(true).All(b => b.gameObject.name != "StoryTreeButton"))
            {
                var stBtnObj = new GameObject("StoryTreeButton");
                stBtnObj.transform.SetParent(transform, false);
                var rt = stBtnObj.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.67f, 0.03f); rt.anchorMax = new Vector2(0.82f, 0.12f);
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                stBtnObj.AddComponent<Image>().color = new Color(0.15f, 0.35f, 0.2f, 1f);
                var txtObj = new GameObject("Text");
                txtObj.transform.SetParent(stBtnObj.transform, false);
                var txtRt = txtObj.AddComponent<RectTransform>();
                txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
                txtRt.offsetMin = Vector2.zero; txtRt.offsetMax = Vector2.zero;
                var tmp = txtObj.AddComponent<TextMeshProUGUI>();
                tmp.text = "剧情树"; tmp.fontSize = 16; tmp.color = new Color(0.9f, 0.8f, 0.3f);
                tmp.alignment = TextAlignmentOptions.Center;
                var libSans2 = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                if (libSans2) tmp.font = libSans2;
                var stBtn = stBtnObj.AddComponent<Button>();
                stBtn.onClick.AddListener(ShowStoryTree);
                Debug.Log("[BaseUI] StoryTreeButton created at runtime");
            }

            // Create RelicStorageButton if not found
            if (GetComponentsInChildren<Button>(true).All(b => b.gameObject.name != "RelicStorageButton"))
            {
                var rsBtnObj = new GameObject("RelicStorageButton");
                rsBtnObj.transform.SetParent(transform, false);
                var rt = rsBtnObj.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.82f, 0.03f); rt.anchorMax = new Vector2(0.97f, 0.12f);
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                rsBtnObj.AddComponent<Image>().color = new Color(0.25f, 0.2f, 0.4f, 1f);
                var txtObj = new GameObject("Text");
                txtObj.transform.SetParent(rsBtnObj.transform, false);
                var txtRt = txtObj.AddComponent<RectTransform>();
                txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
                txtRt.offsetMin = Vector2.zero; txtRt.offsetMax = Vector2.zero;
                var tmp = txtObj.AddComponent<TextMeshProUGUI>();
                tmp.text = "法宝仓"; tmp.fontSize = 14; tmp.color = new Color(0.8f, 0.7f, 1f);
                tmp.alignment = TextAlignmentOptions.Center;
                var libSans3 = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                if (libSans3) tmp.font = libSans3;
                var rsBtn = rsBtnObj.AddComponent<Button>();
                rsBtn.onClick.AddListener(OnRelicStorage);
                Debug.Log("[BaseUI] RelicStorageButton created at runtime");
            }
        }

        private void OnEnable()
        {
            UpdateStatus();
            // 初始化故事树根节点（不再自动弹出）
            try
            {
                this.GetSystem<IStorySystem>().InitRootNodes();
            }
            catch { }
        }

        private void ShowStoryTree()
        {
            var existing = GameObject.Find("StoryTreeCanvas");
            if (existing != null) { existing.SetActive(true); return; }
            var go = new GameObject("StoryTreeCanvas");
            go.AddComponent<StoryTreeUIController>();
            Debug.Log("[BaseUI] Story tree UI opened");
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
            OpenCraftPanel();
        }

        public void OnForging()
        {
            OpenCraftPanel();
        }

        public void OnRitual()
        {
            var existing = GameObject.Find("RitualCanvas");
            if (existing != null) { existing.SetActive(!existing.activeSelf); return; }
            var go = new GameObject("RitualCanvas");
            go.AddComponent<RitualUIController>();
            go.SetActive(true);
        }

        private void OpenCraftPanel()
        {
            var existing = GameObject.Find("CraftCanvas");
            if (existing != null) { existing.SetActive(!existing.activeSelf); return; }
            var go = new GameObject("CraftCanvas");
            go.AddComponent<CraftUIController>();
            go.SetActive(true);
        }

        public void OnInventory()
        {
            var existing = GameObject.Find("InventoryCanvas");
            if (existing != null) { existing.SetActive(!existing.activeSelf); return; }
            var go = new GameObject("InventoryCanvas");
            go.AddComponent<InventoryUIController>();
            go.SetActive(true);
        }

        public void OnLoadout()
        {
            var existing = GameObject.Find("LoadoutCanvas");
            if (existing != null) { existing.SetActive(!existing.activeSelf); return; }
            var go = new GameObject("LoadoutCanvas");
            go.AddComponent<LoadoutUIController>();
            go.SetActive(true);
        }

        public void OnBreakthrough()
        {
            var existing = GameObject.Find("RealmBreakthroughCanvas");
            if (existing != null) { existing.SetActive(true); return; }

            var go = new GameObject("RealmBreakthroughCanvas");
            go.AddComponent<RealmBreakthroughUIController>();
            Debug.Log("[BaseUI] RealmBreakthrough panel opened");
        }

        public void OnCodex()
        {
            // EnemyCodexCanvas is runtime-created, skip prefab lookup
            var existing = GameObject.Find("EnemyCodexCanvas");
            if (existing != null) { existing.SetActive(!existing.activeSelf); return; }
            var codexObj = new GameObject("EnemyCodexCanvas");
            codexObj.AddComponent<EnemyCodexUIController>();
            codexObj.SetActive(true);
        }

        public void OnRelicStorage()
        {
            var existing = GameObject.Find("RelicStorageCanvas");
            if (existing != null) { existing.SetActive(!existing.activeSelf); return; }
            var go = new GameObject("RelicStorageCanvas");
            go.AddComponent<RelicStorageUIController>();
            go.SetActive(true);
        }

        /// <summary>
        /// 打开/关闭子面板，不存在则从 Prefab 实例化或动态创建
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
            {
                go.SetActive(true);
                return;
            }

            // 动态创建（用于没有预制体的面板）
            if (canvasName == "EnemyCodexCanvas")
            {
                var codexObj = new GameObject("EnemyCodexCanvas");
                codexObj.AddComponent<EnemyCodexUIController>();
                codexObj.SetActive(true);
            }
        }

        public void OnAdventure()
        {
            // 打开大地图选点面板（不直接进地图）
            var existing = GameObject.Find("AdventureMapCanvas");
            if (existing != null) { existing.SetActive(true); return; }

            var go = new GameObject("AdventureMapCanvas");
            go.AddComponent<AdventureMapUIController>();
            Debug.Log("[BaseUI] Adventure map panel opened");
        }
    }
}
