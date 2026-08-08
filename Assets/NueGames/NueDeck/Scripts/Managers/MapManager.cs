using System.Collections.Generic;
using CardGame;
using NueGames.NueDeck.Scripts.Enums;
using NueGames.NueDeck.Scripts.UI;
using NueGames.NueDeck.Scripts.Utils;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NueUIManager = NueGames.NueDeck.Scripts.Managers.UIManager;

namespace NueGames.NueDeck.Scripts.Managers
{
    public class MapManager : MonoBehaviour, IController
    {
        [Header("References")]
        [SerializeField] private Transform nodeParent;
        [SerializeField] private GameObject nodeButtonPrefab;
        [SerializeField] private Vector2 nodeSpacing = new Vector2(140, 55);

        [Header("Visual")]
        [SerializeField] private Color combatColor = new Color(0.8f, 0.3f, 0.3f);
        [SerializeField] private Color eliteColor = new Color(0.9f, 0.5f, 0.1f);
        [SerializeField] private Color shopColor = new Color(0.3f, 0.7f, 0.9f);
        [SerializeField] private Color campfireColor = new Color(0.9f, 0.6f, 0.2f);
        [SerializeField] private Color eventColor = new Color(0.6f, 0.4f, 0.8f);
        [SerializeField] private Color treasureColor = new Color(0.9f, 0.8f, 0.2f);
        [SerializeField] private Color bossColor = new Color(0.5f, 0.1f, 0.5f);
        [SerializeField] private Color startColor = new Color(0.5f, 0.5f, 0.5f);
        [SerializeField] private Color lockedColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

        public IArchitecture GetArchitecture() => CardGameArchitecture.Interface;

        private GameManager GameManager => GameManager.Instance;
        private NueUIManager UIManager => NueUIManager.Instance;

        private readonly List<GameObject> _spawnedNodes = new List<GameObject>();
        private readonly Dictionary<int, int> _nodeButtonToMapIndex = new Dictionary<int, int>();

        private void Start()
        {
            BuildMap();
        }

