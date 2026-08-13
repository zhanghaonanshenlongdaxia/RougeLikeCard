using System.Collections.Generic;
using System.IO;
using CardGame;
using NueGames.NueDeck.Scripts.Data.Cultivation;
using NueGames.NueDeck.Scripts.Enums;
using UnityEditor;
using UnityEngine;

namespace CardGame.Editor
{
    public static class CultivationBatchGenerator
    {
        private const string MethodDir = "Assets/NueGames/NueDeck/Data/Cultivation/Methods";
        private const string AbilityDir = "Assets/NueGames/NueDeck/Data/Cultivation/Abilities";

        [MenuItem("Tools/Generate Cultivation Methods")]
        public static void GenerateAll()
        {
            if (!Directory.Exists(MethodDir)) Directory.CreateDirectory(MethodDir);
            if (!Directory.Exists(AbilityDir)) Directory.CreateDirectory(AbilityDir);

            int count = 0;

            // === 神通 === (通用神通，非功法自带)
            count += CreateAbility("da_huashang", "化伤术", "消耗自身灵力治疗目标伤势。", ElementType.None, "5-Heal_Basic", 2);
            count += CreateAbility("da_quyun", "驱云诀", "驱散一片区域的云雾。", ElementType.None, "3-Block_Basic", 1);
            count += CreateAbility("da_lingxi", "灵息诀", "调息恢复灵力，提升灵气上限。", ElementType.None, "5-Heal_Basic", 3);
            count += CreateAbility("da_xuanpi", "玄牝重生法", "重生之法，大幅恢复生命。", ElementType.None, "5-Heal_Basic", 4);
            count += CreateAbility("da_ruding", "入定之术", "进入入定状态，大幅提升灵气恢复。", ElementType.None, "5-Heal_Basic", 3);

            // === 三阳三昧丙丁炼火诀 (火属性, 完整本) ===
            count += CreateSanYang();

            // === 太和十六洞天 (水属性, 完整本) ===
            count += CreateTaiHe();

            // === 长春功 (木属性, 残篇) ===
            count += CreateChangChun();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[CultivationBatchGenerator] Done. Created {count} assets.");
        }

