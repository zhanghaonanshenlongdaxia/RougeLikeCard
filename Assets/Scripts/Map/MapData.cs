using System;
using System.Collections.Generic;

namespace CardGame
{
    /// <summary>
    /// 地图节点类型（参照杀戮尖塔）
    /// </summary>
    public enum MapNodeType
    {
        Combat,     // 普通战斗
        Elite,      // 精英战斗
        Shop,       // 商店
        Campfire,   // 篝火
        Event,      // 随机事件
        Treasure,   // 宝箱
        Boss,       // Boss
        Start       // 起点
    }

    /// <summary>
    /// 地图节点
    /// </summary>
    [Serializable]
    public class MapNode
    {
        public int floor;          // 第几层
        public int column;          // 列位置
        public MapNodeType nodeType;
        public List<int> connectedNodeIndices = new List<int>(); // 连接到的下层节点索引
        public bool visited;
        public bool isAvailable;

        public MapNode(int floor, int column, MapNodeType type)
        {
            this.floor = floor;
            this.column = column;
            this.nodeType = type;
        }
    }

    /// <summary>
    /// 整张地图
    /// </summary>
    [Serializable]
    public class GameMap
    {
        public List<MapNode> nodes = new List<MapNode>();
        public int totalFloors = 15;
        public int currentFloor = 0;
        public int currentNodeIndex = -1;

        public MapNode CurrentNode => currentNodeIndex >= 0 && currentNodeIndex < nodes.Count ? nodes[currentNodeIndex] : null;
        public List<MapNode> AvailableNodes => nodes.FindAll(n => n.floor == currentFloor && n.isAvailable && !n.visited);
    }
}
