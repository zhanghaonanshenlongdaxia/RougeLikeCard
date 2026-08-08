using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using CardGame;

namespace CardGame.Editor
{
    public static class EventBatchGenerator
    {
        [MenuItem("Tools/Generate Events")]
        public static void GenerateAll()
        {
            string baseDir = "Assets/NueGames/NueDeck/Data/Events";
            EnsureFolder(baseDir);

            DeleteOldEvents(baseDir);

            int c = 0;
            c += GenerateLianQi(baseDir);
            c += GenerateZhuJi(baseDir);
            c += GenerateJinDan(baseDir);
            c += GenerateYuanYing(baseDir);
            c += GenerateHuaShen(baseDir);
            c += GenerateDuJie(baseDir);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"事件创建完成: {c}个 (6境界×30=180)");
        }

        // ========== 练气期 (0) ==========
        static int GenerateLianQi(string baseDir)
        {
            string dir = $"{baseDir}/练气期";
            EnsureFolder(dir);
            int c = 0;
            var r = RealmLevel.LianQi;

            c += C(dir, r, "lq_springs", "山间灵泉",
                "山间一汪清泉，泉水晶莹剔透，散发着淡淡灵气。饮之可固本培元。",
                Ch("入泉沐浴", EventEffectType.Heal, 15),
                Ch("收集泉水", EventEffectType.GainPotion, 1),
                Ch("小憩片刻", EventEffectType.Heal, 8));

            c += C(dir, r, "lq_wildbeast", "妖兽袭击",
                "一头灵兽从林中窜出，目露凶光，拦住了你的去路。",
                Ch("与之搏斗", EventEffectType.TakeDamage, 8),
                Ch("抛下财物逃走", EventEffectType.LoseGold, 15),
                Ch("隐匿气息等待离去", EventEffectType.Nothing, 0));

            c += C(dir, r, "lq_discuss", "散修论道",
                "路遇一位云游散修，他邀你坐而论道，切磋修炼心得。",
                Ch("请教修炼心得", EventEffectType.GainStrength, 1),
                Ch("交换一张功法", EventEffectType.GainCard, 1),
                Ch("婉言谢绝", EventEffectType.Nothing, 0));

            c += C(dir, r, "lq_herb_gather", "采药老翁",
                "一位老翁正在山间采药，他背篓里装满了各种灵草。",
                Ch("买下灵草", EventEffectType.GainMaterial, 0),
                Ch("帮忙采药换药方", EventEffectType.GainPotion, 1),
                Ch("自行去别处采", EventEffectType.Heal, 5));

            c += C(dir, r, "lq_lost_disciple", "迷路弟子",
                "一个宗门弟子在山路上迷了路，神情焦急。",
                Ch("给他指路", EventEffectType.GainGold, 15),
                Ch("抢他灵石", EventEffectType.GainGold, 30),
                Ch("不搭理", EventEffectType.Nothing, 0));

            c += C(dir, r, "lq_abandoned_shrine", "荒废小庙",
                "路边有一座荒废的小庙，庙中神像前摆着一枚铜钱和一炷残香。",
                Ch("上香祈福", EventEffectType.Heal, 10),
                Ch("拿走铜钱", EventEffectType.GainGold, 10),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "lq_spirit_rabbit", "灵兔引路",
                "一只通体雪白的小兔出现在你面前，蹦蹦跳跳地向林中跑去。",
                Ch("跟上去", EventEffectType.GainGold, 25),
                Ch("抓住它", EventEffectType.TakeDamage, 5),
                Ch("不感兴趣", EventEffectType.Nothing, 0));

            c += C(dir, r, "lq_rogue_cultivator", "劫修埋伏",
                "一个蒙面修士突然从树后跳出，手中灵剑闪烁寒光。",
                Ch("迎战", EventEffectType.TakeDamage, 10),
                Ch("交出灵石", EventEffectType.LoseGold, 25),
                Ch("施展身法逃走", EventEffectType.Nothing, 0));

            c += C(dir, r, "lq_medicine_market", "坊市药铺",
                "你路过一个小坊市，一家药铺老板正在吆喝出售新炼的丹药。",
                Ch("买丹药", EventEffectType.LoseGold, 20),
                Ch("偷丹药", EventEffectType.TakeDamage, 6),
                Ch("不买", EventEffectType.Nothing, 0));

            c += C(dir, r, "lq_injured_traveler", "受伤旅人",
                "路边有一位受伤的旅人，他捂着伤口，向你求助。",
                Ch("为他疗伤", EventEffectType.Heal, 5),
                Ch("搜他身上", EventEffectType.GainGold, 20),
                Ch("不理会", EventEffectType.Nothing, 0));

            c += C(dir, r, "lq_spirit_stone_vein", "灵石碎矿",
                "你发现了一处浅层灵石矿脉，矿石裸露在地表。",
                Ch("小心开采", EventEffectType.GainGold, 30),
                Ch("疯狂挖掘", EventEffectType.TakeDamage, 6),
                Ch("不做停留", EventEffectType.Nothing, 0));

            c += C(dir, r, "lq_trick_altar", "简陋祭坛",
                "林中有一座用石头堆成的简陋祭坛，上面放着几枚灵石。",
                Ch("拿走灵石", EventEffectType.GainGold, 15),
                Ch("跪拜祭坛", EventEffectType.Heal, 8),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "lq_potion_tester", "丹药试药",
                "一位炼丹师请你试吃他新炼的两枚丹药，一红一蓝，效力未知。",
                Ch("吞服红丹", EventEffectType.Heal, 20),
                Ch("吞服蓝丹", EventEffectType.TakeDamage, 8),
                Ch("拒绝试药", EventEffectType.Nothing, 0));

            c += C(dir, r, "lq_old_scroll", "残破功法",
                "你在树洞中发现一卷残破的功法竹简，字迹模糊。",
                Ch("研读竹简", EventEffectType.UpgradeRandomCard, 1),
                Ch("卖给坊市", EventEffectType.GainGold, 15),
                Ch("丢弃", EventEffectType.Nothing, 0));

            c += C(dir, r, "lq_beggar_sage", "乞丐仙人",
                "一个衣衫褴褛的乞丐坐在路边，但气息却深不可测。",
                Ch("施舍灵石", EventEffectType.LoseGold, 20),
                Ch("请教修炼", EventEffectType.GainStrength, 1),
                Ch("无视", EventEffectType.Nothing, 0));

            c += C(dir, r, "lq_bamboo_forest", "竹林迷途",
                "你走进一片茂密的竹林，四周景象相似，辨不清方向。",
                Ch("静心感知灵气", EventEffectType.Heal, 8),
                Ch("以灵石破阵", EventEffectType.LoseGold, 15),
                Ch("强行冲出", EventEffectType.TakeDamage, 5));

            c += C(dir, r, "lq_rat_swarm", "鼠群围攻",
                "一群灵鼠突然从地洞涌出，吱吱叫着围住了你。",
                Ch("驱散鼠群", EventEffectType.TakeDamage, 4),
                Ch("抛洒食物引开", EventEffectType.LoseGold, 10),
                Ch("跳上树等待", EventEffectType.Nothing, 0));

            c += C(dir, r, "lq_cave_dweller", "洞府隐修",
                "你在山腰发现一个小洞府，里面住着一位闭关修炼的隐修。",
                Ch("请教修炼", EventEffectType.GainStrength, 1),
                Ch("切磋一番", EventEffectType.TakeDamage, 7),
                Ch("悄悄离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "lq_floating_market", "行商小贩",
                "一个背着大包的小贩拦住你，兜售各种杂物。",
                Ch("买药水", EventEffectType.LoseGold, 15),
                Ch("买功法", EventEffectType.LoseGold, 25),
                Ch("不买", EventEffectType.Nothing, 0));

            c += C(dir, r, "lq_fox_spirit", "狐妖迷惑",
                "一只白狐化为人形，妪媚地向你招手。",
                Ch("跟随前去", EventEffectType.TakeDamage, 8),
                Ch("识破幻术", EventEffectType.GainGold, 25),
                Ch("退避三舍", EventEffectType.Nothing, 0));

            c += C(dir, r, "lq_dragon_well", "龙井古泉",
                "一口古老的井，井水碧绿如玉，据说有龙气残留。",
                Ch("饮井水", EventEffectType.GainMaxHP, 3),
                Ch("收集井水", EventEffectType.GainPotion, 1),
                Ch("不碰", EventEffectType.Nothing, 0));

            c += C(dir, r, "lq_card_trade", "功法交换",
                "一位修士提出和你交换一张功法，他展示了一卷泛黄竹简。",
                Ch("交换功法", EventEffectType.GainCard, 1),
                Ch("拒绝交换", EventEffectType.Nothing, 0),
                Ch("骗他功法", EventEffectType.TakeDamage, 6));

            c += C(dir, r, "lq_meditation_stone", "悟道石",
                "路边一块平整的大石，据说坐其上修炼可助悟道。",
                Ch("打坐修炼", EventEffectType.Heal, 10),
                Ch("汲取石中灵气", EventEffectType.GainStrength, 1),
                Ch("无感", EventEffectType.Nothing, 0));

            c += C(dir, r, "lq_spirit_butterfly", "灵蝶引路",
                "一只灵蝶在你面前翩翩起舞，似乎要带你去什么地方。",
                Ch("跟随灵蝶", EventEffectType.GainGold, 20),
                Ch("捉住灵蝶", EventEffectType.GainPotion, 1),
                Ch("不跟随", EventEffectType.Nothing, 0));

            c += C(dir, r, "lq_flood_villager", "水灾难民",
                "村庄遭遇灵气洪水，一位村民向你求助。",
                Ch("帮忙救灾", EventEffectType.Heal, 5),
                Ch("趁乱搜刮", EventEffectType.GainGold, 25),
                Ch("绕道而行", EventEffectType.Nothing, 0));

            c += C(dir, r, "lq_apprentice_duel", "弟子切磋",
                "一位同门弟子向你发起切磋邀请。",
                Ch("全力以赴", EventEffectType.TakeDamage, 5),
                Ch("随意应付", EventEffectType.Nothing, 0),
                Ch("婉拒", EventEffectType.Nothing, 0));

            // (lq_old_armor, lq_spirit_tree, lq_stream_fishing replaced by mini-games below)
            c += C(dir, r, "lq_wandering_merchant", "迷路商人",
                "一位商人在山路上迷了路，他背着一包货物，神色慌张。",
                Ch("给他指路换取报酬", EventEffectType.GainGold, 20),
                Ch("打劫他", EventEffectType.GainGold, 40),
                Ch("不搭理", EventEffectType.Nothing, 0));

            // 小游戏
            c += C(dir, r, "lq_slot", "灵石机",
                "坊市角落摆着一台灵石机，投灵石摇奖，奖品各异！\n消耗: 15灵石",
                Ch("摇一次灵石机", EventEffectType.MiniSlot, 15),
                Ch("不玩", EventEffectType.Nothing, 0));

            c += C(dir, r, "lq_balloon", "灵气球摊",
                "一个摊位前挂满了灵气球，射中不同气球可得不同奖品！\n消耗: 10灵石",
                Ch("射击气球", EventEffectType.MiniBalloon, 10),
                Ch("不玩", EventEffectType.Nothing, 0));

            c += C(dir, r, "lq_doubling", "赌灵石",
                "一个赌摊上坐着一位老修士，他让你押灵石猜大小，赢了翻倍输了全无！\n押: 20灵石",
                Ch("全押！翻倍或归零", EventEffectType.DoubleOrNothing, 20),
                Ch("不赌", EventEffectType.Nothing, 0));

            c += C(dir, r, "lq_lottery", "仙缘抽签",
                "一座小庙前摆着签筒，据说抽到上上签可获仙缘！\n消耗: 10灵石",
                Ch("抽签", EventEffectType.MiniLottery, 10),
                Ch("不抽", EventEffectType.Nothing, 0));

            c += C(dir, r, "lq_coinflip", "灵币翻面",
                "摊主拿出一枚灵币，猜正反，猜对翻倍！\n消耗: 15灵石",
                Ch("猜正面", EventEffectType.MiniCoinFlip, 15),
                Ch("不玩", EventEffectType.Nothing, 0));

            c += C(dir, r, "lq_treasure", "寻宝迷踪",
                "地上摆着三个宝箱，一个陷阱一个普通一个稀有！\n消耗: 20灵石",
                Ch("选一个宝箱", EventEffectType.MiniTreasureHunt, 20),
                Ch("不选", EventEffectType.Nothing, 0));

            return c;
        }

        // ========== 筑基期 (1) ==========
        static int GenerateZhuJi(string baseDir)
        {
            string dir = $"{baseDir}/筑基期";
            EnsureFolder(dir);
            int c = 0;
            var r = RealmLevel.ZhuJi;

            c += C(dir, r, "zj_abandoned_alchemy", "遗弃丹炉",
                "路边有一座被遗弃的丹炉，炉中似乎还有未取出的丹药残渣。",
                Ch("翻找残丹", EventEffectType.GainPotion, 1),
                Ch("拆走炉材", EventEffectType.GainMaterial, 1),
                Ch("不感兴趣", EventEffectType.Nothing, 0));

            c += C(dir, r, "zj_beast_ambush", "妖兽伏击",
                "一头二阶灵兽从密林中窜出，浑身缠绕着妖气，拦住了你的去路。",
                Ch("与之搏斗", EventEffectType.TakeDamage, 12),
                Ch("抛下财物逃走", EventEffectType.LoseGold, 35),
                Ch("隐匿气息等待离去", EventEffectType.Nothing, 0));

            c += C(dir, r, "zj_old_battlefield", "古战场残魂",
                "脚下是千年前修士交战的古战场，残魂在空中游荡，散发着肃杀之气。",
                Ch("吸收战场煞气", EventEffectType.GainStrength, 2),
                Ch("与残魂搏斗", EventEffectType.TakeDamage, 10),
                Ch("以灵石超度", EventEffectType.LoseGold, 40));

            c += C(dir, r, "zj_illusion_mist", "幻境迷途",
                "你误入一片迷雾，四周景象不断变换，似真似幻，难以分辨方向。",
                Ch("静坐调息化解幻境", EventEffectType.Heal, 18),
                Ch("以灵石破阵", EventEffectType.LoseGold, 45),
                Ch("强行冲破幻境", EventEffectType.TakeDamage, 8));

            c += C(dir, r, "zj_spirit_ore_vein", "灵石矿脉",
                "你发现了一处裸露的灵石矿脉，矿脉中灵石闪烁着诱人的光芒。",
                Ch("小心开采", EventEffectType.GainGold, 60),
                Ch("疯狂挖掘", EventEffectType.TakeDamage, 8),
                Ch("不做停留", EventEffectType.Nothing, 0));

            c += C(dir, r, "zj_old_altar", "古老祭坛",
                "密林深处有一座古老祭坛，祭坛上刻满了神秘符文，似可献祭。",
                Ch("以精血献祭求力", EventEffectType.TakeDamage, 12),
                Ch("以灵石献祭求运", EventEffectType.LoseGold, 55),
                Ch("毁掉祭坛", EventEffectType.GainMaxHP, 5));

            c += C(dir, r, "zj_rogue_ambush", "劫修围攻",
                "三名蒙面劫修拦住你的去路，为首者手中灵剑寒光闪烁。",
                Ch("迎战", EventEffectType.TakeDamage, 14),
                Ch("交出灵石", EventEffectType.LoseGold, 40),
                Ch("施展身法逃脱", EventEffectType.Nothing, 0));

            c += C(dir, r, "zj_lost_merchant", "迷路商人",
                "一位商人在山路上迷了路，他背着一包货物，神色慌张。",
                Ch("买下他的货物", EventEffectType.GainPotion, 1),
                Ch("打劫他", EventEffectType.GainGold, 60),
                Ch("给他指路", EventEffectType.GainGold, 20));

            c += C(dir, r, "zj_cultivator_spar", "修士切磋",
                "一位筑基修士向你发起切磋邀请，他想验证自己的修炼成果。",
                Ch("全力以赴", EventEffectType.TakeDamage, 8),
                Ch("随意应付", EventEffectType.TakeDamage, 4),
                Ch("婉拒", EventEffectType.Nothing, 0));

            c += C(dir, r, "zj_potion_trial", "丹药试药",
                "一位炼丹师请你试吃他新炼的三枚丹药，效力各异。",
                Ch("吞服红丹", EventEffectType.Heal, 25),
                Ch("吞服蓝丹", EventEffectType.TakeDamage, 10),
                Ch("拒绝试药", EventEffectType.Nothing, 0));

            c += C(dir, r, "zj_ancient_scroll", "古籍残页",
                "你捡到几页泛黄的古籍残页，上面记载着某种功法的片段。",
                Ch("研读残页", EventEffectType.UpgradeRandomCard, 1),
                Ch("卖给坊市", EventEffectType.GainGold, 40),
                Ch("随手丢弃", EventEffectType.Nothing, 0));

            c += C(dir, r, "zj_spirit_beast_test", "灵兽挑战",
                "一头成年灵兽挡在路上，它发出低沉的吼声，似乎在考验你。",
                Ch("应战", EventEffectType.TakeDamage, 15),
                Ch("尝试驯服", EventEffectType.GainMaxHP, 6),
                Ch("退避三舍", EventEffectType.Nothing, 0));

            c += C(dir, r, "zj_formation_chance", "机缘阵法",
                "地面上浮现出一座光阵，阵中灵气浓郁，似乎暗藏机缘。",
                Ch("踏入阵中", EventEffectType.GainRelic, 1),
                Ch("参悟阵法", EventEffectType.GainCard, 1),
                Ch("绕道而行", EventEffectType.Nothing, 0));

            c += C(dir, r, "zj_herb_garden", "药园奇遇",
                "你发现了一片荒废的药园，园中仍有几株灵药顽强生长。",
                Ch("采摘灵药", EventEffectType.GainMaterial, 1),
                Ch("在园中打坐恢复", EventEffectType.Heal, 18),
                Ch("挖走灵土", EventEffectType.GainGold, 35));

            c += C(dir, r, "zj_blacksmith_relic", "铸剑师遗愿",
                "你走进了一间荒废的铸剑坊，坊中有一柄未完成的古剑，旁边散落着铸剑笔记。",
                Ch("取走古剑", EventEffectType.GainCard, 1),
                Ch("研读铸剑笔记", EventEffectType.UpgradeRandomCard, 1),
                Ch("变卖铸造材料", EventEffectType.GainGold, 50));

            c += C(dir, r, "zj_sealed_demon", "封印残妖",
                "一块巨石下传来微弱的妖气，似乎封印着什么生灵。",
                Ch("加固封印", EventEffectType.Heal, 15),
                Ch("解开封印收服", EventEffectType.TakeDamage, 12),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "zj_spirit_lake", "灵湖宝镜",
                "湖面平静如镜，湖底隐约可见灵光流转。",
                Ch("潜入湖底取宝", EventEffectType.TakeDamage, 10),
                Ch("以灵石感应", EventEffectType.LoseGold, 30),
                Ch("欣赏片刻", EventEffectType.Heal, 10));

            c += C(dir, r, "zj_dragon_vein", "龙脉残息",
                "你感应到脚下有龙脉残息，灵气在此汇聚。",
                Ch("吸收龙脉灵气", EventEffectType.GainMaxHP, 5),
                Ch("挖掘龙脉灵石", EventEffectType.GainGold, 50),
                Ch("无福消受", EventEffectType.Nothing, 0));

            c += C(dir, r, "zj_evil_cultivator", "邪修诱骗",
                "一位面容阴鸷的修士拦住你，称有一本秘籍相赠，但眼神中透着诡异。",
                Ch("与他动手", EventEffectType.TakeDamage, 14),
                Ch("接过秘籍", EventEffectType.LoseMaxHP, 4),
                Ch("转身就跑", EventEffectType.Nothing, 0));

            c += C(dir, r, "zj_fortune_teller", "占卜师",
                "路边坐着一位蒙面占卜师，身旁浮着一面铜镜，她说可以为你占卜前程。",
                Ch("占卜财运", EventEffectType.GainGold, 45),
                Ch("占卜战力", EventEffectType.GainStrength, 2),
                Ch("不信鬼神", EventEffectType.Nothing, 0));

            c += C(dir, r, "zj_orphan_beast", "灵兽幼崽",
                "灌木丛中传来细微叫声，你拨开一看，是一只受伤的灵兽幼崽。",
                Ch("收养它", EventEffectType.GainMaxHP, 5),
                Ch("取它的灵核", EventEffectType.TakeDamage, 4),
                Ch("放它离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "zj_sword_cave", "剑冢石洞",
                "一个石洞中插满了锈蚀的剑，洞中弥漫着剑意。",
                Ch("拔出一柄剑", EventEffectType.GainCard, 1),
                Ch("吸收剑意", EventEffectType.GainStrength, 2),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "zj_spirit_wine", "灵酒坊",
                "你路过一座灵酒坊，酒香四溢，老板正在招揽过路修士。",
                Ch("买灵酒", EventEffectType.LoseGold, 25),
                Ch("偷喝灵酒", EventEffectType.FullHeal, 0),
                Ch("不感兴趣", EventEffectType.Nothing, 0));

            c += C(dir, r, "zj_cliff_pine", "悬崖古松",
                "悬崖边一棵古松盘根错节，树干上刻着几个古朴文字。",
                Ch("参悟古字", EventEffectType.UpgradeRandomCard, 1),
                Ch("摘松子", EventEffectType.Heal, 12),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "zj_underground_river", "地下暗河",
                "地面塌陷露出一条地下暗河，河水冰冷刺骨，隐约可见河底灵光。",
                Ch("潜入河中", EventEffectType.TakeDamage, 8),
                Ch("以灵石感应", EventEffectType.LoseGold, 35),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "zj_ghost_market", "鬼市入口",
                "夜晚，一处鬼市入口出现在你面前，鬼火幽幽，人影绰绰。",
                Ch("进入鬼市", EventEffectType.LoseGold, 30),
                Ch("在外观望", EventEffectType.GainGold, 35),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "zj_spirit_vine", "灵藤缠绕",
                "一片灵藤突然活了过来，藤蔓向你缠绕而来。",
                Ch("斩断灵藤", EventEffectType.TakeDamage, 6),
                Ch("采集灵藤", EventEffectType.GainMaterial, 1),
                Ch("后退躲避", EventEffectType.Nothing, 0));

            c += C(dir, r, "zj_mission_board", "悬赏榜单",
                "坊市口立着一块悬赏榜，上面贴满了各种任务。",
                Ch("接悬赏任务", EventEffectType.TakeDamage, 10),
                Ch("接跑腿任务", EventEffectType.GainGold, 30),
                Ch("不看", EventEffectType.Nothing, 0));

            c += C(dir, r, "zj_spring_of_life", "生命之泉",
                "一股从地底涌出的泉水散发着浓郁的生命灵气。",
                Ch("饮用泉水", EventEffectType.GainMaxHP, 4),
                Ch("收集泉水", EventEffectType.GainPotion, 1),
                Ch("在旁修炼", EventEffectType.Heal, 15));

            // (zj_talisman_shop replaced by mini-game below)
            // (zj_recipe_drop replaced by mini-game below)

            // 小游戏
            c += C(dir, r, "zj_dice", "掷骰问运",
                "一位游方赌客摆出骰子摊，押灵石猜大小，点数越高奖励越好！\n消耗: 30灵石",
                Ch("掷骰", EventEffectType.MiniDice, 30),
                Ch("不玩", EventEffectType.Nothing, 0));

            c += C(dir, r, "zj_ring_toss", "套灵兽",
                "摊位上摆着几只灵兽笼子，投灵石圈套中可得不同奖励！\n消耗: 25灵石",
                Ch("投圈套灵兽", EventEffectType.MiniRingToss, 25),
                Ch("不玩", EventEffectType.Nothing, 0));

            return c;
        }

        // ========== 金丹期 (2) ==========
        static int GenerateJinDan(string baseDir)
        {
            string dir = $"{baseDir}/金丹期";
            EnsureFolder(dir);
            int c = 0;
            var r = RealmLevel.JinDan;

            c += C(dir, r, "jd_immortal_tomb", "仙人遗冢",
                "你在一座荒山中发现了上古仙人的遗冢，冢门半开，隐约有灵光溢出。",
                Ch("探入冢中寻宝", EventEffectType.GainRelic, 1),
                Ch("取走散落的灵石", EventEffectType.GainGold, 80),
                Ch("恭敬离去", EventEffectType.Nothing, 0));

            c += C(dir, r, "jd_sword_spirit", "剑灵认主",
                "一柄古剑悬浮空中，剑灵现形，问你是否愿意成为它的主人。",
                Ch("接受古剑", EventEffectType.GainCard, 1),
                Ch("索取剑灵之力", EventEffectType.GainStrength, 3),
                Ch("婉拒离去", EventEffectType.Nothing, 0));

            c += C(dir, r, "jd_heaven_opportunity", "天降奇遇",
                "前方空中忽然裂开一道缝隙，一股奇异的灵气涌出，似乎是天降奇遇！",
                Ch("抢夺机缘", EventEffectType.GainGold, 100),
                Ch("与人分享", EventEffectType.GainRelic, 1),
                Ch("犹豫不决", EventEffectType.Nothing, 0));

            c += C(dir, r, "jd_ancient_stele", "仙缘石碑",
                "路边矗立着一块古老石碑，碑上刻着晦涩的仙文，似乎蕴含着某种大道之力。",
                Ch("参悟碑文", EventEffectType.GainMaxHP, 8),
                Ch("触碰石碑", EventEffectType.GainStrength, 3),
                Ch("不感兴趣", EventEffectType.Nothing, 0));

            c += C(dir, r, "jd_blood_abyss", "血祭深渊",
                "脚下的地面裂开，露出一个深不见底的深渊，其中隐约可见宝光闪烁。",
                Ch("以精血祭渊求力", EventEffectType.LoseMaxHP, 4),
                Ch("潜入深渊寻宝", EventEffectType.TakeDamage, 12),
                Ch("离开深渊", EventEffectType.Nothing, 0));

            c += C(dir, r, "jd_dragon_palace", "龙宫遗迹",
                "海底隐约可见一座龙宫遗迹，宫门大开，灵光涌动。",
                Ch("潜入龙宫", EventEffectType.TakeDamage, 10),
                Ch("以灵石开路", EventEffectType.LoseGold, 60),
                Ch("无福消受", EventEffectType.Nothing, 0));

            c += C(dir, r, "jd_three_trials", "三才试炼",
                "你来到一座三才阵前，阵中有金、木、水三道试炼。",
                Ch("挑战金道", EventEffectType.TakeDamage, 8),
                Ch("挑战木道", EventEffectType.Heal, 25),
                Ch("挑战水道", EventEffectType.GainMaxHP, 5));

            c += C(dir, r, "jd_demon_seal", "魔封古井",
                "一口古井被封印阵法覆盖，井中传来低沉的魔吼声。",
                Ch("加固封印", EventEffectType.GainMaxHP, 6),
                Ch("解开封印取魔宝", EventEffectType.TakeDamage, 15),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "jd_alchemy_master", "丹道宗师",
                "一位丹道宗师在山间炼丹，他邀你帮忙看火候。",
                Ch("帮忙看火候", EventEffectType.GainPotion, 1),
                Ch("请教丹道", EventEffectType.GainCard, 1),
                Ch("不感兴趣", EventEffectType.Nothing, 0));

            c += C(dir, r, "jd_mine_collapse", "矿脉塌陷",
                "你正在通过一处灵石矿脉时，矿道突然塌陷。",
                Ch("强行突破", EventEffectType.TakeDamage, 12),
                Ch("等待救援", EventEffectType.Heal, 10),
                Ch("以灵石开路", EventEffectType.LoseGold, 50));

            c += C(dir, r, "jd_phantom_market", "幻影坊市",
                "一座坊市凭空出现，坊市中的商人都面带微笑，却无影无形。",
                Ch("购买商品", EventEffectType.LoseGold, 50),
                Ch("识破幻影", EventEffectType.GainGold, 60),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "jd_ancient_warrior", "古将残魂",
                "一位身披甲胄的古将残魂拦住你，要求与你一战。",
                Ch("应战", EventEffectType.TakeDamage, 10),
                Ch("以灵石供奉", EventEffectType.LoseGold, 45),
                Ch("后退离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "jd_spirit_tree_core", "灵树之心",
                "一棵巨大的灵树躯干中空，内有一颗灵树之心散发幽光。",
                Ch("取灵树之心", EventEffectType.GainMaxHP, 7),
                Ch("吸收灵树灵气", EventEffectType.Heal, 20),
                Ch("砍伐灵树", EventEffectType.TakeDamage, 8));

            c += C(dir, r, "jd_dark_cultivator", "暗影邪修",
                "一位修炼暗影功法的邪修从阴影中现身，手中黑气翻涌。",
                Ch("与之交战", EventEffectType.TakeDamage, 14),
                Ch("用灵石买命", EventEffectType.LoseGold, 55),
                Ch("以术法遁走", EventEffectType.Nothing, 0));

            c += C(dir, r, "jd_volcano", "火山灵脉",
                "一座活火山正在喷发，火山口中灵气翻涌。",
                Ch("冒险采集灵火", EventEffectType.TakeDamage, 12),
                Ch("以灵石护身采集", EventEffectType.LoseGold, 40),
                Ch("远观", EventEffectType.Nothing, 0));

            c += C(dir, r, "jd_ice_cave", "冰窟秘藏",
                "你进入一座冰窟，冰壁中冻结着各种灵材和宝物。",
                Ch("凿冰取宝", EventEffectType.TakeDamage, 8),
                Ch("以火系功法融化", EventEffectType.LoseGold, 35),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "jd_flying_sword", "飞剑认主",
                "一柄无人驾驭的飞剑在你头顶盘旋，发出嗡鸣声。",
                Ch("伸手接剑", EventEffectType.TakeDamage, 6),
                Ch("以灵力引导", EventEffectType.GainCard, 1),
                Ch("退避", EventEffectType.Nothing, 0));

            c += C(dir, r, "jd_ghost_marriage", "鬼婚仪典",
                "你偶遇一场鬼婚仪式，鬼火缭绕，纸钱纷飞。",
                Ch("观礼", EventEffectType.Heal, 15),
                Ch("破坏仪式", EventEffectType.TakeDamage, 10),
                Ch("远离", EventEffectType.Nothing, 0));

            c += C(dir, r, "jd_spirit_ring", "灵戒空间",
                "你捡到一枚灵戒，戒面闪烁微光，内含一个小型储物空间。",
                Ch("打开空间", EventEffectType.GainGold, 70),
                Ch("抹去印记据为己有", EventEffectType.GainRelic, 1),
                Ch("丢弃", EventEffectType.Nothing, 0));

            c += C(dir, r, "jd_secret_realm_gate", "秘境之门",
                "空中出现一道裂缝，裂缝后是一片灵气浓郁的秘境。",
                Ch("踏入秘境", EventEffectType.TakeDamage, 12),
                Ch("以灵石稳定裂缝", EventEffectType.LoseGold, 60),
                Ch("等待裂缝关闭", EventEffectType.Nothing, 0));

            c += C(dir, r, "jd_beast_king_trial", "兽王考验",
                "一头灵兽王拦住你，它不开口，只是用眼神示意你跟随。",
                Ch("跟随", EventEffectType.GainMaxHP, 6),
                Ch("反抗", EventEffectType.TakeDamage, 14),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "jd_water_dragon", "水龙吐珠",
                "湖面突然翻涌，一条水龙从湖中升起，口含一颗灵珠。",
                Ch("夺灵珠", EventEffectType.TakeDamage, 15),
                Ch("以灵石交换", EventEffectType.LoseGold, 70),
                Ch("退避", EventEffectType.Nothing, 0));

            c += C(dir, r, "jd_starlight_pool", "星辉灵池",
                "夜晚，一处灵池映照星空，池水含有星辰之力。",
                Ch("入池沐浴", EventEffectType.GainMaxHP, 5),
                Ch("收集星辉水", EventEffectType.GainPotion, 1),
                Ch("欣赏", EventEffectType.Heal, 18));

            c += C(dir, r, "jd_corpse_puppet", "傀儡遗骸",
                "你发现一具人形傀儡，傀儡身上刻满了灵纹。",
                Ch("激活傀儡", EventEffectType.TakeDamage, 10),
                Ch("拆解灵纹", EventEffectType.UpgradeRandomCard, 1),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "jd_medicine_field", "灵药仙田",
                "你发现一片被人精心照料的灵药田，药香扑鼻。",
                Ch("采摘灵药", EventEffectType.GainMaterial, 2),
                Ch("购买灵药", EventEffectType.LoseGold, 45),
                Ch("偷取种子", EventEffectType.TakeDamage, 8));

            c += C(dir, r, "jd_sky_ladder", "通天梯",
                "一道石梯从地面直入云霄，梯上灵气浓郁。",
                Ch("攀登石梯", EventEffectType.TakeDamage, 10),
                Ch("打坐修炼", EventEffectType.Heal, 20),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "jd_gambling_stone", "赌石坊",
                "一位赌石坊老板邀你选一块原石，说内有灵玉。",
                Ch("选大的", EventEffectType.LoseGold, 50),
                Ch("选小的", EventEffectType.LoseGold, 25),
                Ch("不赌", EventEffectType.Nothing, 0));

            c += C(dir, r, "jd_mirror_world", "镜中世界",
                "一面古镜中映出一个不同的世界，似乎可以踏入其中。",
                Ch("踏入镜中", EventEffectType.TakeDamage, 12),
                Ch("以灵石感应", EventEffectType.LoseGold, 55),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "jd_dragon_garden", "龙园旧址",
                "一处龙族旧址，园中石龙栩栩如生，龙目中有灵光流转。",
                Ch("叩拜石龙", EventEffectType.GainMaxHP, 6),
                Ch("取龙目灵珠", EventEffectType.TakeDamage, 14),
                Ch("离开", EventEffectType.Nothing, 0));

            // (jd_recipe_scroll replaced by mini-game below)

            // 小游戏
            c += C(dir, r, "jd_pinball", "灵珠弹射",
                "一座灵珠弹射台，弹珠随机落入不同区域，奖品各异！\n消耗: 50灵石",
                Ch("弹射灵珠", EventEffectType.MiniPinball, 50),
                Ch("不玩", EventEffectType.Nothing, 0));

            c += C(dir, r, "jd_slot", "灵石机",
                "坊市深处有一台高级灵石机，奖品更丰厚！\n消耗: 40灵石",
                Ch("摇灵石机", EventEffectType.MiniSlot, 40),
                Ch("不玩", EventEffectType.Nothing, 0));

            return c;
        }

        // ========== 元婴期 (3) ==========
        static int GenerateYuanYing(string baseDir)
        {
            string dir = $"{baseDir}/元婴期";
            EnsureFolder(dir);
            int c = 0;
            var r = RealmLevel.YuanYing;

            c += C(dir, r, "yy_tribulation_minor", "小天劫降临",
                "天空突然乌云翻涌，一道天劫雷霆劈下，似乎是冲你而来！",
                Ch("以肉身硬抗天劫", EventEffectType.TakeDamage, 20),
                Ch("以宝物引开天劫", EventEffectType.LoseGold, 80),
                Ch("施展身法闪避", EventEffectType.Nothing, 0));

            c += C(dir, r, "yy_ancient_tomb_lord", "仙君陵寝",
                "你发现了一座上古仙君的陵寝，陵门刻满封印符文。",
                Ch("破封入陵", EventEffectType.TakeDamage, 18),
                Ch("以灵石供奉求宝", EventEffectType.LoseGold, 100),
                Ch("恭敬离去", EventEffectType.Nothing, 0));

            c += C(dir, r, "yy_demon_invasion", "魔气入侵",
                "一股浓郁魔气从地缝涌出，周围灵兽纷纷逃散。",
                Ch("镇压魔气", EventEffectType.TakeDamage, 16),
                Ch("吸收魔气修炼", EventEffectType.GainStrength, 4),
                Ch("退避", EventEffectType.Nothing, 0));

            c += C(dir, r, "yy_void_crack", "虚空裂缝",
                "虚空中出现一道裂缝，裂缝另一侧是混沌虚空。",
                Ch("踏入虚空", EventEffectType.TakeDamage, 18),
                Ch("以灵石封缝", EventEffectType.LoseGold, 90),
                Ch("远离", EventEffectType.Nothing, 0));

            c += C(dir, r, "yy_spirit_king", "灵兽之王",
                "一头四阶灵兽王拦住你的去路，它气息深不可测。",
                Ch("与之交战", EventEffectType.TakeDamage, 20),
                Ch("以灵石示好", EventEffectType.LoseGold, 70),
                Ch("绕道而行", EventEffectType.Nothing, 0));

            c += C(dir, r, "yy_dragon_transformation", "化龙池",
                "一处化龙池，池水含有龙族血脉之力。",
                Ch("入池化龙", EventEffectType.GainMaxHP, 10),
                Ch("收集池水", EventEffectType.GainPotion, 1),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "yy_ancient_pavilion", "藏经古阁",
                "一座漂浮在空中的古阁，阁门大开，内有无数功法竹简。",
                Ch("进入取经", EventEffectType.GainCard, 1),
                Ch("参悟阁前碑文", EventEffectType.UpgradeRandomCard, 1),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "yy_star_fall", "星辰坠落",
                "一颗星辰从天空坠落，砸在不远处，灵气四溢。",
                Ch("采集星核", EventEffectType.TakeDamage, 15),
                Ch("吸收星辉", EventEffectType.GainMaxHP, 8),
                Ch("远观", EventEffectType.Nothing, 0));

            c += C(dir, r, "yy_evil_soul", "邪魂入侵",
                "一道邪魂侵入你的识海，试图夺取你的身体。",
                Ch("以神识驱逐", EventEffectType.TakeDamage, 14),
                Ch("以灵石强化识海", EventEffectType.LoseGold, 80),
                Ch("念经超度", EventEffectType.Nothing, 0));

            c += C(dir, r, "yy_fire_vein", "地火灵脉",
                "地底灵火涌出地表，周围一切都在燃烧。",
                Ch("采集地火", EventEffectType.TakeDamage, 12),
                Ch("以灵石护体采集", EventEffectType.LoseGold, 60),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "yy_immortal_herb", "仙药园",
                "你发现一片仙药园，园中灵药散发仙气。",
                Ch("采摘仙药", EventEffectType.GainMaterial, 3),
                Ch("在园中修炼", EventEffectType.GainMaxHP, 7),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "yy_battle_spirit", "战魂试炼",
                "一位远古战魂拦住你，要求与你比试武道。",
                Ch("应战", EventEffectType.TakeDamage, 16),
                Ch("请教武道", EventEffectType.GainStrength, 4),
                Ch("婉拒", EventEffectType.Nothing, 0));

            c += C(dir, r, "yy_mirror_trial", "镜中幻我",
                "古镜中走出一个和你一模一样的幻影，手持相同功法。",
                Ch("与幻我交战", EventEffectType.TakeDamage, 15),
                Ch("与幻我和解", EventEffectType.GainMaxHP, 6),
                Ch("逃离", EventEffectType.Nothing, 0));

            c += C(dir, r, "yy_ice_phoenix", "冰凰遗羽",
                "一根冰凰遗羽飘落在雪地中，散发寒气。",
                Ch("拾取遗羽", EventEffectType.TakeDamage, 10),
                Ch("吸收寒冰灵气", EventEffectType.GainMaxHP, 6),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "yy_time_river", "时间残流",
                "你进入一片时间流速异常的区域，过去与未来交织。",
                Ch("感悟时间法则", EventEffectType.GainMaxHP, 8),
                Ch("强行穿越", EventEffectType.TakeDamage, 20),
                Ch("退出", EventEffectType.Nothing, 0));

            c += C(dir, r, "yy_demon_gate", "魔门入侵",
                "魔门打开，涌出一群魔修，为首者气息达到元婴境。",
                Ch("迎战魔修", EventEffectType.TakeDamage, 18),
                Ch("以灵石贿赂", EventEffectType.LoseGold, 90),
                Ch("退避", EventEffectType.Nothing, 0));

            c += C(dir, r, "yy_sword_tomb", "万剑冢",
                "一座巨大的剑冢，数万柄剑插在地面上，剑意冲天。",
                Ch("拔万剑之王", EventEffectType.TakeDamage, 16),
                Ch("吸收万剑剑意", EventEffectType.GainStrength, 4),
                Ch("参悟剑阵", EventEffectType.UpgradeRandomCard, 1));

            c += C(dir, r, "yy_mine_spirit", "矿脉之灵",
                "一处灵石矿脉中诞生了矿脉之灵，它守护着整条矿脉。",
                Ch("与矿灵交涉", EventEffectType.LoseGold, 80),
                Ch("强夺矿脉", EventEffectType.TakeDamage, 16),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "yy_cauldron_lord", "丹炉器灵",
                "一座古丹炉的器灵现身，它邀请你进入丹炉内部空间。",
                Ch("进入丹炉", EventEffectType.GainPotion, 1),
                Ch("夺取器灵", EventEffectType.TakeDamage, 14),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "yy_star_palace", "星宫遗迹",
                "一座漂浮在星空中的宫殿遗迹，宫门大开。",
                Ch("进入星宫", EventEffectType.TakeDamage, 15),
                Ch("以灵石开路", EventEffectType.LoseGold, 100),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "yy_ghost_bridge", "黄泉渡口",
                "你来到一处黄泉渡口，摆渡的鬼差向你招手。",
                Ch("上船渡河", EventEffectType.TakeDamage, 18),
                Ch("给鬼差灵石", EventEffectType.LoseGold, 70),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "yy_spirit_storm", "灵气风暴",
                "一场灵气风暴席卷而来，风暴中心灵气浓度惊人。",
                Ch("冲入风暴中心", EventEffectType.TakeDamage, 16),
                Ch("吸收边缘灵气", EventEffectType.Heal, 25),
                Ch("退避", EventEffectType.Nothing, 0));

            c += C(dir, r, "yy_pill_tribulation", "丹劫",
                "一颗丹药正在渡劫，丹劫之力远超寻常。",
                Ch("抢夺渡劫丹", EventEffectType.TakeDamage, 20),
                Ch("协助渡劫", EventEffectType.GainPotion, 1),
                Ch("远观", EventEffectType.Nothing, 0));

            c += C(dir, r, "yy_secret_realm", "秘境残片",
                "一块秘境残片飘落，残片内还残留着部分空间法则。",
                Ch("参悟空间法则", EventEffectType.GainMaxHP, 8),
                Ch("炼化残片", EventEffectType.LoseMaxHP, 3),
                Ch("丢弃", EventEffectType.Nothing, 0));

            c += C(dir, r, "yy_dark_phoenix", "暗凰降世",
                "暗凰从虚空中降临，周围一切陷入黑暗。",
                Ch("对抗暗凰", EventEffectType.TakeDamage, 18),
                Ch("以灵石买命", EventEffectType.LoseGold, 100),
                Ch("遁走", EventEffectType.Nothing, 0));

            c += C(dir, r, "yy_tao_stone", "道韵石",
                "一块蕴含大道韵律的道韵石悬浮在空中。",
                Ch("参悟道韵", EventEffectType.UpgradeRandomCard, 1),
                Ch("吸收道韵", EventEffectType.GainMaxHP, 7),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "yy_chaos_vein", "混沌灵脉",
                "一条混沌灵脉在地下涌动，灵脉中灵气呈混沌色。",
                Ch("吸收混沌灵气", EventEffectType.TakeDamage, 15),
                Ch("采集混沌灵石", EventEffectType.GainGold, 120),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "yy_reincarnation_pool", "轮回之池",
                "一处轮回之池，池中映照出你的前世今生。",
                Ch("参悟轮回", EventEffectType.GainMaxHP, 10),
                Ch("跳入池中", EventEffectType.TakeDamage, 20),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "yy_dragon_gate", "龙门天关",
                "一道龙门悬浮在空中，龙门后灵气如瀑布倾泻。",
                Ch("跳过龙门", EventEffectType.TakeDamage, 18),
                Ch("在龙门下修炼", EventEffectType.GainMaxHP, 8),
                Ch("离开", EventEffectType.Nothing, 0));

            // (yy_mana_well replaced by mini-game below)

            // 小游戏
            c += C(dir, r, "yy_ring_toss", "套灵兽",
                "元婴期灵兽园，可投灵石套取高阶灵兽奖励！\n消耗: 80灵石",
                Ch("投圈套灵兽", EventEffectType.MiniRingToss, 80),
                Ch("不玩", EventEffectType.Nothing, 0));

            return c;
        }

        // ========== 化神期 (4) ==========
        static int GenerateHuaShen(string baseDir)
        {
            string dir = $"{baseDir}/化神期";
            EnsureFolder(dir);
            int c = 0;
            var r = RealmLevel.HuaShen;

            c += C(dir, r, "hs_tribulation_major", "大天劫降临",
                "九重天上雷云翻涌，一道远超寻常的天劫雷霆蓄势待发，直指你而来！",
                Ch("以肉身硬抗大天劫", EventEffectType.TakeDamage, 30),
                Ch("以法宝引开天劫", EventEffectType.LoseGold, 150),
                Ch("施展大神通闪避", EventEffectType.Nothing, 0));

            c += C(dir, r, "hs_immortal_cave", "仙人洞府",
                "一位飞升仙人遗留的洞府，洞府门上刻着仙文封印。",
                Ch("破封入洞府", EventEffectType.TakeDamage, 22),
                Ch("以仙石供奉", EventEffectType.LoseGold, 200),
                Ch("恭敬离去", EventEffectType.Nothing, 0));

            c += C(dir, r, "hs_demon_emperor", "魔帝降临",
                "一位魔帝分身降临，周围空间被魔气压塌。",
                Ch("与魔帝交战", EventEffectType.TakeDamage, 28),
                Ch("以巨量灵石换命", EventEffectType.LoseGold, 180),
                Ch("施展遁术逃离", EventEffectType.Nothing, 0));

            c += C(dir, r, "hs_chaos_crack", "混沌裂缝",
                "一道混沌裂缝出现在空中，裂缝另一侧是原始混沌。",
                Ch("踏入混沌", EventEffectType.TakeDamage, 25),
                Ch("采集混沌之气", EventEffectType.GainMaxHP, 12),
                Ch("封印裂缝", EventEffectType.LoseGold, 120));

            c += C(dir, r, "hs_dragon_emperor", "龙帝遗宝",
                "一位龙帝遗留的宝库，宝库前有龙魂守护。",
                Ch("与龙魂交战", EventEffectType.TakeDamage, 24),
                Ch("以龙血供奉", EventEffectType.LoseGold, 150),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "hs_star_core", "星辰内核",
                "一颗即将熄灭的星辰内核，蕴含着恐怖的星辰之力。",
                Ch("吸收星辰之力", EventEffectType.GainMaxHP, 15),
                Ch("采集星核", EventEffectType.TakeDamage, 22),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "hs_immortal_medicine", "仙药秘境",
                "一片仙药秘境，药园中生长着各种仙品灵药。",
                Ch("采摘仙药", EventEffectType.GainMaterial, 3),
                Ch("在仙药园修炼", EventEffectType.GainMaxHP, 12),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "hs_war_god", "战神残魂",
                "一位远古战神的残魂现身，手持一柄残破战枪。",
                Ch("与战神交战", EventEffectType.TakeDamage, 25),
                Ch("请教武道", EventEffectType.GainStrength, 5),
                Ch("退避", EventEffectType.Nothing, 0));

            c += C(dir, r, "hs_void_phoenix", "虚空凤皇",
                "一只虚空凤皇从虚空中现身，浑身燃烧虚空之火。",
                Ch("对抗凤皇", EventEffectType.TakeDamage, 26),
                Ch("以灵石供奉", EventEffectType.LoseGold, 160),
                Ch("退避", EventEffectType.Nothing, 0));

            c += C(dir, r, "hs_time_river", "时间长河",
                "你来到时间长河的岸边，河水映照着无数前世的画面。",
                Ch("参悟时间长河", EventEffectType.GainMaxHP, 15),
                Ch("跳入河中", EventEffectType.TakeDamage, 30),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "hs_soul_trial", "神魂试炼",
                "一处神魂试炼场，场中有无数幻魔攻击神魂。",
                Ch("以神魂迎战", EventEffectType.TakeDamage, 20),
                Ch("参悟魂道", EventEffectType.UpgradeRandomCard, 1),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "hs_creation_stone", "造化之石",
                "一块蕴含造化之力的神石悬浮在空中，石中有法则流转。",
                Ch("参悟造化法则", EventEffectType.GainMaxHP, 12),
                Ch("吸收造化之力", EventEffectType.GainStrength, 5),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "hs_realm_gate", "界门",
                "一道通往其他界面的界门出现，门后灵气性质截然不同。",
                Ch("踏入界门", EventEffectType.TakeDamage, 24),
                Ch("以灵石稳定界门", EventEffectType.LoseGold, 150),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "hs_demon_seal_break", "魔封破除",
                "一处远古魔封正在松动，封印中的大魔即将脱困。",
                Ch("加固封印", EventEffectType.GainMaxHP, 10),
                Ch("解封收魔", EventEffectType.TakeDamage, 28),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "hs_starship", "星空古船",
                "一艘星空古船漂浮在虚空中，船帆已破，但仍有灵气。",
                Ch("登船探索", EventEffectType.TakeDamage, 22),
                Ch("以灵石开船", EventEffectType.LoseGold, 140),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "hs_heavenly_court", "天庭旧址",
                "一处天庭旧址，金砖玉瓦散落一地，仙气犹存。",
                Ch("搜寻仙宝", EventEffectType.GainGold, 200),
                Ch("参悟天庭法则", EventEffectType.GainMaxHP, 12),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "hs_phoenix_rebirth", "凤皇涅槃",
                "一只凤皇正在涅槃重生，浴火之地灵气冲天。",
                Ch("在涅槃火中修炼", EventEffectType.TakeDamage, 24),
                Ch("收集涅槃火", EventEffectType.GainPotion, 1),
                Ch("远观", EventEffectType.Nothing, 0));

            c += C(dir, r, "hs_sword_domain", "剑域",
                "一片独立的剑域空间，域中万剑齐鸣。",
                Ch("参悟剑域", EventEffectType.UpgradeRandomCard, 1),
                Ch("以力破剑域", EventEffectType.TakeDamage, 22),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "hs_yin_yang_stone", "阴阳神石",
                "一块阴阳神石悬浮在空中，一半炽热一半冰冷。",
                Ch("参悟阴阳", EventEffectType.GainMaxHP, 12),
                Ch("吸收阴阳之力", EventEffectType.GainStrength, 5),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "hs_five_elements", "五行灵地",
                "一处五行灵气汇聚之地，金木水火土五行齐全。",
                Ch("参悟五行", EventEffectType.UpgradeRandomCard, 1),
                Ch("吸收五行灵气", EventEffectType.GainMaxHP, 10),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "hs_demon_pit", "魔渊",
                "一个通往魔界的深渊，深渊中魔气滔天。",
                Ch("深入魔渊", EventEffectType.TakeDamage, 28),
                Ch("采集魔气结晶", EventEffectType.GainGold, 180),
                Ch("封印魔渊", EventEffectType.LoseGold, 150));

            c += C(dir, r, "hs_spirit_storm", "混沌灵暴",
                "一场混沌灵暴席卷而来，暴中灵气呈混沌色，破坏力惊人。",
                Ch("冲入暴眼", EventEffectType.TakeDamage, 26),
                Ch("吸收边缘灵气", EventEffectType.GainMaxHP, 10),
                Ch("退避", EventEffectType.Nothing, 0));

            c += C(dir, r, "hs_cauldron_immortal", "仙丹炉",
                "一座仙级丹炉，炉中有一颗已成的仙丹。",
                Ch("取仙丹", EventEffectType.TakeDamage, 22),
                Ch("参悟丹道", EventEffectType.UpgradeRandomCard, 1),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "hs_reincarnation_path", "黄泉路",
                "你踏上了黄泉路，路两旁是无尽的彼岸花。",
                Ch("沿路前行", EventEffectType.TakeDamage, 24),
                Ch("参悟生死", EventEffectType.GainMaxHP, 15),
                Ch("回头", EventEffectType.Nothing, 0));

            c += C(dir, r, "hs_god_relic", "神兵遗宝",
                "一柄神兵悬浮在空中，兵器灵已散，但威压犹在。",
                Ch("认主神兵", EventEffectType.TakeDamage, 25),
                Ch("以灵石购买", EventEffectType.LoseGold, 200),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "hs_dao_stone", "大道石碑",
                "一块刻满大道符文的石碑，碑上每道符文都蕴含一条大道。",
                Ch("参悟大道", EventEffectType.GainMaxHP, 15),
                Ch("触摸石碑", EventEffectType.GainStrength, 6),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "hs_chaos_pavilion", "混沌藏经阁",
                "一座漂浮在混沌中的藏经阁，阁内有无上功法。",
                Ch("入阁取经", EventEffectType.TakeDamage, 24),
                Ch("参悟阁前碑文", EventEffectType.UpgradeRandomCard, 1),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "hs_spirit_emperor", "灵帝陵",
                "一位灵帝的陵寝，陵门上刻着大道封印。",
                Ch("破封入陵", EventEffectType.TakeDamage, 26),
                Ch("以仙石供奉", EventEffectType.LoseGold, 250),
                Ch("恭敬离去", EventEffectType.Nothing, 0));

            c += C(dir, r, "hs_void_dragon", "虚空龙骨",
                "一具虚空龙族的龙骨散落在虚空中，骨中蕴含空间法则。",
                Ch("参悟空间法则", EventEffectType.GainMaxHP, 12),
                Ch("采集龙骨", EventEffectType.TakeDamage, 22),
                Ch("离开", EventEffectType.Nothing, 0));

            // (hs_star_tomb replaced by mini-game below)

            // 小游戏
            c += C(dir, r, "hs_slot", "仙界灵石机",
                "仙界灵石机，奖品丰厚，可获仙品灵材甚至遗物！\n消耗: 150灵石",
                Ch("摇仙界灵石机", EventEffectType.MiniSlot, 150),
                Ch("不玩", EventEffectType.Nothing, 0));

            return c;
        }

        // ========== 渡劫期 (5) ==========
        static int GenerateDuJie(string baseDir)
        {
            string dir = $"{baseDir}/渡劫期";
            EnsureFolder(dir);
            int c = 0;
            var r = RealmLevel.DuJie;

            c += C(dir, r, "dj_final_tribulation", "终极天劫",
                "天空裂开九道缝隙，九道天劫同时降下，这是飞升前的终极考验！",
                Ch("以肉身硬抗九劫", EventEffectType.TakeDamage, 40),
                Ch("以全部法宝抵御", EventEffectType.LoseGold, 300),
                Ch("施展无上身法闪避", EventEffectType.Nothing, 0));

            c += C(dir, r, "dj_immortal_gate", "仙界之门",
                "一道通往仙界的大门出现，门后仙气如潮水般涌出。",
                Ch("踏入仙门", EventEffectType.TakeDamage, 35),
                Ch("在门前修炼", EventEffectType.GainMaxHP, 20),
                Ch("暂不飞升", EventEffectType.Nothing, 0));

            c += C(dir, r, "dj_demon_emperor_true", "真魔帝降临",
                "一位真正的魔帝降临此界，天地为之色变。",
                Ch("与魔帝决战", EventEffectType.TakeDamage, 35),
                Ch("以天材地宝换命", EventEffectType.LoseGold, 280),
                Ch("施展遁天术逃离", EventEffectType.Nothing, 0));

            c += C(dir, r, "dj_creation_pool", "造化之池",
                "一处造化之池，池中液体蕴含天地造化之力。",
                Ch("入池沐浴", EventEffectType.GainMaxHP, 20),
                Ch("收集池水", EventEffectType.GainPotion, 1),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "dj_chaos_origin", "混沌本源",
                "混沌本源之地，一切法则的源头。",
                Ch("参悟本源", EventEffectType.GainMaxHP, 25),
                Ch("吸收本源之力", EventEffectType.GainStrength, 8),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "dj_immortal_tomb_lord", "仙帝陵寝",
                "一位仙帝的陵寝，陵门上有仙帝亲刻的封印。",
                Ch("破封入陵", EventEffectType.TakeDamage, 32),
                Ch("以天材地宝供奉", EventEffectType.LoseGold, 300),
                Ch("恭敬离去", EventEffectType.Nothing, 0));

            c += C(dir, r, "dj_reincarnation_wheel", "轮回天轮",
                "巨大的轮回天轮悬浮在空中，轮上映照六道轮回。",
                Ch("参悟六道轮回", EventEffectType.GainMaxHP, 22),
                Ch("跳入轮回", EventEffectType.TakeDamage, 35),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "dj_dragon_god", "龙神遗骨",
                "一具龙神的遗骨，骨中残留着龙神的意志。",
                Ch("参悟龙神意志", EventEffectType.GainStrength, 8),
                Ch("采集龙神骨", EventEffectType.TakeDamage, 28),
                Ch("恭敬离去", EventEffectType.Nothing, 0));

            c += C(dir, r, "dj_star_emperor", "星帝",
                "一位星帝现身，他掌控着万千星辰之力。",
                Ch("与星帝论道", EventEffectType.GainMaxHP, 20),
                Ch("与星帝交战", EventEffectType.TakeDamage, 35),
                Ch("退避", EventEffectType.Nothing, 0));

            c += C(dir, r, "dj_void_origin", "虚空本源",
                "虚空本源之地，空间法则的最深处。",
                Ch("参悟空间本源", EventEffectType.GainMaxHP, 20),
                Ch("踏入虚空本源", EventEffectType.TakeDamage, 30),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "dj_time_origin", "时间本源",
                "时间长河的源头，过去现在未来在此交汇。",
                Ch("参悟时间本源", EventEffectType.UpgradeRandomCard, 1),
                Ch("跳入时间源头", EventEffectType.TakeDamage, 32),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "dj_yin_yang_origin", "阴阳本源",
                "阴阳本源之地，阴与阳在此完美平衡。",
                Ch("参悟阴阳本源", EventEffectType.GainMaxHP, 22),
                Ch("吸收阴阳本源", EventEffectType.GainStrength, 7),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "dj_five_elements_origin", "五行本源",
                "五行本源之地，金木水火土五行在此轮转。",
                Ch("参悟五行本源", EventEffectType.UpgradeRandomCard, 1),
                Ch("吸收五行本源", EventEffectType.GainMaxHP, 18),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "dj_demon_origin", "魔之本源",
                "魔之本源之地，一切魔道的根源。",
                Ch("吸收魔之本源", EventEffectType.TakeDamage, 30),
                Ch("参悟魔道", EventEffectType.GainStrength, 8),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "dj_immortal_medicine", "仙品神药",
                "一株仙品神药在虚空中绽放，药香弥漫数万里。",
                Ch("采摘神药", EventEffectType.GainPotion, 1),
                Ch("在神药旁修炼", EventEffectType.GainMaxHP, 18),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "dj_war_god_true", "真战神",
                "一位远古真战神现身，他手持开天神枪。",
                Ch("与战神决战", EventEffectType.TakeDamage, 35),
                Ch("请教无上武道", EventEffectType.GainStrength, 8),
                Ch("退避", EventEffectType.Nothing, 0));

            c += C(dir, r, "dj_star_core_origin", "星辰本源",
                "一颗正在形成的星辰本源，蕴含创世之力。",
                Ch("参悟创世法则", EventEffectType.GainMaxHP, 20),
                Ch("吸收创世之力", EventEffectType.TakeDamage, 30),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "dj_phoenix_origin", "凤皇本源",
                "凤皇本源之地，一切火之本源所在。",
                Ch("参悟火之本源", EventEffectType.GainStrength, 7),
                Ch("吸收凤皇本源", EventEffectType.TakeDamage, 28),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "dj_reincarnation_origin", "轮回本源",
                "轮回本源之地，生死在此轮转不息。",
                Ch("参悟轮回本源", EventEffectType.GainMaxHP, 25),
                Ch("跳入轮回", EventEffectType.TakeDamage, 35),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "dj_dao_origin", "大道本源",
                "大道本源之地，一切法则的终极源头。",
                Ch("参悟大道本源", EventEffectType.GainMaxHP, 25),
                Ch("触摸大道", EventEffectType.GainStrength, 10),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "dj_void_battle", "虚空大战",
                "两位虚空大能在虚空中交战，余波波及周围一切。",
                Ch("观战悟道", EventEffectType.UpgradeRandomCard, 1),
                Ch("加入战斗", EventEffectType.TakeDamage, 30),
                Ch("远离", EventEffectType.Nothing, 0));

            c += C(dir, r, "dj_chaos_battle", "混沌大战",
                "混沌中两位混沌大能在交战，混沌不断被撕裂又愈合。",
                Ch("观战参悟", EventEffectType.UpgradeRandomCard, 1),
                Ch("采集混沌碎片", EventEffectType.GainGold, 250),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "dj_immortal_relic", "仙器认主",
                "一件仙器悬浮在虚空中，器灵已现，等待有缘人。",
                Ch("认主仙器", EventEffectType.TakeDamage, 30),
                Ch("以灵石供奉", EventEffectType.LoseGold, 300),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "dj_heavenly_court", "天庭",
                "天庭出现在你面前，仙兵仙将在天门外巡逻。",
                Ch("闯入天庭", EventEffectType.TakeDamage, 35),
                Ch("在庭外修炼", EventEffectType.GainMaxHP, 18),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "dj_realm_dissolve", "界面崩塌",
                "整个界面开始崩塌，虚空裂缝遍布四方。",
                Ch("稳住界面", EventEffectType.GainMaxHP, 20),
                Ch("采集崩塌灵气", EventEffectType.TakeDamage, 30),
                Ch("逃离界面", EventEffectType.Nothing, 0));

            c += C(dir, r, "dj_dao_tribulation", "道劫",
                "天地降下道劫，这是对悟道者的终极考验。",
                Ch("以道抗劫", EventEffectType.TakeDamage, 35),
                Ch("以灵石抵御", EventEffectType.LoseGold, 280),
                Ch("退避", EventEffectType.Nothing, 0));

            c += C(dir, r, "dj_creation", "创世之地",
                "一处创世之地，新世界正在诞生。",
                Ch("参悟创世", EventEffectType.GainMaxHP, 25),
                Ch("参与创世", EventEffectType.TakeDamage, 35),
                Ch("离开", EventEffectType.Nothing, 0));

            c += C(dir, r, "dj_immortal_trial", "仙人考验",
                "一位仙人现身，他要考验你是否够资格飞升。",
                Ch("接受考验", EventEffectType.TakeDamage, 32),
                Ch("请教仙道", EventEffectType.UpgradeRandomCard, 1),
                Ch("婉拒", EventEffectType.Nothing, 0));

            c += C(dir, r, "dj_final_gate", "飞升之门",
                "最终的飞升之门出现了，门后是永恒的仙界。",
                Ch("踏入飞升之门", EventEffectType.GainMaxHP, 30),
                Ch("在门前最终修炼", EventEffectType.FullHeal, 0),
                Ch("暂不飞升", EventEffectType.Nothing, 0));

            // (dj_dao_battle replaced by mini-game below)

            // 小游戏
            c += C(dir, r, "dj_dice", "天命掷骰",
                "飞升前最后一赌，天命骰决定你的命运！\n消耗: 300灵石",
                Ch("掷天命骰", EventEffectType.MiniDice, 300),
                Ch("不赌", EventEffectType.Nothing, 0));

            return c;
        }

        // ========== 工具方法 ==========
        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        static void DeleteOldEvents(string baseDir)
        {
            var guids = AssetDatabase.FindAssets("t:EventData", new[] { baseDir });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var dir = Path.GetDirectoryName(path).Replace('\\', '/');
                if (dir == baseDir)
                    AssetDatabase.DeleteAsset(path);
            }
        }

        static List<EventChoice> Choices(params EventChoice[] items)
        {
            return new List<EventChoice>(items);
        }

        static EventChoice Ch(string text, EventEffectType type, int value, string cardId = "")
        {
            return new EventChoice
            {
                choiceText = text,
                effectType = type,
                effectValue = value,
                cardId = cardId
            };
        }

        static int C(string dir, RealmLevel realm, string id, string evtName, string desc, params EventChoice[] choices)
        {
            string path = $"{dir}/{id}.asset";
            if (AssetDatabase.LoadAssetAtPath<EventData>(path) != null)
                AssetDatabase.DeleteAsset(path);
            var e = ScriptableObject.CreateInstance<EventData>();
            e.eventId = id;
            e.name = evtName;
            e.description = desc;
            e.requiredRealm = realm;
            e.choices = new List<EventChoice>(choices);
            AssetDatabase.CreateAsset(e, path);
            return 1;
        }
    }
}
