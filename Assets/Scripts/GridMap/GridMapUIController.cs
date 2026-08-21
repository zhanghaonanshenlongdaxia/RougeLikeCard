using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CardGame.UI;

namespace CardGame
{
    /// <summary>
    /// 格子地图UI控制器 — 背包式格子地图
    /// 整张地图 = GridMapCanvas预制体；每个格子 = GridCellItem实例（克隆自模板）
    /// 点击可达格子BFS寻路移动，进入格子触发交互事件
    /// 大图作为Canvas背景（Resources/GridMapArt/{mapId}_ground）
    /// </summary>
    public class GridMapUIController : MonoBehaviour
    {
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI infoText;
        [SerializeField] private RectTransform gridContainer;
        [SerializeField] private GridCellItem cellTemplate;

        GridMapData _mapData;
        GridCellItem[,] _cellItems;
        Vector2Int _playerPos;
        bool _isMoving;
        Sprite _playerPortrait;

        public Vector2Int PlayerPos => _playerPos;
        public GridMapData MapData => _mapData;

        // 地表底色（半透明，让背景大图透出来）
        static readonly Color GrassC = new Color(0.30f, 0.55f, 0.25f, 0.45f);
        static readonly Color DirtC = new Color(0.55f, 0.42f, 0.25f, 0.45f);
        static readonly Color StoneC = new Color(0.50f, 0.52f, 0.56f, 0.45f);
        static readonly Color SandC = new Color(0.70f, 0.62f, 0.42f, 0.45f);
        static readonly Color WoodC = new Color(0.58f, 0.44f, 0.26f, 0.45f);
        static readonly Color DarkGrassC = new Color(0.16f, 0.38f, 0.15f, 0.45f);

        void Awake() => AutoBindReferences();

        void AutoBindReferences()
        {
            if (backgroundImage == null) { var t = transform.Find("BG"); if (t != null) backgroundImage = t.GetComponent<Image>(); }
            if (titleText == null) { var t = transform.Find("TitleText"); if (t != null) titleText = t.GetComponent<TextMeshProUGUI>(); }
            if (infoText == null) { var t = transform.Find("InfoText"); if (t != null) infoText = t.GetComponent<TextMeshProUGUI>(); }
            if (gridContainer == null) { var t = transform.Find("MapScroll/Viewport/Content"); if (t != null) gridContainer = t as RectTransform; }
        }

        void Update()
        {
            // E键交互当前所在格子
            if (Input.GetKeyDown(KeyCode.E) && !_isMoving && _mapData != null)
                InteractCurrentCell();
        }

        void OnEnable()
        {
            GridInteractionEvents.OnInteractionComplete += HandleInteractionComplete;
        }

        void OnDisable()
        {
            GridInteractionEvents.OnInteractionComplete -= HandleInteractionComplete;
        }

        #region 初始化与构建

        /// <summary>初始化地图</summary>
        public void Init(GridMapData data)
        {
            _mapData = data;
            _playerPortrait = Resources.Load<Sprite>("GridMapArt/player_token");

            if (titleText != null) titleText.text = data.mapName;
            LoadBackground(data);
            BuildGrid(data);

            // 战后返回：玩家站在被击败敌人的格子上，敌人已清除
            var winCell = BattleLauncher.ConsumeGridWin(data.mapId);
            if (winCell.HasValue)
            {
                var enemyItem = GetItem(winCell.Value.x, winCell.Value.y);
                if (enemyItem != null && enemyItem.Cell != null)
                {
                    enemyItem.Cell.interactType = GridInteractType.None;
                    enemyItem.ClearObject();
                }
                Debug.Log($"[GridMap] 战后返回: 玩家位于({winCell.Value.x},{winCell.Value.y})，敌人已清除");
            }
            PlacePlayer(winCell ?? data.spawnPoint);
        }

        void LoadBackground(GridMapData data)
        {
            if (backgroundImage == null) return;
            var bg = GridMapArt.LoadGroundImage(data.mapId);
            if (bg != null)
            {
                backgroundImage.sprite = bg;
                backgroundImage.color = Color.white;
                backgroundImage.preserveAspect = true;
            }
        }

