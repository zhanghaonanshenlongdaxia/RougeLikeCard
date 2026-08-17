using System.Collections.Generic;
using System.Linq;
using NueGames.NueDeck.Scripts.Data.Containers;
using NueGames.NueDeck.Scripts.Enums;
using UnityEngine;

namespace CardGame
{
    /// <summary>
    /// 运行时资源缓存。编辑器下用AssetDatabase预加载，打包后用Resources.LoadAll。
    /// 替代所有AssetDatabase.FindAssets/LoadAssetAtPath调用，支持build。
    /// </summary>
    public static class ResourceCache
    {
        private static bool _initialized = false;
        private static List<MaterialData> _materials;
        private static List<RecipeData> _recipes;
        private static List<RelicData> _relics;
        private static List<PotionData> _potions;
        private static List<EventData> _events;
        private static List<NueGames.NueDeck.Scripts.Data.Collection.CardData> _cards;
        private static Dictionary<int, EncounterData> _encountersByRegion;

        public static void Init()
        {
            if (_initialized) return;

#if UNITY_EDITOR
            LoadFromAssetDatabase();
#else
            LoadFromResources();
#endif
            _initialized = true;
            Debug.Log($"[ResourceCache] Initialized: Materials={_materials?.Count}, Recipes={_recipes?.Count}, Relics={_relics?.Count}, Potions={_potions?.Count}, Events={_events?.Count}, Cards={_cards?.Count}");
        }

#if UNITY_EDITOR
        static void LoadFromAssetDatabase()
        {
            // Materials
            _materials = new List<MaterialData>();
            var matGuids = UnityEditor.AssetDatabase.FindAssets("t:MaterialData", new[] { "Assets/NueGames/NueDeck/Data/Materials" });
            foreach (var g in matGuids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<MaterialData>(path);
                if (mat != null) _materials.Add(mat);
            }

            // Recipes
            _recipes = new List<RecipeData>();
            var recGuids = UnityEditor.AssetDatabase.FindAssets("t:RecipeData", new[] { "Assets/NueGames/NueDeck/Data/Recipes" });
            foreach (var g in recGuids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                var r = UnityEditor.AssetDatabase.LoadAssetAtPath<RecipeData>(path);
                if (r != null) _recipes.Add(r);
            }

            // Relics
            _relics = new List<RelicData>();
            var relGuids = UnityEditor.AssetDatabase.FindAssets("t:RelicData", new[] { "Assets/NueGames/NueDeck/Data/Relics" });
            foreach (var g in relGuids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                var r = UnityEditor.AssetDatabase.LoadAssetAtPath<RelicData>(path);
                if (r != null) _relics.Add(r);
            }

            // Potions
            _potions = new List<PotionData>();
            var potGuids = UnityEditor.AssetDatabase.FindAssets("t:PotionData", new[] { "Assets/NueGames/NueDeck/Data/Potions" });
            foreach (var g in potGuids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                var p = UnityEditor.AssetDatabase.LoadAssetAtPath<PotionData>(path);
                if (p != null) _potions.Add(p);
            }

            // Events
            _events = new List<EventData>();
            var evtGuids = UnityEditor.AssetDatabase.FindAssets("t:EventData", new[] { "Assets/NueGames/NueDeck/Data/Events" });
            foreach (var g in evtGuids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                var e = UnityEditor.AssetDatabase.LoadAssetAtPath<EventData>(path);
                if (e != null) _events.Add(e);
            }

            // Cards
            _cards = new List<NueGames.NueDeck.Scripts.Data.Collection.CardData>();
            var cardGuids = UnityEditor.AssetDatabase.FindAssets("t:CardData", new[] { "Assets/NueGames/NueDeck/Data/Cards" });
            foreach (var g in cardGuids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                var c = UnityEditor.AssetDatabase.LoadAssetAtPath<NueGames.NueDeck.Scripts.Data.Collection.CardData>(path);
                if (c != null) _cards.Add(c);
            }

            // EncounterData by region
            _encountersByRegion = new Dictionary<int, EncounterData>();
            var encNames = new[] { "山野荒原", "幽冥秘境", "万蛊沼泽", "天魔裂隙" };
            for (int i = 0; i < encNames.Length; i++)
            {
                var encPath = $"Assets/NueGames/NueDeck/Data/Encounters/Encounter_{encNames[i]}.asset";
                var enc = UnityEditor.AssetDatabase.LoadAssetAtPath<EncounterData>(encPath);
                if (enc != null)
                {
                    _encountersByRegion[i] = enc;
                }
                else
                {
                    Debug.LogWarning($"[ResourceCache] EncounterData not found: {encPath}");
                }
            }
        }
#else
        static void LoadFromResources()
        {
            _materials = new List<MaterialData>(Resources.LoadAll<MaterialData>("Data/Materials"));
            _recipes = new List<RecipeData>(Resources.LoadAll<RecipeData>("Data/Recipes"));
            _relics = new List<RelicData>(Resources.LoadAll<RelicData>("Data/Relics"));
            _potions = new List<PotionData>(Resources.LoadAll<PotionData>("Data/Potions"));
            _events = new List<EventData>(Resources.LoadAll<EventData>("Data/Events"));
            _cards = new List<NueGames.NueDeck.Scripts.Data.Collection.CardData>(Resources.LoadAll<NueGames.NueDeck.Scripts.Data.Collection.CardData>("Data/Cards"));

            _encountersByRegion = new Dictionary<int, EncounterData>();
            var encArr = Resources.LoadAll<EncounterData>("Data/Encounters");
            var encNames = new[] { "山野荒原", "幽冥秘境", "万蛊沼泽", "天魔裂隙" };
            for (int i = 0; i < encNames.Length; i++)
            {
                var enc = System.Array.Find(encArr, e => e.name.Contains(encNames[i]));
                if (enc != null) _encountersByRegion[i] = enc;
            }
        }
#endif

