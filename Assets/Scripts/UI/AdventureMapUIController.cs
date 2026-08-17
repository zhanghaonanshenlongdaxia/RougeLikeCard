using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using CardGame.Audio;

namespace CardGame.UI
{
    /// <summary>
    /// 大地图选点面板。Prefab 驱动，MapItem 用预制体。
    /// </summary>
    public class AdventureMapUIController : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject _mapItemPrefab;

        [Header("References")]
        [SerializeField] private Transform _mapListRoot;
        [SerializeField] private TextMeshProUGUI _detailText;
        [SerializeField] private Button _departButton;
        [SerializeField] private Button _backButton;
        [SerializeField] private TextMeshProUGUI _departButtonText;

        private TMP_FontAsset _font;
        private AdventureMapConfig _config;
        private AdventureMapData _selectedMap;
        private AdventureDifficulty _selectedDifficulty;

        public IArchitecture GetArchitecture() => CardGameArchitecture.Interface;

        private void Awake()
        {
            _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            LoadConfig();
            AutoBindReferences();
            PopulateMapList();
            WireButtons();
        }

        private void AutoBindReferences()
        {
            var panel = transform.Find("Panel");
            if (panel == null) return;

            if (_mapItemPrefab == null)
                _mapItemPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Prefabs/UI/Map/AdventureMap/MapItem.prefab");

            var mapList = panel.Find("MapList");
            if (mapList != null && _mapListRoot == null)
            {
                var scroll = mapList.Find("Scroll");
                if (scroll != null)
                    _mapListRoot = scroll.Find("Viewport/Content");
            }

            var detailPanel = panel.Find("DetailPanel");
            if (detailPanel != null)
            {
                if (_detailText == null) _detailText = detailPanel.Find("DetailText")?.GetComponent<TextMeshProUGUI>();
                if (_departButton == null) _departButton = detailPanel.Find("DepartButton")?.GetComponent<Button>();
                if (_backButton == null) _backButton = detailPanel.Find("BackButton")?.GetComponent<Button>();
                if (_departButtonText == null && _departButton != null)
                    _departButtonText = _departButton.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
            }
        }

