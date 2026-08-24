using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using CardGame.UI;

namespace CardGame
{
    /// <summary>
    /// 世界地图场景入口 — 实例化WorldMapCanvas并初始化
    /// 场景需含：CoreLoader（独立GameObject）+ Camera（含SceneBGMAutoPlayer）
    /// </summary>
    public class WorldMapSceneManager : MonoBehaviour
    {
        const string CanvasPrefabPath = "Assets/Prefabs/UI/WorldMap/WorldMapCanvas.prefab";

        IEnumerator Start()
        {
            yield return null; // 等一帧让场景组件就绪

            GameObject canvasPrefab = null;
#if UNITY_EDITOR
            canvasPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CanvasPrefabPath);
#endif
            if (canvasPrefab == null)
                canvasPrefab = Resources.Load<GameObject>("WorldMap/WorldMapCanvas");

            if (canvasPrefab == null)
            {
                Debug.LogError("[WorldMapScene] WorldMapCanvas预制体未找到");
                yield break;
            }

            var canvasGo = Instantiate(canvasPrefab);
            canvasGo.name = "WorldMapCanvas";
            Debug.Log("[WorldMapScene] WorldMapCanvas已实例化");
        }
    }
}
