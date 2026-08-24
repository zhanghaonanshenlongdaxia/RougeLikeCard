using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using QFramework;
using CardGame.UI;

namespace CardGame
{
    /// <summary>
    /// 世界地图UI控制器 — 觅长生式大世界地图
    /// 大背景图 + 地点节点 + 路径线 + 玩家token
    /// 点击地点→Dijkstra寻路→逐段移动动画→每段消耗游戏时间
    /// 到达地点→点击进入格子地图
    /// </summary>
    public class WorldMapUIController : MonoBehaviour, IController
    {
        [Header("引用")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private RectTransform nodesRoot;      // 节点容器（含路径线）
        [SerializeField] private RectTransform pathsRoot;      // 路径线容器
        [SerializeField] private TextMeshProUGUI timeText;     // 时间HUD
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private GameObject nodeTemplate;      // 地点节点模板
        [SerializeField] private Image playerToken;            // 玩家标记

        [Header("节点样式")]
        [SerializeField] private float nodeSize = 64f;

        IGameTimeModel _timeModel;
        IWorldMapModel _mapModel;
        WorldMapConfig _config;

        readonly Dictionary<string, RectTransform> _nodeMap = new Dictionary<string, RectTransform>();
        readonly List<GameObject> _pathLines = new List<GameObject>();
        Coroutine _travelRoutine;
        bool _isTraveling;

        // 节点类型颜色
        static readonly Color CityC = new Color(0.92f, 0.85f, 0.55f);
        static readonly Color SectC = new Color(0.55f, 0.75f, 0.95f);
        static readonly Color MountainC = new Color(0.65f, 0.72f, 0.60f);
        static readonly Color CaveC = new Color(0.60f, 0.55f, 0.70f);
        static readonly Color SecretC = new Color(0.95f, 0.65f, 0.30f);
        static readonly Color HomeC = new Color(0.80f, 0.60f, 0.90f);
        static readonly Color VillageC = new Color(0.85f, 0.70f, 0.55f);
        static readonly Color DojoC = new Color(0.75f, 0.75f, 0.75f);

        static readonly Dictionary<WorldLocationType, string> TypeLabels = new Dictionary<WorldLocationType, string>
        {
            { WorldLocationType.City, "城" },
            { WorldLocationType.Sect, "宗" },
            { WorldLocationType.Mountain, "山" },
            { WorldLocationType.Cave, "穴" },
            { WorldLocationType.SecretRealm, "境" },
            { WorldLocationType.PlayerHome, "府" },
            { WorldLocationType.Village, "集" },
            { WorldLocationType.Dojo, "祠" },
        };

        void Awake()
        {
            AutoBindReferences();
            _timeModel = this.GetModel<IGameTimeModel>();
            _mapModel = this.GetModel<IWorldMapModel>();
        }

        void AutoBindReferences()
        {
            if (backgroundImage == null) { var t = transform.Find("BG"); if (t != null) backgroundImage = t.GetComponent<Image>(); }
            if (nodesRoot == null) { var t = transform.Find("MapArea/NodesRoot"); if (t != null) nodesRoot = t as RectTransform; }
            if (pathsRoot == null) { var t = transform.Find("MapArea/PathsRoot"); if (t != null) pathsRoot = t as RectTransform; }
            if (timeText == null) { var t = transform.Find("TimeHUD/TimeText"); if (t != null) timeText = t.GetComponent<TextMeshProUGUI>(); }
            if (titleText == null) { var t = transform.Find("TimeHUD/TitleText"); if (t != null) titleText = t.GetComponent<TextMeshProUGUI>(); }
            if (playerToken == null) { var t = transform.Find("MapArea/PlayerToken"); if (t != null) playerToken = t.GetComponent<Image>(); }
        }

        void Start()
        {
            _config = this.GetSystem<IWorldMapSystem>().GetConfig();
            if (_config == null)
            {
                Debug.LogError("[WorldMap] 配置缺失");
                return;
            }

            if (titleText != null) titleText.text = _config.worldName;
            LoadBackground();
            BuildNodes();
            BuildPaths();
            RefreshTimeHUD();
            RefreshPlayerToken();

            // 时间变化刷新HUD
            _timeModel.Year.Register(OnTimeChanged);
            _timeModel.Month.Register(OnTimeChanged);
            _timeModel.Day.Register(OnTimeChanged);
            _timeModel.Shichen.Register(OnTimeChanged);
        }

        void OnDestroy()
        {
            if (_timeModel != null)
            {
                _timeModel.Year.UnRegister(OnTimeChanged);
                _timeModel.Month.UnRegister(OnTimeChanged);
                _timeModel.Day.UnRegister(OnTimeChanged);
                _timeModel.Shichen.UnRegister(OnTimeChanged);
            }
        }

        void OnTimeChanged(int _) => RefreshTimeHUD();

        void LoadBackground()
        {
            if (backgroundImage == null) return;
            var bg = Resources.Load<Sprite>("WorldMap/worldmap_bg");
            if (bg != null)
            {
                backgroundImage.sprite = bg;
                backgroundImage.color = Color.white;
                backgroundImage.preserveAspect = false; // 铺满
            }
            else
            {
                backgroundImage.color = new Color(0.16f, 0.22f, 0.20f);
            }
        }

        void RefreshTimeHUD()
        {
            if (timeText == null) return;
            timeText.text = this.GetSystem<IGameTimeSystem>().GetDisplayString();
        }

        // ===================== 构建节点与路径 =====================

        void BuildNodes()
        {
            // 清理旧节点（模板除外）
            foreach (var kv in _nodeMap)
                if (kv.Value != null) Destroy(kv.Value.gameObject);
            _nodeMap.Clear();

            if (nodeTemplate != null) nodeTemplate.SetActive(false);

            foreach (var loc in _config.locations)
            {
                var go = nodeTemplate != null
                    ? Instantiate(nodeTemplate, nodesRoot)
                    : new GameObject($"Node_{loc.locationId}", typeof(RectTransform), typeof(Image), typeof(Button));
                go.name = $"Node_{loc.locationName}";
                go.SetActive(true);
                var rt = go.transform as RectTransform;

                // 归一化位置 → 实际UI位置
                var area = nodesRoot.rect;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(
                    (loc.uiPosition.x - 0.5f) * area.width,
                    (loc.uiPosition.y - 0.5f) * area.height);
                rt.sizeDelta = new Vector2(nodeSize, nodeSize);

                bool unlocked = IsUnlocked(loc);
                var img = go.GetComponent<Image>();
                img.color = unlocked ? GetTypeColor(loc.locationType) : new Color(0.3f, 0.3f, 0.3f, 0.7f);
                img.raycastTarget = true;

                // 名称标签
                var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelGo.transform.SetParent(go.transform, false);
                var labelRt = labelGo.transform as RectTransform;
                labelRt.anchorMin = labelRt.anchorMax = new Vector2(0.5f, 0f);
                labelRt.anchoredPosition = new Vector2(0, -nodeSize * 0.55f);
                labelRt.sizeDelta = new Vector2(120, 24);
                var label = labelGo.GetComponent<TextMeshProUGUI>();
                label.text = unlocked ? loc.locationName : "？？？";
                label.fontSize = 22;
                label.alignment = TextAlignmentOptions.Center;
                label.color = unlocked ? Color.white : new Color(0.6f, 0.6f, 0.6f);

                // 类型字
                var typeGo = new GameObject("TypeChar", typeof(RectTransform), typeof(TextMeshProUGUI));
                typeGo.transform.SetParent(go.transform, false);
                var typeRt = typeGo.transform as RectTransform;
                typeRt.anchorMin = typeRt.anchorMax = new Vector2(0.5f, 0.5f);
                typeRt.sizeDelta = new Vector2(nodeSize, nodeSize);
                var typeLabel = typeGo.GetComponent<TextMeshProUGUI>();
                TypeLabels.TryGetValue(loc.locationType, out var typeChar);
                typeLabel.text = unlocked ? (typeChar ?? "?") : "?";
                typeLabel.fontSize = 30;
                typeLabel.alignment = TextAlignmentOptions.Center;
                typeLabel.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);

                // 点击
                var btn = go.GetComponent<Button>();
                var locId = loc.locationId;
                btn.onClick.AddListener(() => OnNodeClicked(locId));

                _nodeMap[loc.locationId] = rt;
            }
        }

