using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using NueGames.NueDeck.Scripts.Data.Collection;
using NueGames.NueDeck.Scripts.Data.Settings;
using QFramework;
using UnityEngine;

namespace CardGame
{
    /// <summary>
    /// 存档/读档系统。使用BinaryFormatter序列化到 persistentDataPath。
    /// </summary>
    public static class SaveSystem
    {
        private static readonly string SavePath = Path.Combine(Application.persistentDataPath, "xianxia_save.dat");

        /// <summary>保存游戏数据</summary>
        public static void Save()
        {
            var gm = NueGames.NueDeck.Scripts.Managers.GameManager.Instance;
            if (gm == null) { Debug.LogError("[Save] GameManager is null"); return; }

            var pd = gm.PersistentGameplayData;
            var arch = CardGameArchitecture.Interface;

            var data = new SaveData
            {
                currentGold = pd.CurrentGold,
                currentMana = pd.MaxMana,
                drawCount = pd.DrawCount,
                currentEncounterId = pd.CurrentEncounterId,
                currentStageId = pd.CurrentStageId,
                isFinalEncounter = pd.IsFinalEncounter,
                realmLevel = arch.GetModel<IRealmModel>().CurrentRealm.Value,
                maxShenShi = arch.GetModel<ILoadoutModel>().MaxShenShi.Value,
                cardIds = new List<string>(),
                unlockedRecipes = new List<string>(),
                unlockedEnemyIds = new List<string>(),
                ownedRelicIds = new List<string>(),
                relicDurabilities = new List<SaveData.RelicDurabilityEntry>(),
                ownedPotionIds = new List<string>(),
                inventoryItems = new List<SaveData.InventoryItemEntry>(),
                safeBoxItems = new List<SaveData.InventoryItemEntry>(),
                allyHealth = new List<SaveData.AllyHealthEntry>(),
                // Cultivation
                comprehensionPoints = 0,
                learnedMethodIds = new List<string>(),
                activeMethodId = "",
                unlockedNodeIds = new List<string>(),
                learnedAbilityIds = new List<string>(),
                equippedAbilityIds = new List<string>(),
                acquiredAbilityBookIds = new List<string>(),
                acquiredMethodFragmentIds = new List<string>(),
                // GameTime
                gameTimeYear = 1,
                gameTimeMonth = 1,
                gameTimeDay = 1,
                gameTimeShichen = 4,
                gameTimeTotalDays = 0,
                // WorldMap
                worldLocationId = "",
                unlockedLocations = new List<string>(),
            };

            // 卡牌
            foreach (var card in pd.CurrentCardsList)
                data.cardIds.Add(card.Id);

            // 遗物
            var relicModel = arch.GetModel<IRelicModel>();
            if (relicModel.OwnedRelics != null)
            {
                foreach (var r in relicModel.OwnedRelics)
                {
                    data.ownedRelicIds.Add(r.relicId);
                    data.relicDurabilities.Add(new SaveData.RelicDurabilityEntry
                    {
                        relicId = r.relicId,
                        currentDurability = r.currentDurability,
                        isUsed = r.isUsed
                    });
                }
            }

            // 药水
            var potionModel = arch.GetModel<IPotionModel>();
            if (potionModel.OwnedPotions != null)
                foreach (var p in potionModel.OwnedPotions)
                    if (p != null) data.ownedPotionIds.Add(p.name);

            // 灵材
            var invModel = arch.GetModel<IInventoryModel>();
            if (invModel.Slots != null)
                foreach (var slot in invModel.Slots)
                    if (slot != null && slot.item != null)
                        data.inventoryItems.Add(new SaveData.InventoryItemEntry
                        {
                            itemId = slot.item.ItemId,
                            count = slot.count
                        });

            // 乾坤袋（安全箱）
            if (invModel.SafeBoxSlots != null)
                foreach (var slot in invModel.SafeBoxSlots)
                    if (slot != null && slot.item != null)
                        data.safeBoxItems.Add(new SaveData.InventoryItemEntry
                        {
                            itemId = slot.item.ItemId,
                            count = slot.count
                        });

            // 已解锁配方
            var craftSystem = arch.GetSystem<ICraftSystem>();
            if (craftSystem != null)
                foreach (var rid in craftSystem.GetUnlockedRecipeIds())
                    data.unlockedRecipes.Add(rid);

            // 已解锁敌人图鉴
            var codexModel = arch.GetModel<IEnemyCodexModel>();
            if (codexModel != null && codexModel.UnlockedEnemyIds != null)
                foreach (var eid in codexModel.UnlockedEnemyIds)
                    data.unlockedEnemyIds.Add(eid);

            // 角色血量
            if (pd.AllyHealthDataList != null)
                foreach (var hd in pd.AllyHealthDataList)
                    data.allyHealth.Add(new SaveData.AllyHealthEntry
                    {
                        characterId = hd.CharacterId,
                        currentHealth = hd.CurrentHealth,
                        maxHealth = hd.MaxHealth
                    });

            // 功法系统
            var cultModel = arch.GetModel<ICultivationModel>();
            if (cultModel != null)
            {
                data.comprehensionPoints = cultModel.ComprehensionPoints.Value;
                data.activeMethodId = cultModel.ActiveMethodId.Value;
                data.learnedMethodIds = new List<string>(cultModel.LearnedMethodIds);
                data.unlockedNodeIds = new List<string>(cultModel.UnlockedNodeIds);
                data.learnedAbilityIds = new List<string>(cultModel.LearnedAbilityIds);
                data.equippedAbilityIds = new List<string>(cultModel.EquippedAbilityIds);
                data.acquiredAbilityBookIds = new List<string>(cultModel.AcquiredAbilityBookIds);
                data.acquiredMethodFragmentIds = new List<string>(cultModel.AcquiredMethodFragmentIds);
            }

            // 游戏时间
            var timeModel = arch.GetModel<IGameTimeModel>();
            if (timeModel != null)
            {
                data.gameTimeYear = timeModel.Year.Value;
                data.gameTimeMonth = timeModel.Month.Value;
                data.gameTimeDay = timeModel.Day.Value;
                data.gameTimeShichen = timeModel.Shichen.Value;
                data.gameTimeTotalDays = timeModel.TotalDays.Value;
            }

            // 世界地图
            var worldModel = arch.GetModel<IWorldMapModel>();
            if (worldModel != null)
            {
                data.worldLocationId = worldModel.CurrentLocationId.Value;
                data.unlockedLocations = new List<string>(worldModel.UnlockedLocationIds);
            }

            try
            {
                using (var fs = new FileStream(SavePath, FileMode.Create))
                {
                    var bf = new BinaryFormatter();
                    bf.Serialize(fs, data);
                }
                Debug.Log($"[Save] 存档保存成功: {SavePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] 存档失败: {e.Message}");
            }
        }

