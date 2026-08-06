using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using NueGames.NueDeck.Scripts.Characters;
using NueGames.NueDeck.Scripts.Data.Characters;
using NueGames.NueDeck.Scripts.Data.Containers;
using NueGames.NueDeck.Scripts.Enums;
using System.IO;

namespace NueGames.NueDeck.Scripts.Editor
{
    /// <summary>
    /// 敌人批量生成器：从设计文档数据批量创建 EnemyCharacterData SO。
    /// 用法：Tools/Generate Enemies
    /// </summary>
    public static class EnemyBatchGenerator
    {
        // Shared references (set once)
        static EnemyBase prefabRef;
        static EnemyIntentionData attackIntent, defendIntent, healIntent, debuffIntent, specialIntent;

        [MenuItem("Tools/Generate Enemies")]
        public static void GenerateAll()
        {
            string baseDir = "Assets/NueGames/NueDeck/Data/Enemies";
            EnsureDir(Path.Combine(baseDir, "Region1_ShanYe"));
            EnsureDir(Path.Combine(baseDir, "Region2_YouMing"));
            EnsureDir(Path.Combine(baseDir, "Region3_WanGu"));
            EnsureDir(Path.Combine(baseDir, "Region4_TianMo"));

            // Load shared prefab + intentions
            prefabRef = AssetDatabase.LoadAssetAtPath<EnemyBase>("Assets/NueGames/NueDeck/Prefabs/Characters/Enemy 1.prefab");
            attackIntent = LoadIntention("Attack Intention");
            defendIntent = LoadIntention("Defend Intention");
            healIntent = LoadIntention("Heal Intention");
            debuffIntent = LoadIntention("Debuff Intention");
            specialIntent = LoadIntention("Special Intention");

            int created = 0;
            created += GenerateRegion1(baseDir + "/Region1_ShanYe");
            created += GenerateRegion2(baseDir + "/Region2_YouMing");
            created += GenerateRegion3(baseDir + "/Region3_WanGu");
            created += GenerateRegion4(baseDir + "/Region4_TianMo");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"EnemyBatchGenerator: Created {created} enemy assets");
        }

