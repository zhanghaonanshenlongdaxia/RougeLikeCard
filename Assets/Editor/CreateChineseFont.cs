using TMPro;
using UnityEditor;
using UnityEngine;

public class CreateChineseFont : MonoBehaviour
{
    [MenuItem("Tools/Create Chinese Font Asset")]
    public static void Create()
    {
        AssetDatabase.DeleteAsset("Assets/Fonts/SimHei SDF.asset");
        AssetDatabase.ImportAsset("Assets/Fonts/SimHei.ttf", ImportAssetOptions.ForceUpdate);
        var sourceFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/SimHei.ttf");
        if (sourceFont == null) { Debug.LogError("Failed to load SimHei.ttf"); return; }

        // Step 1: Create font asset with simple overload
        var fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont);
        if (fontAsset == null) { Debug.LogError("Failed to create font asset"); return; }

        fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;

        // Step 2: Create atlas texture
        var atlas = new Texture2D(1024, 1024, TextureFormat.RGBA32, false, true);
        atlas.name = "SimHei Atlas";
        atlas.filterMode = FilterMode.Bilinear;
        atlas.wrapMode = TextureWrapMode.Clamp;

        // Step 3: Create material
        var mat = new Material(Shader.Find("TextMeshPro/Distance Field"));
        mat.name = "SimHei Atlas Material";
        mat.SetTexture("_MainTex", atlas);

        // Step 4: Assign atlas and material to font asset via SerializedObject
        var so = new SerializedObject(fontAsset);
        var atlasArr = so.FindProperty("m_AtlasTextures");
        if (atlasArr != null)
        {
            atlasArr.arraySize = 1;
            atlasArr.GetArrayElementAtIndex(0).objectReferenceValue = atlas;
        }
        var matProp = so.FindProperty("material");
        if (matProp != null) matProp.objectReferenceValue = mat;
        so.ApplyModifiedPropertiesWithoutUndo();

        // Step 5: Save as asset FIRST
        AssetDatabase.CreateAsset(fontAsset, "Assets/Fonts/SimHei SDF.asset");
        AssetDatabase.AddObjectToAsset(atlas, fontAsset);
        AssetDatabase.AddObjectToAsset(mat, fontAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Created SimHei SDF: dynamic={fontAsset.atlasPopulationMode}, atlas={fontAsset.atlasTexture?.width}x{fontAsset.atlasTexture?.height}, mat={fontAsset.material?.name}");

        // Step 6: Update LiberationSans SDF fallback
        var libSans = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if (libSans != null)
        {
            if (libSans.fallbackFontAssetTable == null)
                libSans.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
            libSans.fallbackFontAssetTable.RemoveAll(f => f == null || f.name == "SimHei SDF");
            libSans.fallbackFontAssetTable.Add(fontAsset);
            EditorUtility.SetDirty(libSans);
            AssetDatabase.SaveAssets();
            Debug.Log("Updated LiberationSans SDF fallback");
        }

        // Step 7: Update all prefabs
        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/NueGames/NueDeck/Prefabs" });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;
            var tmps = prefab.GetComponentsInChildren<TMP_Text>(true);
            bool changed = false;
            foreach (var tmp in tmps)
            {
                if (tmp.font != libSans) { tmp.font = libSans; changed = true; }
            }
            if (changed) { PrefabUtility.SavePrefabAsset(prefab); Debug.Log($"Updated prefab: {path}"); }
        }

        Debug.Log("Done! Chinese font setup complete.");
    }
}