        /// <summary>检查是否有存档</summary>
        public static bool HasSave()
        {
            return File.Exists(SavePath);
        }

        /// <summary>加载存档</summary>
        public static bool Load()
        {
            if (!File.Exists(SavePath)) { Debug.LogWarning("[Save] 无存档"); return false; }

            var gm = NueGames.NueDeck.Scripts.Managers.GameManager.Instance;
            if (gm == null) { Debug.LogError("[Save] GameManager is null"); return false; }

            var pd = gm.PersistentGameplayData;
            var arch = CardGameArchitecture.Interface;

            try
            {
                SaveData data;
                using (var fs = new FileStream(SavePath, FileMode.Open))
                {
                    var bf = new BinaryFormatter();
                    data = (SaveData)bf.Deserialize(fs);
                }

                // 恢复基础数据
                pd.CurrentGold = data.currentGold;
                pd.MaxMana = data.currentMana;
                pd.CurrentMana = data.currentMana;
                pd.DrawCount = data.drawCount;
                pd.CurrentEncounterId = data.currentEncounterId;
                pd.CurrentStageId = data.currentStageId;
                pd.IsFinalEncounter = data.isFinalEncounter;

                // 恢复境界
                arch.GetModel<IRealmModel>().CurrentRealm.Value = data.realmLevel;
                arch.GetModel<ILoadoutModel>().MaxShenShi.Value = data.maxShenShi;

                // 恢复卡牌
                pd.CurrentCardsList.Clear();
                foreach (var cardId in data.cardIds)
                {
                    var card = gm.GameplayData.AllCardsList.Find(c => c.Id == cardId);
                    if (card != null) pd.CurrentCardsList.Add(card);
                }

                // 恢复角色血量
                pd.AllyHealthDataList.Clear();
                foreach (var hd in data.allyHealth)
                {
                    pd.SetAllyHealthData(hd.characterId, hd.currentHealth, hd.maxHealth);
                }

                // 恢复遗物（含耐久度）
                var relicModel = arch.GetModel<IRelicModel>();
                relicModel.OwnedRelics.Clear();
                var relicSystem = arch.GetSystem<IRelicSystem>();
                foreach (var relicId in data.ownedRelicIds)
                {
                    var relic = ResourceCache.GetRelics().Find(r => r.relicId == relicId);
                    if (relic != null) relicSystem.AddRelic(relic);
                }

                // 恢复遗物耐久度
                if (data.relicDurabilities != null)
                {
                    foreach (var dur in data.relicDurabilities)
                    {
                        var inst = relicModel.OwnedRelics.Find(r => r.relicId == dur.relicId);
                        if (inst != null)
                        {
                            inst.currentDurability = dur.currentDurability;
                            inst.isUsed = dur.isUsed;
                        }
                    }
                }

                // 恢复灵材
                var invModel = arch.GetModel<IInventoryModel>();
                invModel.Slots.Clear();
                var invSystem = arch.GetSystem<IInventorySystem>();
                foreach (var item in data.inventoryItems)
                {
                    var mat = ResourceCache.GetMaterials().Find(m => m.materialId == item.itemId);
                    if (mat != null) invSystem.AddItem(mat, item.count);
                }

                // 恢复乾坤袋
                if (data.safeBoxItems != null)
                {
                    invModel.SafeBoxSlots?.Clear();
                    foreach (var item in data.safeBoxItems)
                    {
                        var mat = ResourceCache.GetMaterials().Find(m => m.materialId == item.itemId);
                        if (mat != null) invSystem.AddToSafeBox(mat, item.count);
                    }
                }

                // 恢复药水
                var potionSystem = arch.GetSystem<IPotionSystem>();
                if (potionSystem != null)
                {
                    potionSystem.ClearPotions();
                    foreach (var potionId in data.ownedPotionIds)
                    {
                        var potion = ResourceCache.GetPotions().Find(p => p.name == potionId || p.potionId == potionId);
                        if (potion != null) potionSystem.ObtainPotion(potion);
                    }
                }

                // 恢复已解锁配方
                var craftSystem = arch.GetSystem<ICraftSystem>();
                if (craftSystem != null && data.unlockedRecipes != null)
                {
                    foreach (var rid in data.unlockedRecipes)
                        craftSystem.UnlockRecipe(rid);
                }

                // 恢复敌人图鉴
                var codexSystem = arch.GetSystem<IEnemyCodexSystem>();
                if (codexSystem != null && data.unlockedEnemyIds != null)
                {
                    foreach (var eid in data.unlockedEnemyIds)
                        codexSystem.OnEncounter(eid);
                }

                // 恢复功法系统
                var cultModel2 = arch.GetModel<ICultivationModel>();
                if (cultModel2 != null)
                {
                    cultModel2.ComprehensionPoints.Value = data.comprehensionPoints;
                    cultModel2.ActiveMethodId.Value = data.activeMethodId ?? "";
                    cultModel2.LearnedMethodIds.Clear();
                    if (data.learnedMethodIds != null)
                        foreach (var id in data.learnedMethodIds) cultModel2.LearnedMethodIds.Add(id);
                    cultModel2.UnlockedNodeIds.Clear();
                    if (data.unlockedNodeIds != null)
                        foreach (var id in data.unlockedNodeIds) cultModel2.UnlockedNodeIds.Add(id);
                    cultModel2.LearnedAbilityIds.Clear();
                    if (data.learnedAbilityIds != null)
                        foreach (var id in data.learnedAbilityIds) cultModel2.LearnedAbilityIds.Add(id);
                    cultModel2.EquippedAbilityIds.Clear();
                    if (data.equippedAbilityIds != null)
                        foreach (var id in data.equippedAbilityIds) cultModel2.EquippedAbilityIds.Add(id);
                    cultModel2.AcquiredAbilityBookIds.Clear();
                    if (data.acquiredAbilityBookIds != null)
                        foreach (var id in data.acquiredAbilityBookIds) cultModel2.AcquiredAbilityBookIds.Add(id);
                    cultModel2.AcquiredMethodFragmentIds.Clear();
                    if (data.acquiredMethodFragmentIds != null)
                        foreach (var id in data.acquiredMethodFragmentIds) cultModel2.AcquiredMethodFragmentIds.Add(id);
                }

                // 恢复游戏时间
                var timeModel = arch.GetModel<IGameTimeModel>();
                if (timeModel != null)
                {
                    timeModel.Year.Value = data.gameTimeYear;
                    timeModel.Month.Value = data.gameTimeMonth;
                    timeModel.Day.Value = data.gameTimeDay;
                    timeModel.Shichen.Value = data.gameTimeShichen;
                    timeModel.TotalDays.Value = data.gameTimeTotalDays;
                }

                // 恢复世界地图
                var worldModel = arch.GetModel<IWorldMapModel>();
                if (worldModel != null)
                {
                    worldModel.CurrentLocationId.Value = data.worldLocationId ?? "";
                    worldModel.UnlockedLocationIds.Clear();
                    if (data.unlockedLocations != null)
                        foreach (var id in data.unlockedLocations) worldModel.UnlockedLocationIds.Add(id);
                }

                Debug.Log("[Save] 存档加载成功");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] 读档失败: {e.Message}");
                return false;
            }
        }

