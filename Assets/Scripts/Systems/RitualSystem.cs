using System.Collections.Generic;
using System.Linq;
using QFramework;
using UnityEngine;

namespace CardGame
{
    /// <summary>
    /// 祭祀抽奖系统：
    /// - 献祭材料类型决定产出类型（灵草→丹药，矿石→法宝，妖丹→卡牌，魂石→配方，混合→随机）
    /// - 材料品阶总和决定产出品阶
    /// - 10%概率+1品阶，5%概率+2品阶
    /// </summary>
    public class RitualSystem : AbstractSystem, IRitualSystem
    {
        protected IInventorySystem InventorySystem => this.GetSystem<IInventorySystem>();
        protected IRelicSystem RelicSystem => this.GetSystem<IRelicSystem>();
        protected IPotionSystem PotionSystem => this.GetSystem<IPotionSystem>();
        protected ICraftSystem CraftSystem => this.GetSystem<ICraftSystem>();

        protected override void OnInit() {}

        // 材料品阶 → 数值
        static int RarityToValue(MaterialRarity r) => r switch
        {
            MaterialRarity.FanPin => 0,
            MaterialRarity.LingPin => 1,
            MaterialRarity.XuanPin => 2,
            MaterialRarity.XianPin => 3,
            _ => 0
        };

        static string RarityName(int level) => level switch
        {
            0 => "黄", 1 => "玄", 2 => "地", 3 => "天", _ => "黄"
        };

        public int PreviewRarityLevel(List<(MaterialData material, int count)> offerings)
        {
            if (offerings == null || offerings.Count == 0) return 0;
            int totalValue = 0, totalCount = 0;
            foreach (var (mat, cnt) in offerings)
            {
                totalValue += RarityToValue(mat.rarity) * cnt;
                totalCount += cnt;
            }
            if (totalCount == 0) return 0;
            return Mathf.Clamp(totalValue / totalCount, 0, 3);
        }

        public string PreviewOutputType(List<(MaterialData material, int count)> offerings)
        {
            if (offerings == null || offerings.Count == 0) return "无";
            var types = offerings.Select(o => o.material.materialType).Distinct().ToList();
            if (types.Count == 1)
                return MaterialTypeToOutputName(types[0]);
            return "随机";
        }

        public RitualResult Sacrifice(List<(MaterialData material, int count)> offerings)
        {
            if (offerings == null || offerings.Count == 0) return null;

            // 1. 消耗材料
            foreach (var (mat, cnt) in offerings)
                InventorySystem.RemoveItem(mat.materialId, cnt);

            // 2. 计算品阶
            int baseLevel = PreviewRarityLevel(offerings);

            // 3. roll品阶提升
            int finalLevel = baseLevel;
            int luckyUp = 0;
            float roll = Random.value;
            if (roll < 0.05f) { luckyUp = 2; finalLevel = Mathf.Min(baseLevel + 2, 3); }
            else if (roll < 0.15f) { luckyUp = 1; finalLevel = Mathf.Min(baseLevel + 1, 3); }

            // 4. 决定产出类型
            var types = offerings.Select(o => o.material.materialType).Distinct().ToList();
            MaterialType outputMaterialType;
            if (types.Count == 1)
                outputMaterialType = types[0];
            else
                outputMaterialType = types[Random.Range(0, types.Count)];

            // 5. 抽奖
            var result = new RitualResult
            {
                rarityLevel = finalLevel,
                isLuckyUp = luckyUp > 0,
                luckyUpLevels = luckyUp
            };

            GrantReward(outputMaterialType, finalLevel, result);
            return result;
        }

        void GrantReward(MaterialType matType, int rarityLevel, RitualResult result)
        {
            switch (matType)
            {
                case MaterialType.SpiritHerb:
                    // 灵草 → 丹药
                    GrantPotion(rarityLevel, result);
                    break;
                case MaterialType.Ore:
                case MaterialType.HeavenlyTreasure:
                    // 矿石/天材 → 法宝
                    GrantRelic(rarityLevel, result);
                    break;
                case MaterialType.DemonCore:
                    // 妖丹 → 卡牌
                    GrantCard(rarityLevel, result);
                    break;
                case MaterialType.SoulStone:
                    // 魂石 → 配方
                    GrantRecipe(rarityLevel, result);
                    break;
                case MaterialType.SpiritWood:
                case MaterialType.BeastBone:
                case MaterialType.SpiritWater:
                case MaterialType.Fragment:
                    // 灵木/灵兽骨/灵水/残片 → 材料
                    GrantMaterial(rarityLevel, result);
                    break;
                default:
                    // 随机选一个
                    int r = Random.Range(0, 4);
                    if (r == 0) GrantPotion(rarityLevel, result);
                    else if (r == 1) GrantRelic(rarityLevel, result);
                    else if (r == 2) GrantCard(rarityLevel, result);
                    else GrantMaterial(rarityLevel, result);
                    break;
            }
        }