        void BuildPaths()
        {
            foreach (var line in _pathLines)
                if (line != null) Destroy(line);
            _pathLines.Clear();

            var canFly = this.GetSystem<IWorldMapSystem>().CanFlyNow();

            foreach (var path in _config.paths)
            {
                if (!_nodeMap.TryGetValue(path.fromId, out var fromRt)) continue;
                if (!_nodeMap.TryGetValue(path.toId, out var toRt)) continue;

                var lineGo = new GameObject($"Path_{path.fromId}_{path.toId}", typeof(RectTransform), typeof(Image));
                lineGo.transform.SetParent(pathsRoot, false);
                var lineRt = lineGo.transform as RectTransform;

                var from = fromRt.anchoredPosition;
                var to = toRt.anchoredPosition;
                var dir = to - from;
                float len = dir.magnitude;

                lineRt.anchorMin = lineRt.anchorMax = new Vector2(0.5f, 0.5f);
                lineRt.sizeDelta = new Vector2(len, 4f);
                lineRt.anchoredPosition = (from + to) * 0.5f;
                lineRt.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

                var lineImg = lineGo.GetComponent<Image>();
                // flyOnly且不能飞 → 虚弱虚线效果（半透明）；正常路径金色
                bool blocked = path.flyOnly && !canFly;
                lineImg.color = blocked
                    ? new Color(0.7f, 0.7f, 0.75f, 0.25f)
                    : new Color(0.90f, 0.78f, 0.45f, 0.65f);

                _pathLines.Add(lineGo);
            }
        }