        void BuildGrid(GridMapData data)
        {
            // 清理运行时格子（保留模板）
            if (gridContainer != null)
            {
                for (int i = gridContainer.childCount - 1; i >= 0; i--)
                {
                    var child = gridContainer.GetChild(i);
                    if (cellTemplate == null || child != cellTemplate.transform)
                        Destroy(child.gameObject);
                }
            }
            if (cellTemplate != null) cellTemplate.gameObject.SetActive(false);

            var layout = gridContainer.GetComponent<GridLayoutGroup>();
            if (layout != null)
            {
                layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                layout.constraintCount = data.width;
            }

            _cellItems = new GridCellItem[data.width, data.height];

            // GridLayout从上到下排列：y大的行先生成（y=0在底部）
            for (int y = data.height - 1; y >= 0; y--)
            {
                for (int x = 0; x < data.width; x++)
                {
                    var cell = new GridCell(x, y)
                    {
                        ground = data.GetGround(x, y),
                        overlay = data.GetOverlay(x, y),
                        isWalkable = data.IsWalkable(x, y),
                        moveCost = data.GetMoveCost(x, y),
                        isExplored = true
                    };

                    var interactCell = data.GetInteractCell(x, y);
                    if (interactCell != null)
                    {
                        cell.interactType = interactCell.interactType;
                        cell.interactTargetId = interactCell.interactTargetId;
                    }

                    // 有交互物的格子强制可通行（必须能走上去触发）
                    if (cell.interactType != GridInteractType.None)
                        cell.isWalkable = true;

                    var item = Instantiate(cellTemplate, gridContainer);
                    item.name = $"Cell_{x}_{y}";
                    item.gameObject.SetActive(true);
                    item.Init(cell, OnCellClicked);
                    item.SetVisual(GetCellColor(cell), GetCellIcon(cell), GetCellLabel(cell));

                    _cellItems[x, y] = item;
                }
            }

            // 撤离点
            foreach (var exit in data.exitPoints)
            {
                if (exit.x < 0 || exit.x >= data.width || exit.y < 0 || exit.y >= data.height) continue;
                var cell = _cellItems[exit.x, exit.y].Cell;
                cell.interactType = GridInteractType.Exit;
                cell.isWalkable = true;
                _cellItems[exit.x, exit.y].SetVisual(GetCellColor(cell), GetCellIcon(cell), "撤离");
            }
        }

        void PlacePlayer(Vector2Int spawn)
        {
            _playerPos = spawn;
            var item = GetItem(spawn.x, spawn.y);
            if (item != null) item.SetPlayerHere(true, _playerPortrait);
            RefreshHighlights();
            UpdateInfoText();
        }

        #endregion

        #region 移动与交互

        /// <summary>点击格子：点击所在格=交互，点击其他格=寻路移动</summary>
        public void OnCellClicked(GridCellItem item)
        {
            if (_isMoving || _mapData == null || item == null || item.Cell == null) return;
            var cell = item.Cell;

            // 点击当前所在格子 → 交互
            if (cell.x == _playerPos.x && cell.y == _playerPos.y)
            {
                InteractCurrentCell();
                return;
            }

            if (!cell.isWalkable) return;

            var path = FindPath(_playerPos, new Vector2Int(cell.x, cell.y));
            if (path == null)
            {
                FloatingTip.ShowWarning("无法到达该位置");
                return;
            }
            if (path.Count > 0)
                StartCoroutine(MoveAlongPath(path));
        }

        /// <summary>交互当前所在格子</summary>
        public void InteractCurrentCell()
        {
            var cell = GetCell(_playerPos.x, _playerPos.y);
            if (cell == null || cell.interactType == GridInteractType.None) return;
            GridInteractionEvents.TriggerCellInteract(cell);
        }

        List<Vector2Int> FindPath(Vector2Int from, Vector2Int to)
        {
            if (from == to) return new List<Vector2Int>();

            var dirs = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            var prev = new Dictionary<Vector2Int, Vector2Int>();
            var visited = new HashSet<Vector2Int> { from };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(from);

            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                foreach (var d in dirs)
                {
                    var next = cur + d;
                    if (next.x < 0 || next.x >= _mapData.width || next.y < 0 || next.y >= _mapData.height) continue;
                    var nextCell = GetCell(next.x, next.y);
                    if (nextCell == null || !nextCell.isWalkable) continue;
                    if (visited.Contains(next)) continue;

                    visited.Add(next);
                    prev[next] = cur;

                    if (next == to)
                    {
                        var path = new List<Vector2Int>();
                        var node = to;
                        while (node != from)
                        {
                            path.Add(node);
                            node = prev[node];
                        }
                        path.Reverse();
                        return path;
                    }
                    queue.Enqueue(next);
                }
            }
            return null;
        }

        IEnumerator MoveAlongPath(List<Vector2Int> path)
        {
            _isMoving = true;

            // 移动中清除所有高亮
            for (int x = 0; x < _mapData.width; x++)
                for (int y = 0; y < _mapData.height; y++)
                    _cellItems[x, y].SetHighlight(false);

            foreach (var step in path)
            {
                var prevItem = GetItem(_playerPos.x, _playerPos.y);
                if (prevItem != null) prevItem.SetPlayerHere(false, null);
                _playerPos = step;

                var item = GetItem(step.x, step.y);
                if (item != null) item.SetPlayerHere(true, _playerPortrait);

                var cell = item != null ? item.Cell : null;
                if (cell != null) OnCellEntered(cell);

                // 敌人/撤离点：进入即触发，中断移动
                if (cell != null && (cell.interactType == GridInteractType.Enemy || cell.interactType == GridInteractType.Exit))
                {
                    Debug.Log($"[GridMap] 移动中断于({step.x},{step.y}) {cell.interactType}");
                    break;
                }

                yield return new WaitForSeconds(0.12f);
            }

            _isMoving = false;
            RefreshHighlights();
        }

