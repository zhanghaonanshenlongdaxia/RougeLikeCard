using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CardGame
{
    /// <summary>
    /// 格子地图场景加载器 — 静态传递要加载的地图ID
    /// 世界地图点击地点 → 设置PendingGridMapId → 跳转GridMap场景 → GridMapManager读取
    /// 格子地图退出 → 回世界地图场景
    /// </summary>
    public static class GridMapSceneLoader
    {
        /// <summary>待加载的格子地图ID（跨场景传递）</summary>
        public static string PendingGridMapId { get; set; }

        /// <summary>退出格子地图：回世界地图</summary>
        public static void ExitToWorldMap()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("6- WorldMap");
        }

        /// <summary>加载格子地图数据（编辑器用AssetDatabase，打包用Resources）</summary>
        public static GridMapData LoadMapData(string mapId)
        {
            if (string.IsNullOrEmpty(mapId)) return null;
#if UNITY_EDITOR
            var guids = UnityEditor.AssetDatabase.FindAssets($"t:GridMapData {mapId}");
            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var map = UnityEditor.AssetDatabase.LoadAssetAtPath<GridMapData>(path);
                if (map != null && map.mapId == mapId)
                    return map;
            }
            // 按资产名兜底
            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var map = UnityEditor.AssetDatabase.LoadAssetAtPath<GridMapData>(path);
                if (map != null) return map;
            }
#endif
            return null;
        }
    }
}