        private void BuildMap()
        {
            var mapModel = this.GetModel<IMapModel>();

            // 第一次进入或需要重新生成时生成地图
            if (mapModel.CurrentMap == null || mapModel.CurrentMap.currentFloor == 0 && mapModel.CurrentMap.nodes[0].visited == false && _spawnedNodes.Count == 0)
            {
                // 检查是否是完全新地图（起点未访问且没有已生成节点）
                if (mapModel.CurrentMap == null)
                {
                    mapModel.CurrentMap = MapGenerator.GenerateMap(8, 3);
                    Debug.Log($"[MapManager] Generated new map: {mapModel.CurrentMap.nodes.Count} nodes");
                }
            }

            var map = mapModel.CurrentMap;

            // 清除旧节点
            foreach (var node in _spawnedNodes)
            {
                if (node) Destroy(node);
            }
            _spawnedNodes.Clear();
            _nodeButtonToMapIndex.Clear();

            // 计算每层的最大列数用于居中
            var floorNodeCounts = new Dictionary<int, int>();
            var floorNodeIndices = new Dictionary<int, List<int>>();
            foreach (var node in map.nodes)
            {
                if (!floorNodeCounts.ContainsKey(node.floor))
                {
                    floorNodeCounts[node.floor] = 0;
                    floorNodeIndices[node.floor] = new List<int>();
                }
                floorNodeCounts[node.floor]++;
                floorNodeIndices[node.floor].Add(map.nodes.IndexOf(node));
            }

            // 按列号排序每层节点
            foreach (var kvp in floorNodeIndices)
            {
                kvp.Value.Sort((a, b) =>
                    map.nodes[a].column.CompareTo(map.nodes[b].column));
            }

            // 计算可用区域
            float canvasHeight = 900f; // Canvas可用高度
            float canvasWidth = 1600f; // Canvas可用宽度
            int maxFloors = map.totalFloors + 1;
            float floorHeight = canvasHeight / maxFloors;

            foreach (var floorKvp in floorNodeIndices)
            {
                int floor = floorKvp.Key;
                var nodeIndices = floorKvp.Value;

                // Y 位置：floor 0 在顶部，往下递增
                float y = canvasHeight / 2f - (floor + 0.5f) * floorHeight;

                int nodeCount = nodeIndices.Count;
                float colSpacing = Mathf.Min(nodeSpacing.x, canvasWidth / Mathf.Max(nodeCount, 1));
                float totalWidth = (nodeCount - 1) * colSpacing;
                float startX = -totalWidth / 2f;

                for (int i = 0; i < nodeCount; i++)
                {
                    int mapIndex = nodeIndices[i];
                    var mapNode = map.nodes[mapIndex];

                    float x = startX + i * colSpacing;
                    Vector2 pos = new Vector2(x, y);

                    var nodeObj = Instantiate(nodeButtonPrefab, nodeParent);
                    var nodeRT = nodeObj.GetComponent<RectTransform>();
                    nodeRT.anchoredPosition = pos;
                    _spawnedNodes.Add(nodeObj);

                    int instanceId = nodeObj.GetInstanceID();
                    _nodeButtonToMapIndex[instanceId] = mapIndex;

                    // 设置节点文本和颜色
                    var text = nodeObj.GetComponentInChildren<TextMeshProUGUI>();
                    var button = nodeObj.GetComponent<Button>();
                    var image = nodeObj.GetComponent<Image>();

                    string nodeText = GetNodeDisplayText(mapNode.nodeType);
                    bool isAvailable = mapNode.isAvailable && !mapNode.visited;
                    bool isVisited = mapNode.visited;

                    if (text) text.text = nodeText;

                    // 颜色
                    Color nodeColor = GetNodeColor(mapNode.nodeType);
                    if (isVisited)
                        nodeColor = Color.gray;
                    else if (!isAvailable)
                        nodeColor = lockedColor;

                    if (image) image.color = nodeColor;
                    if (button) button.interactable = isAvailable;

                    // 绑定点击事件
                    int capturedIndex = mapIndex;
                    if (isAvailable && button)
                    {
                        button.onClick.AddListener(() => OnNodeClicked(capturedIndex));
                    }
                }
            }

            Debug.Log($"[MapManager] Map built with {_spawnedNodes.Count} nodes, available: {map.AvailableNodes.Count}");
        }

        private void OnNodeClicked(int mapIndex)
        {
            var mapModel = this.GetModel<IMapModel>();
            var map = mapModel.CurrentMap;
            if (mapIndex < 0 || mapIndex >= map.nodes.Count) return;

            var node = map.nodes[mapIndex];
            if (!node.isAvailable || node.visited) return;

            Debug.Log($"[MapManager] Clicked node: Floor {node.floor}, Type {node.nodeType}");

            // 访问节点
            MapGenerator.VisitNode(map, mapIndex);

            // 根据节点类型进入对应场景或UI
            switch (node.nodeType)
            {
                case MapNodeType.Combat:
                case MapNodeType.Elite:
                case MapNodeType.Boss:
                    EnterCombat();
                    break;
                case MapNodeType.Shop:
                    OpenUI("ShopCanvas");
                    break;
                case MapNodeType.Campfire:
                    OpenUI("CampfireCanvas");
                    break;
                case MapNodeType.Event:
                    OpenEventUI();
                    break;
                case MapNodeType.Treasure:
                    OpenUI("TreasureCanvas");
                    break;
                case MapNodeType.Start:
                    BuildMap();
                    break;
            }
        }

        private void EnterCombat()
        {
            if (GameManager && UIManager)
            {
                UIManager.ChangeScene(GameManager.SceneData.combatSceneIndex);
            }
        }