        #region 三阳三昧丙丁炼火诀
        private static int CreateSanYang()
        {
            var method = CreateMethod("sy_method", "三阳三昧丙丁炼火诀",
                "火属性功法，太一门镇派五行真诀之一。借少阳、阳明、太阳三阳经络，凝结天地人三才之火，无物不燃，极具破坏力。擅术法攻击，突破瓶颈需冒较大风险。",
                ElementType.Fire, CultivationMethodGrade.Complete, RealmLevel.DuJie);

            var nodes = new List<CultivationNodeData>
            {
                // ── 练气期 (人火之境) ──
                Node("sy_lq_1", "化伤术", "三阳三昧丙丁炼火诀入门神通，提供火属性基础卡组。", RealmLevel.LianQi,
                    unlockType: NodeUnlockType.Comprehension, cost: 0,
                    reward: NodeRewardType.Card, rewardIds: new List<string>{
                        "gen_strike","gen_strike","gen_strike","gen_strike",
                        "gen_defend","gen_defend","gen_defend"},
                    pos: new Vector2(0, 0)),
                Node("sy_lq_2", "驱云诀", "驱散云雾，基础御气之法。", RealmLevel.LianQi,
                    prereq: "sy_lq_1", unlockType: NodeUnlockType.Comprehension, cost: 5,
                    reward: NodeRewardType.Card, rewardIds: new List<string>{"3-Block_Basic"},
                    pos: new Vector2(0, 1)),
                Node("sy_lq_3", "金乌观", "观想金乌，提升灵气上限。", RealmLevel.LianQi,
                    prereq: "sy_lq_2", unlockType: NodeUnlockType.Comprehension, cost: 8,
                    reward: NodeRewardType.PassiveStat, passive: PassiveStatType.MaxMana, passiveVal: 1,
                    pos: new Vector2(0, 2)),
                Node("sy_lq_4", "先天罡气", "炼就先天罡气，提升灵气上限。", RealmLevel.LianQi,
                    prereq: "sy_lq_3", unlockType: NodeUnlockType.Comprehension, cost: 10,
                    reward: NodeRewardType.PassiveStat, passive: PassiveStatType.MaxMana, passiveVal: 1,
                    pos: new Vector2(0, 3)),

                // ── 筑基期 (突破) ──
                Node("sy_zj_1", "突破·人火", "凝练人火，踏入筑基之境。", RealmLevel.ZhuJi,
                    prereq: "sy_lq_4", unlockType: NodeUnlockType.Minigame, cost: 15,
                    reward: NodeRewardType.PassiveStat, passive: PassiveStatType.MaxHP, passiveVal: 20,
                    pos: new Vector2(1, 0)),

                // ── 结丹/金丹期 (地火之境) ──
                Node("sy_jd_1", "三阳真脉", "打通三阳经脉，提升根骨。", RealmLevel.JinDan,
                    prereq: "sy_zj_1", unlockType: NodeUnlockType.Comprehension, cost: 20,
                    reward: NodeRewardType.PassiveStat, passive: PassiveStatType.ShenShi, passiveVal: 3,
                    pos: new Vector2(2, 0)),
                Node("sy_jd_2a", "壹·五内蕴火", "五脏蕴火，提升全五维。", RealmLevel.JinDan,
                    prereq: "sy_jd_1", unlockType: NodeUnlockType.Comprehension, mutex: "sy_jd_branch",
                    reward: NodeRewardType.PassiveStat, passive: PassiveStatType.MaxHP, passiveVal: 30,
                    pos: new Vector2(2, 1)),
                Node("sy_jd_2b", "贰·五内蕴火", "更深层的五内蕴火，大幅提升全五维。", RealmLevel.JinDan,
                    prereq: "sy_jd_1", unlockType: NodeUnlockType.Comprehension, mutex: "sy_jd_branch",
                    reward: NodeRewardType.PassiveStat, passive: PassiveStatType.MaxHP, passiveVal: 50,
                    pos: new Vector2(2, 2)),
                Node("sy_jd_2c", "叁·五内蕴火", "最深层的五内蕴火，极大提升全五维。", RealmLevel.JinDan,
                    prereq: "sy_jd_1", unlockType: NodeUnlockType.Comprehension, mutex: "sy_jd_branch",
                    reward: NodeRewardType.PassiveStat, passive: PassiveStatType.MaxHP, passiveVal: 70,
                    pos: new Vector2(2, 3)),
                Node("sy_jd_3", "御真秘典", "参悟御真之法，提升术法威力。", RealmLevel.JinDan,
                    prereq: "sy_jd_2a", unlockType: NodeUnlockType.Comprehension, cost: 20,
                    reward: NodeRewardType.PassiveStat, passive: PassiveStatType.MaxHP, passiveVal: 1,
                    pos: new Vector2(2, 4)),
                Node("sy_jd_4", "烈焰诀", "修炼烈焰术法。", RealmLevel.JinDan,
                    prereq: "sy_jd_3", unlockType: NodeUnlockType.Comprehension, cost: 15,
                    reward: NodeRewardType.Card, rewardIds: new List<string>{"sw_br_ignite"},
                    pos: new Vector2(2, 5)),
                Node("sy_jd_5", "净火灼心", "以净火炼心，提升悟性。", RealmLevel.JinDan,
                    prereq: "sy_jd_4", unlockType: NodeUnlockType.Comprehension, cost: 15,
                    reward: NodeRewardType.PassiveStat, passive: PassiveStatType.MaxMana, passiveVal: 1,
                    pos: new Vector2(2, 6)),

                // ── 元婴期 (天火之境) ──
                Node("sy_yy_1", "入定之术", "修炼入定之法。", RealmLevel.YuanYing,
                    prereq: "sy_jd_5", unlockType: NodeUnlockType.Comprehension, cost: 30,
                    reward: NodeRewardType.Card, rewardIds: new List<string>{"5-Heal_Basic"},
                    pos: new Vector2(3, 0)),
                Node("sy_yy_2", "灵息诀", "调息之法，提升灵气上限。", RealmLevel.YuanYing,
                    prereq: "sy_yy_1", unlockType: NodeUnlockType.Comprehension, cost: 25,
                    reward: NodeRewardType.Card, rewardIds: new List<string>{"5-Heal_Basic"},
                    pos: new Vector2(3, 1)),
                Node("sy_yy_3", "玄牝重生法", "重生秘法。", RealmLevel.YuanYing,
                    prereq: "sy_yy_2", unlockType: NodeUnlockType.Comprehension, cost: 30,
                    reward: NodeRewardType.Card, rewardIds: new List<string>{"5-Heal_Basic"},
                    pos: new Vector2(3, 2)),
                Node("sy_yy_4a", "壹·始日真诀", "提升术法威力。", RealmLevel.YuanYing,
                    prereq: "sy_yy_3", unlockType: NodeUnlockType.Comprehension, mutex: "sy_yy_branch1",
                    reward: NodeRewardType.PassiveStat, passive: PassiveStatType.MaxHP, passiveVal: 2,
                    pos: new Vector2(3, 3)),
                Node("sy_yy_4b", "贰·焚日真法", "更强的术法威力提升。", RealmLevel.YuanYing,
                    prereq: "sy_yy_3", unlockType: NodeUnlockType.Comprehension, mutex: "sy_yy_branch1",
                    reward: NodeRewardType.PassiveStat, passive: PassiveStatType.MaxHP, passiveVal: 3,
                    pos: new Vector2(3, 4)),
                Node("sy_yy_5", "三阳赤心咒", "提升法术威力。", RealmLevel.YuanYing,
                    prereq: "sy_yy_4a", unlockType: NodeUnlockType.Comprehension, cost: 25,
                    reward: NodeRewardType.PassiveStat, passive: PassiveStatType.MaxHP, passiveVal: 1,
                    pos: new Vector2(3, 5)),

                // ── 化神期 (神火之境) ──
                Node("sy_hs_1a", "壹·焚风术", "焚烧罡风，攻击术法。", RealmLevel.HuaShen,
                    prereq: "sy_yy_5", unlockType: NodeUnlockType.Comprehension, mutex: "sy_hs_branch1",
                    reward: NodeRewardType.Card, rewardIds: new List<string>{"sw_br_annihilate"},
                    pos: new Vector2(4, 0)),
                Node("sy_hs_1b", "贰·九阳神火界", "九阳神火领域，群攻术法。(真仙)", RealmLevel.HuaShen,
                    prereq: "sy_yy_5", unlockType: NodeUnlockType.Comprehension, mutex: "sy_hs_branch1",
                    reward: NodeRewardType.Card, rewardIds: new List<string>{"leg_spirit_chaos"},
                    pos: new Vector2(4, 1)),
                Node("sy_hs_2", "命火筑灵", "以命火筑灵基，大幅提升灵气上限。", RealmLevel.HuaShen,
                    prereq: "sy_hs_1a", unlockType: NodeUnlockType.Comprehension, cost: 40,
                    reward: NodeRewardType.PassiveStat, passive: PassiveStatType.MaxMana, passiveVal: 2,
                    pos: new Vector2(4, 2)),
                Node("sy_hs_3", "伐毛洗髓", "伐毛洗髓，提升五维。", RealmLevel.HuaShen,
                    prereq: "sy_hs_2", unlockType: NodeUnlockType.Material, cost: 0,
                    reward: NodeRewardType.PassiveStat, passive: PassiveStatType.MaxHP, passiveVal: 50,
                    pos: new Vector2(4, 3)),
                Node("sy_hs_4a", "壹·心火", "降低术法灵气消耗。", RealmLevel.HuaShen,
                    prereq: "sy_hs_3", unlockType: NodeUnlockType.Comprehension, mutex: "sy_hs_branch2",
                    reward: NodeRewardType.PassiveStat, passive: PassiveStatType.MaxMana, passiveVal: 1,
                    pos: new Vector2(4, 4)),
                Node("sy_hs_4b", "贰·心炎", "更深层次降低消耗。", RealmLevel.HuaShen,
                    prereq: "sy_hs_3", unlockType: NodeUnlockType.Comprehension, mutex: "sy_hs_branch2",
                    reward: NodeRewardType.PassiveStat, passive: PassiveStatType.MaxMana, passiveVal: 2,
                    pos: new Vector2(4, 5)),
                Node("sy_hs_5a", "壹·真火炼身", "提升法术威力。", RealmLevel.HuaShen,
                    prereq: "sy_hs_4a", unlockType: NodeUnlockType.Comprehension, mutex: "sy_hs_branch3",
                    reward: NodeRewardType.PassiveStat, passive: PassiveStatType.MaxHP, passiveVal: 2,
                    pos: new Vector2(4, 6)),
                Node("sy_hs_5b", "贰·火真之体", "更高层次法术威力。", RealmLevel.HuaShen,
                    prereq: "sy_hs_4a", unlockType: NodeUnlockType.Comprehension, mutex: "sy_hs_branch3",
                    reward: NodeRewardType.PassiveStat, passive: PassiveStatType.MaxHP, passiveVal: 3,
                    pos: new Vector2(4, 7)),
                Node("sy_hs_6", "通天神火", "通天神火术法。", RealmLevel.HuaShen,
                    prereq: "sy_hs_5a", unlockType: NodeUnlockType.CombatTrigger, cost: 0,
                    reward: NodeRewardType.Card, rewardIds: new List<string>{"leg_sword_god2"},
                    pos: new Vector2(4, 8)),
                Node("sy_hs_7a", "壹·天火同人", "提升机缘。", RealmLevel.HuaShen,
                    prereq: "sy_hs_6", unlockType: NodeUnlockType.Comprehension, mutex: "sy_hs_branch4",
                    reward: NodeRewardType.PassiveStat, passive: PassiveStatType.ShenShi, passiveVal: 3,
                    pos: new Vector2(4, 9)),
                Node("sy_hs_7b", "贰·天火同人", "更深层次机缘提升。", RealmLevel.HuaShen,
                    prereq: "sy_hs_6", unlockType: NodeUnlockType.Comprehension, mutex: "sy_hs_branch4",
                    reward: NodeRewardType.PassiveStat, passive: PassiveStatType.ShenShi, passiveVal: 5,
                    pos: new Vector2(4, 10)),

                // ── 渡劫期 ──
                Node("sy_dj_1", "突破·飞升", "渡过天劫，飞升成仙。", RealmLevel.DuJie,
                    prereq: "sy_hs_7a", unlockType: NodeUnlockType.CombatTrigger, cost: 0,
                    reward: NodeRewardType.Card, rewardIds: new List<string>{"leg_gen_judgment"},
                    pos: new Vector2(5, 0)),
            };

            method.EditNodes(nodes);
            EditorUtility.SetDirty(method);
            return 1;
        }
        #endregion

