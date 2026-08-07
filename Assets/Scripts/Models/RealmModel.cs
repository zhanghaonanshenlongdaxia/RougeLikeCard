using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace CardGame
{
    /// <summary>
    /// 境界枚举
    /// </summary>
    public enum RealmLevel
    {
        LianQi = 0,     // 练气期
        ZhuJi = 1,      // 筑基期
        JinDan = 2,     // 金丹期
        YuanYing = 3,   // 元婴期
        HuaShen = 4,    // 化神期
        DuJie = 5       // 渡劫期
    }

    /// <summary>
    /// 境界突破条件
    /// </summary>
    [System.Serializable]
    public class RealmBreakthroughRequirement
    {
        public RealmLevel targetRealm;
        public string realmName;
        public int goldCost;              // 灵石
        public string materialRarity;     // 所需材料品阶 "LingPin"/"XuanPin"/"XianPin"/"HeavenlyTreasure"
        public int materialCount;         // 材料数量
        public string requiredPotionId;   // 所需丹药ID
        public int hpBonus;               // HP加成
        public int shenShiBonus;          // 神识上限加成
        public string description;        // 境界描述
    }

    /// <summary>
    /// 境界数据模型
    /// </summary>
    public interface IRealmModel : IModel
    {
        BindableProperty<int> CurrentRealm { get; }      // 当前境界等级(RealmLevel的int值)
        BindableProperty<int> RealmHpBonus { get; }      // 累计HP加成
        BindableProperty<int> RealmShenShiBonus { get; }  // 累计神识加成
        List<RealmBreakthroughRequirement> RealmTable { get; }
    }

    public class RealmModel : AbstractModel, IRealmModel
    {
        public BindableProperty<int> CurrentRealm { get; } = new BindableProperty<int>(0);
        public BindableProperty<int> RealmHpBonus { get; } = new BindableProperty<int>(0);
        public BindableProperty<int> RealmShenShiBonus { get; } = new BindableProperty<int>(0);
        public List<RealmBreakthroughRequirement> RealmTable { get; } = new List<RealmBreakthroughRequirement>();

        protected override void OnInit()
        {
            // 境界突破表
            RealmTable.Add(new RealmBreakthroughRequirement {
                targetRealm = RealmLevel.ZhuJi, realmName = "筑基期",
                goldCost = 100, materialRarity = "LingPin", materialCount = 5,
                requiredPotionId = "potion_zhuji", hpBonus = 30, shenShiBonus = 5,
                description = "筑基铸根，灵力初凝。从此踏上修仙正途。"
            });
            RealmTable.Add(new RealmBreakthroughRequirement {
                targetRealm = RealmLevel.JinDan, realmName = "金丹期",
                goldCost = 300, materialRarity = "XuanPin", materialCount = 5,
                requiredPotionId = "potion_jindan", hpBonus = 50, shenShiBonus = 10,
                description = "金丹大成，灵力凝实。可御剑飞行，翻江倒海。"
            });
            RealmTable.Add(new RealmBreakthroughRequirement {
                targetRealm = RealmLevel.YuanYing, realmName = "元婴期",
                goldCost = 800, materialRarity = "XianPin", materialCount = 3,
                requiredPotionId = "potion_yuanying", hpBonus = 80, shenShiBonus = 15,
                description = "元婴出窍，神游太虚。寿元千载，挥手灭城。"
            });
            RealmTable.Add(new RealmBreakthroughRequirement {
                targetRealm = RealmLevel.HuaShen, realmName = "化神期",
                goldCost = 2000, materialRarity = "XianPin", materialCount = 8,
                requiredPotionId = "potion_huashen", hpBonus = 120, shenShiBonus = 20,
                description = "化神归元，神通自成。天地法则，信手拈来。"
            });
            RealmTable.Add(new RealmBreakthroughRequirement {
                targetRealm = RealmLevel.DuJie, realmName = "渡劫期",
                goldCost = 5000, materialRarity = "HeavenlyTreasure", materialCount = 5,
                requiredPotionId = "potion_dujie", hpBonus = 200, shenShiBonus = 30,
                description = "渡劫飞升，超脱轮回。天雷洗礼，成就真仙。"
            });
        }
    }
}
