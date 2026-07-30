using System.Collections.Generic;
using UnityEngine;

namespace CardGame
{
    /// <summary>
    /// 地图生成器 — 生成分支路径树（参照杀戮尖塔）
    /// </summary>
    public static class MapGenerator
    {
        static readonly MapNodeType[] NormalFloorTypes = 
        {
            MapNodeType.Combat, MapNodeType.Combat, MapNodeType.Combat,
            MapNodeType.Event, MapNodeType.Event,
            MapNodeType.Shop, MapNodeType.Campfire, MapNodeType.Treasure
        };

        static readonly MapNodeType[] EliteFloorTypes =
        {
            MapNodeType.Elite, MapNodeType.Elite, MapNodeType.Combat
        };

        /// <summary>
        /// 生成一张完整地图
        /// </summary>
        public static GameMap GenerateMap(int totalFloors = 15, int maxColumns = 4)
        {
            var map = new GameMap { totalFloors = totalFloors, currentFloor = 0 };

            // 起点
            var startNode = new MapNode(0, 0, MapNodeType.Start);
            startNode.isAvailable = true;
            map.nodes.Add(startNode);

            // 每层随机生成节点
            for (int floor = 1; floor < totalFloors; floor++)
            {
                int nodeCount = Random.Range(2, maxColumns + 1);
                var usedColumns = new HashSet<int>();

                for (int i = 0; i < nodeCount; i++)
                {
                    int col = Random.Range(0, maxColumns);
                    while (usedColumns.Contains(col))
                        col = Random.Range(0, maxColumns);
                    usedColumns.Add(col);

                    var type = GetRandomNodeType(floor, totalFloors);
                    var node = new MapNode(floor, col, type);
                    node.isAvailable = false;
                    map.nodes.Add(node);
                }
            }

            // Boss层
            var bossNode = new MapNode(totalFloors, 0, MapNodeType.Boss);
            bossNode.isAvailable = false;
            map.nodes.Add(bossNode);

            // 连接节点
            ConnectNodes(map, maxColumns);

            // 第一层连接起点
            foreach (var node in map.nodes)
            {
                if (node.floor == 1)
                {
                    startNode.connectedNodeIndices.Add(map.nodes.IndexOf(node));
                    node.isAvailable = true;
                }
            }

            map.currentNodeIndex = 0;
            return map;
        }

        static MapNodeType GetRandomNodeType(int floor, int totalFloors)
        {
            // 每5层有概率出精英
            if (floor % 5 == 0 && floor < totalFloors)
            {
                if (Random.value < 0.3f)
                    return MapNodeType.Elite;
            }

            // 每3层可能有商店或篝火
            if (floor % 3 == 0)
            {
                float r = Random.value;
                if (r < 0.3f) return MapNodeType.Shop;
                if (r < 0.6f) return MapNodeType.Campfire;
            }

            // 偶尔出宝箱
            if (floor > 2 && Random.value < 0.08f)
                return MapNodeType.Treasure;

            // 偶尔出事件
            if (Random.value < 0.2f)
                return MapNodeType.Event;

            return NormalFloorTypes[Random.Range(0, NormalFloorTypes.Length)];
        }

        static void ConnectNodes(GameMap map, int maxColumns)
        {
            for (int floor = 1; floor < map.totalFloors; floor++)
            {
                var currentFloorNodes = map.nodes.FindAll(n => n.floor == floor);
                var nextFloorNodes = map.nodes.FindAll(n => n.floor == floor + 1);

                if (nextFloorNodes.Count == 0) continue;

                foreach (var node in currentFloorNodes)
                {
                    // 每个节点连接1-2个下层节点
                    int connections = Random.value < 0.4f ? 2 : 1;

                    // 优先连接列号相近的节点
                    var sorted = nextFloorNodes.ConvertAll(n => n);
                    sorted.Sort((a, b) => Mathf.Abs(a.column - node.column).CompareTo(Mathf.Abs(b.column - node.column)));

                    for (int i = 0; i < Mathf.Min(connections, sorted.Count); i++)
                    {
                        int targetIndex = map.nodes.IndexOf(sorted[i]);
                        if (!node.connectedNodeIndices.Contains(targetIndex))
                            node.connectedNodeIndices.Add(targetIndex);
                    }

                    // 确保至少有一个连接
                    if (node.connectedNodeIndices.Count == 0 && nextFloorNodes.Count > 0)
                    {
                        int targetIndex = map.nodes.IndexOf(nextFloorNodes[0]);
                        node.connectedNodeIndices.Add(targetIndex);
                    }
                }
            }

            // 确保每个下层节点至少有一个上层节点连接
            for (int floor = 2; floor <= map.totalFloors; floor++)
            {
                var floorNodes = map.nodes.FindAll(n => n.floor == floor);
                var prevFloorNodes = map.nodes.FindAll(n => n.floor == floor - 1);

                foreach (var node in floorNodes)
                {
                    int nodeIndex = map.nodes.IndexOf(node);
                    bool hasConnection = false;
                    foreach (var prevNode in prevFloorNodes)
                    {
                        if (prevNode.connectedNodeIndices.Contains(nodeIndex))
                        {
                            hasConnection = true;
                            break;
                        }
                    }

                    if (!hasConnection && prevFloorNodes.Count > 0)
                    {
                        var closest = prevFloorNodes[0];
                        int minDist = Mathf.Abs(closest.column - node.column);
                        foreach (var prev in prevFloorNodes)
                        {
                            int d = Mathf.Abs(prev.column - node.column);
                            if (d < minDist) { minDist = d; closest = prev; }
                        }
                        closest.connectedNodeIndices.Add(nodeIndex);
                    }
                }
            }
        }

        /// <summary>
        /// 访问节点后，更新下一层可用节点
        /// </summary>
        public static void VisitNode(GameMap map, int nodeIndex)
        {
            if (nodeIndex < 0 || nodeIndex >= map.nodes.Count) return;

            var node = map.nodes[nodeIndex];
            node.visited = true;

            // 清除当前层其他可用节点
            foreach (var n in map.nodes)
            {
                if (n.floor == node.floor && n.isAvailable)
                    n.isAvailable = false;
            }

            // 激活连接的下一层节点
            foreach (var connectedIdx in node.connectedNodeIndices)
            {
                map.nodes[connectedIdx].isAvailable = true;
            }

            map.currentNodeIndex = nodeIndex;
            map.currentFloor = node.floor;
        }
    }
}