        /// <summary>删除存档</summary>
        public static void DeleteSave()
        {
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
                Debug.Log("[Save] 存档已删除");
            }
        }
    }

    [Serializable]
    public class SaveData
    {
        public int currentGold;
        public int currentMana;
        public int drawCount;
        public int currentEncounterId;
        public int currentStageId;
        public bool isFinalEncounter;
        public int realmLevel;
        public int maxShenShi;

        public List<string> cardIds;
        public List<string> ownedRelicIds;
        public List<RelicDurabilityEntry> relicDurabilities;
        public List<string> ownedPotionIds;
        public List<string> unlockedRecipes;
        public List<string> unlockedEnemyIds;
        public List<InventoryItemEntry> inventoryItems;
        public List<InventoryItemEntry> safeBoxItems;
        public List<AllyHealthEntry> allyHealth;

        // Cultivation
        public int comprehensionPoints;
        public string activeMethodId;
        public List<string> learnedMethodIds;
        public List<string> unlockedNodeIds;
        public List<string> learnedAbilityIds;
        public List<string> equippedAbilityIds;
        public List<string> acquiredAbilityBookIds;
        public List<string> acquiredMethodFragmentIds;

        // GameTime
        public int gameTimeYear;
        public int gameTimeMonth;
        public int gameTimeDay;
        public int gameTimeShichen;
        public int gameTimeTotalDays;

        // WorldMap
        public string worldLocationId;
        public List<string> unlockedLocations;

        [Serializable]
        public class InventoryItemEntry
        {
            public string itemId;
            public int count;
        }

        [Serializable]
        public class AllyHealthEntry
        {
            public string characterId;
            public int currentHealth;
            public int maxHealth;
        }

        [Serializable]
        public class RelicDurabilityEntry
        {
            public string relicId;
            public int currentDurability;
            public bool isUsed;
        }
    }
}