        bool IsUnlocked(WorldLocation loc)
        {
            var sys = this.GetSystem<IWorldMapSystem>();
            return sys.IsLocationUnlocked(loc.locationId) ||
                   this.GetModel<IRealmModel>().CurrentRealm.Value >= (int)loc.unlockRealm;
        }

        Color GetTypeColor(WorldLocationType type)
        {
            switch (type)
            {
                case WorldLocationType.City: return CityC;
                case WorldLocationType.Sect: return SectC;
                case WorldLocationType.Mountain: return MountainC;
                case WorldLocationType.Cave: return CaveC;
                case WorldLocationType.SecretRealm: return SecretC;
                case WorldLocationType.PlayerHome: return HomeC;
                case WorldLocationType.Village: return VillageC;
                case WorldLocationType.Dojo: return DojoC;
                default: return Color.gray;
            }
        }

        // ===================== 交互 =====================

        void OnNodeClicked(string locationId)
        {
            if (_isTraveling) return;

            var loc = _config.GetLocation(locationId);
            if (loc == null) return;

            // 已在此地 → 进入
            if (_mapModel.CurrentLocationId.Value == locationId)
            {
                EnterLocation(loc);
                return;
            }

            // 未解锁 → 提示
            if (!IsUnlocked(loc))
            {
                FloatingTip.ShowWarning("尚未探明此地，境界不足或未曾听闻");
                return;
            }

            // 寻路旅行
            var canFly = this.GetSystem<IWorldMapSystem>().CanFlyNow();
            var route = this.GetSystem<IWorldMapSystem>().FindRoute(locationId, canFly);
            if (route == null || route.Count == 0)
            {
                FloatingTip.ShowWarning("没有可行的路径");
                return;
            }

            int totalDays = 0;
            foreach (var step in route) totalDays += step.days;

            if (_travelRoutine != null) StopCoroutine(_travelRoutine);
            _travelRoutine = StartCoroutine(TravelRoutine(route, totalDays));
        }

