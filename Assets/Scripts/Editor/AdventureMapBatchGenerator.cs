using UnityEngine;
using UnityEditor;

namespace CardGame.Editor
{
    public static class AdventureMapBatchGenerator
    {
        [MenuItem("Tools/Generate Adventure Maps")]
        public static void GenerateAll()
        {
            string dir = "Assets/NueGames/NueDeck/Data/AdventureMaps";
            if (!AssetDatabase.IsValidFolder(dir))
            {
                var parent = "Assets/NueGames/NueDeck/Data";
                AssetDatabase.CreateFolder(parent, "AdventureMaps");
            }

            var config = ScriptableObject.CreateInstance<AdventureMapConfig>();

            // === 地图1: 山野荒原 ===
            var map1 = new AdventureMapData
            {
                mapId = "map_shanye",
                mapName = "山野荒原",
                description = "山间荒野，散修与灵兽出没之地。适合初入修仙之路的修士历练。",
                regionId = 0,
                unlockRealmLevel = 0,
                difficulties = new System.Collections.Generic.List<AdventureDifficulty>
                {
                    new AdventureDifficulty
                    {
                        difficultyType = DifficultyType.Normal,
                        difficultyName = "普通",
                        description = "敌人较弱，适合练气期修士。",
                        requiredShenShi = 0,
                        mapFloors = 8, mapColumns = 3,
                        enemyHpMultiplier = 1f, enemyDamageMultiplier = 1f,
                        eliteChance = 0.2f, bossPhaseCount = 1,
                        lootMultiplier = 1f, lootRarityBonus = 0, goldRewardMultiplier = 1
                    },
                    new AdventureDifficulty
                    {
                        difficultyType = DifficultyType.Hard,
                        difficultyName = "困难",
                        description = "敌人更强，精英怪更多，需要一定卡牌实力。",
                        requiredShenShi = 0,
                        mapFloors = 12, mapColumns = 3,
                        enemyHpMultiplier = 1.3f, enemyDamageMultiplier = 1.3f,
                        eliteChance = 0.3f, bossPhaseCount = 2,
                        lootMultiplier = 1.3f, lootRarityBonus = 1, goldRewardMultiplier = 2
                    }
                }
            };
            config.maps.Add(map1);

            // === 地图2: 幽冥秘境 ===
            var map2 = new AdventureMapData
            {
                mapId = "map_youming",
                mapName = "幽冥秘境",
                description = "幽冥之气弥漫，鬼修与冥兽潜伏。需要筑基以上修为方可踏入。",
                regionId = 1,
                unlockRealmLevel = 1,
                difficulties = new System.Collections.Generic.List<AdventureDifficulty>
                {
                    new AdventureDifficulty
                    {
                        difficultyType = DifficultyType.Hard,
                        difficultyName = "困难",
                        description = "幽冥秘境的最低难度，鬼修凶猛。",
                        requiredShenShi = 20,
                        mapFloors = 10, mapColumns = 3,
                        enemyHpMultiplier = 1.3f, enemyDamageMultiplier = 1.3f,
                        eliteChance = 0.3f, bossPhaseCount = 2,
                        lootMultiplier = 1.3f, lootRarityBonus = 1, goldRewardMultiplier = 2
                    },
                    new AdventureDifficulty
                    {
                        difficultyType = DifficultyType.Extreme,
                        difficultyName = "极难",
                        description = "冥气浓重，Boss拥有三个阶段，极度危险。",
                        requiredShenShi = 50,
                        mapFloors = 12, mapColumns = 4,
                        enemyHpMultiplier = 1.6f, enemyDamageMultiplier = 1.6f,
                        eliteChance = 0.4f, bossPhaseCount = 3,
                        lootMultiplier = 1.6f, lootRarityBonus = 2, goldRewardMultiplier = 3
                    }
                }
            };
            config.maps.Add(map2);

            // === 地图3: 万蛊沼泽 ===
            var map3 = new AdventureMapData
            {
                mapId = "map_wangu",
                mapName = "万蛊沼泽",
                description = "毒蛊遍地，蛊虫与毒物横行。金丹期以上方可涉足。",
                regionId = 2,
                unlockRealmLevel = 2,
                difficulties = new System.Collections.Generic.List<AdventureDifficulty>
                {
                    new AdventureDifficulty
                    {
                        difficultyType = DifficultyType.Extreme,
                        difficultyName = "极难",
                        description = "毒雾弥漫，敌人剧毒无比。",
                        requiredShenShi = 50,
                        mapFloors = 12, mapColumns = 4,
                        enemyHpMultiplier = 1.6f, enemyDamageMultiplier = 1.6f,
                        eliteChance = 0.4f, bossPhaseCount = 3,
                        lootMultiplier = 1.6f, lootRarityBonus = 2, goldRewardMultiplier = 3
                    },
                    new AdventureDifficulty
                    {
                        difficultyType = DifficultyType.Hell,
                        difficultyName = "地狱",
                        description = "万蛊齐出，Boss拥有全部阶段，地狱级挑战。",
                        requiredShenShi = 100,
                        mapFloors = 15, mapColumns = 4,
                        enemyHpMultiplier = 2f, enemyDamageMultiplier = 2f,
                        eliteChance = 0.5f, bossPhaseCount = 3,
                        lootMultiplier = 2f, lootRarityBonus = 3, goldRewardMultiplier = 5
                    }
                }
            };
            config.maps.Add(map3);

            // === 地图4: 天魔裂隙 ===
            var map4 = new AdventureMapData
            {
                mapId = "map_tianmo",
                mapName = "天魔裂隙",
                description = "天魔降临之地，空间裂隙中涌出无尽魔物。唯有元婴以上大能方可一试。",
                regionId = 3,
                unlockRealmLevel = 3,
                difficulties = new System.Collections.Generic.List<AdventureDifficulty>
                {
                    new AdventureDifficulty
                    {
                        difficultyType = DifficultyType.Hell,
                        difficultyName = "地狱",
                        description = "天魔裂隙只有一个难度：地狱。准备好迎接最终挑战。",
                        requiredShenShi = 100,
                        mapFloors = 15, mapColumns = 4,
                        enemyHpMultiplier = 2f, enemyDamageMultiplier = 2f,
                        eliteChance = 0.5f, bossPhaseCount = 3,
                        lootMultiplier = 2f, lootRarityBonus = 3, goldRewardMultiplier = 5
                    }
                }
            };
            config.maps.Add(map4);

            string path = $"{dir}/AdventureMapConfig.asset";
            if (AssetDatabase.LoadAssetAtPath<AdventureMapConfig>(path) != null)
                AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"冒险地图配置创建完成: {config.maps.Count}个地图 at {path}");
        }
    }
}
