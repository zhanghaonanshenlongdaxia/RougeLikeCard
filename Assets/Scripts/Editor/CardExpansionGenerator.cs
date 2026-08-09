using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;
using NueGames.NueDeck.Scripts.Enums;
using NueGames.NueDeck.Scripts.Data.Collection;

namespace CardGame.Editor
{
    /// <summary>
    /// 批量生成新卡牌SO，扩充卡池到130+张。
    /// 分布：通用10 + 剑道25 + 体道25 + 灵道25 + 传奇10 = 95张新增
    /// </summary>
    public static class CardExpansionGenerator
    {
        [MenuItem("Tools/Expand Cards")]
        public static void GenerateAll()
        {
            int count = 0;
            count += GenerateCommonCards();
            count += GenerateSwordCards();
            count += GenerateBodyCards();
            count += GenerateSpiritCards();
            count += GenerateLegendaryCards();
            
            // 设置升级数据
            SetUpgradeData();
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"卡牌扩充完成: 新增{count}张，总计{count + 63}张");
        }

        // 卡牌创建工具
        static int CreateCard(string dir, string id, string cardName, int cost, string desc,
            CardActionType actionType, int actionValue, int target = 0,
            RarityType rarity = RarityType.Common, PathType path = PathType.None, BuildTag build = BuildTag.None,
            bool power = false, bool exhaust = false, bool noTarget = false,
            int powerTier = 0, string upgradeName = null, int upgradeCost = -1, int upgradeValue = 0)
        {
            if (!AssetDatabase.IsValidFolder(dir))
            {
                var parent = System.IO.Path.GetDirectoryName(dir).Replace('\\', '/');
                AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(dir));
            }