        void GrantPotion(int level, RitualResult result)
        {
            result.itemTypeName = "丹药";
            // PotionRarity 只有 Common/Uncommon/Rare，level 3 降级到 Rare
            var targetRarity = level switch
            {
                0 => PotionRarity.Common,
                1 => PotionRarity.Uncommon,
                _ => PotionRarity.Rare // level 2 和 3 都用 Rare
            };
            var pool = ResourceCache.GetPotions().FindAll(p => p.rarity == targetRarity);
            if (pool.Count == 0)
            {
                for (int l = level - 1; l >= 0; l--)
                {
                    var fallback = l switch { 0 => PotionRarity.Common, 1 => PotionRarity.Uncommon, _ => PotionRarity.Rare };
                    pool = ResourceCache.GetPotions().FindAll(p => p.rarity == fallback);
                    if (pool.Count > 0) break;
                }
            }
            if (pool.Count == 0) pool = ResourceCache.GetPotions();
            if (pool.Count == 0) { result.itemName = "无"; return; }

            var potion = pool[Random.Range(0, pool.Count)];
            PotionSystem.ObtainPotion(potion);
            result.itemName = potion.name;
            Debug.Log($"[Ritual] 丹药抽奖: {potion.name} ({RarityName(level)}) luckyUp={result.luckyUpLevels}");
        }

        void GrantRelic(int level, RitualResult result)
        {
            result.itemTypeName = "法宝";
            // RelicRarity 只有 Common/Uncommon/Rare/Boss/Shop，level 0-3 映射
            var targetRarity = level switch
            {
                0 => RelicRarity.Common,
                1 => RelicRarity.Uncommon,
                2 => RelicRarity.Rare,
                3 => RelicRarity.Boss, // 仙品→Boss级法宝
                _ => RelicRarity.Common
            };
            var pool = ResourceCache.GetRelics().FindAll(r => r.rarity == targetRarity);
            // 如果该品阶池为空，降级找最近的
            if (pool.Count == 0)
            {
                for (int l = level - 1; l >= 0; l--)
                {
                    var fallback = l switch { 0 => RelicRarity.Common, 1 => RelicRarity.Uncommon, 2 => RelicRarity.Rare, _ => RelicRarity.Common };
                    pool = ResourceCache.GetRelics().FindAll(r => r.rarity == fallback);
                    if (pool.Count > 0) break;
                }
            }
            if (pool.Count == 0) pool = ResourceCache.GetRelics();
            if (pool.Count == 0) { result.itemName = "无"; return; }

            var relic = pool[Random.Range(0, pool.Count)];
            RelicSystem.AddRelic(relic);
            result.itemName = relic.name;
            Debug.Log($"[Ritual] 法宝抽奖: {relic.name} ({RarityName(level)}) luckyUp={result.luckyUpLevels}");
        }

        void GrantCard(int level, RitualResult result)
        {
            result.itemTypeName = "卡牌";
            // RarityType: 0=Common, 1=Uncommon, 2=Rare, 3=Legendary
            var targetRarity = (NueGames.NueDeck.Scripts.Enums.RarityType)Mathf.Min(level, 3);
            var pool = ResourceCache.GetCardsFromAllList().FindAll(c => c.Rarity == targetRarity);
            // 降级查找
            if (pool.Count == 0)
            {
                for (int l = level - 1; l >= 0; l--)
                {
                    pool = ResourceCache.GetCardsFromAllList().FindAll(c => c.Rarity == (NueGames.NueDeck.Scripts.Enums.RarityType)l);
                    if (pool.Count > 0) break;
                }
            }
            if (pool.Count == 0) pool = ResourceCache.GetCardsFromAllList();
            if (pool.Count == 0) { result.itemName = "无"; return; }

            var card = pool[Random.Range(0, pool.Count)];
            var gm = NueGames.NueDeck.Scripts.Managers.GameManager.Instance;
            if (gm != null)
                gm.PersistentGameplayData.CurrentCardsList.Add(card);
            result.itemName = card.CardName;
            Debug.Log($"[Ritual] 卡牌抽奖: {card.CardName} ({RarityName(level)}) luckyUp={result.luckyUpLevels}");
        }

        void GrantRecipe(int level, RitualResult result)
        {
            result.itemTypeName = "配方";
            var allRecipes = ResourceCache.GetRecipes();
            // 按产出类型分组，选一个未解锁的
            var locked = allRecipes.Where(r => !CraftSystem.IsRecipeUnlocked(r.recipeId)).ToList();
            if (locked.Count == 0)
            {
                // 全部解锁了 → 给材料替代
                GrantMaterial(level, result);
                return;
            }
            var recipe = locked[Random.Range(0, locked.Count)];
            CraftSystem.UnlockRecipe(recipe.recipeId);
            result.itemName = recipe.name;
            Debug.Log($"[Ritual] 配方抽奖: {recipe.name} luckyUp={result.luckyUpLevels}");
        }

        void GrantMaterial(int level, RitualResult result)
        {
            result.itemTypeName = "材料";
            var targetRarity = (MaterialRarity)Mathf.Min(level, 3);
            var pool = ResourceCache.GetMaterials().FindAll(m => m.rarity == targetRarity);
            if (pool.Count == 0) pool = ResourceCache.GetMaterials();
            if (pool.Count == 0) { result.itemName = "无"; return; }

            var mat = pool[Random.Range(0, pool.Count)];
            InventorySystem.AddItem(mat, 1);
            result.itemName = mat.name;
            Debug.Log($"[Ritual] 材料抽奖: {mat.name} ({RarityName(level)}) luckyUp={result.luckyUpLevels}");
        }

        string MaterialTypeToOutputName(MaterialType t) => t switch
        {
            MaterialType.SpiritHerb => "丹药",
            MaterialType.Ore => "法宝",
            MaterialType.HeavenlyTreasure => "法宝",
            MaterialType.DemonCore => "卡牌",
            MaterialType.SoulStone => "配方",
            MaterialType.SpiritWood => "材料",
            MaterialType.BeastBone => "材料",
            MaterialType.SpiritWater => "材料",
            MaterialType.Fragment => "材料",
            _ => "随机"
        };
    }
}
