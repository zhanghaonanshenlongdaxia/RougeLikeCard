using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace CardGame.Editor
{
    /// <summary>
    /// 生成4个区域的遭遇表 + 扩充卡牌奖励池
    /// 每个区域：10个普通遭遇 + 3个Boss遭遇
    /// </summary>
    public static class EncounterDataGenerator
    {
        [MenuItem("Tools/Generate Encounter Data")]
        public static void GenerateAll()
        {
            int count = 0;

            // 加载所有敌人按区域分组
            var allEnemies = LoadEnemiesByRegion();

            // 为每个区域创建遭遇表
            count += GenerateRegionEncounter(0, "山野荒原", allEnemies);
            count += GenerateRegionEncounter(1, "幽冥秘境", allEnemies);
            count += GenerateRegionEncounter(2, "万蛊沼泽", allEnemies);
            count += GenerateRegionEncounter(3, "天魔裂隙", allEnemies);

            // 扩充卡牌奖励池
            count += ExpandCardRewardPool();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"遭遇表+奖励池生成完成: {count}项");
        }

        static Dictionary<int, List<ScriptableObject>> LoadEnemiesByRegion()
        {
            var result = new Dictionary<int, List<ScriptableObject>>();
            var regions = new[] { "Region1_ShanYe", "Region2_YouMing", "Region3_WanGu", "Region4_TianMo" };
            var regionIds = new[] { 0, 1, 2, 3 };

            for (int i = 0; i < 4; i++)
            {
                var path = $"Assets/NueGames/NueDeck/Data/Enemies/{regions[i]}";
                if (!AssetDatabase.IsValidFolder(path)) continue;

                var guids = AssetDatabase.FindAssets("t:EnemyCharacterData", new[] { path });
                var list = new List<ScriptableObject>();
                foreach (var g in guids)
                {
                    var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(g));
                    if (so != null) list.Add(so);
                }
                result[regionIds[i]] = list;
                Debug.Log($"Region {regionIds[i]} ({regions[i]}): {list.Count} enemies");
            }
            return result;
        }

        static int GenerateRegionEncounter(int regionId, string regionName, Dictionary<int, List<ScriptableObject>> allEnemies)
        {
            if (!allEnemies.ContainsKey(regionId) || allEnemies[regionId].Count == 0)
            {
                Debug.LogWarning($"Region {regionId} has no enemies, skipping");
                return 0;
            }

            var enemies = allEnemies[regionId];
            
            // 分类：normal / elite / boss
            var normalEnemies = new List<ScriptableObject>();
            var eliteEnemies = new List<ScriptableObject>();
            var bossEnemies = new List<ScriptableObject>();

            foreach (var e in enemies)
            {
                var tierField = e.GetType().GetField("enemyTier", BindingFlags.NonPublic | BindingFlags.Instance);
                var tier = tierField?.GetValue(e);
                if (tier == null) continue;
                var tierStr = tier.ToString();
                if (tierStr == "Normal") normalEnemies.Add(e);
                else if (tierStr == "Elite") eliteEnemies.Add(e);
                else if (tierStr == "Boss") bossEnemies.Add(e);
            }

            Debug.Log($"  Normal: {normalEnemies.Count}, Elite: {eliteEnemies.Count}, Boss: {bossEnemies.Count}");

            // 创建EncounterData SO
            string dir = "Assets/NueGames/NueDeck/Data/Encounters";
            if (!AssetDatabase.IsValidFolder(dir))
                AssetDatabase.CreateFolder("Assets/NueGames/NueDeck/Data", "Encounters");

            string assetPath = $"{dir}/Encounter_{regionName}.asset";
            if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath) != null)
                AssetDatabase.DeleteAsset(assetPath);

            var encData = ScriptableObject.CreateInstance<NueGames.NueDeck.Scripts.Data.Containers.EncounterData>();
            var encType = encData.GetType();

            // encounterRandomlyAtStage = true
            var randField = encType.GetField("encounterRandomlyAtStage", BindingFlags.NonPublic | BindingFlags.Instance);
            randField.SetValue(encData, true);

            // 创建Stage
            var stageType = typeof(NueGames.NueDeck.Scripts.Data.Containers.EnemyEncounterStage);
            var stage = System.Activator.CreateInstance(stageType);

            // 设置Stage属性
            var stageNameField = stageType.GetField("name", BindingFlags.NonPublic | BindingFlags.Instance);
            stageNameField?.SetValue(stage, $"{regionName} Stage 0");
            var stageIdField = stageType.GetField("stageId", BindingFlags.NonPublic | BindingFlags.Instance);
            stageIdField?.SetValue(stage, 0);

            // 创建10个普通遭遇
            var enemyEncounterList = new List<NueGames.NueDeck.Scripts.Data.Containers.EnemyEncounter>();
            var encType2 = typeof(NueGames.NueDeck.Scripts.Data.Containers.EnemyEncounter);
            var enemyListFieldType = encType2.GetField("enemyList", BindingFlags.NonPublic | BindingFlags.Instance);

            for (int i = 0; i < 10; i++)
            {
                var encounter = (NueGames.NueDeck.Scripts.Data.Containers.EnemyEncounter)System.Activator.CreateInstance(encType2);
                var enemyList = new List<NueGames.NueDeck.Scripts.Data.Characters.EnemyCharacterData>();

                // 随机1-3个普通敌人
                int count = Random.Range(1, 4);
                var pool = new List<ScriptableObject>(normalEnemies);
                for (int j = 0; j < count && pool.Count > 0; j++)
                {
                    var idx = Random.Range(0, pool.Count);
                    enemyList.Add((NueGames.NueDeck.Scripts.Data.Characters.EnemyCharacterData)pool[idx]);
                    pool.RemoveAt(idx);
                }

                // 30%几率加一个精英
                if (eliteEnemies.Count > 0 && Random.value < 0.3f)
                {
                    enemyList.Add((NueGames.NueDeck.Scripts.Data.Characters.EnemyCharacterData)eliteEnemies[Random.Range(0, eliteEnemies.Count)]);
                }

                enemyListFieldType?.SetValue(encounter, enemyList);
                enemyEncounterList.Add(encounter);
            }

            // 设置EnemyEncounterList
            var enemyListField = stageType.GetField("enemyEncounterList", BindingFlags.NonPublic | BindingFlags.Instance);
            enemyListField?.SetValue(stage, enemyEncounterList);

            // 创建Boss遭遇
            var bossEncounterList = new List<NueGames.NueDeck.Scripts.Data.Containers.EnemyEncounter>();
            foreach (var boss in bossEnemies)
            {
                var encounter = (NueGames.NueDeck.Scripts.Data.Containers.EnemyEncounter)System.Activator.CreateInstance(encType2);
                var enemyList = new List<NueGames.NueDeck.Scripts.Data.Characters.EnemyCharacterData> {
                    (NueGames.NueDeck.Scripts.Data.Characters.EnemyCharacterData)boss
                };
                enemyListFieldType?.SetValue(encounter, enemyList);
                bossEncounterList.Add(encounter);
            }

            var bossListField = stageType.GetField("bossEncounterList", BindingFlags.NonPublic | BindingFlags.Instance);
            bossListField?.SetValue(stage, bossEncounterList);

            // 设置stageList到EncounterData
            var stageListField = encType.GetField("enemyEncounterList", BindingFlags.NonPublic | BindingFlags.Instance);
            stageListField?.SetValue(encData, new List<NueGames.NueDeck.Scripts.Data.Containers.EnemyEncounterStage> { 
                (NueGames.NueDeck.Scripts.Data.Containers.EnemyEncounterStage)stage 
            });

            AssetDatabase.CreateAsset(encData, assetPath);
            Debug.Log($"Created: {assetPath} (normal={enemyEncounterList.Count}, boss={bossEncounterList.Count})");
            return 1;
        }

        static int ExpandCardRewardPool()
        {
            string dir = "Assets/NueGames/NueDeck/Data/Rewards";
            
            // 创建3个新的CardRewardData（按稀有度分池）
            // 需要加载所有卡牌按稀有度分组
            var cardGuids = AssetDatabase.FindAssets("t:CardData", new[] { "Assets/NueGames/NueDeck/Data/Cards" });
            var commonCards = new List<NueGames.NueDeck.Scripts.Data.Collection.CardData>();
            var uncommonCards = new List<NueGames.NueDeck.Scripts.Data.Collection.CardData>();
            var rareCards = new List<NueGames.NueDeck.Scripts.Data.Collection.CardData>();

            foreach (var g in cardGuids)
            {
                var card = AssetDatabase.LoadAssetAtPath<NueGames.NueDeck.Scripts.Data.Collection.CardData>(AssetDatabase.GUIDToAssetPath(g));
                if (card == null) continue;
                var rarityField = card.GetType().GetField("rarity", BindingFlags.NonPublic | BindingFlags.Instance);
                var rarity = (NueGames.NueDeck.Scripts.Enums.RarityType)rarityField.GetValue(card);
                
                if (rarity == NueGames.NueDeck.Scripts.Enums.RarityType.Common) commonCards.Add(card);
                else if (rarity == NueGames.NueDeck.Scripts.Enums.RarityType.Uncommon) uncommonCards.Add(card);
                else if (rarity == NueGames.NueDeck.Scripts.Enums.RarityType.Rare) rareCards.Add(card);
            }

            Debug.Log($"Card pool: Common={commonCards.Count}, Uncommon={uncommonCards.Count}, Rare={rareCards.Count}");

            int count = 0;

            // 创建灵品卡牌奖励池
            count += CreateCardRewardData($"{dir}/Card Reward Uncommon.asset", "灵品卡牌奖励", uncommonCards);
            // 创建玄品卡牌奖励池
            count += CreateCardRewardData($"{dir}/Card Reward Rare.asset", "玄品卡牌奖励", rareCards);

            return count;
        }

        static int CreateCardRewardData(string path, string rewardDesc, List<NueGames.NueDeck.Scripts.Data.Collection.CardData> cards)
        {
            if (cards.Count == 0) return 0;
            if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(path) != null)
                AssetDatabase.DeleteAsset(path);

            var so = ScriptableObject.CreateInstance<NueGames.NueDeck.Scripts.Data.Collection.RewardData.CardRewardData>();
            var t = so.GetType();

            // 设置rewardDescription
            var descField = t.BaseType.GetField("rewardDescription", BindingFlags.NonPublic | BindingFlags.Instance);
            descField?.SetValue(so, rewardDesc);

            // 设置rewardCardList
            var listField = t.GetField("rewardCardList", BindingFlags.NonPublic | BindingFlags.Instance);
            listField?.SetValue(so, cards);

            AssetDatabase.CreateAsset(so, path);
            Debug.Log($"Created: {path} ({cards.Count} cards)");
            return 1;
        }
    }
}
