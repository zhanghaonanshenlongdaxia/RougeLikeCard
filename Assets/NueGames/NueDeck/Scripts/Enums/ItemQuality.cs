using CardGame;
using UnityEngine;

namespace NueGames.NueDeck.Scripts.Enums
{
    /// <summary>
    /// 统一品质系统 — 5个境界 × 4个品阶 = 20个品质等级。
    /// 练气: 灰/白/绿/蓝
    /// 筑基: 紫/金/橙/红
    /// 结丹: 红+特效(基础) × 4种颜色
    /// 元婴: 红+特效(更华丽) × 4种颜色
    /// 化神: 红+特效(最华丽) × 4种颜色
    /// 渡劫: 无物品
    /// </summary>
    public enum ItemQuality
    {
        // 练气期 (灰/白/绿/蓝)
        LianQi_T1 = 0,
        LianQi_T2 = 1,
        LianQi_T3 = 2,
        LianQi_T4 = 3,
        // 筑基期 (紫/金/橙/红)
        ZhuJi_T1 = 4,
        ZhuJi_T2 = 5,
        ZhuJi_T3 = 6,
        ZhuJi_T4 = 7,
        // 结丹期 (红+基础特效, 4色)
        JinDan_T1 = 8,
        JinDan_T2 = 9,
        JinDan_T3 = 10,
        JinDan_T4 = 11,
        // 元婴期 (红+华丽特效, 4色)
        YuanYing_T1 = 12,
        YuanYing_T2 = 13,
        YuanYing_T3 = 14,
        YuanYing_T4 = 15,
        // 化神期 (红+最华丽特效, 4色)
        HuaShen_T1 = 16,
        HuaShen_T2 = 17,
        HuaShen_T3 = 18,
        HuaShen_T4 = 19,
    }

    /// <summary>
    /// 品质辅助工具
    /// </summary>
    public static class ItemQualityHelper
    {
        private static readonly Color[] Colors =
        {
            // 练气: 灰/白/绿/蓝
            new Color(0.5f, 0.5f, 0.5f),    // LianQi_T1 灰
            new Color(0.9f, 0.9f, 0.9f),    // LianQi_T2 白
            new Color(0.3f, 0.8f, 0.3f),    // LianQi_T3 绿
            new Color(0.3f, 0.5f, 0.9f),    // LianQi_T4 蓝
            // 筑基: 紫/金/橙/红
            new Color(0.6f, 0.3f, 0.9f),    // ZhuJi_T1 紫
            new Color(0.9f, 0.8f, 0.2f),    // ZhuJi_T2 金
            new Color(0.9f, 0.6f, 0.2f),    // ZhuJi_T3 橙
            new Color(0.9f, 0.3f, 0.3f),    // ZhuJi_T4 红
            // 结丹: 红+基础特效, 4色 (红底, 特效颜色区分)
            new Color(0.9f, 0.3f, 0.3f),    // JinDan_T1
            new Color(0.9f, 0.4f, 0.3f),    // JinDan_T2
            new Color(0.9f, 0.3f, 0.4f),    // JinDan_T3
            new Color(0.9f, 0.5f, 0.3f),    // JinDan_T4
            // 元婴: 红+更华丽特效, 4色
            new Color(0.9f, 0.3f, 0.5f),    // YuanYing_T1
            new Color(0.9f, 0.5f, 0.4f),    // YuanYing_T2
            new Color(0.8f, 0.3f, 0.6f),    // YuanYing_T3
            new Color(0.9f, 0.6f, 0.3f),    // YuanYing_T4
            // 化神: 红+最华丽特效, 4色
            new Color(1f, 0.3f, 0.6f),      // HuaShen_T1
            new Color(1f, 0.5f, 0.3f),      // HuaShen_T2
            new Color(0.9f, 0.4f, 0.7f),    // HuaShen_T3
            new Color(1f, 0.6f, 0.4f),      // HuaShen_T4
        };

        private static readonly string[] DisplayNames =
        {
            "练气·1", "练气·2", "练气·3", "练气·4",
            "筑基·5", "筑基·6", "筑基·7", "筑基·8",
            "结丹·9", "结丹·10", "结丹·11", "结丹·12",
            "元婴·13", "元婴·14", "元婴·15", "元婴·16",
            "化神·17", "化神·18", "化神·19", "化神·20",
        };

        /// <summary>获取品质对应的境界</summary>
        public static RealmLevel GetRealm(ItemQuality quality)
        {
            int v = (int)quality;
            if (v <= 3) return RealmLevel.LianQi;
            if (v <= 7) return RealmLevel.ZhuJi;
            if (v <= 11) return RealmLevel.JinDan;
            if (v <= 15) return RealmLevel.YuanYing;
            return RealmLevel.HuaShen;
        }

        /// <summary>获取品质在当前境界内的品阶 (1-4)</summary>
        public static int GetTier(ItemQuality quality)
        {
            return (int)quality % 4 + 1;
        }

        /// <summary>获取品阶数字标签 (①②③④)</summary>
        public static string GetTierLabel(ItemQuality quality)
        {
            return GetTier(quality) switch
            {
                1 => "①", 2 => "②", 3 => "③", 4 => "④", _ => ""
            };
        }

        /// <summary>获取品质对应的颜色</summary>
        public static Color GetColor(ItemQuality quality)
        {
            return Colors[(int)quality];
        }

        /// <summary>获取品质显示名</summary>
        public static string GetDisplayName(ItemQuality quality)
        {
            return DisplayNames[(int)quality];
        }

        /// <summary>根据境界和品阶获取品质</summary>
        public static ItemQuality FromRealmAndTier(RealmLevel realm, int tier)
        {
            int baseVal = (int)realm * 4;
            return (ItemQuality)(baseVal + (tier - 1));
        }

        /// <summary>旧 RarityType → ItemQuality 映射 (Common=LianQi_T1, Uncommon=LianQi_T2, Rare=LianQi_T3, Legendary=LianQi_T4)</summary>
        public static ItemQuality FromOldRarity(RarityType old)
        {
            return old switch
            {
                RarityType.Common => ItemQuality.LianQi_T1,
                RarityType.Uncommon => ItemQuality.LianQi_T2,
                RarityType.Rare => ItemQuality.LianQi_T3,
                RarityType.Legendary => ItemQuality.LianQi_T4,
                _ => ItemQuality.LianQi_T1
            };
        }

        /// <summary>旧 MaterialRarity → ItemQuality 映射</summary>
        public static ItemQuality FromOldMaterialRarity(CardGame.MaterialRarity old)
        {
            return old switch
            {
                MaterialRarity.FanPin => ItemQuality.LianQi_T1,
                MaterialRarity.LingPin => ItemQuality.LianQi_T2,
                MaterialRarity.XuanPin => ItemQuality.LianQi_T3,
                MaterialRarity.XianPin => ItemQuality.LianQi_T4,
                _ => ItemQuality.LianQi_T1
            };
        }
    }
}
