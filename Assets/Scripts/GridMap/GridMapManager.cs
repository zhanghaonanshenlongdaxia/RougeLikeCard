using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using CardGame.UI;

namespace CardGame
{
    /// <summary>
    /// 格子地图场景入口 — 实例化GridMapCanvas预制体并初始化地图数据
    /// 优先读取 GridMapSceneLoader.PendingGridMapId（世界地图进入），
    /// 为空时回退 Inspector 指定的 testMapData（直连场景调试）
    /// </summary>
    public class GridMapManager : MonoBehaviour
    {
        public GridMapData testMapData;

        const string CanvasPrefabPath = "Assets/Prefabs/UI/Map/GridMap/GridMapCanvas.prefab";

        IEnumerator Start()
        {
            yield return null; // 等一帧让场景组件就绪

            GameObject canvasPrefab = null;
#if UNITY_EDITOR
            canvasPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CanvasPrefabPath);
#endif
            if (canvasPrefab == null)
                canvasPrefab = Resources.Load<GameObject>("GridMapArt/GridMapCanvas");

            if (canvasPrefab == null)
            {
                Debug.LogError("[GridMapManager] GridMapCanvas预制体未找到");
                yield break;
            }

            var canvasGo = Instantiate(canvasPrefab);
            canvasGo.name = "GridMapCanvas";

            var controller = canvasGo.GetComponent<GridMapUIController>();
            if (controller == null)
            {
                Debug.LogError("[GridMapManager] GridMapUIController缺失");
                yield break;
            }

            // 世界地图进入 → 加载对应地图；秘境中 → 加载当前层；否则用Inspector调试地图
            GridMapData mapData = null;
            var arch = CardGameArchitecture.Interface;
            var realmModel = arch.GetModel<ISecretRealmModel>();
            if (!string.IsNullOrEmpty(realmModel.ActiveRealmId.Value))
            {
                // 秘境爬塔中：加载当前层地图
                var floorMapId = arch.GetSystem<ISecretRealmSystem>().GetCurrentFloorMapId();
                if (!string.IsNullOrEmpty(floorMapId))
                {
                    mapData = GridMapSceneLoader.LoadMapData(floorMapId);
                    Debug.Log($"[GridMapManager] 秘境第{realmModel.CurrentFloor.Value}层地图: {floorMapId}");
                }
            }

            if (mapData == null && !string.IsNullOrEmpty(GridMapSceneLoader.PendingGridMapId))
            {
                mapData = GridMapSceneLoader.LoadMapData(GridMapSceneLoader.PendingGridMapId);
                if (mapData == null)
                    Debug.LogError($"[GridMapManager] 地图未找到: {GridMapSceneLoader.PendingGridMapId}");
            }
            if (mapData == null)
                mapData = testMapData;

            if (mapData != null)
            {
                controller.Init(mapData);
                Debug.Log($"[GridMapManager] 地图已加载: {mapData.mapName}({mapData.mapId})");
            }
            else
            {
                Debug.LogError("[GridMapManager] 无可用地图数据");
            }
        }
    }
}