            string assetPath = $"{dir}/{id}.asset";
            if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath) != null) return 0;

            var so = ScriptableObject.CreateInstance<CardData>();
            var t = so.GetType();

            SetField(t, so, "id", id);
            SetField(t, so, "cardName", cardName);
            SetField(t, so, "manaCost", cost);
            SetField(t, so, "rarity", rarity);
            SetField(t, so, "pathType", path);
            SetField(t, so, "buildTag", build);
            SetField(t, so, "powerTier", powerTier);
            SetField(t, so, "isPowerCard", power);
            SetField(t, so, "exhaustAfterPlay", exhaust);
            SetField(t, so, "usableWithoutTarget", noTarget);
            if (upgradeName != null) SetField(t, so, "upgradedCardName", upgradeName);
            SetField(t, so, "upgradedManaCost", upgradeCost);

            // 创建CardActionData
            var action = new CardActionData();
            action.EditActionType(actionType);
            action.EditActionValue(actionValue);
            // target: 0=Enemy, 1=Ally, 2=AllEnemies, 3=AllAllies, 4=RandomEnemy, 5=RandomAlly
            var targetType = target switch { 0 => ActionTargetType.Enemy, 1 => ActionTargetType.Ally, 2 => ActionTargetType.AllEnemies, 3 => ActionTargetType.AllAllies, _ => ActionTargetType.Enemy };
            action.EditActionTarget(targetType);
            
            var actionList = new List<CardActionData> { action };
            if (upgradeValue > 0)
            {
                var upAction = new CardActionData();
                upAction.EditActionType(actionType);
                upAction.EditActionValue(upgradeValue);
                upAction.EditActionTarget(targetType);
                var upgradeList = new List<CardActionData> { upAction };
                SetField(t, so, "upgradedCardActionDataList", upgradeList);
            }
            SetField(t, so, "cardActionDataList", actionList);

            // 创建描述
            var descData = new CardDescriptionData();
            descData.EditDescriptionText(desc);
            var descList = new List<CardDescriptionData> { descData };
            SetField(t, so, "cardDescriptionDataList", descList);

            AssetDatabase.CreateAsset(so, assetPath);
            return 1;
        }

        static void SetField(System.Type t, object obj, string name, object value)
        {
            var f = t.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            if (f != null) f.SetValue(obj, value);
        }

        // ===== 通用卡（无路线）10张 =====
        static int GenerateCommonCards()
        {
            string dir = "Assets/NueGames/NueDeck/Data/Cards/General";
            int c = 0;
            c += CreateCard(dir, "gen_strike", "突袭", 1, "造成7点伤害", CardActionType.Attack, 7, 0, RarityType.Common, PathType.None, BuildTag.None, false, false, false, 0, "突袭+", 0, 10);
            c += CreateCard(dir, "gen_defend", "盾墙", 1, "获得5点格挡", CardActionType.Block, 5, 1, RarityType.Common, PathType.None, BuildTag.None, false, false, true, 0, "盾墙+", 0, 8);
            c += CreateCard(dir, "gen_clarity", "静心诀", 0, "抽1张牌", CardActionType.Draw, 1, 1, RarityType.Common, PathType.None, BuildTag.None, false, false, true, 0, "静心诀+", 0, 2);
            c += CreateCard(dir, "gen_recover", "养气诀", 1, "回复8点生命", CardActionType.Heal, 8, 1, RarityType.Common, PathType.None, BuildTag.None, false, false, true, 0, "养气诀+", 0, 12);
            c += CreateCard(dir, "gen_power_str", "锻体诀", 1, "获得1层力量", CardActionType.IncreaseStrength, 1, 1, RarityType.Uncommon, PathType.None, BuildTag.None, true, true, true, 0, "锻体诀+", 0, 2);
            c += CreateCard(dir, "gen_power_dex", "灵步诀", 1, "获得1层敏捷", CardActionType.IncreaseDexterity, 1, 1, RarityType.Uncommon, PathType.None, BuildTag.None, true, true, true, 0, "灵步诀+", 0, 2);
            c += CreateCard(dir, "gen_pierce", "破甲击", 2, "造成10点伤害", CardActionType.Attack, 10, 0, RarityType.Uncommon, PathType.None, BuildTag.None, false, false, false, 0, "破甲击+", 1, 14);
            c += CreateCard(dir, "gen_meditate", "吐纳法", 0, "获得3点格挡，抽1张牌", CardActionType.Block, 3, 1, RarityType.Uncommon, PathType.None, BuildTag.None, false, false, true, 0, "吐纳法+", 0, 5);
            c += CreateCard(dir, "gen_stun", "定身术", 2, "使敌人眩晕1回合", CardActionType.Stun, 1, 0, RarityType.Rare, PathType.None, BuildTag.None, false, false, false, 0, "定身术+", 1, 2);
            c += CreateCard(dir, "gen_lifesteal", "噬血术", 2, "造成8点伤害，回复3点生命", CardActionType.LifeSteal, 8, 0, RarityType.Rare, PathType.None, BuildTag.None, false, false, false, 0, "噬血术+", 1, 12);
            return c;
        }

        // ===== 剑道 25张 =====
        static int GenerateSwordCards()
        {
            string dir = "Assets/NueGames/NueDeck/Data/Cards/Path/Sword";
            int c = 0;
            // 多段连击流(MultiHit) 8张
            c += CreateCard(dir, "sw_mh_twin", "双影斩", 1, "造成2次4点伤害", CardActionType.Attack, 4, 0, RarityType.Common, PathType.Sword, BuildTag.MultiHit, false, false, false, 0, "双影斩+", 0, 6);
            c += CreateCard(dir, "sw_mh_triple", "三才斩", 2, "造成3次3点伤害", CardActionType.Attack, 3, 0, RarityType.Common, PathType.Sword, BuildTag.MultiHit, false, false, false, 0, "三才斩+", 1, 5);
            c += CreateCard(dir, "sw_mh_rain", "剑雨术", 2, "对全体敌人造成4次2点伤害", CardActionType.Attack, 2, 2, RarityType.Uncommon, PathType.Sword, BuildTag.MultiHit, false, false, false, 0, "剑雨术+", 1, 3);
            c += CreateCard(dir, "sw_mh_frenzy", "狂剑术", 1, "造成4次2点伤害，消耗", CardActionType.Attack, 2, 0, RarityType.Uncommon, PathType.Sword, BuildTag.MultiHit, false, true, false, 0, "狂剑术+", 0, 3);
            c += CreateCard(dir, "sw_mh_storm", "剑刃风暴", 3, "造成5次3点伤害", CardActionType.Attack, 3, 0, RarityType.Rare, PathType.Sword, BuildTag.MultiHit, false, false, false, 1, "剑刃风暴+", 2, 5);
            c += CreateCard(dir, "sw_mh_whirl", "回旋斩", 1, "造成2次5点伤害", CardActionType.Attack, 5, 0, RarityType.Common, PathType.Sword, BuildTag.MultiHit, false, false, false, 0, "回旋斩+", 0, 7);
            c += CreateCard(dir, "sw_mh_flurry", "乱舞", 0, "造成3次1点伤害", CardActionType.Attack, 1, 0, RarityType.Common, PathType.Sword, BuildTag.MultiHit, false, false, false, 0, "乱舞+", 0, 2);
            c += CreateCard(dir, "sw_mh_eclipse", "日月斩", 3, "造成3次6点伤害，消耗", CardActionType.Attack, 6, 0, RarityType.Rare, PathType.Sword, BuildTag.MultiHit, false, true, false, 1, "日月斩+", 2, 9);
            // 蓄力爆发流(Burst) 8张
            c += CreateCard(dir, "sw_br_charge2", "蓄势待发", 0, "获得2层力量，下回合消失", CardActionType.IncreaseStrength, 2, 1, RarityType.Common, PathType.Sword, BuildTag.Burst, false, false, true, 0, "蓄势待发+", 0, 3);
            c += CreateCard(dir, "sw_br_gather", "聚元斩", 1, "获得1层力量", CardActionType.IncreaseStrength, 1, 1, RarityType.Common, PathType.Sword, BuildTag.Burst, false, false, true, 0, "聚元斩+", 0, 2);
            c += CreateCard(dir, "sw_br_release", "爆发斩", 2, "造成12点伤害", CardActionType.Attack, 12, 0, RarityType.Common, PathType.Sword, BuildTag.Burst, false, false, false, 0, "爆发斩+", 1, 16);
            c += CreateCard(dir, "sw_br_ignite", "炎刃", 1, "造成6点伤害，获得1层力量", CardActionType.Attack, 6, 0, RarityType.Uncommon, PathType.Sword, BuildTag.Burst, false, false, false, 0, "炎刃+", 0, 9);
            c += CreateCard(dir, "sw_br_quake", "裂地斩", 2, "造成15点伤害", CardActionType.Attack, 15, 0, RarityType.Uncommon, PathType.Sword, BuildTag.Burst, false, false, false, 0, "裂地斩+", 1, 20);
            c += CreateCard(dir, "sw_br_annihilate", "湮灭斩", 3, "造成20点伤害，消耗", CardActionType.Attack, 20, 0, RarityType.Rare, PathType.Sword, BuildTag.Burst, false, true, false, 1, "湮灭斩+", 2, 28);
            c += CreateCard(dir, "sw_br_overload", "过载斩", 1, "造成8点伤害，获得2层力量，下回合消失", CardActionType.Attack, 8, 0, RarityType.Uncommon, PathType.Sword, BuildTag.Burst, false, false, false, 0, "过载斩+", 0, 12);
            c += CreateCard(dir, "sw_br_execute", "处决", 3, "造成18点伤害", CardActionType.Attack, 18, 0, RarityType.Rare, PathType.Sword, BuildTag.Burst, false, false, false, 1, "处决+", 2, 25);
            // 通用剑道 9张
            c += CreateCard(dir, "sw_sword_qi", "剑气斩", 1, "造成6点伤害", CardActionType.Attack, 6, 0, RarityType.Common, PathType.Sword, BuildTag.None, false, false, false, 0, "剑气斩+", 0, 9);
            c += CreateCard(dir, "sw_sword_block", "剑盾术", 1, "获得6点格挡", CardActionType.Block, 6, 1, RarityType.Common, PathType.Sword, BuildTag.None, false, false, true, 0, "剑盾术+", 0, 9);
            c += CreateCard(dir, "sw_sword_counter", "反击术", 1, "获得4点格挡，造成4点伤害", CardActionType.Block, 4, 1, RarityType.Uncommon, PathType.Sword, BuildTag.None, false, false, true, 0, "反击术+", 0, 6);
            c += CreateCard(dir, "sw_sword_rush", "突进斩", 1, "造成5点伤害，抽1张牌", CardActionType.Attack, 5, 0, RarityType.Uncommon, PathType.Sword, BuildTag.None, false, false, false, 0, "突进斩+", 0, 8);
            c += CreateCard(dir, "sw_sword_meditate", "剑心诀", 0, "获得1层力量", CardActionType.IncreaseStrength, 1, 1, RarityType.Common, PathType.Sword, BuildTag.None, true, true, true, 0, "剑心诀+", 0, 2);
            c += CreateCard(dir, "sw_sword_heal", "剑息术", 1, "回复5点生命", CardActionType.Heal, 5, 1, RarityType.Common, PathType.Sword, BuildTag.None, false, false, true, 0, "剑息术+", 0, 8);
            c += CreateCard(dir, "sw_sword_draw", "剑灵引", 0, "抽2张牌", CardActionType.Draw, 2, 1, RarityType.Uncommon, PathType.Sword, BuildTag.None, false, false, true, 0, "剑灵引+", 0, 3);
            c += CreateCard(dir, "sw_sword_lifesteal", "噬剑术", 2, "造成10点伤害，回复5点生命", CardActionType.LifeSteal, 10, 0, RarityType.Rare, PathType.Sword, BuildTag.None, false, false, false, 1, "噬剑术+", 1, 15);
            c += CreateCard(dir, "sw_sword_weak", "破势斩", 1, "造成5点伤害，施加1层虚弱", CardActionType.ApplyWeak, 1, 0, RarityType.Uncommon, PathType.Sword, BuildTag.None, false, false, false, 0, "破势斩+", 0, 7);
            return c;
        }

        // ===== 体道 25张 =====
        static int GenerateBodyCards()
        {
            string dir = "Assets/NueGames/NueDeck/Data/Cards/Path/Body";
            int c = 0;
            // 叠甲反伤流(Thorn) 8张
            c += CreateCard(dir, "bd_th_spike", "荆棘术", 0, "获得1层反伤", CardActionType.Thorn, 1, 1, RarityType.Common, PathType.Body, BuildTag.Thorn, true, true, true, 0, "荆棘术+", 0, 2);
            c += CreateCard(dir, "bd_th_shield", "反伤甲", 1, "获得8点格挡，获得1层反伤", CardActionType.Block, 8, 1, RarityType.Common, PathType.Body, BuildTag.Thorn, false, false, true, 0, "反伤甲+", 0, 12);
            c += CreateCard(dir, "bd_th_bramble", "荆棘甲", 1, "获得6点格挡，获得2层反伤", CardActionType.Thorn, 2, 1, RarityType.Uncommon, PathType.Body, BuildTag.Thorn, false, false, true, 0, "荆棘甲+", 0, 3);
            c += CreateCard(dir, "bd_th_reflect2", "镜面甲", 2, "获得12点格挡，获得3层反伤", CardActionType.Thorn, 3, 1, RarityType.Uncommon, PathType.Body, BuildTag.Thorn, false, false, true, 0, "镜面甲+", 1, 4);
            c += CreateCard(dir, "bd_th_fortress", "铁壁", 2, "获得15点格挡，获得2层反伤", CardActionType.Block, 15, 1, RarityType.Rare, PathType.Body, BuildTag.Thorn, false, false, true, 1, "铁壁+", 1, 20);
            c += CreateCard(dir, "bd_th_counter", "反击体", 1, "获得6点格挡，获得1层反伤", CardActionType.Thorn, 1, 1, RarityType.Common, PathType.Body, BuildTag.Thorn, false, false, true, 0, "反击体+", 0, 2);
            c += CreateCard(dir, "bd_th_hedgehog", "刺猬甲", 1, "获得5点格挡，获得3层反伤", CardActionType.Thorn, 3, 1, RarityType.Uncommon, PathType.Body, BuildTag.Thorn, false, false, true, 0, "刺猬甲+", 0, 4);
            c += CreateCard(dir, "bd_th_needle", "万针体", 3, "获得20点格挡，获得5层反伤", CardActionType.Thorn, 5, 1, RarityType.Rare, PathType.Body, BuildTag.Thorn, false, false, true, 1, "万针体+", 2, 7);
            // 持续消耗流(Sustain) 8张
            c += CreateCard(dir, "bd_su_regen", "回春体", 1, "回复10点生命", CardActionType.Heal, 10, 1, RarityType.Common, PathType.Body, BuildTag.Sustain, false, false, true, 0, "回春体+", 0, 15);
            c += CreateCard(dir, "bd_su_shield2", "灵盾术", 1, "获得10点格挡", CardActionType.Block, 10, 1, RarityType.Common, PathType.Body, BuildTag.Sustain, false, false, true, 0, "灵盾术+", 0, 14);
            c += CreateCard(dir, "bd_su_meditate", "打坐", 0, "回复5点生命，获得3点格挡", CardActionType.Heal, 5, 1, RarityType.Common, PathType.Body, BuildTag.Sustain, false, false, true, 0, "打坐+", 0, 8);
            c += CreateCard(dir, "bd_su_breathe", "吐纳法", 0, "获得4点格挡，抽1张牌", CardActionType.Block, 4, 1, RarityType.Common, PathType.Body, BuildTag.Sustain, false, false, true, 0, "吐纳法+", 0, 6);
            c += CreateCard(dir, "bd_su_recover", "自愈体", 1, "回复15点生命", CardActionType.Heal, 15, 1, RarityType.Uncommon, PathType.Body, BuildTag.Sustain, false, false, true, 0, "自愈体+", 0, 22);
            c += CreateCard(dir, "bd_su_stoneskin", "石肤术", 2, "获得18点格挡", CardActionType.Block, 18, 1, RarityType.Uncommon, PathType.Body, BuildTag.Sustain, false, false, true, 0, "石肤术+", 1, 25);
            c += CreateCard(dir, "bd_su_immortal", "不灭体", 3, "回复25点生命，获得10点格挡", CardActionType.Heal, 25, 1, RarityType.Rare, PathType.Body, BuildTag.Sustain, false, false, true, 1, "不灭体+", 2, 35);
            c += CreateCard(dir, "bd_su_maxhp", "炼体诀", 1, "永久增加3点最大生命", CardActionType.IncreaseMaxHealth, 3, 1, RarityType.Uncommon, PathType.Body, BuildTag.Sustain, true, true, true, 0, "炼体诀+", 0, 5);
            // 通用体道 9张
            c += CreateCard(dir, "bd_block_basic", "铁壁挡", 1, "获得8点格挡", CardActionType.Block, 8, 1, RarityType.Common, PathType.Body, BuildTag.None, false, false, true, 0, "铁壁挡+", 0, 11);
            c += CreateCard(dir, "bd_heal_basic", "疗伤术", 1, "回复6点生命", CardActionType.Heal, 6, 1, RarityType.Common, PathType.Body, BuildTag.None, false, false, true, 0, "疗伤术+", 0, 10);
            c += CreateCard(dir, "bd_str_body", "锻体术", 1, "获得1层力量", CardActionType.IncreaseStrength, 1, 1, RarityType.Common, PathType.Body, BuildTag.None, true, true, true, 0, "锻体术+", 0, 2);
            c += CreateCard(dir, "bd_dex_body", "灵猿体", 1, "获得1层敏捷", CardActionType.IncreaseDexterity, 1, 1, RarityType.Uncommon, PathType.Body, BuildTag.None, true, true, true, 0, "灵猿体+", 0, 2);
            c += CreateCard(dir, "bd_attack_body", "力劈", 2, "造成10点伤害", CardActionType.Attack, 10, 0, RarityType.Common, PathType.Body, BuildTag.None, false, false, false, 0, "力劈+", 1, 14);
            c += CreateCard(dir, "bd_draw_body", "冥想", 0, "抽2张牌", CardActionType.Draw, 2, 1, RarityType.Uncommon, PathType.Body, BuildTag.None, false, false, true, 0, "冥想+", 0, 3);
            c += CreateCard(dir, "bd_vuln_body", "破绽击", 1, "造成5点伤害，施加2层易伤", CardActionType.ApplyVulnerable, 2, 0, RarityType.Uncommon, PathType.Body, BuildTag.None, false, false, false, 0, "破绽击+", 0, 7);
            c += CreateCard(dir, "bd_weak_body", "压制", 1, "造成4点伤害，施加2层虚弱", CardActionType.ApplyWeak, 2, 0, RarityType.Uncommon, PathType.Body, BuildTag.None, false, false, false, 0, "压制+", 0, 6);
            c += CreateCard(dir, "bd_combo", "攻防一体", 2, "造成8点伤害，获得8点格挡", CardActionType.Attack, 8, 0, RarityType.Rare, PathType.Body, BuildTag.None, false, false, false, 1, "攻防一体+", 1, 12);
            return c;
        }

        // ===== 灵道 25张 =====
        static int GenerateSpiritCards()
        {
            string dir = "Assets/NueGames/NueDeck/Data/Cards/Path/Spirit";
            int c = 0;
            // Debuff连锁流 8张
            c += CreateCard(dir, "sp_db_curse", "诅咒术", 1, "施加3层虚弱", CardActionType.ApplyWeak, 3, 0, RarityType.Common, PathType.Spirit, BuildTag.Debuff, false, false, false, 0, "诅咒术+", 0, 4);
            c += CreateCard(dir, "sp_db_shatter", "碎甲术", 1, "施加3层脆弱", CardActionType.ApplyFrail, 3, 0, RarityType.Common, PathType.Spirit, BuildTag.Debuff, false, false, false, 0, "碎甲术+", 0, 4);
            c += CreateCard(dir, "sp_db_expose", "破绽术", 1, "施加3层易伤", CardActionType.ApplyVulnerable, 3, 0, RarityType.Common, PathType.Spirit, BuildTag.Debuff, false, false, false, 0, "破绽术+", 0, 4);
            c += CreateCard(dir, "sp_db_hex", "天咒", 2, "对全体敌人施加2层虚弱、2层脆弱、2层易伤", CardActionType.ApplyWeak, 2, 2, RarityType.Rare, PathType.Spirit, BuildTag.Debuff, false, false, false, 1, "天咒+", 1, 3);
            c += CreateCard(dir, "sp_db_plague", "瘟疫术", 1, "对全体敌人施加2层虚弱", CardActionType.ApplyWeak, 2, 2, RarityType.Uncommon, PathType.Spirit, BuildTag.Debuff, false, false, false, 0, "瘟疫术+", 0, 3);
            c += CreateCard(dir, "sp_db_corrupt", "腐蚀术", 1, "对全体敌人施加2层脆弱", CardActionType.ApplyFrail, 2, 2, RarityType.Uncommon, PathType.Spirit, BuildTag.Debuff, false, false, false, 0, "腐蚀术+", 0, 3);
            c += CreateCard(dir, "sp_db_weakpoint", "弱点打击", 0, "施加1层易伤", CardActionType.ApplyVulnerable, 1, 0, RarityType.Common, PathType.Spirit, BuildTag.Debuff, false, false, false, 0, "弱点打击+", 0, 2);
            c += CreateCard(dir, "sp_db_devour", "噬魂术", 2, "施加5层虚弱，造成5点伤害", CardActionType.ApplyWeak, 5, 0, RarityType.Uncommon, PathType.Spirit, BuildTag.Debuff, false, false, false, 0, "噬魂术+", 1, 7);
            // 灵力爆发流(ManaBurst) 8张
            c += CreateCard(dir, "sp_mn_bolt2", "雷击术", 1, "造成8点伤害", CardActionType.Attack, 8, 0, RarityType.Common, PathType.Spirit, BuildTag.ManaBurst, false, false, false, 0, "雷击术+", 0, 11);
            c += CreateCard(dir, "sp_mn_charge", "蓄灵术", 0, "获得1点灵力", CardActionType.EarnMana, 1, 1, RarityType.Common, PathType.Spirit, BuildTag.ManaBurst, false, false, true, 0, "蓄灵术+", 0, 2);
            c += CreateCard(dir, "sp_mn_free", "无消耗", 0, "造成3点伤害", CardActionType.Attack, 3, 0, RarityType.Common, PathType.Spirit, BuildTag.ManaBurst, false, false, false, 0, "无消耗+", 0, 5);
            c += CreateCard(dir, "sp_mn_overload", "灵力过载", 2, "造成15点伤害", CardActionType.Attack, 15, 0, RarityType.Uncommon, PathType.Spirit, BuildTag.ManaBurst, false, false, false, 0, "灵力过载+", 1, 20);
            c += CreateCard(dir, "sp_mn_nova", "灵力新星", 3, "对全体敌人造成10点伤害", CardActionType.Attack, 10, 2, RarityType.Uncommon, PathType.Spirit, BuildTag.ManaBurst, false, false, false, 0, "灵力新星+", 2, 15);
            c += CreateCard(dir, "sp_mn_storm2", "雷霆风暴", 3, "对全体敌人造成12点伤害", CardActionType.Attack, 12, 2, RarityType.Rare, PathType.Spirit, BuildTag.ManaBurst, false, false, false, 1, "雷霆风暴+", 2, 16);
            c += CreateCard(dir, "sp_mn_focus", "凝灵术", 1, "获得2点灵力", CardActionType.EarnMana, 2, 1, RarityType.Uncommon, PathType.Spirit, BuildTag.ManaBurst, false, false, true, 0, "凝灵术+", 0, 3);
            c += CreateCard(dir, "sp_mn_annihilate", "灵力湮灭", 4, "造成30点伤害，消耗", CardActionType.Attack, 30, 0, RarityType.Rare, PathType.Spirit, BuildTag.ManaBurst, false, true, false, 1, "灵力湮灭+", 3, 40);
            // 通用灵道 9张
            c += CreateCard(dir, "sp_heal_basic", "灵疗术", 1, "回复8点生命", CardActionType.Heal, 8, 1, RarityType.Common, PathType.Spirit, BuildTag.None, false, false, true, 0, "灵疗术+", 0, 12);
            c += CreateCard(dir, "sp_block_basic", "灵盾", 1, "获得7点格挡", CardActionType.Block, 7, 1, RarityType.Common, PathType.Spirit, BuildTag.None, false, false, true, 0, "灵盾+", 0, 10);
            c += CreateCard(dir, "sp_draw_basic", "灵视", 0, "抽2张牌", CardActionType.Draw, 2, 1, RarityType.Common, PathType.Spirit, BuildTag.None, false, false, true, 0, "灵视+", 0, 3);
            c += CreateCard(dir, "sp_str_basic", "灵力增幅", 1, "获得1层力量", CardActionType.IncreaseStrength, 1, 1, RarityType.Common, PathType.Spirit, BuildTag.None, true, true, true, 0, "灵力增幅+", 0, 2);
            c += CreateCard(dir, "sp_dex_basic", "灵巧", 1, "获得1层敏捷", CardActionType.IncreaseDexterity, 1, 1, RarityType.Uncommon, PathType.Spirit, BuildTag.None, true, true, true, 0, "灵巧+", 0, 2);
            c += CreateCard(dir, "sp_mana_basic", "回灵", 0, "获得1点灵力", CardActionType.EarnMana, 1, 1, RarityType.Common, PathType.Spirit, BuildTag.None, false, false, true, 0, "回灵+", 0, 2);
            c += CreateCard(dir, "sp_stun_basic", "定神术", 2, "使敌人眩晕1回合", CardActionType.Stun, 1, 0, RarityType.Uncommon, PathType.Spirit, BuildTag.None, false, false, false, 0, "定神术+", 1, 2);
            c += CreateCard(dir, "sp_lifesteal_basic", "噬灵术", 2, "造成8点伤害，回复4点生命", CardActionType.LifeSteal, 8, 0, RarityType.Rare, PathType.Spirit, BuildTag.None, false, false, false, 1, "噬灵术+", 1, 12);
            c += CreateCard(dir, "sp_maxhp_basic", "灵体锻", 1, "永久增加3点最大生命", CardActionType.IncreaseMaxHealth, 3, 1, RarityType.Uncommon, PathType.Spirit, BuildTag.None, true, true, true, 0, "灵体锻+", 0, 5);
            return c;
        }

        // ===== 传奇卡 10张 =====
        static int GenerateLegendaryCards()
        {
            string dir = "Assets/NueGames/NueDeck/Data/Cards/Legendary";
            int c = 0;
            c += CreateCard(dir, "leg_sword_unity", "万剑归一", 4, "造成8次5点伤害", CardActionType.Attack, 5, 0, RarityType.Legendary, PathType.Sword, BuildTag.MultiHit, false, true, false, 1, "万剑归一+", 3, 7);
            c += CreateCard(dir, "leg_sword_god2", "剑神降临", 5, "造成50点伤害", CardActionType.Attack, 50, 0, RarityType.Legendary, PathType.Sword, BuildTag.Burst, false, true, false, 1, "剑神降临+", 4, 65);
            c += CreateCard(dir, "leg_body_immortal", "金刚不坏体", 4, "获得60点格挡，获得10层反伤", CardActionType.Block, 60, 1, RarityType.Legendary, PathType.Body, BuildTag.Thorn, false, true, true, 1, "金刚不坏体+", 3, 80);
            c += CreateCard(dir, "leg_body_saint2", "肉身成圣", 5, "永久增加15点最大生命，获得5层力量", CardActionType.IncreaseMaxHealth, 15, 1, RarityType.Legendary, PathType.Body, BuildTag.Sustain, true, true, true, 1, "肉身成圣+", 4, 20);
            c += CreateCard(dir, "leg_spirit_chaos", "混沌术", 5, "对全体敌人造成30点伤害，施加5层虚弱、5层脆弱、5层易伤", CardActionType.Attack, 30, 2, RarityType.Legendary, PathType.Spirit, BuildTag.Debuff, false, true, false, 1, "混沌术+", 4, 40);
            c += CreateCard(dir, "leg_spirit_void", "虚空吞噬", 4, "造成40点伤害，回复20点生命", CardActionType.LifeSteal, 40, 0, RarityType.Legendary, PathType.Spirit, BuildTag.ManaBurst, false, true, false, 1, "虚空吞噬+", 3, 55);
            c += CreateCard(dir, "leg_gen_rebirth", "凤凰涅槃", 5, "回复全部生命值，获得20点格挡", CardActionType.Heal, 999, 1, RarityType.Legendary, PathType.None, BuildTag.None, false, true, true, 1, "凤凰涅槃+", 4, 999);
            c += CreateCard(dir, "leg_gen_omni", "太极归元", 4, "获得5层力量、5层敏捷、5层反伤", CardActionType.IncreaseStrength, 5, 1, RarityType.Legendary, PathType.None, BuildTag.None, true, true, true, 1, "太极归元+", 3, 7);
            c += CreateCard(dir, "leg_gen_judgment", "天劫", 5, "对全体敌人造成50点伤害", CardActionType.Attack, 50, 2, RarityType.Legendary, PathType.None, BuildTag.None, false, true, false, 1, "天劫+", 4, 70);
            c += CreateCard(dir, "leg_gen_eternal", "永生诀", 5, "永久增加20点最大生命，回复全部生命", CardActionType.IncreaseMaxHealth, 20, 1, RarityType.Legendary, PathType.None, BuildTag.None, true, true, true, 1, "永生诀+", 4, 30);
            return c;
        }

        // 为所有新卡设置升级数据（如果还没有的话）
        static void SetUpgradeData()
        {
            var guids = AssetDatabase.FindAssets("t:CardData", new[] { "Assets/NueGames/NueDeck/Data/Cards" });
            int count = 0;
            foreach (var g in guids)
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                var card = AssetDatabase.LoadAssetAtPath<ScriptableObject>(p);
                if (card == null) continue;

                var t = card.GetType();
                var upgradeCostField = t.GetField("upgradedManaCost", BindingFlags.NonPublic | BindingFlags.Instance);
                var currentUpgradeCost = (int)upgradeCostField.GetValue(card);
                
                // 如果upgradedManaCost==-1（未设置），设为manaCost-1（升级减1费）
                if (currentUpgradeCost == -1)
                {
                    var manaField = t.GetField("manaCost", BindingFlags.NonPublic | BindingFlags.Instance);
                    var manaCost = (int)manaField.GetValue(card);
                    if (manaCost > 0)
                    {
                        upgradeCostField.SetValue(card, manaCost - 1);
                        EditorUtility.SetDirty(card);
                        count++;
                    }
                }
            }
            Debug.Log($"设置升级数据: {count}张卡");
        }
    }
}