        public static List<MaterialData> GetMaterials()
        {
            if (!_initialized) Init();
            return _materials ?? new List<MaterialData>();
        }

        public static List<RecipeData> GetRecipes()
        {
            if (!_initialized) Init();
            return _recipes ?? new List<RecipeData>();
        }

        public static List<NueGames.NueDeck.Scripts.Data.Collection.CardData> GetCardsFromAllList()
        {
            if (!_initialized) Init();
            return _cards ?? new List<NueGames.NueDeck.Scripts.Data.Collection.CardData>();
        }

        public static List<RelicData> GetRelics()
        {
            if (!_initialized) Init();
            return _relics ?? new List<RelicData>();
        }

        public static List<PotionData> GetPotions()
        {
            if (!_initialized) Init();
            return _potions ?? new List<PotionData>();
        }

        public static List<EventData> GetEvents()
        {
            if (!_initialized) Init();
            return _events ?? new List<EventData>();
        }

        /// <summary>根据regionId获取对应的EncounterData</summary>
        public static EncounterData GetEncounterData(int regionId)
        {
            if (!_initialized) Init();
            if (_encountersByRegion != null && _encountersByRegion.TryGetValue(regionId, out var enc))
                return enc;
            Debug.LogWarning($"[ResourceCache] No EncounterData for regionId={regionId}");
            return null;
        }

        public static RelicData GetRandomRelic()
        {
            var list = GetRelics();
            return list.Count > 0 ? list[Random.Range(0, list.Count)] : null;
        }

        public static PotionData GetRandomPotion()
        {
            var list = GetPotions();
            return list.Count > 0 ? list[Random.Range(0, list.Count)] : null;
        }

        /// <summary>按品质获取随机材料</summary>
        public static MaterialData GetRandomMaterialByQuality(ItemQuality quality)
        {
            var list = GetMaterials();
            var filtered = list.FindAll(m => m.quality == quality);
            if (filtered.Count == 0) filtered = list;
            return filtered.Count > 0 ? filtered[Random.Range(0, filtered.Count)] : null;
        }

        /// <summary>按旧品阶名称获取随机材料（兼容旧调用）</summary>
        public static MaterialData GetRandomMaterialByRarity(string rarityName)
        {
            var list = GetMaterials();
            var filtered = list.FindAll(m => m.rarity.ToString() == rarityName);
            if (filtered.Count == 0) filtered = list;
            return filtered.Count > 0 ? filtered[Random.Range(0, filtered.Count)] : null;
        }

        public static MaterialData GetRandomMaterial()
        {
            var list = GetMaterials();
            return list.Count > 0 ? list[Random.Range(0, list.Count)] : null;
        }

        /// <summary>获取未解锁的随机配方</summary>
        public static RecipeData GetRandomLockedRecipe(System.Func<string, bool> isUnlocked)
        {
            var list = GetRecipes();
            var locked = list.FindAll(r => !r.unlockByDefault && !isUnlocked(r.recipeId));
            return locked.Count > 0 ? locked[Random.Range(0, locked.Count)] : null;
        }
    }
}