        /// <summary>
        /// 打开指定名称的 UI Canvas（查找场景中已有的或从 Prefab 实例化）
        /// </summary>
        private void OpenUI(string canvasName)
        {
            Debug.Log($"[MapManager] Open UI: {canvasName}");

            // 先查找场景中是否已有
            var existing = GameObject.Find(canvasName);
            if (existing != null)
            {
                existing.SetActive(true);
                // 宝箱需要初始化奖励
                if (canvasName == "TreasureCanvas")
                {
                    var ctrl = existing.GetComponent<CardGame.UI.TreasureUIController>();
                    if (ctrl != null)
                        ctrl.ShowTreasure("你发现了一个古老的宝箱，里面藏着……");
                }
                return;
            }

            // 从 Prefab 实例化
            GameObject prefab = null;
#if UNITY_EDITOR
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/NueGames/NueDeck/Prefabs/UI/{canvasName}.prefab");
#endif
            if (prefab == null)
                prefab = UnityEngine.Resources.Load<GameObject>($"UI/{canvasName}");

            if (prefab != null)
            {
                var instance = Instantiate(prefab);
                instance.name = canvasName;
                Debug.Log($"[MapManager] Instantiated {canvasName}");
                // 宝箱需要初始化奖励
                if (canvasName == "TreasureCanvas")
                {
                    var ctrl = instance.GetComponent<CardGame.UI.TreasureUIController>();
                    if (ctrl != null)
                        ctrl.ShowTreasure("你发现了一个古老的宝箱，里面藏着……");
                }
            }
            else
            {
                Debug.LogWarning($"[MapManager] UI prefab not found: {canvasName}");
                BuildMap();
            }
        }

        /// <summary>
        /// 打开事件UI，随机获取一个事件
        /// </summary>
        private void OpenEventUI()
        {
            Debug.Log("[MapManager] Open Event UI");
            var eventSystem = this.GetSystem<CardGame.IEventSystem>();
            var eventData = eventSystem.GetRandomEvent();
            if (eventData == null)
            {
                Debug.LogWarning("[MapManager] No event data found, refreshing map");
                BuildMap();
                return;
            }

            // 查找或创建 EventCanvas
            var existing = GameObject.Find("EventCanvas");
            if (existing != null)
            {
                var controller = existing.GetComponent<CardGame.UI.EventUIController>();
                if (controller != null)
                {
                    controller.ShowEvent(eventData);
                    existing.SetActive(true);
                    return;
                }
            }

            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/NueGames/NueDeck/Prefabs/UI/EventCanvas.prefab");
            if (prefab != null)
            {
                var instance = Instantiate(prefab);
                instance.name = "EventCanvas";
                var controller = instance.GetComponent<CardGame.UI.EventUIController>();
                if (controller != null)
                {
                    controller.ShowEvent(eventData);
                    Debug.Log($"[MapManager] Event UI shown: {eventData.name}");
                }
            }
            else
            {
                BuildMap();
            }
        }

        private string GetNodeDisplayText(MapNodeType type)
        {
            switch (type)
            {
                case MapNodeType.Combat: return "战斗";
                case MapNodeType.Elite: return "精英";
                case MapNodeType.Shop: return "商店";
                case MapNodeType.Campfire: return "篝火";
                case MapNodeType.Event: return "事件";
                case MapNodeType.Treasure: return "宝箱";
                case MapNodeType.Boss: return "Boss";
                case MapNodeType.Start: return "起点";
                default: return "?";
            }
        }

        private Color GetNodeColor(MapNodeType type)
        {
            switch (type)
            {
                case MapNodeType.Combat: return combatColor;
                case MapNodeType.Elite: return eliteColor;
                case MapNodeType.Shop: return shopColor;
                case MapNodeType.Campfire: return campfireColor;
                case MapNodeType.Event: return eventColor;
                case MapNodeType.Treasure: return treasureColor;
                case MapNodeType.Boss: return bossColor;
                case MapNodeType.Start: return startColor;
                default: return Color.white;
            }
        }
    }
}