        /// <summary>逐段移动动画 + 消耗时间</summary>
        IEnumerator TravelRoutine(List<TravelStep> route, int totalDays)
        {
            _isTraveling = true;
            _mapModel.IsTraveling.Value = true;
            FloatingTip.ShowSuccess($"启程！旅途约{totalDays}天");

            var timeSys = this.GetSystem<IGameTimeSystem>();

            foreach (var step in route)
            {
                if (!_nodeMap.TryGetValue(step.fromId, out var fromRt) ||
                    !_nodeMap.TryGetValue(step.toId, out var toRt))
                    continue;

                // token移动动画（0.8秒/段）
                float t = 0;
                Vector2 from = fromRt.anchoredPosition;
                Vector2 to = toRt.anchoredPosition;
                while (t < 1f)
                {
                    t += Time.deltaTime / 0.8f;
                    if (playerToken != null)
                        playerToken.rectTransform.anchoredPosition = Vector2.Lerp(from, to, Mathf.Clamp01(t));
                    yield return null;
                }

                // 消耗时间（半天粒度：每段按days天推进）
                timeSys.AdvanceDays(step.days);

                // 途经地点解锁（能路过就代表知道这地方）
                this.GetSystem<IWorldMapSystem>().UnlockLocation(step.toId);
            }

            // 到达
            var finalStep = route[route.Count - 1];
            this.GetSystem<IWorldMapSystem>().ArriveAt(finalStep.toId);
            _isTraveling = false;
            _mapModel.IsTraveling.Value = false;

            FloatingTip.ShowSuccess($"抵达{ _config.GetLocation(finalStep.toId)?.locationName }");
            BuildPaths(); // 刷新路径显示（可能解锁了新路径）
        }

        /// <summary>进入当前地点（格子地图/秘境爬塔/占位）</summary>
        void EnterLocation(WorldLocation loc)
        {
            // 秘境类型 → 爬塔流程
            if (loc.locationType == WorldLocationType.SecretRealm)
            {
                var realmSys = this.GetSystem<ISecretRealmSystem>();
                if (!realmSys.IsRealmOpen(loc.locationId))
                {
                    var config = realmSys.GetConfig(loc.locationId);
                    FloatingTip.ShowWarning(config != null && this.GetSystem<ISecretRealmSystem>().GetConfig(loc.locationId) != null && _HasCleared(loc.locationId)
                        ? "此秘境已被探索殆尽"
                        : "秘境尚未开启");
                    return;
                }
                if (realmSys.EnterRealm(loc.locationId))
                {
                    FloatingTip.ShowSuccess($"踏入{loc.locationName}");
                    this.GetSystem<IGameTimeSystem>().AdvanceShichen(6);
                    GridMapSceneLoader.PendingGridMapId = realmSys.GetCurrentFloorMapId();
                    UnityEngine.SceneManagement.SceneManager.LoadScene("5- GridMap");
                }
                return;
            }

            if (string.IsNullOrEmpty(loc.gridMapId))
            {
                FloatingTip.Show($"{loc.locationName}：{loc.description}");
                return;
            }

            // 记录当前地点的格子地图，跳转GridMap场景
            GridMapSceneLoader.PendingGridMapId = loc.gridMapId;
            FloatingTip.ShowSuccess($"进入{loc.locationName}");
            // 进图消耗半天
            this.GetSystem<IGameTimeSystem>().AdvanceShichen(6);
            UnityEngine.SceneManagement.SceneManager.LoadScene("5- GridMap");
        }

        bool _HasCleared(string realmId)
        {
            var model = this.GetModel<ISecretRealmModel>();
            return model.ClearedRealmIds.Contains(realmId);
        }

        void RefreshPlayerToken()
        {
            if (playerToken == null) return;
            var sprite = Resources.Load<Sprite>("GridMapArt/player_token");
            if (sprite != null) playerToken.sprite = sprite;

            if (_nodeMap.TryGetValue(_mapModel.CurrentLocationId.Value, out var rt))
                playerToken.rectTransform.anchoredPosition = rt.anchoredPosition;
        }

        public IArchitecture GetArchitecture() => CardGameArchitecture.Interface;
    }
}
