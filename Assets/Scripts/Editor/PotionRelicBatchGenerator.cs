using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;

namespace CardGame.Editor
{
    public static class PotionRelicBatchGenerator
    {
        [MenuItem("Tools/Generate Potions & Relics")]
        public static void GenerateAll()
        {
            int potionCount = GeneratePotions();
            int relicCount = GenerateRelics();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"丹药法宝生成完成: {potionCount}个丹药 + {relicCount}个法宝 = {potionCount + relicCount}个");
        }

        static int GeneratePotions()
        {
            string dir = "Assets/NueGames/NueDeck/Data/Potions";
            if (!AssetDatabase.IsValidFolder(dir))
                AssetDatabase.CreateFolder("Assets/NueGames/NueDeck/Data", "Potions");

            int count = 0;

            // 药水数据：50个，分3种稀有度
            var potions = new[]
            {
                // ===== 凡品(Common) - 20个 =====
                P("potion_heal_s", "回血丹", "回复15点生命值", 15, PotionRarity.Common, PotionTargetType.None),
                P("potion_heal_m", "疗伤丹", "回复25点生命值", 25, PotionRarity.Common, PotionTargetType.None),
                P("potion_block_s", "护体丹", "获得10点格挡", 10, PotionRarity.Common, PotionTargetType.None),
                P("potion_block_m", "铁壁丹", "获得15点格挡", 15, PotionRarity.Common, PotionTargetType.None),
                P("potion_energy_s", "灵力丹", "恢复1点灵力", 1, PotionRarity.Common, PotionTargetType.None),
                P("potion_energy_m", "凝灵丹", "恢复2点灵力", 2, PotionRarity.Common, PotionTargetType.None),
                P("potion_str_s", "力壮丹", "获得1层力量", 1, PotionRarity.Common, PotionTargetType.None),
                P("potion_str_m", "蛮力丹", "获得2层力量", 2, PotionRarity.Common, PotionTargetType.None),
                P("potion_dex_s", "轻身丹", "获得1层敏捷", 1, PotionRarity.Common, PotionTargetType.None),
                P("potion_draw_s", "明心丹", "抽1张牌", 1, PotionRarity.Common, PotionTargetType.None),
                P("potion_draw_m", "开窍丹", "抽2张牌", 2, PotionRarity.Common, PotionTargetType.None),
                P("potion_weak_e", "蚀骨散", "对敌人施加2层虚弱", 2, PotionRarity.Common, PotionTargetType.Enemy),
                P("potion_frail_e", "碎甲散", "对敌人施加2层脆弱", 2, PotionRarity.Common, PotionTargetType.Enemy),
                P("potion_vuln_e", "破绽散", "对敌人施加2层易伤", 2, PotionRarity.Common, PotionTargetType.Enemy),
                P("potion_weak_all", "迷瘴散", "对全体敌人施加1层虚弱", 1, PotionRarity.Common, PotionTargetType.AllEnemies),
                P("potion_poison_e", "蛊毒丹", "对敌人施加5层中毒", 5, PotionRarity.Common, PotionTargetType.Enemy),
                P("potion_dmg_e", "雷火丹", "对敌人造成15点伤害", 15, PotionRarity.Common, PotionTargetType.Enemy),
                P("potion_dmg_all", "天雷符", "对全体敌人造成8点伤害", 8, PotionRarity.Common, PotionTargetType.AllEnemies),
                P("potion_thorn_s", "荆棘丹", "获得3层反伤", 3, PotionRarity.Common, PotionTargetType.None),
                P("potion_stun_e", "定身符", "使敌人眩晕1回合", 1, PotionRarity.Common, PotionTargetType.Enemy),

                // ===== 灵品(Uncommon) - 20个 =====
                P("potion_heal_l", "九转还魂丹", "回复40点生命值", 40, PotionRarity.Uncommon, PotionTargetType.None),
                P("potion_heal_full", "造化还丹", "回复全部生命值", 999, PotionRarity.Uncommon, PotionTargetType.None),
                P("potion_block_l", "金钟罩丹", "获得30点格挡", 30, PotionRarity.Uncommon, PotionTargetType.None),
                P("potion_str_l", "龙力丹", "获得4层力量", 4, PotionRarity.Uncommon, PotionTargetType.None),
                P("potion_dex_l", "灵猿丹", "获得4层敏捷", 4, PotionRarity.Uncommon, PotionTargetType.None),
                P("potion_energy_l", "聚灵丹", "恢复3点灵力", 3, PotionRarity.Uncommon, PotionTargetType.None),
                P("potion_draw_l", "天眼丹", "抽3张牌", 3, PotionRarity.Uncommon, PotionTargetType.None),
                P("potion_str_dex", "混元丹", "获得2层力量和2层敏捷", 2, PotionRarity.Uncommon, PotionTargetType.None),
                P("potion_block_str", "攻守丹", "获得15格挡和2层力量", 15, PotionRarity.Uncommon, PotionTargetType.None),
                P("potion_weak_all2", "万蛊散", "对全体敌人施加3层虚弱", 3, PotionRarity.Uncommon, PotionTargetType.AllEnemies),
                P("potion_vuln_all", "天眼符", "对全体敌人施加2层易伤", 2, PotionRarity.Uncommon, PotionTargetType.AllEnemies),
                P("potion_poison_l", "万毒丹", "对敌人施加12层中毒", 12, PotionRarity.Uncommon, PotionTargetType.Enemy),
                P("potion_dmg_l", "天火符", "对敌人造成30点伤害", 30, PotionRarity.Uncommon, PotionTargetType.Enemy),
                P("potion_dmg_all_l", "九霄雷符", "对全体敌人造成15点伤害", 15, PotionRarity.Uncommon, PotionTargetType.AllEnemies),
                P("potion_thorn_l", "万棘丹", "获得6层反伤", 6, PotionRarity.Uncommon, PotionTargetType.None),
                P("potion_maxhp_s", "培元丹", "永久增加5点最大生命", 5, PotionRarity.Uncommon, PotionTargetType.None),
                P("potion_maxhp_m", "固本丹", "永久增加10点最大生命", 10, PotionRarity.Uncommon, PotionTargetType.None),
                P("potion_mana_perm", "凝元丹", "永久增加1点灵力上限", 1, PotionRarity.Uncommon, PotionTargetType.None),
                P("potion_gold", "招财符", "获得50灵石", 50, PotionRarity.Uncommon, PotionTargetType.None),
                P("potion_cleanse", "净心丹", "清除所有负面状态", 0, PotionRarity.Uncommon, PotionTargetType.None),

                // ===== 玄品(Rare) - 10个 =====
                P("potion_heal_full_r", "太乙仙丹", "回复全部生命并增加10点最大生命", 10, PotionRarity.Rare, PotionTargetType.None),
                P("potion_block_huge", "不动明王丹", "获得50点格挡", 50, PotionRarity.Rare, PotionTargetType.None),
                P("potion_str_huge", "盘古丹", "获得8层力量", 8, PotionRarity.Rare, PotionTargetType.None),
                P("potion_dex_huge", "女娲丹", "获得8层敏捷", 8, PotionRarity.Rare, PotionTargetType.None),
                P("potion_energy_huge", "混沌丹", "恢复5点灵力", 5, PotionRarity.Rare, PotionTargetType.None),
                P("potion_draw_huge", "悟道丹", "抽5张牌", 5, PotionRarity.Rare, PotionTargetType.None),
                P("potion_dmg_huge", "诛仙符", "对敌人造成60点伤害", 60, PotionRarity.Rare, PotionTargetType.Enemy),
                P("potion_dmg_all_huge", "灭世雷劫", "对全体敌人造成30点伤害", 30, PotionRarity.Rare, PotionTargetType.AllEnemies),
                P("potion_poison_huge", "万蛊噬魂丹", "对敌人施加25层中毒", 25, PotionRarity.Rare, PotionTargetType.Enemy),
                P("potion_omni", "太极丹", "获得5层力量、5层敏捷、5层反伤", 5, PotionRarity.Rare, PotionTargetType.None),
            };

            foreach (var (id, name, desc, val, rar, target) in potions)
            {
                string path = $"{dir}/{id}.asset";
                // 跳过已存在的（保留旧的5个）
                if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(path) != null) continue;
                
                var so = ScriptableObject.CreateInstance<PotionData>();
                so.potionId = id;
                so.name = name;
                so.description = desc;
                so.effectValue = val;
                so.rarity = rar;
                so.targetType = target;
                AssetDatabase.CreateAsset(so, path);
                count++;
            }
            return count;
        }

        static int GenerateRelics()
        {
            string dir = "Assets/NueGames/NueDeck/Data/Relics";
            if (!AssetDatabase.IsValidFolder(dir))
                AssetDatabase.CreateFolder("Assets/NueGames/NueDeck/Data", "Relics");

            int count = 0;

            var relics = new[]
            {
                // ===== 凡品(Common) - 20个 =====
                R("relic_attack_cost", "破军剑心", "每打出一张攻击牌, 获得1层力量", RelicTriggerType.OnAttackPlayed, 1, false, RelicRarity.Common),
                R("relic_skill_draw", "悟道玉佩", "每打出一张技能牌, 抽1张牌", RelicTriggerType.OnSkillPlayed, 1, false, RelicRarity.Common),
                R("relic_turn_heal", "生生不息", "每回合结束时回复2点生命", RelicTriggerType.OnTurnEnd, 2, false, RelicRarity.Common),
                R("relic_turn_block", "金钟罩", "每回合开始时获得3点格挡", RelicTriggerType.OnTurnStart, 3, false, RelicRarity.Common),
                R("relic_draw_extra", "灵目戒", "每回合开始时多抽1张牌", RelicTriggerType.OnTurnStart, 1, false, RelicRarity.Common),
                R("relic_energy_extra", "聚灵珠", "每场战斗开始时多1点灵力", RelicTriggerType.OnCombatStart, 1, false, RelicRarity.Common),
                R("relic_enemy_death_gold", "财迷心", "每击杀一个敌人获得5灵石", RelicTriggerType.OnEnemyDeath, 5, false, RelicRarity.Common),
                R("relic_player_damaged_str", "怒焰心", "受到伤害时获得1层力量", RelicTriggerType.OnPlayerDamaged, 1, false, RelicRarity.Common),
                R("relic_card_played_block", "护体灵纹", "每打出一张牌获得1点格挡", RelicTriggerType.OnCardPlayed, 1, false, RelicRarity.Common),
                R("relic_enemy_damaged_heal", "吸血鬼牙", "对敌人造成伤害时回复1点生命", RelicTriggerType.OnEnemyDamaged, 1, false, RelicRarity.Common),
                R("relic_turn_end_draw", "夜思玉", "每回合结束时抽1张牌", RelicTriggerType.OnTurnEnd, 1, false, RelicRarity.Common),
                R("relic_card_drawn_block", "灵盾坠", "每抽一张牌获得1点格挡", RelicTriggerType.OnCardDrawn, 1, false, RelicRarity.Common),
                R("relic_attack_dmg", "锋锐符", "攻击牌伤害+2", RelicTriggerType.OnAttackPlayed, 2, false, RelicRarity.Common),
                R("relic_turn_start_energy", "灵泉石", "每回合开始时恢复1点灵力", RelicTriggerType.OnTurnStart, 1, false, RelicRarity.Common),
                R("relic_enemy_death_heal", "噬魂珠", "每击杀一个敌人回复3点生命", RelicTriggerType.OnEnemyDeath, 3, false, RelicRarity.Common),
                R("relic_combat_start_block", "战甲符", "每场战斗开始时获得10点格挡", RelicTriggerType.OnCombatStart, 10, false, RelicRarity.Common),
                R("relic_player_damaged_block", "荆棘甲", "受到伤害时获得3点格挡", RelicTriggerType.OnPlayerDamaged, 3, false, RelicRarity.Common),
                R("relic_use_potion_draw", "药王壶", "使用药水时抽2张牌", RelicTriggerType.OnUsePotion, 2, false, RelicRarity.Common),
                R("relic_turn_start_str", "战意符", "每回合开始时获得1层力量", RelicTriggerType.OnTurnStart, 1, false, RelicRarity.Common),
                R("relic_gain_gold_extra", "聚宝盆", "每次获得灵石时额外获得1灵石", RelicTriggerType.OnGainGold, 1, false, RelicRarity.Common),

                // ===== 灵品(Uncommon) - 20个 =====
                R("relic_attack_str2", "修罗心", "每打出攻击牌获得2层力量", RelicTriggerType.OnAttackPlayed, 2, false, RelicRarity.Uncommon),
                R("relic_turn_heal3", "万年灵芝", "每回合结束时回复3点生命", RelicTriggerType.OnTurnEnd, 3, false, RelicRarity.Uncommon),
                R("relic_turn_block5", "玄武甲", "每回合开始时获得5点格挡", RelicTriggerType.OnTurnStart, 5, false, RelicRarity.Uncommon),
                R("relic_draw2", "天书残卷", "每回合开始时多抽2张牌", RelicTriggerType.OnTurnStart, 2, false, RelicRarity.Uncommon),
                R("relic_energy2", "灵脉石", "每场战斗开始时多2点灵力", RelicTriggerType.OnCombatStart, 2, false, RelicRarity.Uncommon),
                R("relic_enemy_death_gold10", "贪狼令", "每击杀敌人获得10灵石", RelicTriggerType.OnEnemyDeath, 10, false, RelicRarity.Uncommon),
                R("relic_player_dmg_str2", "逆血丹心", "受到伤害时获得2层力量", RelicTriggerType.OnPlayerDamaged, 2, false, RelicRarity.Uncommon),
                R("relic_card_block2", "灵纹铠", "每打出牌获得2点格挡", RelicTriggerType.OnCardPlayed, 2, false, RelicRarity.Uncommon),
                R("relic_enemy_dmg_heal2", "噬血珠", "造成伤害时回复2点生命", RelicTriggerType.OnEnemyDamaged, 2, false, RelicRarity.Uncommon),
                R("relic_turn_end_draw2", "冥思玉", "每回合结束时抽2张牌", RelicTriggerType.OnTurnEnd, 2, false, RelicRarity.Uncommon),
                R("relic_combat_start_str", "战神图", "战斗开始时获得3层力量", RelicTriggerType.OnCombatStart, 3, false, RelicRarity.Uncommon),
                R("relic_combat_start_dex", "灵猿图", "战斗开始时获得3层敏捷", RelicTriggerType.OnCombatStart, 3, false, RelicRarity.Uncommon),
                R("relic_turn_start_block10", "不动符", "每回合开始时获得10点格挡", RelicTriggerType.OnTurnStart, 10, false, RelicRarity.Uncommon),
                R("relic_use_potion_str", "药灵符", "使用药水时获得3层力量", RelicTriggerType.OnUsePotion, 3, false, RelicRarity.Uncommon),
                R("relic_player_dmg_thorn", "万棘甲", "受到伤害时反弹5点伤害", RelicTriggerType.OnPlayerDamaged, 5, false, RelicRarity.Uncommon),
                R("relic_card_drawn_str", "战意坠", "每抽牌时获得1层力量", RelicTriggerType.OnCardDrawn, 1, false, RelicRarity.Uncommon),
                R("relic_turn_end_block", "镇守玉", "每回合结束时获得5点格挡", RelicTriggerType.OnTurnEnd, 5, false, RelicRarity.Uncommon),
                R("relic_attack_heal", "噬血剑心", "打出攻击牌时回复1点生命", RelicTriggerType.OnAttackPlayed, 1, false, RelicRarity.Uncommon),
                R("relic_enemy_dmg_gold", "聚财符", "造成伤害时获得2灵石", RelicTriggerType.OnEnemyDamaged, 2, false, RelicRarity.Uncommon),
                R("relic_skill_block", "悟道甲", "打出技能牌时获得5点格挡", RelicTriggerType.OnSkillPlayed, 5, false, RelicRarity.Uncommon),

                // ===== 玄品(Rare) - 10个 =====
                R("relic_attack_str3", "杀戮之心", "打出攻击牌获得3层力量", RelicTriggerType.OnAttackPlayed, 3, false, RelicRarity.Rare),
                R("relic_turn_heal5", "不死之身", "每回合结束时回复5点生命", RelicTriggerType.OnTurnEnd, 5, false, RelicRarity.Rare),
                R("relic_turn_block15", "太虚甲", "每回合开始时获得15点格挡", RelicTriggerType.OnTurnStart, 15, false, RelicRarity.Rare),
                R("relic_draw3", "万象天书", "每回合开始时多抽3张牌", RelicTriggerType.OnTurnStart, 3, false, RelicRarity.Rare),
                R("relic_energy3", "混沌灵石", "每场战斗开始时多3点灵力", RelicTriggerType.OnCombatStart, 3, false, RelicRarity.Rare),
                R("relic_combat_str5", "修罗图", "战斗开始时获得5层力量", RelicTriggerType.OnCombatStart, 5, false, RelicRarity.Rare),
                R("relic_combat_block30", "金刚甲", "战斗开始时获得30点格挡", RelicTriggerType.OnCombatStart, 30, false, RelicRarity.Rare),
                R("relic_turn_end_heal5_block5", "太极玉", "每回合结束回复5生命并获得5格挡", RelicTriggerType.OnTurnEnd, 5, false, RelicRarity.Rare),
                R("relic_player_dmg_str3_heal", "逆天丹心", "受到伤害时获得3层力量并回复2生命", RelicTriggerType.OnPlayerDamaged, 3, false, RelicRarity.Rare),
                R("relic_card_played_str_dex", "混元坠", "每打出牌获得1层力量和1层敏捷", RelicTriggerType.OnCardPlayed, 1, false, RelicRarity.Rare),
            };

            foreach (var (id, name, desc, trigger, val, oneTime, rar) in relics)
            {
                string path = $"{dir}/{id}.asset";
                if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(path) != null) continue;
                
                var so = ScriptableObject.CreateInstance<RelicData>();
                so.relicId = id;
                so.name = name;
                so.description = desc;
                so.triggerType = trigger;
                so.effectValue = val;
                so.oneTimeUse = oneTime;
                so.rarity = rar;
                AssetDatabase.CreateAsset(so, path);
                count++;
            }
            return count;
        }

        static (string, string, string, int, PotionRarity, PotionTargetType) P(
            string id, string name, string desc, int val, PotionRarity rar, PotionTargetType target)
            => (id, name, desc, val, rar, target);

        static (string, string, string, RelicTriggerType, int, bool, RelicRarity) R(
            string id, string name, string desc, RelicTriggerType trigger, int val, bool oneTime, RelicRarity rar)
            => (id, name, desc, trigger, val, oneTime, rar);
    }
}