        #region 太和十六洞天
        private static int CreateTaiHe()
        {
            var method = CreateMethod("th_method", "太和十六洞天",
                "水属性功法，太一门镇派五行真诀之一。擅神通，功能强大，可辅助门派建设。共四境四四一十六洞天瓶颈，洞天圆满得以登仙。",
                ElementType.Water, CultivationMethodGrade.Complete, RealmLevel.DuJie);

            var nodes = new List<CultivationNodeData>
            {
                // ── 练气期 (凝气期) ──
                Node("th_lq_1", "封灵术", "太和十六洞天入门神通，提供水属性基础卡组。", RealmLevel.LianQi,
                    unlockType: NodeUnlockType.Comprehension, cost: 0,
                    reward: NodeRewardType.Card, rewardIds: new List<string>{
                        "sp_mn_bolt2","sp_mn_bolt2","sp_mn_bolt2","sp_mn_bolt2",
                        "sp_block_basic","sp_block_basic","sp_block_basic"},
                    pos: new Vector2(0, 0)),
                Node("th_lq_2", "赋灵术", "激活手工傀儡。", RealmLevel.LianQi,
                    prereq: "th_lq_1", unlockType: NodeUnlockType.Comprehension, cost: 5,
                    reward: NodeRewardType.Card, rewardIds: new List<string>{"3-Block_Basic"},
                    pos: new Vector2(0, 1)),
                Node("th_lq_3", "五泉涤心", "五泉洗涤心神，提升灵气上限。", RealmLevel.LianQi,
                    prereq: "th_lq_2", unlockType: NodeUnlockType.Comprehension, cost: 8,
                    reward: NodeRewardType.PassiveStat, passive: PassiveStatType.MaxMana, passiveVal: 1,
                    pos: new Vector2(0, 2)),
                Node("th_lq_4", "先天罡气", "炼就先天罡气，提升灵气上限。", RealmLevel.LianQi,
                    prereq: "th_lq_3", unlockType: NodeUnlockType.Comprehension, cost: 10,
                    reward: NodeRewardType.PassiveStat, passive: PassiveStatType.MaxMana, passiveVal: 1,
                    pos: new Vector2(0, 3)),
                Node("th_lq_5", "化伤术", "基础治疗术。", RealmLevel.LianQi,
                    prereq: "th_lq_4", unlockType: NodeUnlockType.Comprehension, cost: 8,
                    reward: NodeRewardType.Card, rewardIds: new List<string>{"5-Heal_Basic"},
                    pos: new Vector2(0, 4)),

                // ── 筑基期 (突破) ──
                Node("th_zj_1", "突破·周天", "打通周天，踏入筑基之境。", RealmLevel.ZhuJi,
                    prereq: "th_lq_5", unlockType: NodeUnlockType.Minigame, cost: 15,
                    reward: NodeRewardType.PassiveStat, passive: PassiveStatType.MaxHP, passiveVal: 20,
                    pos: new Vector2(1, 0)),

                // ── 结丹/金丹期 ──
                Node("th_jd_1", "求雨术", "求雨之术，可改变天气。", RealmLevel.JinDan,
                    prereq: "th_zj_1", unlockType: NodeUnlockType.Comprehension, cost: 20,
                    reward: NodeRewardType.Card, rewardIds: new List<string>{"sp_mn_bolt2"},
                    pos: new Vector2(2, 0)),
                Node("th_jd_2", "壹·甘霖术", "施肥灭火，恢复之术。", RealmLevel.JinDan,
                    prereq: "th_jd_1", unlockType: NodeUnlockType.Comprehension, cost: 15,
                    reward: NodeRewardType.Card, rewardIds: new List<string>{"5-Heal_Basic"},
                    pos: new Vector2(2, 1)),
                Node("th_jd_3", "风水鉴定", "鉴定镇物信息。", RealmLevel.JinDan,
                    prereq: "th_jd_2", unlockType: NodeUnlockType.Comprehension, cost: 15,
                    reward: NodeRewardType.CraftBonus, craftType: CraftBonusType.AlchemySuccess, craftVal: 0.1f,
                    pos: new Vector2(2, 2)),
                Node("th_jd_4", "太和御气", "提升术法威力。", RealmLevel.JinDan,
                    prereq: "th_jd_3", unlockType: NodeUnlockType.Comprehension, cost: 20,
                    reward: NodeRewardType.PassiveStat, passive: PassiveStatType.MaxHP, passiveVal: 1,
                    pos: new Vector2(2, 3)),
                Node("th_jd_5", "清心咒", "清心宁神。", RealmLevel.JinDan,
                    prereq: "th_jd_4", unlockType: NodeUnlockType.Comprehension, cost: 15,
                    reward: NodeRewardType.PassiveStat, passive: PassiveStatType.MaxMana, passiveVal: 1,
                    pos: new Vector2(2, 4)),
                Node("th_jd_6a", "壹·御木诀", "以阳寿施肥。", RealmLevel.JinDan,
                    prereq: "th_jd_5", unlockType: NodeUnlockType.Comprehension, mutex: "th_jd_branch1",
                    reward: NodeRewardType.Card, rewardIds: new List<string>{"sw_mh_twin"},
                    pos: new Vector2(2, 5)),
                Node("th_jd_6b", "贰·天霖诀", "以灵气施肥。", RealmLevel.JinDan,
                    prereq: "th_jd_5", unlockType: NodeUnlockType.Comprehension, mutex: "th_jd_branch1",
                    reward: NodeRewardType.Card, rewardIds: new List<string>{"sw_mh_storm"},
                    pos: new Vector2(2, 6)),
                Node("th_jd_7a", "壹·天伤化水", "恢复友方灵气。", RealmLevel.JinDan,
                    prereq: "th_jd_6a", unlockType: NodeUnlockType.Comprehension, mutex: "th_jd_branch2",
                    reward: NodeRewardType.PassiveStat, passive: PassiveStatType.MaxMana, passiveVal: 1,
                    pos: new Vector2(2, 7)),
                Node("th_jd_7b", "贰·天一真法", "大幅恢复友方灵气。", RealmLevel.JinDan,
                    prereq: "th_jd_6a", unlockType: NodeUnlockType.Comprehension, mutex: "th_jd_branch2",
                    reward: NodeRewardType.PassiveStat, passive: PassiveStatType.MaxMana, passiveVal: 2,
                    pos: new Vector2(2, 8)),
                Node("th_jd_8", "采水诀", "采集灵水，提升灵气。", RealmLevel.JinDan,
                    prereq: "th_jd_7a", unlockType: NodeUnlockType.Comprehension, cost: 15,
                    reward: NodeRewardType.PassiveStat, passive: PassiveStatType.MaxMana, passiveVal: 1,
                    pos: new Vector2(2, 9)),

                // ── 元婴期 (真丹期) ──
                Node("th_yy_1a", "壹·入定之术", "修炼入定之法。", RealmLevel.YuanYing,
                    prereq: "th_jd_8", unlockType: NodeUnlockType.Comprehension, mutex: "th_yy_branch1",
                    reward: NodeRewardType.Card, rewardIds: new List<string>{"5-Heal_Basic"},
                    pos: new Vector2(3, 0)),
                Node("th_yy_1b", "贰·水镜观心定", "更深层次入定。(元神)", RealmLevel.YuanYing,
                    prereq: "th_jd_8", unlockType: NodeUnlockType.Comprehension, mutex: "th_yy_branch1",
                    reward: NodeRewardType.Card, rewardIds: new List<string>{"5-Heal_Basic"},
                    pos: new Vector2(3, 1)),
                Node("th_yy_2", "灵息诀", "调息恢复灵力。", RealmLevel.YuanYing,
                    prereq: "th_yy_1a", unlockType: NodeUnlockType.Comprehension, cost: 25,
                    reward: NodeRewardType.Card, rewardIds: new List<string>{"5-Heal_Basic"},
                    pos: new Vector2(3, 2)),
                Node("th_yy_3", "玄牝重生法", "重生秘法。", RealmLevel.YuanYing,
                    prereq: "th_yy_2", unlockType: NodeUnlockType.Comprehension, cost: 30,
                    reward: NodeRewardType.Card, rewardIds: new List<string>{"5-Heal_Basic"},
                    pos: new Vector2(3, 3)),
                Node("th_yy_4a", "壹·乘风诀", "乘风之术。", RealmLevel.YuanYing,
                    prereq: "th_yy_3", unlockType: NodeUnlockType.Comprehension, mutex: "th_yy_branch2",
                    reward: NodeRewardType.PassiveStat, passive: PassiveStatType.ShenShi, passiveVal: 3,
                    pos: new Vector2(3, 4)),
                Node("th_yy_4b", "贰·同风诀", "更深层次乘风。", RealmLevel.YuanYing,
                    prereq: "th_yy_3", unlockType: NodeUnlockType.Comprehension, mutex: "th_yy_branch2",
                    reward: NodeRewardType.PassiveStat, passive: PassiveStatType.ShenShi, passiveVal: 5,
                    pos: new Vector2(3, 5)),
                Node("th_yy_5a", "壹·奔流诀", "奔流之术。", RealmLevel.YuanYing,
                    prereq: "th_yy_4a", unlockType: NodeUnlockType.Comprehension, mutex: "th_yy_branch3",
                    reward: NodeRewardType.PassiveStat, passive: PassiveStatType.MaxHP, passiveVal: 2,
                    pos: new Vector2(3, 6)),
                Node("th_yy_5b", "贰·归海诀", "更深层次奔流。", RealmLevel.YuanYing,
                    prereq: "th_yy_4a", unlockType: NodeUnlockType.Comprehension, mutex: "th_yy_branch3",
                    reward: NodeRewardType.PassiveStat, passive: PassiveStatType.MaxHP, passiveVal: 3,
                    pos: new Vector2(3, 7)),
                // 三选一互斥: 水精真形/水气真形/水神真形
                Node("th_yy_6a", "水精真形", "提升根骨。", RealmLevel.YuanYing,
                    prereq: "th_yy_5a", unlockType: NodeUnlockType.Comprehension, mutex: "th_yy_triple",
                    reward: NodeRewardType.PassiveStat, passive: PassiveStatType.ShenShi, passiveVal: 5,
                    pos: new Vector2(3, 8)),
                Node("th_yy_6b", "水气真形", "提升魅力。", RealmLevel.YuanYing,
                    prereq: "th_yy_5a", unlockType: NodeUnlockType.Comprehension, mutex: "th_yy_triple",
                    reward: NodeRewardType.PassiveStat, passive: PassiveStatType.MaxHP, passiveVal: 40,
                    pos: new Vector2(3, 9)),
                Node("th_yy_6c", "水神真形", "提升悟性。", RealmLevel.YuanYing,
                    prereq: "th_yy_5a", unlockType: NodeUnlockType.Comprehension, mutex: "th_yy_triple",
                    reward: NodeRewardType.PassiveStat, passive: PassiveStatType.MaxMana, passiveVal: 1,
                    pos: new Vector2(3, 10)),

                // ── 化神期 (大乘期) ──
                Node("th_hs_1a", "壹·暴雨术", "召唤暴雨。", RealmLevel.HuaShen,
                    prereq: "th_yy_6a", unlockType: NodeUnlockType.Comprehension, mutex: "th_hs_branch1",
                    reward: NodeRewardType.Card, rewardIds: new List<string>{"sp_mn_storm2"},
                    pos: new Vector2(4, 0)),
                Node("th_hs_1b", "贰·九阴真水界", "九阴真水领域。(真仙)", RealmLevel.HuaShen,
                    prereq: "th_yy_6a", unlockType: NodeUnlockType.Comprehension, mutex: "th_hs_branch1",
                    reward: NodeRewardType.Card, rewardIds: new List<string>{"leg_spirit_chaos"},
                    pos: new Vector2(4, 1)),
                Node("th_hs_2", "玄魂造命", "使物品通灵。", RealmLevel.HuaShen,
                    prereq: "th_hs_1a", unlockType: NodeUnlockType.Material, cost: 0,
                    reward: NodeRewardType.CraftBonus, craftType: CraftBonusType.AlchemyQuality, craftVal: 0.15f,
                    pos: new Vector2(4, 2)),
                Node("th_hs_3a", "壹·罡煞合流", "罡煞合一。", RealmLevel.HuaShen,
                    prereq: "th_hs_2", unlockType: NodeUnlockType.Comprehension, mutex: "th_hs_branch2",
                    reward: NodeRewardType.PassiveStat, passive: PassiveStatType.MaxHP, passiveVal: 2,
                    pos: new Vector2(4, 3)),
                Node("th_hs_3b", "贰·罡煞合流", "更深层次罡煞合一。", RealmLevel.HuaShen,
                    prereq: "th_hs_2", unlockType: NodeUnlockType.Comprehension, mutex: "th_hs_branch2",
                    reward: NodeRewardType.PassiveStat, passive: PassiveStatType.MaxHP, passiveVal: 3,
                    pos: new Vector2(4, 4)),
                Node("th_hs_3c", "叁·罡煞合流", "最深层次罡煞合一。", RealmLevel.HuaShen,
                    prereq: "th_hs_2", unlockType: NodeUnlockType.Comprehension, mutex: "th_hs_branch2",
                    reward: NodeRewardType.PassiveStat, passive: PassiveStatType.MaxHP, passiveVal: 4,
                    pos: new Vector2(4, 5)),
                Node("th_hs_4", "醍醐灌顶", "五维淬体，提升悟性。", RealmLevel.HuaShen,
                    prereq: "th_hs_3a", unlockType: NodeUnlockType.CombatTrigger, cost: 0,
                    reward: NodeRewardType.PassiveStat, passive: PassiveStatType.MaxMana, passiveVal: 1,
                    pos: new Vector2(4, 6)),

                // ── 渡劫期 (飞升) ──
                Node("th_dj_1", "突破·飞升", "渡过天劫，飞升成仙。", RealmLevel.DuJie,
                    prereq: "th_hs_4", unlockType: NodeUnlockType.CombatTrigger, cost: 0,
                    reward: NodeRewardType.Card, rewardIds: new List<string>{"leg_taiji"},
                    pos: new Vector2(5, 0)),
            };

            method.EditNodes(nodes);
            EditorUtility.SetDirty(method);
            return 1;
        }
        #endregion

