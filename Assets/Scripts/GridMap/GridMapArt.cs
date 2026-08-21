using UnityEngine;

namespace CardGame
{
    /// <summary>
    /// 格子地图美术资源 — 大图地表 + 透明物体立绘
    /// 方案C：地表=整张AI生成大图，物体=独立透明立绘
    /// </summary>
    public static class GridMapArt
    {
        const string ArtRoot = "GridMapArt";

        /// <summary>
        /// 加载指定地图的地表大图
        /// </summary>
        public static Sprite LoadGroundImage(string mapId)
        {
            // Resources/GridMapArt/{mapId}_ground
            var sprite = Resources.Load<Sprite>($"{ArtRoot}/{mapId}_ground");
            if (sprite == null)
            {
                // 尝试Texture2D方式加载再转sprite
                var tex = Resources.Load<Texture2D>($"{ArtRoot}/{mapId}_ground");
                if (tex != null)
                    sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f), tex.width / 20f); // 默认20格宽
            }
            return sprite;
        }

        /// <summary>
        /// 加载物体立绘（树/岩石/灵草等）
        /// </summary>
        public static Sprite LoadProp(string propName)
        {
            var sprite = Resources.Load<Sprite>($"{ArtRoot}/{propName}");
            if (sprite == null)
            {
                var tex = Resources.Load<Texture2D>($"{ArtRoot}/{propName}");
                if (tex != null)
                    sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f), 256f);
            }
            return sprite;
        }

        // 常用立绘名
        public const string PropTree = "prop_tree";
        public const string PropRock = "prop_rock";
        public const string PropHerb = "prop_herb";

        /// <summary>
        /// 加载敌人立绘（编辑器下从EnemyCharacterData读取enemyPortrait）
        /// </summary>
        public static Sprite LoadEnemyPortrait(string enemyId)
        {
            if (string.IsNullOrEmpty(enemyId)) return null;
#if UNITY_EDITOR
            var guids = UnityEditor.AssetDatabase.FindAssets($"t:EnemyCharacterData {enemyId}");
            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var enemy = UnityEditor.AssetDatabase.LoadAssetAtPath<NueGames.NueDeck.Scripts.Data.Characters.EnemyCharacterData>(path);
                if (enemy != null && enemy.name == enemyId && enemy.EnemyPortrait != null)
                    return enemy.EnemyPortrait;
            }
#endif
            return null;
        }
    }
}
