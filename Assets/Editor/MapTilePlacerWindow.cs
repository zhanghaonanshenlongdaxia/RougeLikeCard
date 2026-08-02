using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace CardProject.Editor
{
    public class MapTilePlacerWindow : EditorWindow
    {
        private DefaultAsset folder;
        private float hOverlap = 0.15f;
        private float vOverlap = 0.15f;
        private string sortingLayer = "Default";
        private int sortingOrder = 1;
        private Vector2 spritePivot = new Vector2(0.5f, 0.5f);

        [MenuItem("Tools/地图拼接工具/自动摆放地图")]
        private static void Open()
        {
            GetWindow<MapTilePlacerWindow>("自动摆放地图");
        }

        private void OnGUI()
        {
            GUILayout.Label("地图 Tile 自动摆放", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            folder = (DefaultAsset)EditorGUILayout.ObjectField(
                "图片文件夹", folder, typeof(DefaultAsset), false);

            hOverlap = EditorGUILayout.Slider("左右重叠", hOverlap, 0f, 0.5f);
            vOverlap = EditorGUILayout.Slider("上下重叠", vOverlap, 0f, 0.5f);

            sortingLayer = EditorGUILayout.TextField("Sorting Layer", sortingLayer);
            sortingOrder = EditorGUILayout.IntField("Sorting Order", sortingOrder);

            EditorGUILayout.HelpBox(
                "图片名格式：tile_x_y.png，例如 tile_0_0.png、tile_1_0.png、tile_0_-1.png。\n" +
                "坐标按传统坐标轴：中间 (0,0)，右 (1,0)，左 (-1,0)，上 (0,1)，下 (0,-1)。",
                MessageType.Info);

            EditorGUILayout.Space();

            GUI.enabled = folder != null;
            if (GUILayout.Button("生成地图", GUILayout.Height(32)))
            {
                PlaceTiles();
            }
            GUI.enabled = true;
        }

        private void PlaceTiles()
        {
            string path = AssetDatabase.GetAssetPath(folder);
            if (string.IsNullOrEmpty(path) || !AssetDatabase.IsValidFolder(path))
            {
                EditorUtility.DisplayDialog("错误", "请选择有效的文件夹", "确定");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { path });
            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("错误", "文件夹中没有 Sprite 图片", "确定");
                return;
            }

            GameObject root = new GameObject("MapTiles_" + sortingLayer);
            Undo.RegisterCreatedObjectUndo(root, "Place Map Tiles");

            int placed = 0;
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if (sprite == null) continue;

                Match match = Regex.Match(sprite.name, @"tile_(-?\d+)_(-?\d+)");
                if (!match.Success) continue;

                int x = int.Parse(match.Groups[1].Value);
                int y = int.Parse(match.Groups[2].Value);

                float tileW = sprite.rect.width / sprite.pixelsPerUnit;
                float tileH = sprite.rect.height / sprite.pixelsPerUnit;
                float stepX = tileW * (1f - hOverlap);
                float stepY = tileH * (1f - vOverlap);

                // Offset so that (0,0) tile center is at world origin when pivot is center.
                Vector3 pos = new Vector3(x * stepX, y * stepY, 0f);

                GameObject go = new GameObject(sprite.name);
                go.transform.SetParent(root.transform);
                go.transform.position = pos;
                Undo.RegisterCreatedObjectUndo(go, "Place Map Tile");

                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingLayerName = sortingLayer;
                sr.sortingOrder = sortingOrder;

                placed++;
            }

            EditorUtility.DisplayDialog("完成", $"已摆放 {placed} 张图片到 {root.name}", "确定");
        }
    }
}
