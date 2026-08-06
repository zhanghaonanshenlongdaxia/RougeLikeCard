using UnityEngine;
using UnityEditor;
using System.IO;

namespace EditorTools
{
    public static class SpriteSlicer
    {
        [MenuItem("Tools/Slice Role Sprites")]
        public static void SliceAll()
        {
            SliceWalk();
            SliceIdle();
            AssetDatabase.Refresh();
            Debug.Log("All sprite sheets sliced!");
        }

        static void SliceWalk()
        {
            string path = "Assets/Res/sprites/role/protagonist_walk.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) { Debug.LogError($"Cannot find: {path}"); return; }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 100;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;

            int cols = 8, rows = 5, cw = 136, ch = 121;
            var sheet = new SpriteMetaData[cols * rows];
            for (int row = 0; row < rows; row++)
                for (int col = 0; col < cols; col++)
                {
                    int idx = row * cols + col;
                    sheet[idx] = new SpriteMetaData
                    {
                        name = $"walk_{row}_{col}",
                        rect = new Rect(col * cw, (rows - 1 - row) * ch, cw, ch),
                        alignment = 6,
                        pivot = new Vector2(0.5f, 0f),
                    };
                }

            var so = new SerializedObject(importer);
            var prop = so.FindProperty("m_SpriteSheet.m_Sprites");
            prop.arraySize = sheet.Length;
            for (int i = 0; i < sheet.Length; i++)
            {
                var entry = prop.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("name").stringValue = sheet[i].name;
                entry.FindPropertyRelative("rect").rectValue = sheet[i].rect;
                entry.FindPropertyRelative("alignment").intValue = sheet[i].alignment;
                entry.FindPropertyRelative("pivot").vector2Value = sheet[i].pivot;
                entry.FindPropertyRelative("border").vector4Value = Vector4.zero;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            importer.SaveAndReimport();

            var sprites = AssetDatabase.LoadAllAssetsAtPath(path);
            int count = 0;
            foreach (var s in sprites) if (s is Sprite) count++;
            Debug.Log($"Walk sliced: {count} sprites ({cols}x{rows})");
        }

        static void SliceIdle()
        {
            string path = "Assets/Res/sprites/role/protagonist_idle.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) { Debug.LogError($"Cannot find: {path}"); return; }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 100;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;

            int cols = 1, rows = 5, cw = 768, ch = 204;
            var sheet = new SpriteMetaData[cols * rows];
            for (int row = 0; row < rows; row++)
                for (int col = 0; col < cols; col++)
                {
                    int idx = row * cols + col;
                    sheet[idx] = new SpriteMetaData
                    {
                        name = $"idle_{row}",
                        rect = new Rect(col * cw, (rows - 1 - row) * ch, cw, ch),
                        alignment = 6,
                        pivot = new Vector2(0.5f, 0f),
                    };
                }

            var so = new SerializedObject(importer);
            var prop = so.FindProperty("m_SpriteSheet.m_Sprites");
            prop.arraySize = sheet.Length;
            for (int i = 0; i < sheet.Length; i++)
            {
                var entry = prop.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("name").stringValue = sheet[i].name;
                entry.FindPropertyRelative("rect").rectValue = sheet[i].rect;
                entry.FindPropertyRelative("alignment").intValue = sheet[i].alignment;
                entry.FindPropertyRelative("pivot").vector2Value = sheet[i].pivot;
                entry.FindPropertyRelative("border").vector4Value = Vector4.zero;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            importer.SaveAndReimport();

            var sprites = AssetDatabase.LoadAllAssetsAtPath(path);
            int count = 0;
            foreach (var s in sprites) if (s is Sprite) count++;
            Debug.Log($"Idle sliced: {count} sprites ({cols}x{rows})");
        }
    }
}