        void OnCellEntered(GridCell cell)
        {
            GridInteractionEvents.TriggerCellEntered(cell);

            // 敌人/撤离点自动触发交互
            if (cell.interactType == GridInteractType.Enemy || cell.interactType == GridInteractType.Exit)
                GridInteractionEvents.TriggerCellInteract(cell);

            UpdateInfoText();
        }

        void RefreshHighlights()
        {
            if (_cellItems == null || _mapData == null) return;

            for (int x = 0; x < _mapData.width; x++)
                for (int y = 0; y < _mapData.height; y++)
                    _cellItems[x, y].SetHighlight(false);

            // 高亮玩家四周可通行格子
            foreach (var d in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
            {
                var n = _playerPos + d;
                if (n.x < 0 || n.x >= _mapData.width || n.y < 0 || n.y >= _mapData.height) continue;
                if (GetCell(n.x, n.y).isWalkable)
                    _cellItems[n.x, n.y].SetHighlight(true);
            }
        }

        void UpdateInfoText()
        {
            if (infoText == null) return;
            var cell = GetCell(_playerPos.x, _playerPos.y);
            if (cell == null) { infoText.text = ""; return; }

            switch (cell.interactType)
            {
                case GridInteractType.Gather: infoText.text = "点击本格或按E键采集"; break;
                case GridInteractType.Npc: infoText.text = "点击本格或按E键对话"; break;
                case GridInteractType.StoryTrigger: infoText.text = "点击本格或按E键查看"; break;
                case GridInteractType.Building: infoText.text = "点击本格或按E键调查"; break;
                case GridInteractType.Exit: infoText.text = "撤离点：点击本格或按E键撤离"; break;
                default: infoText.text = "点击高亮格子移动（支持连续点击寻路）"; break;
            }
        }

        #endregion

        #region 交互结果UI刷新

        void HandleInteractionComplete(GridCell cell, string message)
        {
            if (message != null && message.StartsWith("gather"))
            {
                var item = GetItem(cell.x, cell.y);
                if (item != null) item.ClearObject();
                UpdateInfoText();
            }
        }

        #endregion

        #region 格子视觉辅助

        public GridCellItem GetItem(int x, int y)
        {
            if (_cellItems == null || _mapData == null) return null;
            if (x < 0 || x >= _mapData.width || y < 0 || y >= _mapData.height) return null;
            return _cellItems[x, y];
        }

        public GridCell GetCell(int x, int y) => GetItem(x, y)?.Cell;

        Color GetCellColor(GridCell cell)
        {
            switch (cell.overlay)
            {
                case OverlayType.Wall: return new Color(0.22f, 0.22f, 0.26f, 0.92f);
                case OverlayType.Water: return new Color(0.15f, 0.35f, 0.60f, 0.88f);
                case OverlayType.Tree: return new Color(0.12f, 0.30f, 0.10f, 0.88f);
                case OverlayType.Rock: return new Color(0.42f, 0.40f, 0.36f, 0.88f);
                case OverlayType.Fence: return new Color(0.45f, 0.34f, 0.20f, 0.88f);
                case OverlayType.Swamp: return new Color(0.38f, 0.32f, 0.18f, 0.55f);
                case OverlayType.PoisonFog: return new Color(0.45f, 0.20f, 0.48f, 0.50f);
                case OverlayType.SpiritEye: return new Color(0.40f, 0.75f, 0.68f, 0.50f);
                case OverlayType.Ruins: return new Color(0.55f, 0.48f, 0.38f, 0.55f);
                case OverlayType.Flower: return new Color(0.75f, 0.45f, 0.55f, 0.50f);
            }

            switch (cell.ground)
            {
                case GroundType.Dirt: return DirtC;
                case GroundType.Stone: return StoneC;
                case GroundType.Sand: return SandC;
                case GroundType.Wood: return WoodC;
                case GroundType.DarkGrass: return DarkGrassC;
                default: return GrassC;
            }
        }

        Sprite GetCellIcon(GridCell cell)
        {
            switch (cell.overlay)
            {
                case OverlayType.Tree: return GridMapArt.LoadProp(GridMapArt.PropTree);
                case OverlayType.Rock: return GridMapArt.LoadProp(GridMapArt.PropRock);
            }

            switch (cell.interactType)
            {
                case GridInteractType.Gather: return GridMapArt.LoadProp(GridMapArt.PropHerb);
                case GridInteractType.Enemy: return GridMapArt.LoadEnemyPortrait(cell.interactTargetId);
                default: return null;
            }
        }

        string GetCellLabel(GridCell cell)
        {
            switch (cell.interactType)
            {
                case GridInteractType.Npc: return "NPC";
                case GridInteractType.StoryTrigger: return "?";
                case GridInteractType.Building: return "建筑";
                case GridInteractType.Exit: return "撤离";
                case GridInteractType.Enemy:
                    return GridMapArt.LoadEnemyPortrait(cell.interactTargetId) != null ? "" : "敌";
                default: return "";
            }
        }

        #endregion
    }
}
