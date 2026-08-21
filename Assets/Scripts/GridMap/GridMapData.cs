using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardGame
{
    /// <summary>
    /// 地表层类型（最底层）
    /// </summary>
    public enum GroundType
    {
        Grass = 0,       // 草地
        Dirt = 1,        // 泥土路
        Stone = 2,       // 石板路
        Sand = 3,        // 沙地
        Wood = 4,        // 木地板
        DarkGrass = 5    // 深色草地
    }

    /// <summary>
    /// 覆盖层类型（地表之上的物体）
    /// </summary>
    public enum OverlayType
    {
        None = 0,        // 无覆盖
        Wall = 1,        // 墙壁（不可通行）
        Water = 2,       // 水域（不可通行）
        Tree = 3,        // 树木（不可通行）
        Rock = 4,        // 岩石（不可通行）
        Fence = 5,       // 栅栏（不可通行）
        Swamp = 6,       // 沼泽（减速但可通行）
        PoisonFog = 7,  // 毒雾（可通行但掉血）
        SpiritEye = 8,   // 灵眼（可通行，灵气浓郁）
        Ruins = 9,       // 废墟装饰（可通行）
        Flower = 10      // 花草装饰（可通行）
    }

    /// <summary>
    /// 格子交互类型
    /// </summary>
    public enum GridInteractType
    {
        None = 0,
        Enemy = 1,
        Npc = 2,
        Gather = 3,
        StoryTrigger = 4,
        Building = 5,
        Exit = 6
    }

    /// <summary>
    /// 兼容旧代码的地形类型映射
    /// </summary>
    public enum GridTerrainType
    {
        Plain = 0, Forest = 1, Mountain = 2, Water = 3,
        Swamp = 4, SpiritEye = 5, Ruins = 6, PoisonFog = 7
    }

    /// <summary>
    /// 单个格子数据
    /// </summary>
    [Serializable]
    public class GridCell
    {
        public int x;
        public int y;
        public GroundType ground = GroundType.Grass;
        public OverlayType overlay = OverlayType.None;
        public GridInteractType interactType = GridInteractType.None;
        public string interactTargetId;
        public bool isWalkable = true;
        public bool isExplored;
        public int moveCost = 1;

        // 兼容旧代码
        public GridTerrainType terrain
        {
            get
            {
                switch (overlay)
                {
                    case OverlayType.Water: return GridTerrainType.Water;
                    case OverlayType.Wall: return GridTerrainType.Mountain;
                    case OverlayType.Rock: return GridTerrainType.Mountain;
                    case OverlayType.Tree: return GridTerrainType.Forest;
                    case OverlayType.Swamp: return GridTerrainType.Swamp;
                    case OverlayType.PoisonFog: return GridTerrainType.PoisonFog;
                    case OverlayType.SpiritEye: return GridTerrainType.SpiritEye;
                    case OverlayType.Ruins: return GridTerrainType.Ruins;
                    default: return GridTerrainType.Plain;
                }
            }
        }

        public GridCell(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }

    /// <summary>
    /// 格子地图配置（ScriptableObject）
    /// 多层：地表层 + 覆盖层（墙壁/水/建筑/树木等）
    /// </summary>
    [CreateAssetMenu(fileName = "GridMapData", menuName = "CardGame/GridMapData")]
    public class GridMapData : ScriptableObject
    {
        [Header("基础信息")]
        public string mapId;
        public string mapName;
        [TextArea] public string description;

        [Header("地图尺寸")]
        public int width = 20;
        public int height = 15;

        [Header("地表层（行优先，y=0是底部）")]
        public GroundType[] groundArray;

        [Header("覆盖层（墙壁/水/树木/建筑等）")]
        public OverlayType[] overlayArray;

        [Header("出生点")]
        public Vector2Int spawnPoint = new Vector2Int(0, 0);

        [Header("撤离点")]
        public List<Vector2Int> exitPoints = new List<Vector2Int>();

        [Header("交互格子")]
        public List<GridCell> interactCells = new List<GridCell>();

        /// <summary>获取地表类型</summary>
        public GroundType GetGround(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height) return GroundType.Stone;
            int idx = y * width + x;
            if (groundArray == null || idx < 0 || idx >= groundArray.Length) return GroundType.Grass;
            return groundArray[idx];
        }

        /// <summary>获取覆盖类型</summary>
        public OverlayType GetOverlay(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height) return OverlayType.Wall;
            int idx = y * width + x;
            if (overlayArray == null || idx < 0 || idx >= overlayArray.Length) return OverlayType.None;
            return overlayArray[idx];
        }

        /// <summary>获取交互格子</summary>
        public GridCell GetInteractCell(int x, int y)
        {
            return interactCells.Find(c => c.x == x && c.y == y);
        }

        /// <summary>是否可通行</summary>
        public bool IsWalkable(int x, int y)
        {
            var overlay = GetOverlay(x, y);
            switch (overlay)
            {
                case OverlayType.Wall:
                case OverlayType.Water:
                case OverlayType.Tree:
                case OverlayType.Rock:
                case OverlayType.Fence:
                    return false;
                default:
                    return true;
            }
        }

        /// <summary>移动消耗</summary>
        public int GetMoveCost(int x, int y)
        {
            return GetOverlay(x, y) == OverlayType.Swamp ? 2 : 1;
        }

        // ====== 兼容旧代码 ======
        public GridTerrainType GetTerrain(int x, int y)
        {
            var overlay = GetOverlay(x, y);
            switch (overlay)
            {
                case OverlayType.Water: return GridTerrainType.Water;
                case OverlayType.Wall: return GridTerrainType.Mountain;
                case OverlayType.Rock: return GridTerrainType.Mountain;
                case OverlayType.Tree: return GridTerrainType.Forest;
                case OverlayType.Swamp: return GridTerrainType.Swamp;
                case OverlayType.PoisonFog: return GridTerrainType.PoisonFog;
                case OverlayType.SpiritEye: return GridTerrainType.SpiritEye;
                case OverlayType.Ruins: return GridTerrainType.Ruins;
                default: return GridTerrainType.Plain;
            }
        }
    }
}