        static EnemyIntentionData LoadIntention(string name)
        {
            var guids = AssetDatabase.FindAssets(name, new[]{"Assets/NueGames/NueDeck/Data/EnemyIntentionData"});
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                if (path.EndsWith(".asset"))
                    return AssetDatabase.LoadAssetAtPath<EnemyIntentionData>(path);
            }
            Debug.LogWarning($"Intention not found: {name}");
            return null;
        }

        static int GenerateRegion1(string dir)
        {
            int created = 0;
            // ===== 普通敌人 (30) =====
            created += Create("s1_bandit", "落草散修", 28, EnemyTier.Normal, 0, false, dir,
                "被逐出宗门的散修，占山为寇，专劫过往行商。虽无师门传承，却练就一身扎实剑术。",
                "此山是我开，此树是我栽！识相的留下灵石，饶你不死！",
                "哼，区区散修也敢劫道？看来是活腻了。",
                new[]{
                    Ab("横劈", attackIntent, false, new[]{Ac(EnemyActionType.Attack,6,6)}),
                    Ab("双连斩", attackIntent, false, new[]{Ac(EnemyActionType.Attack,4,4),Ac(EnemyActionType.Attack,4,4)}),
                });
            created += Create("s1_rogue", "逆修者", 36, EnemyTier.Normal, 0, false, dir,
                "逆练功法走火入魔的修士，心智已乱，见人便砍。肉身被魔气侵蚀，力大无穷。",
                "功法……我的功法……都给我……",
                "逆练功法，害人害己，今日便为你超度。",
                new[]{
                    Ab("疯魔斩", attackIntent, false, new[]{Ac(EnemyActionType.Attack,5,5),Ac(EnemyActionType.Attack,5,5)}),
                    Ab("全力一击", attackIntent, false, new[]{Ac(EnemyActionType.Attack,9,9)}),
                });
            created += Create("s1_wolf", "贪狼煞兽", 42, EnemyTier.Normal, 0, false, dir,
                "吞噬山川煞气化形的凶狼，双目赤红，獠牙滴血。啸声能摄人心魄，令猎物胆寒。",
                "嗷呜——！（赤红双目死死盯着你，獠牙毕露）",
                "畜生，受死！",
                new[]{
                    Ab("煞气撕咬", attackIntent, false, new[]{Ac(EnemyActionType.Attack,8,8)}),
                    Ab("慑魂狼嚎", debuffIntent, false, new[]{Ac(EnemyActionType.ApplyWeak,2,2)}),
                    Ab("扑杀", attackIntent, false, new[]{Ac(EnemyActionType.Attack,6,6)}),
                });
            created += Create("s1_golem", "灵石守卫", 55, EnemyTier.Normal, 0, false, dir,
                "上古修士以灵石所炼的护山傀儡，坚不可摧。只认令符不认人，擅闯者必被碾碎。",
                "嗡……（灵石核心亮起，傀儡身躯轰然站起）",
                "一块石头也敢挡路？",
                new[]{
                    Ab("重拳", attackIntent, false, new[]{Ac(EnemyActionType.Attack,5,5)}),
                    Ab("灵石护体", defendIntent, false, new[]{Ac(EnemyActionType.Block,12,12)}),
                });
            created += Create("s1_snake", "碧磷蛇妖", 35, EnemyTier.Normal, 0, false, dir,
                "在灵矿毒气中修炼百年的蛇妖，鳞片泛着碧绿磷光。毒牙藏有剧毒，咬中即腐。",
                "嘶——（蛇瞳冰冷，缓缓缠向你）",
                "毒蛇作祟，留你不得！",
                new[]{
                    Ab("毒牙", attackIntent, false, new[]{Ac(EnemyActionType.Poison,3,3)}),
                    Ab("吐信", attackIntent, false, new[]{Ac(EnemyActionType.Attack,5,5)}),
                });
            created += Create("s1_boar", "野猪妖", 48, EnemyTier.Normal, 0, false, dir,
                "山海间偶得机缘开了灵智的野猪，皮糙肉厚。暴怒时低头猛冲，势不可挡。",
                "哼哼！（野猪妖刨着蹄子，眼中凶光毕露）",
                "好一头蛮牛！",
                new[]{
                    Ab("拱击", attackIntent, false, new[]{Ac(EnemyActionType.Attack,7,7)}),
                    Ab("蓄力猛冲", attackIntent, true, new[]{Ac(EnemyActionType.Attack,14,14)}),
                });
            created += Create("s1_butterfly", "幻舞灵蝶", 30, EnemyTier.Normal, 0, false, dir,
                "山谷灵气孕育的灵蝶，扇动翅膀便能迷惑人心。看似柔弱，实则暗藏幻术。",
                "（灵蝶鳞粉飞扬，化作一片迷离光幕）",
                "好一只妖蝶，竟敢蛊惑于我。",
                new[]{
                    Ab("迷离粉", debuffIntent, false, new[]{Ac(EnemyActionType.ApplyVulnerable,2,2)}),
                    Ab("幻惑", debuffIntent, false, new[]{Ac(EnemyActionType.ApplyWeak,2,2)}),
                    Ab("灵光疗愈", healIntent, false, new[]{Ac(EnemyActionType.Heal,3,3)}),
                });
            created += Create("s1_treeent", "百年树灵", 45, EnemyTier.Normal, 0, false, dir,
                "扎根千年的古树孕育出灵智，枝叶如手臂般挥动。能汲取大地精华恢复自身。",
                "（古树缓缓睁开浑浊的双眼，枝干发出吱嘎声）",
                "树妖拦路，斩了便是。",
                new[]{
                    Ab("藤蔓抽打", attackIntent, false, new[]{Ac(EnemyActionType.Attack,6,6)}),
                    Ab("汲取精华", healIntent, false, new[]{Ac(EnemyActionType.Heal,4,4)}),
                    Ab("树皮护体", defendIntent, false, new[]{Ac(EnemyActionType.Block,8,8)}),
                });
            created += Create("s1_bat", "血蝠", 34, EnemyTier.Normal, 0, false, dir,
                "嗜血如命的暗夜蝙蝠，成群结队出没。吸血时能盗取猎物生机滋养己身。",
                "吱吱吱！（蝠群扑棱着翅膀，盘旋在你头顶）",
                "一群吸血杂碎！",
                new[]{
                    Ab("吸血啄击", attackIntent, false, new[]{Ac(EnemyActionType.Attack,5,5),Ac(EnemyActionType.Heal,3,3)}),
                    Ab("俯冲", attackIntent, false, new[]{Ac(EnemyActionType.Attack,7,7)}),
                });
            // === 普通敌人 10-30 ===
            created += Create("s1_scout", "斥候", 32, EnemyTier.Normal, 0, false, dir,
                "游荡在山野的斥候，擅长侦察与骚扰。身法灵活但攻击不高。",
                "嘘……别动，我先探探你的底。",
                "探什么底，受死吧！",
                new[]{ Ab("偷袭", attackIntent, false, new[]{Ac(EnemyActionType.Attack,6,6)}), Ab("绊脚", debuffIntent, false, new[]{Ac(EnemyActionType.ApplyWeak,1,1)}), Ab("破绽", debuffIntent, false, new[]{Ac(EnemyActionType.ApplyVulnerable,1,1)}) });
            created += Create("s1_spear", "枪兵", 40, EnemyTier.Normal, 0, true, dir,
                "手持灵枪的山野护卫，枪法刚猛。三式循环，攻守兼备。",
                "枪出如龙！接招吧！",
                "枪法不错，可惜对手是我。",
                new[]{ Ab("突刺", attackIntent, false, new[]{Ac(EnemyActionType.Attack,7,7)}), Ab("横扫", attackIntent, false, new[]{Ac(EnemyActionType.Attack,5,5),Ac(EnemyActionType.Attack,5,5)}), Ab("枪盾", defendIntent, false, new[]{Ac(EnemyActionType.Block,5,5)}) });
            created += Create("s1_archer", "弓手", 30, EnemyTier.Normal, 0, true, dir,
                "山中猎户出身的弓手，擅长远程射击。箭矢带破甲之力。",
                "站住！让我看看你的破绽在哪里……",
                "弓箭手，近身便无用。",
                new[]{ Ab("破甲箭", debuffIntent, false, new[]{Ac(EnemyActionType.ApplyVulnerable,2,2)}), Ab("穿云箭", attackIntent, false, new[]{Ac(EnemyActionType.Attack,8,8)}) });
            created += Create("s1_bear", "熊妖", 60, EnemyTier.Normal, 0, true, dir,
                "修炼百年化形的熊妖，皮糙肉厚力大无穷。暴怒时双掌连击。",
                "吼——！（熊妖怒目圆睁，举起巨掌）",
                "好大一头熊！",
                new[]{ Ab("熊掌", attackIntent, false, new[]{Ac(EnemyActionType.Attack,8,8)}), Ab("护体", defendIntent, false, new[]{Ac(EnemyActionType.Block,6,6)}), Ab("暴怒连击", attackIntent, false, new[]{Ac(EnemyActionType.Attack,8,8),Ac(EnemyActionType.Attack,8,8)}) });
            created += Create("s1_fox", "狐妖", 38, EnemyTier.Normal, 0, true, dir,
                "九尾狐族的后裔，擅幻术与魅惑。看似娇弱，实则诡计多端。",
                "嘻嘻，小修士，陪姐姐玩玩？",
                "妖狐，休要蛊惑人心！",
                new[]{ Ab("魅惑", debuffIntent, false, new[]{Ac(EnemyActionType.ApplyWeak,2,2)}), Ab("迷眼", debuffIntent, false, new[]{Ac(EnemyActionType.ApplyVulnerable,2,2)}), Ab("爪击", attackIntent, false, new[]{Ac(EnemyActionType.Attack,5,5)}), Ab("灵光", healIntent, false, new[]{Ac(EnemyActionType.Heal,3,3)}) });
            created += Create("s1_rat", "妖鼠", 25, EnemyTier.Normal, 0, false, dir,
                "啃食灵谷变异的妖鼠，体型虽小但繁殖极快。常成群出没。",
                "吱吱……（妖鼠警惕地嗅着空气）",
                "区区鼠辈。",
                new[]{ Ab("啃咬", attackIntent, false, new[]{Ac(EnemyActionType.Attack,3,3)}), Ab("毒咬", attackIntent, false, new[]{Ac(EnemyActionType.Poison,2,2)}), Ab("偷袭", attackIntent, false, new[]{Ac(EnemyActionType.Attack,3,3)}) });
            created += Create("s1_ratswarm", "鼠群", 42, EnemyTier.Normal, 0, true, dir,
                "数十只妖鼠聚成的鼠群，漫山遍野。单只弱小但数量惊人。",
                "吱吱吱——！（鼠群如潮水般涌来）",
                "一群老鼠，也敢挡路？",
                new[]{ Ab("群涌", attackIntent, false, new[]{Ac(EnemyActionType.Attack,2,2),Ac(EnemyActionType.Attack,2,2),Ac(EnemyActionType.Attack,2,2)}), Ab("群涌", attackIntent, false, new[]{Ac(EnemyActionType.Attack,2,2),Ac(EnemyActionType.Attack,2,2),Ac(EnemyActionType.Attack,2,2)}), Ab("毒鼠", debuffIntent, false, new[]{Ac(EnemyActionType.Poison,2,2)}) });
            created += Create("s1_spider", "蛛魔", 40, EnemyTier.Normal, 0, true, dir,
                "吐丝结网的蛛魔，以毒液麻痹猎物后慢慢享用。网中常有修士骸骨。",
                "嘶……（蛛丝从暗处射来，封住退路）",
                "蛛妖，休想困住我！",
                new[]{ Ab("毒丝", debuffIntent, false, new[]{Ac(EnemyActionType.Poison,3,3)}), Ab("束缚", debuffIntent, false, new[]{Ac(EnemyActionType.ApplyWeak,1,1)}), Ab("撕咬", attackIntent, false, new[]{Ac(EnemyActionType.Attack,4,4)}) });
            created += Create("s1_vulture", "秃鹫妖", 35, EnemyTier.Normal, 0, true, dir,
                "盘旋于战场上的秃鹫妖，以尸体为食。擅长俯冲与啄击，能汲取腐气自愈。",
                "嘎——！（秃鹫妖从空中俯冲而下）",
                "食腐之禽，也敢猖狂？",
                new[]{ Ab("双爪连击", attackIntent, false, new[]{Ac(EnemyActionType.Attack,3,3),Ac(EnemyActionType.Attack,3,3)}), Ab("俯冲猛啄", attackIntent, false, new[]{Ac(EnemyActionType.Attack,8,8)}), Ab("吸腐气", healIntent, false, new[]{Ac(EnemyActionType.Heal,2,2)}) });
            created += Create("s1_crab", "蟹妖", 50, EnemyTier.Normal, 0, true, dir,
                "溪流中修炼的蟹妖，壳如精铁。攻击虽弱但防御极高，极难击破。",
                "咔咔……（蟹妖举起双螯，缩进硬壳）",
                "铁壳乌龟，看我破你！",
                new[]{ Ab("铁壳护体", defendIntent, false, new[]{Ac(EnemyActionType.Block,10,10)}), Ab("钳击", attackIntent, false, new[]{Ac(EnemyActionType.Attack,5,5)}) });
            created += Create("s1_eagle", "鹰妖", 38, EnemyTier.Normal, 0, true, dir,
                "高空盘旋的鹰妖，目光如炬。善于俯冲突击，利爪能撕裂护甲。",
                "唳——！（鹰妖在空中盘旋，锐眼锁定猎物）",
                "扁毛畜生，下来受死！",
                new[]{ Ab("双爪撕", attackIntent, false, new[]{Ac(EnemyActionType.Attack,4,4),Ac(EnemyActionType.Attack,4,4)}), Ab("锐眼锁定", debuffIntent, false, new[]{Ac(EnemyActionType.ApplyVulnerable,1,1)}), Ab("俯冲", attackIntent, false, new[]{Ac(EnemyActionType.Attack,6,6)}) });
            created += Create("s1_turtle", "玄龟", 58, EnemyTier.Normal, 0, true, dir,
                "千年玄龟，龟壳坚如磐石。几乎不主动攻击，但极难被杀死。",
                "……（玄龟缓缓探出头，漠然看着你）",
                "缩头乌龟，看你缩到几时！",
                new[]{ Ab("龟壳", defendIntent, false, new[]{Ac(EnemyActionType.Block,8,8)}), Ab("咬", attackIntent, false, new[]{Ac(EnemyActionType.Attack,4,4)}), Ab("龟壳", defendIntent, false, new[]{Ac(EnemyActionType.Block,8,8)}), Ab("回春", healIntent, false, new[]{Ac(EnemyActionType.Heal,3,3)}) });
            created += Create("s1_beetle", "甲虫", 45, EnemyTier.Normal, 0, true, dir,
                "灵矿中变异的甲虫，甲壳泛着金属光泽。攻防均衡，不好对付。",
                "嗡嗡……（甲虫振翅，金属甲壳反射寒光）",
                "虫子也来送死？",
                new[]{ Ab("甲壳护体", defendIntent, false, new[]{Ac(EnemyActionType.Block,5,5)}), Ab("角撞", attackIntent, false, new[]{Ac(EnemyActionType.Attack,6,6)}), Ab("甲壳护体", defendIntent, false, new[]{Ac(EnemyActionType.Block,5,5)}) });
            created += Create("s1_centi", "蜈蚣精", 48, EnemyTier.Normal, 0, true, dir,
                "百足之虫死而不僵。蜈蚣精全身带毒，多足连击令人防不胜防。",
                "嘶嘶……（蜈蚣精百足蠕动，毒液滴落）",
                "百足之虫，死而不僵？看我斩你百足！",
                new[]{ Ab("毒雾", debuffIntent, false, new[]{Ac(EnemyActionType.Poison,2,2)}), Ab("百足连击", attackIntent, false, new[]{Ac(EnemyActionType.Attack,3,3),Ac(EnemyActionType.Attack,3,3)}), Ab("毒雾", debuffIntent, false, new[]{Ac(EnemyActionType.Poison,2,2)}) });
            created += Create("s1_toad", "蟾蜍精", 42, EnemyTier.Normal, 0, true, dir,
                "池塘中的蟾蜍精，背上布满毒腺。毒液喷射令人虚弱，同时还能鼓腹格挡。",
                "呱……（蟾蜍精鼓起腹部，毒腺膨胀）",
                "癞蛤蟆，想吃天鹅肉？",
                new[]{ Ab("毒液", debuffIntent, false, new[]{Ac(EnemyActionType.Poison,3,3)}), Ab("舌鞭", attackIntent, false, new[]{Ac(EnemyActionType.Attack,4,4)}), Ab("毒液", debuffIntent, false, new[]{Ac(EnemyActionType.Poison,3,3)}), Ab("鼓腹", defendIntent, false, new[]{Ac(EnemyActionType.Block,4,4)}) });
            created += Create("s1_mantis", "螳螂妖", 40, EnemyTier.Normal, 0, true, dir,
                "以快取胜的螳螂妖，双刀如镰。前两回合试探，第三回合致命一击。",
                "嘶……（螳螂妖双刀前伸，蓄势待发）",
                "螳臂当车！",
                new[]{ Ab("双刀斩", attackIntent, false, new[]{Ac(EnemyActionType.Attack,5,5),Ac(EnemyActionType.Attack,5,5)}), Ab("双刀斩", attackIntent, false, new[]{Ac(EnemyActionType.Attack,5,5),Ac(EnemyActionType.Attack,5,5)}), Ab("致命斩", attackIntent, false, new[]{Ac(EnemyActionType.Attack,10,10)}) });
            created += Create("s1_slime", "粘液怪", 35, EnemyTier.Normal, 0, true, dir,
                "无定形的粘液生物，会腐蚀装备护甲。看似无害实则令人头疼。",
                "咕噜……（粘液怪蠕动着，发出黏腻的声响）",
                "恶心的东西。",
                new[]{ Ab("腐蚀粘液", debuffIntent, false, new[]{Ac(EnemyActionType.ApplyFrail,2,2)}), Ab("甩击", attackIntent, false, new[]{Ac(EnemyActionType.Attack,4,4)}), Ab("腐蚀粘液", debuffIntent, false, new[]{Ac(EnemyActionType.ApplyFrail,2,2)}) });
            created += Create("s1_elemental", "火灵", 44, EnemyTier.Normal, 0, true, dir,
                "由灵火凝聚而成的火灵，炽热无比。攻击附带灼烧，令人防御下降。",
                "呼呼——！（火灵炽烈燃烧，热浪扑面而来）",
                "一把火也敢猖狂？",
                new[]{ Ab("火球", attackIntent, false, new[]{Ac(EnemyActionType.Attack,8,8)}), Ab("灼烧", debuffIntent, false, new[]{Ac(EnemyActionType.ApplyVulnerable,1,1)}), Ab("爆裂火球", attackIntent, false, new[]{Ac(EnemyActionType.Attack,10,10)}) });
            created += Create("s1_ghost", "山鬼", 36, EnemyTier.Normal, 0, true, dir,
                "山间游荡的孤魂野鬼，能施放虚弱与治疗。本身攻击不强但极难缠。",
                "呜……（山鬼飘忽不定，忽隐忽现）",
                "区区孤魂，散了吧。",
                new[]{ Ab("寒气", debuffIntent, false, new[]{Ac(EnemyActionType.ApplyWeak,2,2)}), Ab("聚魂", healIntent, false, new[]{Ac(EnemyActionType.Heal,4,4)}), Ab("阴风", attackIntent, false, new[]{Ac(EnemyActionType.Attack,5,5)}) });
            created += Create("s1_brute", "蛮力士", 52, EnemyTier.Normal, 0, true, dir,
                "以蛮力著称的山野莽汉，不懂法术只知挥拳。但那一拳的力道足以碎石。",
                "嘿嘿，来比比拳头？（蛮力士举起砂锅大的拳头）",
                "好大的拳头！",
                new[]{ Ab("重拳", attackIntent, false, new[]{Ac(EnemyActionType.Attack,9,9)}), Ab("重拳", attackIntent, false, new[]{Ac(EnemyActionType.Attack,9,9)}), Ab("护头", defendIntent, false, new[]{Ac(EnemyActionType.Block,6,6)}) });
            created += Create("s1_shaman", "巫祝", 40, EnemyTier.Normal, 0, true, dir,
                "部落的巫祝，精通诅咒之术。本身不擅长攻击，但各种debuff令人苦不堪言。",
                "天地玄黄，万咒归宗……（巫祝念念有词）",
                "装神弄鬼！",
                new[]{ Ab("虚弱咒", debuffIntent, false, new[]{Ac(EnemyActionType.ApplyWeak,2,2)}), Ab("破绽咒", debuffIntent, false, new[]{Ac(EnemyActionType.ApplyVulnerable,2,2)}), Ab("碎甲咒", debuffIntent, false, new[]{Ac(EnemyActionType.ApplyFrail,2,2)}), Ab("回魂", healIntent, false, new[]{Ac(EnemyActionType.Heal,3,3)}) });
            // === 精英敌人 (15) ===
            created += Create("s1_yaojiang", "妖将", 110, EnemyTier.Elite, 0, true, dir,
                "统领山野妖兽的妖将，修炼三百年。善用双刀，攻守兼备。血量过半后会进入狂暴状态，放弃防守全力输出。",
                "尔等凡人，竟敢闯入本将领地！\n今日便让你知道何为天高地厚！\n（妖将拔出双刀，妖气冲天）",
                "咳……咳……\n没想到……本将修炼三百年……\n竟败在你手中……\n罢了，罢了……",
                new[]{ Ab("横斩", attackIntent, false, new[]{Ac(EnemyActionType.Attack,10,10)}), Ab("铁壁斩", attackIntent, false, new[]{Ac(EnemyActionType.Block,8,8),Ac(EnemyActionType.Attack,5,5)}), Ab("旋风斩", attackIntent, false, new[]{Ac(EnemyActionType.Attack,7,7),Ac(EnemyActionType.Attack,7,7)}) });
            created += Create("s1_gushi", "蛊师", 95, EnemyTier.Elite, 0, true, dir,
                "苗疆蛊师的传人，以万蛊噬心之术闻名。前期持续施毒削弱，血量过半后蛊术爆发，debuff与治疗交替。",
                "嘿嘿嘿……又一个送上门的试药人。\n我的蛊虫们可饿了好久了……\n就让你成为它们的养分吧！",
                "不……不可能……\n我的蛊虫……万蛊噬心……\n怎么会败……\n（蛊虫四散逃逸，蛊师倒地）",
                new[]{ Ab("种蛊", debuffIntent, false, new[]{Ac(EnemyActionType.Poison,4,4)}), Ab("蛊蚀", debuffIntent, false, new[]{Ac(EnemyActionType.ApplyWeak,2,2)}), Ab("碎骨蛊", debuffIntent, false, new[]{Ac(EnemyActionType.ApplyFrail,2,2)}), Ab("噬心一击", attackIntent, false, new[]{Ac(EnemyActionType.Attack,5,5)}) });
            created += Create("s1_jianling", "剑灵", 80, EnemyTier.Elite, 0, true, dir,
                "上古剑修留下的剑意凝聚成灵。攻击犀利无匹，血量低于四成时进入剑意爆发，多段连斩。",
                "（剑灵凝形，剑意如山压来）\n剑……出……鞘……\n挡我者，死！",
                "剑意……散了……\n原来……这就是凡人的力量……\n（剑灵化为流光消散）",
                new[]{ Ab("一剑", attackIntent, false, new[]{Ac(EnemyActionType.Attack,12,12)}), Ab("三连斩", attackIntent, false, new[]{Ac(EnemyActionType.Attack,4,4),Ac(EnemyActionType.Attack,4,4),Ac(EnemyActionType.Attack,4,4)}), Ab("破绽", debuffIntent, false, new[]{Ac(EnemyActionType.ApplyVulnerable,2,2)}) });
            created += Create("s1_shimo", "石魔将", 160, EnemyTier.Elite, 0, true, dir,
                "上古石魔将，三阶段强敌。初期龟缩防御，血量七成觉醒攻防一体，三成时崩裂全力爆发。",
                "（石魔将缓缓睁眼，灵石核心轰然亮起）\n入侵者……\n格杀勿论……",
                "石……裂了……\n核心……碎裂……\n（石魔将轰然倒地，化为碎石）",
                new[]{ Ab("石拳", attackIntent, false, new[]{Ac(EnemyActionType.Attack,8,8)}), Ab("石壁", defendIntent, false, new[]{Ac(EnemyActionType.Block,16,16)}), Ab("石拳", attackIntent, false, new[]{Ac(EnemyActionType.Attack,8,8)}) });
            created += Create("s1_yewang", "妖王", 140, EnemyTier.Elite, 0, true, dir,
                "山野妖兽之王，统领百兽。血量过半后狂暴，双爪连击配合虚弱令人绝望。",
                "你便是那闯入我领地的人类？\n本王倒要看看你有几分本事！\n来吧——！",
                "嗷呜……\n百兽之王……竟败于人类之手……\n（妖王倒地，百兽四散）",
                new[]{ Ab("利爪", attackIntent, false, new[]{Ac(EnemyActionType.Attack,12,12)}), Ab("护体妖气", defendIntent, false, new[]{Ac(EnemyActionType.Block,10,10)}), Ab("咆哮", debuffIntent, false, new[]{Ac(EnemyActionType.ApplyWeak,2,2),Ac(EnemyActionType.Attack,7,7)}) });
            created += Create("s1_hupo", "幻婆", 100, EnemyTier.Elite, 0, true, dir,
                "精通幻术的老婆婆，以幻术令人迷失。血量过半后幻术大爆发，全debuff配合攻击。",
                "呵呵呵……小修士，来婆婆这里坐坐……\n让婆婆看看你的心魔……\n嘻嘻嘻……",
                "幻术……破不了……\n你……你心志之坚……超出我预料……\n（幻婆化作青烟消散）",
                new[]{ Ab("迷魂", debuffIntent, false, new[]{Ac(EnemyActionType.ApplyWeak,3,3)}), Ab("破幻", debuffIntent, false, new[]{Ac(EnemyActionType.ApplyVulnerable,3,3)}), Ab("聚气", healIntent, false, new[]{Ac(EnemyActionType.Heal,5,5)}), Ab("爪击", attackIntent, false, new[]{Ac(EnemyActionType.Attack,4,4)}) });
            created += Create("s1_gujiang", "鬼将", 120, EnemyTier.Elite, 0, true, dir,
                "死不瞑目的鬼将，以执念化形。生前善用长枪，死后枪法更胜。血量六成后使出死亡连击。",
                "（鬼将执枪而立，阴风阵阵）\n吾……生前未了之愿……\n挡吾者……死！",
                "执念……散了……\n吾……终于……可以安息了……\n（鬼将化为光点飞散）",
                new[]{ Ab("鬼枪", attackIntent, false, new[]{Ac(EnemyActionType.Attack,12,12)}), Ab("冥气护体", defendIntent, false, new[]{Ac(EnemyActionType.Block,10,10)}), Ab("冥气侵蚀", debuffIntent, false, new[]{Ac(EnemyActionType.ApplyWeak,3,3)}) });
            created += Create("s1_mingshe", "冥蛇后", 100, EnemyTier.Elite, 0, true, dir,
                "幽冥界的蛇后，以毒术闻名。血量过半后召唤蛇群，毒液爆发。",
                "嘶嘶……（冥蛇后竖起蛇瞳，毒牙闪烁）\n又一个……送上门的猎物……",
                "嘶……\n本后……竟败于凡人之手……\n（冥蛇后化为毒雾散去）",
                new[]{ Ab("冥毒", debuffIntent, false, new[]{Ac(EnemyActionType.Poison,5,5)}), Ab("缠绕", debuffIntent, false, new[]{Ac(EnemyActionType.ApplyWeak,2,2)}), Ab("毒牙", attackIntent, false, new[]{Ac(EnemyActionType.Attack,7,7)}) });
            created += Create("s1_panjang", "判官", 105, EnemyTier.Elite, 0, true, dir,
                "幽冥判官，执掌生死簿。善用debuff审判敌人，血量过半后审判升级。",
                "（判官翻开生死簿，朱笔一勾）\n你的名字……已在生死簿上……\n今日便是你的死期！",
                "生死簿上……无你的名字……\n看来……是天意如此……\n（判官合上生死簿，消散于冥雾中）",
                new[]{ Ab("判罚", debuffIntent, false, new[]{Ac(EnemyActionType.ApplyVulnerable,3,3)}), Ab("罪刑", attackIntent, false, new[]{Ac(EnemyActionType.Attack,9,9)}), Ab("枷锁", debuffIntent, false, new[]{Ac(EnemyActionType.ApplyWeak,3,3)}) });
            created += Create("s1_dujiao", "毒蛟", 130, EnemyTier.Elite, 0, true, dir,
                "山间毒蛟，以毒雾闻名。血量六成后毒息爆发，双重施毒。",
                "嘶——（毒蛟从潭中探出，毒雾弥漫）\n踏入我领地者……\n将化为毒潭中的一具白骨！",
                "蛟身……裂了……\n毒潭……干涸……\n（毒蛟翻滚着沉入潭底，再无声息）",
                new[]{ Ab("毒雾", debuffIntent, false, new[]{Ac(EnemyActionType.Poison,5,5)}), Ab("蛟尾扫", attackIntent, false, new[]{Ac(EnemyActionType.Attack,8,8)}), Ab("蛟鳞护体", defendIntent, false, new[]{Ac(EnemyActionType.Block,8,8)}) });
            created += Create("s1_gumu", "蛊母", 105, EnemyTier.Elite, 0, true, dir,
                "万蛊之母，体内寄宿无数蛊虫。血量过半后爆蛊，全debuff加身。",
                "咯咯咯……（蛊母腹部蠕动，蛊虫窥探）\n来吧……成为我的孩子……\n让它们……寄生在你体内……",
                "不……我的孩子们……\n都死了……\n（蛊母腹中蛊虫尽出，母体枯萎）",
                new[]{ Ab("蛊毒", debuffIntent, false, new[]{Ac(EnemyActionType.Poison,4,4),Ac(EnemyActionType.ApplyWeak,2,2)}), Ab("碎甲蛊", debuffIntent, false, new[]{Ac(EnemyActionType.ApplyFrail,3,3)}), Ab("回气", healIntent, false, new[]{Ac(EnemyActionType.Heal,6,6)}) });
            created += Create("s1_guwang", "蛊王", 120, EnemyTier.Elite, 0, true, dir,
                "蛊中之王，万蛊之首。血量过半后蛊王之怒，剧毒与虚弱齐发。",
                "（蛊王盘踞于蛊巢之上，万千蛊虫匍匐）\n你……敢来犯本王？\n死！",
                "本王……不甘……\n万蛊臣服……竟败于……凡人之手……\n（蛊王化为一地蛊壳）",
                new[]{ Ab("蛊毒", debuffIntent, false, new[]{Ac(EnemyActionType.Poison,4,4)}), Ab("蛊击", attackIntent, false, new[]{Ac(EnemyActionType.Attack,7,7)}), Ab("虚弱蛊", debuffIntent, false, new[]{Ac(EnemyActionType.ApplyWeak,3,3)}) });
            created += Create("s1_mojiang", "魔将", 150, EnemyTier.Elite, 0, true, dir,
                "天魔裂隙渗出的魔将残影。攻防均衡，血量六成后魔化，攻击力暴增。",
                "（魔将的影子从地面升起，魔气滔天）\n区区蝼蚁……\n也敢阻挡本将的去路？",
                "魔……气散了……\n看来……这具残躯……也到头了……\n（魔将化为黑烟消散）",
                new[]{ Ab("魔斩", attackIntent, false, new[]{Ac(EnemyActionType.Attack,12,12)}), Ab("魔铠", defendIntent, false, new[]{Ac(EnemyActionType.Block,10,10)}), Ab("双魔斩", attackIntent, false, new[]{Ac(EnemyActionType.Attack,8,8),Ac(EnemyActionType.Attack,8,8)}) });
            created += Create("s1_tianfen", "天魔分身", 120, EnemyTier.Elite, 0, true, dir,
                "天魔的一缕分身，蕴含天魔部分力量。血量过半后裂隙爆发，全debuff配合多段攻击。",
                "（虚空中裂开一道缝隙，天魔分身走出）\n有趣……你引来了本座的注意……\n让本座看看你的极限在哪里。",
                "不错……\n你引起了本座的兴趣……\n下次见面……便是你的末日……\n（分身化为碎片消散）",
                new[]{ Ab("魔压", debuffIntent, false, new[]{Ac(EnemyActionType.ApplyWeak,2,2),Ac(EnemyActionType.ApplyVulnerable,2,2),Ac(EnemyActionType.ApplyFrail,2,2)}), Ab("魔击", attackIntent, false, new[]{Ac(EnemyActionType.Attack,10,10)}), Ab("魔气回流", healIntent, false, new[]{Ac(EnemyActionType.Heal,8,8)}) });
            created += Create("s1_xuemo", "血魔", 130, EnemyTier.Elite, 0, true, dir,
                "以血为食的血魔，攻击附带吸血。血量过半后嗜血，吸血量暴增。",
                "（血魔从血池中升起，浑身赤红）\n你的血……闻起来很香……\n让我尝尝！",
                "我的血……干了……\n不可能……血魔岂会败给……\n失血过多……\n（血魔化为干枯的躯壳碎裂）",
                new[]{ Ab("血爪", attackIntent, false, new[]{Ac(EnemyActionType.Attack,10,10),Ac(EnemyActionType.Heal,5,5)}), Ab("血鞭", attackIntent, false, new[]{Ac(EnemyActionType.Attack,7,7),Ac(EnemyActionType.Attack,7,7)}) });
            // === Boss (3) ===
            created += Create("s1_heifeng", "黑风大圣", 200, EnemyTier.Boss, 0, true, dir,
                "占据黑风岭千年的妖王，自称黑风大圣。本体是一只修炼千年的黑熊精，实力深不可测。三阶段：稳扎稳打→狂暴回血→末日爆发。",
                "哈哈哈——！\n来者何人？竟敢闯入我黑风岭！\n本大圣在此修炼千年，何人敢来送死？\n你？一个毛头小子？\n也罢，让我看看你有什么本事！\n（黑风大圣双掌一合，妖气冲天，地面龟裂）",
                "咳……咳咳……\n千年……千年修为……\n竟败在你手中……\n也罢……也罢……\n黑风岭……今日便交于你了……\n（黑风大圣化为一道黑光消散）",
                new[]{ Ab("黑风掌", attackIntent, false, new[]{Ac(EnemyActionType.Attack,8,8)}), Ab("妖气护体", defendIntent, false, new[]{Ac(EnemyActionType.Block,12,12)}), Ab("妖啸", debuffIntent, false, new[]{Ac(EnemyActionType.ApplyWeak,2,2),Ac(EnemyActionType.Attack,6,6)}) });
            created += Create("s1_shanwang", "山魈王", 230, EnemyTier.Boss, 0, true, dir,
                "山魈一族的王者，能号令百兽。三阶段：稳扎稳打+召唤→暴怒连击→狂暴多段爆发。",
                "嘻嘻嘻嘻……\n人类……你闯进了山魈的领地……\n本王可是这山里的王！\n小的们，给我上！\n让本王看看你的本事！\n（山魈王尖啸一声，群兽应声而动）",
                "嗷呜……\n本王……败了……\n山魈一族……再无王者了……\n你……比我强……\n这山中……便由你做主了……\n（山魈王闭上了眼睛，化为灵光散去）",
                new[]{ Ab("利爪横扫", attackIntent, false, new[]{Ac(EnemyActionType.Attack,10,10)}), Ab("召唤山贼", specialIntent, true, new[]{Ac(EnemyActionType.Attack,3,3)}), Ab("山魈护体", defendIntent, false, new[]{Ac(EnemyActionType.Block,8,8)}) });
            created += Create("s1_yaoshen", "妖神", 260, EnemyTier.Boss, 0, true, dir,
                "万妖之神，山野荒原的终极Boss。传说中修炼万年成神的妖，能号令天地之力。三阶段：攻防均衡→神威降世→陨落爆发。",
                "（天地变色，妖神降临，万妖跪伏）\n凡人……\n你竟走到了这里……\n本神修炼万年，见识过无数英雄豪杰……\n他们如今都已化为尘土……\n你……也会是同样的下场。\n（妖神抬手，山岳为之震动）",
                "万……万年……\n本神……竟也败了……\n凡人……你……超乎了本神的预料……\n看来……这天地……已经不需要妖神了……\n（妖神化作满天星辉，缓缓消散于风中）\n去吧……去面对更大的挑战……\n（最后一缕妖气消散于天地间）",
                new[]{ Ab("妖神掌", attackIntent, false, new[]{Ac(EnemyActionType.Attack,12,12)}), Ab("妖神护体", defendIntent, false, new[]{Ac(EnemyActionType.Block,15,15)}), Ab("妖神回元", healIntent, false, new[]{Ac(EnemyActionType.Heal,8,8)}) });
            return created;
        }

        static int GenerateRegion2(string dir) { return 0; }
        static int GenerateRegion3(string dir) { return 0; }
        static int GenerateRegion4(string dir) { return 0; }

        static int Create(string id, string name, int maxHealth, EnemyTier tier, int region, bool pattern, string dir,
            string desc, string encounterDialogue, string victoryDialogue, EnemyAbilityData[] abilities)
        {
            string path2 = $"{dir}/{id}.asset";
            if (AssetDatabase.LoadAssetAtPath<EnemyCharacterData>(path2) != null)
                AssetDatabase.DeleteAsset(path2);

            var enemy = ScriptableObject.CreateInstance<EnemyCharacterData>();
            SetField(enemy, "characterID", id);
            SetField(enemy, "characterName", name);
            SetField(enemy, "maxHealth", maxHealth);
            SetField(enemy, "enemyPrefab", prefabRef);
            SetField(enemy, "followAbilityPattern", pattern);
            SetField(enemy, "enemyAbilityList", new List<EnemyAbilityData>(abilities));
            SetField(enemy, "enemyTier", tier);
            SetField(enemy, "regionId", region);
            SetField(enemy, "enemyDescription", desc);
            SetField(enemy, "encounterDialogue", encounterDialogue);
            SetField(enemy, "victoryDialogue", victoryDialogue);

            AssetDatabase.CreateAsset(enemy, path2);
            return 1;
        }

        static EnemyAbilityData Ab(string name, EnemyIntentionData intent, bool hideValue, EnemyActionData[] actions)
        {
            var ab = new EnemyAbilityData();
            SetField(ab, "name", name);
            SetField(ab, "intention", intent);
            SetField(ab, "hideActionValue", hideValue);
            SetField(ab, "actionList", new List<EnemyActionData>(actions));
            return ab;
        }

        static EnemyActionData Ac(EnemyActionType type, int min, int max)
        {
            var ac = new EnemyActionData();
            SetField(ac, "actionType", type);
            SetField(ac, "minActionValue", min);
            SetField(ac, "maxActionValue", max);
            return ac;
        }

        static void SetField(object obj, string name, object value)
        {
            var f = obj.GetType().GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (f != null) f.SetValue(obj, value);
        }

        static void EnsureDir(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path).Replace('\\', '/');
                string folderName = Path.GetFileName(path);
                if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                    EnsureDir(parent);
                if (string.IsNullOrEmpty(parent)) parent = "Assets";
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }
    }
}