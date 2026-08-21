using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CardGame
{
    /// <summary>
    /// 格子地图场景入口 — 实例化GridMapCanvas预制体并初始化地图数据
    /// </summary>
    public class GridMapManager : MonoBehaviour
    {
        public GridMapData testMapData;

        const string CanvasPrefabPath = "Assets/Prefabs/UI/Map/GridMap/GridMapCanvas.prefab";

        void Start()
        {
            GameObject canvasPrefab = null;
#if UNITY_EDITOR
            canvasPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CanvasPrefabPath);
#endif
            if (canvasPrefab == null)
                canvasPrefab = Resources.Load<GameObject>("GridMapArt/GridMapCanvas");

            if (canvasPrefab == null)
            {
                Debug.LogError("[GridMapManager] GridMapCanvas预制体未找到");
                return;
            }

            var canvasGo = Instantiate(canvasPrefab);
            canvasGo.name = "GridMapCanvas";

            var controller = canvasGo.GetComponent<GridMapUIController>();
            if (controller != null && testMapData != null)
                controller.Init(testMapData);
            else
                Debug.LogError("[GridMapManager] GridMapUIController缺失或地图数据为空");
        }
    }
}
