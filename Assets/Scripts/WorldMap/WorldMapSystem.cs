using System.Collections.Generic;
using QFramework;
using UnityEngine;
using NueGames.NueDeck.Scripts.Enums;

namespace CardGame
{
    /// <summary>
    /// 世界地图系统 — 寻路/解锁/旅行耗时计算
    /// </summary>
    public interface IWorldMapSystem : ISystem
    {
        /// <summary>初始化世界地图（新游戏时调用）</summary>
        void InitNewGame();

        /// <summary>计算从当前位置到目标的路径序列（Dijkstra最短耗时）。不可达返回null</summary>
        /// <param name="targetId">目标地点</param>
        /// <param name="useFly">是否使用御剑飞行（耗时取flyDays）</param>
        List<TravelStep> FindRoute(string targetId, bool useFly);

        /// <summary>旅行单步耗时（天）</summary>
        int GetTravelDays(string fromId, string toId, bool useFly);

        /// <summary>到达某地点（含解锁）</summary>
        void ArriveAt(string locationId);

        /// <summary>解锁地点</summary>
        void UnlockLocation(string locationId);

        /// <summary>地点是否已解锁可见</summary>
        bool IsLocationUnlocked(string locationId);

        /// <summary>获取配置</summary>
        WorldMapConfig GetConfig();

        /// <summary>当前御剑能力是否可用（金丹期自动解锁御剑）</summary>
        bool CanFlyNow();
    }

    /// <summary>旅行单步：从from走到to耗时days天</summary>
    public class TravelStep
    {
        public string fromId;
        public string toId;
        public int days;
        public bool usedFly;
    }

    public class WorldMapSystem : AbstractSystem, IWorldMapSystem
    {
        IWorldMapModel _model;
        WorldMapConfig _config;

        protected override void OnInit()
        {
            _model = this.GetModel<IWorldMapModel>();
            _config = Resources.Load<WorldMapConfig>("WorldMap/WorldMapConfig");
#if UNITY_EDITOR
            if (_config == null)
            {
                var guids = UnityEditor.AssetDatabase.FindAssets("t:WorldMapConfig");
                foreach (var g in guids)
                {
                    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                    _config = UnityEditor.AssetDatabase.LoadAssetAtPath<WorldMapConfig>(path);
                    if (_config != null) break;
                }
            }
#endif
            if (_config == null)
                Debug.LogWarning("[WorldMap] WorldMapConfig未找到，需创建配置资产");
        }

        public void InitNewGame()
        {
            _model.CurrentLocationId.Value = _config != null ? _config.startLocationId : "";
            _model.IsTraveling.Value = false;
            _model.UnlockedLocationIds.Clear();
            if (_config != null)
                _model.UnlockedLocationIds.Add(_config.startLocationId);
        }

        public List<TravelStep> FindRoute(string targetId, bool useFly)
        {
            if (_config == null || _model == null) return null;
            var fromId = _model.CurrentLocationId.Value;
            if (fromId == targetId) return null;

            // Dijkstra：节点=地点，边=路径耗时
            var dist = new Dictionary<string, int>();
            var prev = new Dictionary<string, string>();   // 记录前驱地点
            var visited = new HashSet<string>();

            foreach (var loc in _config.locations)
                dist[loc.locationId] = int.MaxValue;
            dist[fromId] = 0;

            while (true)
            {
                // 取未访问最小dist节点
                string current = null;
                int minDist = int.MaxValue;
                foreach (var kv in dist)
                {
                    if (!visited.Contains(kv.Key) && kv.Value < minDist)
                    {
                        minDist = kv.Value;
                        current = kv.Key;
                    }
                }
                if (current == null || current == targetId) break;

                visited.Add(current);

                foreach (var path in _config.GetPathsFrom(current))
                {
                    var neighbor = path.fromId == current ? path.toId : path.fromId;

                    // flyOnly路径：没飞行能力则不可走
                    if (path.flyOnly && !useFly) continue;

                    var cost = useFly ? path.flyDays : path.walkDays;
                    if (useFly && path.flyDays <= 0) cost = path.walkDays; // 不可飞的路径按步行

                    var newDist = minDist + cost;
                    if (newDist < dist[neighbor])
                    {
                        dist[neighbor] = newDist;
                        prev[neighbor] = current;
                    }
                }
            }

            if (dist[targetId] == int.MaxValue) return null; // 不可达

            // 回溯路径
            var route = new List<TravelStep>();
            var node = targetId;
            while (node != fromId && prev.ContainsKey(node))
            {
                var prevNode = prev[node];
                var path = _config.GetPath(prevNode, node);
                if (path == null) break;
                int days = useFly ? (path.flyDays > 0 ? path.flyDays : path.walkDays) : path.walkDays;
                route.Insert(0, new TravelStep { fromId = prevNode, toId = node, days = days, usedFly = useFly });
                node = prevNode;
            }
            return route.Count > 0 ? route : null;
        }

        public int GetTravelDays(string fromId, string toId, bool useFly)
        {
            var path = _config.GetPath(fromId, toId);
            if (path == null) return -1;
            if (path.flyOnly && !useFly) return -1;
            return useFly ? (path.flyDays > 0 ? path.flyDays : path.walkDays) : path.walkDays;
        }

        public void ArriveAt(string locationId)
        {
            _model.CurrentLocationId.Value = locationId;
            _model.IsTraveling.Value = false;
            UnlockLocation(locationId);
        }

        public void UnlockLocation(string locationId)
        {
            if (!_model.UnlockedLocationIds.Contains(locationId))
                _model.UnlockedLocationIds.Add(locationId);
        }

        public bool IsLocationUnlocked(string locationId)
        {
            return _model.UnlockedLocationIds.Contains(locationId);
        }

        public WorldMapConfig GetConfig() => _config;

        /// <summary>金丹期即解锁御剑飞行（与RealmTable"金丹可御剑飞行"一致）</summary>
        public bool CanFlyNow()
        {
            if (_model.CanFly.Value) return true;
            return this.GetModel<IRealmModel>().CurrentRealm.Value >= (int)RealmLevel.JinDan;
        }
    }
}
