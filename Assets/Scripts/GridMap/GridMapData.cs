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
    /// 多格建筑/大型物件类型（RPGMaker式建筑件）
    /// </summary>
    public enum GridStructureType
    {
        House = 0,      // 房屋（含药铺/民宅，2x3典型）
        Shop = 1,       // 店铺（带招牌）
        Temple = 2,     // 庙宇/祠堂
        BigTree = 3,    // 大树
        Mountain = 4,   // 山体（3x2+）
        Lake = 5,       // 湖泊（3x2+）
        Cave = 6,       // 洞穴
        Fence = 7,      // 围栏段
        Well = 8,       // 水井（1x1）
        Gate = 9        // 门楼
    }

    /// <summary>
    /// 多格建筑件 — 占据 width×height 格，一张立绘跨格渲染
    /// </summary>
    [Serializable]
    public class GridStructure
    {
        public GridStructureType type = GridStructureType.House;

        [Header("锚点（占地区域左下角格坐标）")]
        public Vector2Int anchor;

        [Header("占地尺寸（格）")]
        public int width = 2;
        public int height = 3;

        [Header("立绘名（Resources/GridMapArt/下，如 struct_house1）")]
        public string artName;

        [Header("立绘超出占地的高度（像素，用于屋顶等悬垂视觉，默认=height格高）")]
        public float artHeightCells = 0f;   // 0=与占地同高

        [Header("是否阻挡（footprint格子不可通行）")]
        public bool blocksMovement = true;

        [Header("交互（站在建筑格子或邻格触发；空=纯装饰）")]
        public GridInteractType interactType = GridInteractType.None;
        public string interactTargetId;

        [Header("NPC型建筑：立绘改为NPC人物")]
        public bool isNpc = false;
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

        [Header("多格建筑件（房屋/湖泊/山体等）")]
        public List<GridStructure> structures = new List<GridStructure>();

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

        /// <summary>建筑件是否覆盖指定格（阻挡判定用）</summary>
        public GridStructure GetStructureAt(int x, int y)
        {
            foreach (var s in structures)
            {
                if (!s.blocksMovement) continue;
                if (x >= s.anchor.x && x < s.anchor.x + s.width &&
                    y >= s.anchor.y && y < s.anchor.y + s.height)
                    return s;
            }
            return null;
        }

        /// <summary>应用建筑件：把占地格标记为不可通行+附加交互</summary>
        public void ApplyStructures()
        {
            foreach (var s in structures)
            {
                for (int dx = 0; dx < s.width; dx++)
                {
                    for (int dy = 0; dy < s.height; dy++)
                    {
                        int x = s.anchor.x + dx;
                        int y = s.anchor.y + dy;
                        if (x < 0 || x >= width || y < 0 || y >= height) continue;

                        // 交互型建筑占地格可站立交互；纯阻挡建筑不可通行
                        if (s.interactType != GridInteractType.None)
                        {
                            var cell = GetInteractCell(x, y);
                            if (cell == null)
                            {
                                cell = new GridCell(x, y)
                                {
                                    interactType = s.interactType,
                                    interactTargetId = s.interactTargetId
                                };
                                interactCells.Add(cell);
                            }
                            else
                            {
                                cell.interactType = s.interactType;
                                cell.interactTargetId = s.interactTargetId;
                            }
                        }
                    }
                }
            }
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