        #region 长春功
        private static int CreateChangChun()
        {
            var method = CreateMethod("cc_method", "长春功",
                "木属性功法，以木灵之气温养经脉，擅持久恢复。修炼者如草木逢春，生生不息。此为残篇，仅可修至筑基。",
                ElementType.Wood, CultivationMethodGrade.Fragment, RealmLevel.ZhuJi);

            var nodes = new List<CultivationNodeData>
            {
                // ── 练气期 (扎根) ──
                Node("cc_lq_1", "回春术", "长春功入门神通，提供木属性基础卡组。", RealmLevel.LianQi,
                    unlockType: NodeUnlockType.Comprehension, cost: 0,
                    reward: NodeRewardType.Card, rewardIds: new List<string>{
                        "bd_th_spike","bd_th_spike","bd_th_spike","bd_th_spike",
                        "bd_su_shield2","bd_su_shield2","bd_su_shield2"},
                    pos: new Vector2(0, 0)),
                Node("cc_lq_2", "木甲术", "以木气凝甲，基础防护之法。", RealmLevel.LianQi,
                    unlockType: NodeUnlockType.Comprehension, cost: 5,
                    reward: NodeRewardType.Card, rewardIds: new List<string>{"3-Block_Basic"},
                    pos: new Vector2(0, 1)),
                Node("cc_lq_3", "蕴灵诀", "引木灵之气入体，提升灵力上限。", RealmLevel.LianQi,
                    prereq: "cc_lq_2", unlockType: NodeUnlockType.Comprehension, cost: 8,
                    reward: NodeRewardType.PassiveStat, passive: PassiveStatType.MaxMana, passiveVal: 1,
                    pos: new Vector2(0, 2)),
                Node("cc_lq_4", "青木功", "强化体质，提升生命上限。", RealmLevel.LianQi,
                    prereq: "cc_lq_3", unlockType: NodeUnlockType.Comprehension, cost: 10,
                    reward: NodeRewardType.PassiveStat, passive: PassiveStatType.MaxHP, passiveVal: 10,
                    pos: new Vector2(0, 3)),
                Node("cc_lq_5", "固根法", "扎根大地，进一步提升灵力。", RealmLevel.LianQi,
                    prereq: "cc_lq_4", unlockType: NodeUnlockType.Comprehension, cost: 8,
                    reward: NodeRewardType.PassiveStat, passive: PassiveStatType.MaxMana, passiveVal: 1,
                    pos: new Vector2(0, 4)),

                // ── 筑基期 (抽枝) ──
                Node("cc_zj_1", "突破·长春", "凝聚木灵之气，冲破筑基瓶颈。", RealmLevel.ZhuJi,
                    prereq: "cc_lq_5", unlockType: NodeUnlockType.Minigame, cost: 15,
                    reward: NodeRewardType.PassiveStat, passive: PassiveStatType.MaxHP, passiveVal: 20,
                    pos: new Vector2(1, 0)),
                Node("cc_zj_2a", "枯木逢春", "专注恢复之道，大幅提升生命上限。", RealmLevel.ZhuJi,
                    prereq: "cc_zj_1", unlockType: NodeUnlockType.Comprehension, mutex: "cc_zj_branch",
                    reward: NodeRewardType.PassiveStat, passive: PassiveStatType.MaxHP, passiveVal: 30,
                    pos: new Vector2(1, 1)),
                Node("cc_zj_2b", "铁木之躯", "专注防御之道，兼顾生命与灵力。", RealmLevel.ZhuJi,
                    prereq: "cc_zj_1", unlockType: NodeUnlockType.Comprehension, mutex: "cc_zj_branch",
                    reward: NodeRewardType.PassiveStat, passive: PassiveStatType.MaxHP, passiveVal: 15,
                    pos: new Vector2(1, 2)),
                Node("cc_zj_3", "荆棘护体", "以木刺反伤，习得荆棘术。", RealmLevel.ZhuJi,
                    prereq: "cc_zj_2a", unlockType: NodeUnlockType.Comprehension, cost: 15,
                    reward: NodeRewardType.Card, rewardIds: new List<string>{"bd_th_spike"},
                    pos: new Vector2(1, 3)),
                Node("cc_zj_4", "自愈术", "习得自愈之术，战斗中持续恢复。", RealmLevel.ZhuJi,
                    prereq: "cc_zj_3", unlockType: NodeUnlockType.Comprehension, cost: 15,
                    reward: NodeRewardType.Card, rewardIds: new List<string>{"bd_su_regen"},
                    pos: new Vector2(1, 4)),
            };

            method.EditNodes(nodes);
            EditorUtility.SetDirty(method);
            return 1;
        }
        #endregion

