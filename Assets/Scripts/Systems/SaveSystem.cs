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
                ownedPotionIds = new List<string>(),
                inventoryItems = new List<SaveData.InventoryItemEntry>(),
                allyHealth = new List<SaveData.AllyHealthEntry>(),
            };

            // 卡牌
            foreach (var card in pd.CurrentCardsList)
                data.cardIds.Add(card.Id);

            // 遗物
            var relicModel = arch.GetModel<IRelicModel>();
            if (relicModel.OwnedRelics != null)
                foreach (var r in relicModel.OwnedRelics)
                    data.ownedRelicIds.Add(r.relicId);

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

            // 角色血量
            if (pd.AllyHealthDataList != null)
                foreach (var hd in pd.AllyHealthDataList)
                    data.allyHealth.Add(new SaveData.AllyHealthEntry
                    {
                        characterId = hd.CharacterId,
                        currentHealth = hd.CurrentHealth,
                        maxHealth = hd.MaxHealth
                    });

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

                // 恢复遗物
                var relicModel = arch.GetModel<IRelicModel>();
                relicModel.OwnedRelics.Clear();
                var relicSystem = arch.GetSystem<IRelicSystem>();
                foreach (var relicId in data.ownedRelicIds)
                {
                    var relic = ResourceCache.GetRelics().Find(r => r.relicId == relicId);
                    if (relic != null) relicSystem.AddRelic(relic);
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
        public List<string> ownedPotionIds;
        public List<string> unlockedRecipes;
        public List<string> unlockedEnemyIds;
        public List<InventoryItemEntry> inventoryItems;
        public List<AllyHealthEntry> allyHealth;

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
    }
}