        private void WireButtons()
        {
            if (_departButton != null) _departButton.onClick.AddListener(OnDepart);
            if (_backButton != null) _backButton.onClick.AddListener(() =>
            {
                if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick);
                gameObject.SetActive(false);
            });
        }

        private void LoadConfig()
        {
#if UNITY_EDITOR
            _config = UnityEditor.AssetDatabase.LoadAssetAtPath<AdventureMapConfig>(
                "Assets/NueGames/NueDeck/Data/AdventureMaps/AdventureMapConfig.asset");
#else
            _config = Resources.Load<AdventureMapConfig>("AdventureMapConfig");
#endif
            if (_config == null)
                Debug.LogError("[AdventureMap] AdventureMapConfig not found!");
        }

        private void PopulateMapList()
        {
            if (_config == null || _mapListRoot == null || _mapItemPrefab == null) return;

            // Clear old items
            for (int i = _mapListRoot.childCount - 1; i >= 0; i--)
                Destroy(_mapListRoot.GetChild(i).gameObject);

            var arch = CardGameArchitecture.Interface;
            var realmModel = arch.GetModel<IRealmModel>();
            var loadoutModel = arch.GetModel<ILoadoutModel>();
            int currentRealm = realmModel.CurrentRealm.Value;
            int currentShenShi = loadoutModel.CurrentShenShi.Value;

            foreach (var mapData in _config.maps)
            {
                bool mapUnlocked = currentRealm >= mapData.unlockRealmLevel;

                // Instantiate MapItem prefab
                var itemGo = Instantiate(_mapItemPrefab, _mapListRoot, false);
                itemGo.name = $"Map_{mapData.mapId}";

                // Set background color
                var img = itemGo.GetComponent<Image>();
                if (img != null) img.color = mapUnlocked ? new Color(0.1f, 0.2f, 0.3f, 1f) : new Color(0.1f, 0.1f, 0.1f, 0.5f);

                // Set name
                var nameTmp = itemGo.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
                if (nameTmp != null)
                {
                    nameTmp.text = mapUnlocked ? mapData.mapName : $"{mapData.mapName} (未解锁)";
                    nameTmp.color = mapUnlocked ? new Color(0.9f, 0.8f, 0.3f) : new Color(0.4f, 0.4f, 0.4f);
                    if (_font) nameTmp.font = _font;
                }

                // Set description
                var descTmp = itemGo.transform.Find("DescText")?.GetComponent<TextMeshProUGUI>();
                if (descTmp != null)
                {
                    descTmp.text = mapData.description;
                    if (_font) descTmp.font = _font;
                }

                // Set difficulty buttons in DiffRow (clone from btnDiff template)
                var diffRow = itemGo.transform.Find("DiffRow");
                if (diffRow != null)
                {
                    // Get the btnDiff template
                    var btnDiffTemplate = diffRow.Find("btnDiff");
                    if (btnDiffTemplate != null)
                    {
                        // Hide the template
                        btnDiffTemplate.gameObject.SetActive(false);

                        foreach (var diff in mapData.difficulties)
                        {
                            bool canEnter = mapUnlocked && currentShenShi >= diff.requiredShenShi;
                            Color btnColor = mapUnlocked
                                ? (canEnter ? GetDifficultyColor(diff.difficultyType) : new Color(0.15f, 0.15f, 0.15f, 0.8f))
                                : new Color(0.15f, 0.15f, 0.15f, 0.8f);

                            // Clone btnDiff
                            var diffBtnGo = Instantiate(btnDiffTemplate.gameObject, diffRow, false);
                            diffBtnGo.name = $"Btn_{diff.difficultyName}";
                            diffBtnGo.SetActive(true);

                            var diffBtn = diffBtnGo.GetComponent<Button>();
                            var diffImg = diffBtnGo.GetComponent<Image>();
                            if (diffImg != null) diffImg.color = btnColor;

                            var diffTmp = diffBtnGo.GetComponentInChildren<TextMeshProUGUI>();
                            if (diffTmp != null)
                            {
                                diffTmp.text = diff.difficultyName;
                                diffTmp.color = new Color(0.95f, 0.85f, 0.4f);
                                if (_font) diffTmp.font = _font;
                            }

                            if (diffBtn != null)
                            {
                                diffBtn.interactable = true;
                                var capturedMap = mapData;
                                var capturedDiff = diff;
                                var capturedCanEnter = canEnter;
                                diffBtn.onClick.AddListener(() =>
                                {
                                    if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick);
                                    OnDifficultySelected(capturedMap, capturedDiff, capturedCanEnter);
                                });
                            }
                        }
                    }
                }
            }
        }

        private Button CreateDiffButton(Transform parent, string label, Color color)
        {
            var go = new GameObject($"Btn_{label}");
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = color;
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 35;
            var txtObj = new GameObject("Text");
            txtObj.transform.SetParent(go.transform, false);
            var txtRt = txtObj.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero; txtRt.offsetMax = Vector2.zero;
            var tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label; tmp.fontSize = 18; tmp.color = new Color(0.95f, 0.85f, 0.4f);
            tmp.alignment = TextAlignmentOptions.Center;
            if (_font) tmp.font = _font;
            return go.AddComponent<Button>();
        }

        private Color GetDifficultyColor(DifficultyType type) => type switch
        {
            DifficultyType.Normal => new Color(0.15f, 0.3f, 0.5f, 1f),
            DifficultyType.Hard => new Color(0.4f, 0.2f, 0.1f, 1f),
            DifficultyType.Extreme => new Color(0.3f, 0.1f, 0.3f, 1f),
            DifficultyType.Hell => new Color(0.5f, 0.1f, 0.1f, 1f),
            _ => Color.gray
        };

        private void OnDifficultySelected(AdventureMapData map, AdventureDifficulty diff, bool canEnter)
        {
            _selectedMap = canEnter ? map : null;
            _selectedDifficulty = diff;

            int currentShenShi = CardGameArchitecture.Interface.GetModel<ILoadoutModel>().CurrentShenShi.Value;
            int currentRealm = CardGameArchitecture.Interface.GetModel<IRealmModel>().CurrentRealm.Value;
            bool realmUnlocked = currentRealm >= map.unlockRealmLevel;
            string realmName = ((RealmLevel)map.unlockRealmLevel).ToString();

            if (_detailText != null)
            {
                _detailText.text = $"{map.mapName} — {diff.difficultyName}\n\n{map.description}\n\n" +
                    $"难度：{diff.difficultyName}\n{diff.description}\n\n" +
                    $"层数：{diff.mapFloors}层\n" +
                    $"敌HP倍率：×{diff.enemyHpMultiplier}\n" +
                    $"敌攻击倍率：×{diff.enemyDamageMultiplier}\n" +
                    $"精英出现率：{diff.eliteChance * 100}%\n" +
                    $"Boss阶段数：{diff.bossPhaseCount}\n\n" +
                    $"掉落倍率：×{diff.lootMultiplier}\n" +
                    $"掉落品阶提升：+{diff.lootRarityBonus}\n" +
                    $"金币奖励倍率：×{diff.goldRewardMultiplier}\n\n" +
                    $"出发需要神识：{diff.requiredShenShi}（当前：{currentShenShi}）\n" +
                    $"解锁境界：{realmName}（当前：{((RealmLevel)currentRealm).ToString()}）";
                if (_font) _detailText.font = _font;
            }

            if (_departButton != null)
            {
                if (canEnter && realmUnlocked)
                {
                    _departButton.interactable = true;
                    _departButton.image.color = new Color(0.2f, 0.5f, 0.3f, 1f);
                    if (_departButtonText) _departButtonText.text = "出  发";
                }
                else
                {
                    _departButton.interactable = false;
                    _departButton.image.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
                    if (_departButtonText)
                    {
                        string req = "";
                        if (!realmUnlocked) req += $"需{realmName}境 ";
                        if (!canEnter) req += $"需神识{diff.requiredShenShi}";
                        _departButtonText.text = $"出发({req})";
                    }
                }
            }
        }

        private void OnDepart()
        {
            if (_selectedMap == null || _selectedDifficulty.difficultyName == null) return;

            if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick);

            var arch = CardGameArchitecture.Interface;
            var advModel = arch.GetModel<IAdventureModel>();
            advModel.SelectedMapId = _selectedMap.mapId;
            advModel.SelectedDifficulty = _selectedDifficulty.difficultyType;

            var loadoutModel = arch.GetModel<ILoadoutModel>();
            loadoutModel.MinShenShiRequired = _selectedDifficulty.requiredShenShi;

            var loadoutSystem = arch.GetSystem<ILoadoutSystem>();
            loadoutSystem.StartAdventure();

            // 根据地图regionId切换遭遇表
            var gm = NueGames.NueDeck.Scripts.Managers.GameManager.Instance;
            if (gm != null)
            {
                var encounterData = CardGame.ResourceCache.GetEncounterData(_selectedMap.regionId);
                if (encounterData != null)
                {
                    var encField = typeof(NueGames.NueDeck.Scripts.Managers.GameManager)
                        .GetField("encounterData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (encField != null)
                    {
                        encField.SetValue(gm, encounterData);
                        Debug.Log($"[AdventureMap] 切换遭遇表: regionId={_selectedMap.regionId} ({_selectedMap.mapName})");
                    }
                }
            }

            gameObject.SetActive(false);
            var uiMgr = NueGames.NueDeck.Scripts.Managers.UIManager.Instance;
            if (uiMgr != null && gm != null)
                uiMgr.ChangeScene(gm.SceneData.mapSceneIndex);
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(3);

            FloatingTip.ShowSuccess($"出发！{_selectedMap.mapName} {_selectedDifficulty.difficultyName}");
        }
    }
}