        #region Helpers
        private static int CreateAbility(string id, string name, string desc,
            ElementType element, string cardId, int energyCost)
        {
            var path = $"{AbilityDir}/{id}.asset";
            var ability = AssetDatabase.LoadAssetAtPath<DivineAbilityData>(path);
            if (ability == null)
            {
                ability = ScriptableObject.CreateInstance<DivineAbilityData>();
                AssetDatabase.CreateAsset(ability, path);
            }
            ability.EditAbilityId(id);
            ability.EditAbilityName(name);
            ability.EditDescription(desc);
            ability.EditElement(element);
            ability.EditCardId(cardId);
            ability.EditEnergyCost(energyCost);
            EditorUtility.SetDirty(ability);
            return 1;
        }

        private static CultivationMethodData CreateMethod(string id, string name, string desc,
            ElementType element, CultivationMethodGrade grade, RealmLevel maxRealm)
        {
            var path = $"{MethodDir}/{id}.asset";
            var method = AssetDatabase.LoadAssetAtPath<CultivationMethodData>(path);
            if (method == null)
            {
                method = ScriptableObject.CreateInstance<CultivationMethodData>();
                AssetDatabase.CreateAsset(method, path);
            }
            method.EditMethodId(id);
            method.EditMethodName(name);
            method.EditDescription(desc);
            method.EditElement(element);
            method.EditGrade(grade);
            method.EditMaxRealm(maxRealm);
            return method;
        }

