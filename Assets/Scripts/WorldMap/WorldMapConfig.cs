using System;
using System.Collections.Generic;
using UnityEngine;
using NueGames.NueDeck.Scripts.Enums;

namespace CardGame
{
    /// <summary>
    /// 世界地点类型
    /// </summary>
    public enum WorldLocationType
    {
        City = 0,        // 城镇（凡人聚居）
        Sect = 1,        // 修仙门派
        Mountain = 2,    // 山岳（野外）
        Cave = 3,        // 洞穴/秘境入口
        SecretRealm = 4, // 秘境（爬塔，定期开放）
        PlayerHome = 5,  // 玩家洞府
        Village = 6,     // 村落集市（如半坡集）
        Dojo = 7         // 特殊地点（土地庙等）
    }

    /// <summary>
    /// 世界地图上的一个地点
    /// </summary>
    [Serializable]
    public class WorldLocation
    {
        public string locationId;
        public string locationName;
        [TextArea] public string description;
        public WorldLocationType locationType = WorldLocationType.City;

        [Header("地图布局位置（0-1归一化，相对世界地图Canvas）")]
        public Vector2 uiPosition = new Vector2(0.5f, 0.5f);

        [Header("解锁条件（不满足则显示为迷雾）")]
        public RealmLevel unlockRealm = RealmLevel.LianQi;

        [Header("关联格子地图（为空则只有对话/占位）")]
        public string gridMapId;

        [Header("该地点可见（未解锁地点也显示名字为???或隐藏）")]
        public bool hiddenUntilUnlocked = false;
    }

    /// <summary>
    /// 地点间路径
    /// </summary>
    [Serializable]
    public class WorldPath
    {
        public string fromId;
        public string toId;

        [Header("步行耗时（天）")]
        public int walkDays = 1;

        [Header("御剑飞行耗时（天）；为0表示此路径不可飞（如穿山小路）")]
        public int flyDays = 1;

        [Header("飞行专用路径（步行不可走，如跨海/高山，必须御剑）")]
        public bool flyOnly = false;
    }

    /// <summary>
    /// 世界地图配置（ScriptableObject）
    /// </summary>
    [CreateAssetMenu(fileName = "WorldMapConfig", menuName = "CardGame/WorldMapConfig")]
    public class WorldMapConfig : ScriptableObject
    {
        [Header("地图名")]
        public string worldName = "九州修仙界";

        [Header("玩家初始地点")]
        public string startLocationId = "qingshi_town";

        [Header("地点列表")]
        public List<WorldLocation> locations = new List<WorldLocation>();

        [Header("路径列表")]
        public List<WorldPath> paths = new List<WorldPath>();

        public WorldLocation GetLocation(string id)
        {
            return locations.Find(l => l.locationId == id);
        }

        public WorldPath GetPath(string fromId, string toId)
        {
            return paths.Find(p =>
                (p.fromId == fromId && p.toId == toId) ||
                (p.fromId == toId && p.toId == fromId));
        }

        /// <summary>某地点的所有邻接路径</summary>
        public List<WorldPath> GetPathsFrom(string locationId)
        {
            return paths.FindAll(p => p.fromId == locationId || p.toId == locationId);
        }
    }
}
