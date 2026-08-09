using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using CardGame.Audio;

namespace CardGame.UI
{
    /// <summary>
    /// 大地图选点面板。点击"下山历练"后弹出，选择地图+难度+确认出发。
    /// 运行时创建，无prefab依赖。
    /// </summary>
    public class AdventureMapUIController : MonoBehaviour
    {
        private TMP_FontAsset _font;
        private AdventureMapConfig _config;
        private Transform _mapListRoot;
        private Transform _detailRoot;
        private TextMeshProUGUI _detailText;
        private Button _departButton;
        private Button _backButton;
        private AdventureMapData _selectedMap;
        private AdventureDifficulty _selectedDifficulty;

        public IArchitecture GetArchitecture() => CardGameArchitecture.Interface;

        private void Awake()
        {
            _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            LoadConfig();
            BuildUI();
        }

        void LoadConfig()
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

        void BuildUI()
        {
            var canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 70;
                var scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                gameObject.AddComponent<GraphicRaycaster>();
            }

            // 背景
            var bg = new GameObject("BG");
            bg.transform.SetParent(transform, false);
            var bgRt = bg.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
            bg.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.1f, 0.95f);

            // 主面板
            var panel = new GameObject("Panel");
            panel.transform.SetParent(transform, false);
            var pRt = panel.AddComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0.05f, 0.05f); pRt.anchorMax = new Vector2(0.95f, 0.95f);
            pRt.offsetMin = Vector2.zero; pRt.offsetMax = Vector2.zero;
            panel.AddComponent<Image>().color = new Color(0.08f, 0.1f, 0.15f, 0.98f);

            var hlg = panel.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20; hlg.padding = new RectOffset(20, 20, 20, 20);
            hlg.childControlWidth = true; hlg.childForceExpandWidth = true;

            // === 左侧：地图列表 ===
            var leftPanel = new GameObject("MapList");
            leftPanel.transform.SetParent(panel.transform, false);
            leftPanel.AddComponent<RectTransform>();
            var leftLe = leftPanel.AddComponent<LayoutElement>();
            leftLe.preferredWidth = 500;
            var leftVLG = leftPanel.AddComponent<VerticalLayoutGroup>();
            leftVLG.spacing = 10; leftVLG.padding = new RectOffset(10, 10, 10, 10);
            leftVLG.childControlWidth = true; leftVLG.childForceExpandHeight = true;

            // 标题
            var title = CreateText(leftPanel.transform, "选择历练地点", 26, new Color(0.9f, 0.8f, 0.3f));
            title.AddComponent<LayoutElement>().preferredHeight = 40;

            // 滚动区域
            var scrollObj = new GameObject("Scroll");
            scrollObj.transform.SetParent(leftPanel.transform, false);
            var scrollRt = scrollObj.AddComponent<RectTransform>();
            var scrollLe = scrollObj.AddComponent<LayoutElement>();
            scrollLe.flexibleHeight = 1;
            var scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.vertical = true;

            var vp = new GameObject("Viewport");
            vp.transform.SetParent(scrollObj.transform, false);
            var vpRt = vp.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero; vpRt.offsetMax = Vector2.zero;
            vp.AddComponent<Image>().color = new Color(0.05f, 0.08f, 0.12f, 1f);
            vp.AddComponent<RectMask2D>();
            scroll.viewport = vpRt;

            var content = new GameObject("Content");
            content.transform.SetParent(vp.transform, false);
            var cRt = content.AddComponent<RectTransform>();
            cRt.anchorMin = Vector2.zero; cRt.anchorMax = new Vector2(1, 1);
            cRt.pivot = new Vector2(0.5f, 1f);
            cRt.offsetMin = Vector2.zero; cRt.offsetMax = Vector2.zero;
            var cVLG = content.AddComponent<VerticalLayoutGroup>();
            cVLG.spacing = 8; cVLG.padding = new RectOffset(5, 5, 5, 5);
            cVLG.childControlWidth = true; cVLG.childForceExpandHeight = false;
            var csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = cRt;
            _mapListRoot = content.transform;

            // === 右侧：详情面板 ===
            var rightPanel = new GameObject("DetailPanel");
            rightPanel.transform.SetParent(panel.transform, false);
            rightPanel.AddComponent<RectTransform>();
            var rightLe = rightPanel.AddComponent<LayoutElement>();
            rightLe.flexibleWidth = 1;
            var rightVLG = rightPanel.AddComponent<VerticalLayoutGroup>();
            rightVLG.spacing = 15; rightVLG.padding = new RectOffset(15, 15, 15, 15);
            rightVLG.childControlWidth = true; rightVLG.childAlignment = TextAnchor.UpperCenter;
            _detailRoot = rightPanel.transform;

            // 详情标题
            var detailTitle = CreateText(rightPanel.transform, "地图详情", 24, new Color(0.9f, 0.8f, 0.3f));
            detailTitle.AddComponent<LayoutElement>().preferredHeight = 35;

            // 详情内容
            _detailText = CreateText(rightPanel.transform, "请选择左侧的历练地点", 18, Color.white).GetComponent<TextMeshProUGUI>();
            _detailText.alignment = TextAlignmentOptions.Left;
            var detailLe = _detailText.gameObject.AddComponent<LayoutElement>();
            detailLe.flexibleHeight = 1;

            // 出发按钮
            _departButton = CreateButton(rightPanel.transform, "出  发", new Color(0.2f, 0.5f, 0.3f, 1f));
            _departButton.interactable = false;
            _departButton.onClick.AddListener(OnDepart);

            // 返回按钮
            _backButton = CreateButton(rightPanel.transform, "返  回", new Color(0.3f, 0.2f, 0.1f, 1f));
            _backButton.onClick.AddListener(() =>
            {
                if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick);
                gameObject.SetActive(false);
            });

            PopulateMapList();
        }

        void PopulateMapList()
        {
            if (_config == null) return;

            var arch = CardGameArchitecture.Interface;
            var realmModel = arch.GetModel<IRealmModel>();
            var loadoutModel = arch.GetModel<ILoadoutModel>();
            int currentRealm = realmModel.CurrentRealm.Value;
            int currentShenShi = loadoutModel.CurrentShenShi.Value;

            foreach (var mapData in _config.maps)
            {
                bool mapUnlocked = currentRealm >= mapData.unlockRealmLevel;

                var mapItem = new GameObject($"Map_{mapData.mapId}");
                mapItem.transform.SetParent(_mapListRoot, false);
                var img = mapItem.AddComponent<Image>();
                img.color = mapUnlocked ? new Color(0.1f, 0.2f, 0.3f, 1f) : new Color(0.1f, 0.1f, 0.1f, 0.5f);
                var le = mapItem.AddComponent<LayoutElement>();
                le.preferredHeight = 100;
                var vlg = mapItem.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = 3; vlg.padding = new RectOffset(10, 10, 8, 8);
                vlg.childControlWidth = true; vlg.childForceExpandHeight = false;

                // 地图名
                var nameText = CreateText(mapItem.transform,
                    mapUnlocked ? mapData.mapName : $"{mapData.mapName} (未解锁)",
                    20, mapUnlocked ? new Color(0.9f, 0.8f, 0.3f) : new Color(0.4f, 0.4f, 0.4f));
                nameText.AddComponent<LayoutElement>().preferredHeight = 25;

                // 描述
                var descText = CreateText(mapItem.transform, mapData.description, 14, new Color(0.7f, 0.7f, 0.7f));
                descText.AddComponent<LayoutElement>().preferredHeight = 30;

                // 难度按钮行
                var diffRow = new GameObject("DiffRow");
                diffRow.transform.SetParent(mapItem.transform, false);
                diffRow.AddComponent<RectTransform>();
                var diffHLG = diffRow.AddComponent<HorizontalLayoutGroup>();
                diffHLG.spacing = 5; diffHLG.childControlWidth = true; diffHLG.childForceExpandWidth = true;
                diffRow.AddComponent<LayoutElement>().preferredHeight = 35;

                if (mapUnlocked)
                {
                    foreach (var diff in mapData.difficulties)
                    {
                        // 所有难度都可点击查看信息，但颜色区分是否满足要求
                        bool canEnter = currentShenShi >= diff.requiredShenShi;
                        var diffBtn = CreateButton(diffRow.transform, diff.difficultyName,
                            canEnter ? GetDifficultyColor(diff.difficultyType) : new Color(0.15f, 0.15f, 0.15f, 0.8f));
                        diffBtn.interactable = true; // 始终可点击查看

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
                else
                {
                    // 未解锁的地图也显示难度按钮（灰色但可点击查看详情）
                    foreach (var diff in mapData.difficulties)
                    {
                        var diffBtn = CreateButton(diffRow.transform, diff.difficultyName,
                            new Color(0.15f, 0.15f, 0.15f, 0.8f));
                        diffBtn.interactable = true; // 可点击查看详情

                        var capturedMap = mapData;
                        var capturedDiff = diff;
                        diffBtn.onClick.AddListener(() =>
                        {
                            if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick);
                            OnDifficultySelected(capturedMap, capturedDiff, false);
                        });
                    }
                }
            }
        }

        Color GetDifficultyColor(DifficultyType type)
        {
            return type switch
            {
                DifficultyType.Normal => new Color(0.15f, 0.3f, 0.5f, 1f),
                DifficultyType.Hard => new Color(0.4f, 0.2f, 0.1f, 1f),
                DifficultyType.Extreme => new Color(0.3f, 0.1f, 0.3f, 1f),
                DifficultyType.Hell => new Color(0.5f, 0.1f, 0.1f, 1f),
                _ => Color.gray
            };
        }

        void OnDifficultySelected(AdventureMapData map, AdventureDifficulty diff, bool canEnter)
        {
            _selectedMap = canEnter ? map : null;
            _selectedDifficulty = diff;

            int currentShenShi = CardGameArchitecture.Interface.GetModel<ILoadoutModel>().CurrentShenShi.Value;
            int currentRealm = CardGameArchitecture.Interface.GetModel<IRealmModel>().CurrentRealm.Value;
            bool realmUnlocked = currentRealm >= map.unlockRealmLevel;
            string realmName = ((RealmLevel)map.unlockRealmLevel).ToString();

            // 更新详情
            _detailText.text = $"{map.mapName} — {diff.difficultyName}\n\n{map.description}\n\n" +
                $"难度：{diff.difficultyName}\n" +
                $"{diff.description}\n\n" +
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

            // 出发按钮：满足条件绿色可点，不满足灰色显示要求
            if (canEnter && realmUnlocked)
            {
                _departButton.interactable = true;
                _departButton.GetComponent<Image>().color = new Color(0.2f, 0.5f, 0.3f, 1f);
                var departTmp = _departButton.GetComponentInChildren<TextMeshProUGUI>();
                if (departTmp) departTmp.text = "出  发";
            }
            else
            {
                _departButton.interactable = false;
                _departButton.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
                var departTmp = _departButton.GetComponentInChildren<TextMeshProUGUI>();
                if (departTmp)
                {
                    string req = "";
                    if (!realmUnlocked) req += $"需{realmName}境 ";
                    if (!canEnter) req += $"需神识{diff.requiredShenShi}";
                    departTmp.text = $"出发({req})";
                }
            }
        }

        void OnDepart()
        {
            if (_selectedMap == null || _selectedDifficulty.difficultyName == null) return;

            if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick);

            // 保存选择到Model
            var arch = CardGameArchitecture.Interface;
            var advModel = arch.GetModel<IAdventureModel>();
            advModel.SelectedMapId = _selectedMap.mapId;
            advModel.SelectedDifficulty = _selectedDifficulty.difficultyType;

            // 设置编队最低神识要求
            var loadoutModel = arch.GetModel<ILoadoutModel>();
            loadoutModel.MinShenShiRequired = _selectedDifficulty.requiredShenShi;

            // 验证编队
            var loadoutSystem = arch.GetSystem<ILoadoutSystem>();
            loadoutSystem.StartAdventure();

            // 切到地图场景
            gameObject.SetActive(false);
            var gm = NueGames.NueDeck.Scripts.Managers.GameManager.Instance;
            var uiMgr = NueGames.NueDeck.Scripts.Managers.UIManager.Instance;
            if (uiMgr != null && gm != null)
                uiMgr.ChangeScene(gm.SceneData.mapSceneIndex);
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(3);

            FloatingTip.ShowSuccess($"出发！{_selectedMap.mapName} {_selectedDifficulty.difficultyName}");
        }

        // === 工具方法 ===
        GameObject CreateText(Transform parent, string text, float size, Color color)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = size; tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            if (_font) tmp.font = _font;
            return go;
        }

        Button CreateButton(Transform parent, string label, Color color)
        {
            var go = new GameObject($"Btn_{label}");
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = color;
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 45;
            var txtObj = new GameObject("Text");
            txtObj.transform.SetParent(go.transform, false);
            var txtRt = txtObj.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero; txtRt.offsetMax = Vector2.zero;
            var tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label; tmp.fontSize = 22; tmp.color = new Color(0.95f, 0.85f, 0.4f);
            tmp.alignment = TextAlignmentOptions.Center;
            if (_font) tmp.font = _font;
            return go.AddComponent<Button>();
        }
    }
}