        private static CultivationNodeData Node(string id, string name, string desc, RealmLevel realm,
            string prereq = null, NodeUnlockType unlockType = NodeUnlockType.Comprehension,
            int cost = 0, string mutex = null,
            NodeRewardType reward = NodeRewardType.PassiveStat, List<string> rewardIds = null,
            PassiveStatType passive = PassiveStatType.None, int passiveVal = 0,
            CraftBonusType craftType = CraftBonusType.None, float craftVal = 0f,
            Vector2 pos = default)
        {
            var node = new CultivationNodeData();
            node.EditNodeId(id);
            node.EditNodeName(name);
            node.EditDescription(desc);
            node.EditRealm(realm);
            node.EditGridIndex(pos);
            node.EditUnlockType(unlockType);
            node.EditComprehensionCost(cost);
            node.EditMutexGroup(mutex);
            node.EditRewardType(reward);
            node.EditRewardIds(rewardIds ?? new List<string>());
            node.EditPassiveStat(passive);
            node.EditPassiveValue(passiveVal);
            node.EditCraftBonusType(craftType);
            node.EditCraftBonusValue(craftVal);
            var prereqList = string.IsNullOrEmpty(prereq) ? new List<string>() : new List<string> { prereq };
            node.EditPrerequisites(prereqList);
            return node;
        }
        #endregion
    }
}
