using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using CardGame;

namespace CardGame.Editor
{
    public static class RecipeBatchGenerator
    {
        [MenuItem("Tools/Generate Recipes")]
        public static void GenerateAll()
        {
            string dir = "Assets/NueGames/NueDeck/Data/Recipes";
            if (!AssetDatabase.IsValidFolder(dir)) { var p = Path.GetDirectoryName(dir).Replace('\\','/'); AssetDatabase.CreateFolder(p, Path.GetFileName(dir)); }

            int c = 0;

            // ===== 丹道 Alchemy (20) — 灵草+灵水+妖丹 → 卡牌 =====
            c += Create("alchemy_huixue","回血丹方","以灵芝草与灵泉水炼制的回血丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"5_heal_basic",1,0.9f,true, Ing(("herb_lingzhi",2),("water_quan",1)));
            c += Create("alchemy_gedi","固本丹方","以百年何首乌与玉露炼制的固本丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"body_su_breath",1,0.8f,true, Ing(("herb_heishouwu",1),("herb_lingzhi",1),("water_lu",1)));
            c += Create("alchemy_jianqi","剑气丹方","以剑灵剑丹与龙涎草炼制的剑气丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"jian_mh_pierce",1,0.6f,false, Ing(("core_jianling",1),("herb_longxian",1),("water_longxian",1)));
            c += Create("alchemy_jinzhong","金钟丹方","以熊妖力丹与铁木心炼制的金钟罩丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"body_th_bell",1,0.6f,false, Ing(("core_bearforce",1),("ore_xuantie",2),("water_lu",1)));
            c += Create("alchemy_fanzhen","反震丹方","以贪狼煞丹与黄精炼制的反震诀丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"body_th_reflect",1,0.6f,false, Ing(("core_langsha",1),("herb_huangjing",2),("water_quan",1)));
            c += Create("alchemy_gudu","蛊毒丹方","以碧磷蛇丹与蛇苗草炼制的蛊毒丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"spr_db_poison",1,0.6f,false, Ing(("core_snakepoison",1),("herb_duwei",1),("water_duquan",1)));
            c += Create("alchemy_shenxing","调息丹方","以雪参与茯苓炼制的调息丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"body_su_breath",1,0.85f,true, Ing(("herb_snowginseng",1),("herb_fuling",1),("water_lu",1)));
            c += Create("alchemy_tianlei","天雷丹方","以紫雷晶与雷公藤炼制的引雷诀丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"spr_mn_thunder",1,0.5f,false, Ing(("ore_zilei",1),("herb_leigong",1),("water_leitan",1)));
            c += Create("alchemy_huopo","火魄丹方","以火魄焰丹与赤焰铜炼制的炎魔之力丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"jian_br_charge",1,0.5f,false, Ing(("core_huopolie",1),("ore_chiyan",1),("water_yanquan",1)));
            c += Create("alchemy_bingpo","冰魄丹方","以冰魄寒丹与寒玉石炼制的冰系丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"body_su_root",1,0.5f,false, Ing(("core_bingpohan",1),("ore_hanyu",1),("water_bingjing",1)));
            c += Create("alchemy_lingli","灵力丹方","以灵晶石与灵泉水炼制的聚灵丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"8_skill_earnMana",1,0.85f,true, Ing(("ore_lingjing",2),("water_quan",1)));
            c += Create("alchemy_miepo","灭魂丹方","以鬼将冥丹与冥魄玉炼制的灭魂丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"spr_db_chaos",1,0.45f,false, Ing(("core_ghostjiang",1),("soul_mingpo",1),("water_minglu",1)));
            c += Create("alchemy_tiejia","铁甲丹方","以玄铁矿与熊妖力丹炼制的铁布衫丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"body_th_iron",1,0.7f,false, Ing(("ore_xuantie",2),("core_bearforce",1),("water_lu",1)));
            c += Create("alchemy_xunji","迅捷丹方","以鹰妖眼丹与风灵木炼制的迅捷斩丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"2_attack_fast",1,0.8f,true, Ing(("core_eagleeye",1),("wood_fengling",1),("water_fenglu",1)));
            c += Create("alchemy_fengxue","凤血丹方","以凤血藤与蟠桃酒炼制的凤血丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"body_su_spring",1,0.5f,false, Ing(("herb_fengxueteng",1),("water_pantao",1),("herb_jingxue",1)));
            c += Create("alchemy_longya","龙芽丹方","以紫韵龙芽与龙涎香液炼制的龙芽丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"jian_mh_chain",1,0.45f,false, Ing(("herb_longya",1),("water_longxian",1),("core_snakepoison",1)));
            c += Create("alchemy_yaowang","药王丹方","以太乙神芝与九转琼浆炼制的仙品丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"body_su_gold",1,0.3f,false, Ing(("herb_taiyi",1),("water_jiuzhuan",1),("herb_snowlotus",1)));
            c += Create("alchemy_mieying","灭影丹方","以影煞暗丹与暗晶魂石炼制的灭影丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"jian_br_void",1,0.4f,false, Ing(("core_yingsha",1),("soul_anjing",1),("water_moqi",1)));
            c += Create("alchemy_bilian","碧莲丹方","以碧灵花与凝魂石炼制的安魂丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"body_su_turtle",1,0.6f,false, Ing(("herb_bilinghua",1),("soul_ning",1),("water_minglu",1)));
            c += Create("alchemy_niepan","涅槃丹方","以涅槃果与三光神水炼制的涅槃丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"body_su_gold",1,0.25f,false, Ing(("herb_niepan",1),("water_sanguang",1),("core_heifeng",1)));

            // ===== 器道 Forging (20) — 矿石+灵木+灵兽骨 → 遗物 =====
            c += Create("forging_xuantie","玄铁剑图","以玄铁矿与松心木锻造的玄铁剑图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_war_horn",1,0.7f,true, Ing(("ore_xuantie",3),("wood_songxinmu",1),("bone_0",1)));
            c += Create("forging_tianxing","天星器图","以天星铁与星辰砂锻造的天星器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_anger_stone",1,0.5f,false, Ing(("ore_tianxingtie",1),("ore_xingchen",1),("bone_9",1)));
            c += Create("forging_jinzhong","金钟器图","以赤焰铜与铁木心锻造的金钟器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_gold_armor",1,0.5f,false, Ing(("ore_chiyan",2),("wood_18",1),("bone_2",1)));
            c += Create("forging_guiwang","鬼王器图","以冥砂与墨玉锻造的鬼王器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_burning_blood",1,0.45f,false, Ing(("ore_mingsha",2),("ore_heiyu",1),("bone_25",1)));
            c += Create("forging_leigong","雷公器图","以紫雷晶与雷击木锻造的雷公器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_snake_ring",1,0.5f,false, Ing(("ore_zilei",1),("wood_20",1),("bone_11",1)));
            c += Create("forging_longjia","龙甲器图","以龙骨灵与龙血木锻造的龙甲器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_war_horn",1,0.4f,false, Ing(("bone_9",1),("wood_26",1),("ore_zijin",2)));
            c += Create("forging_bingjia","冰甲器图","以冰魄银与冰木灵锻造的冰甲器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_gold_armor",1,0.5f,false, Ing(("ore_bingpo",1),("wood_19",1),("bone_19",1)));
            c += Create("forging_fengying","风鹰器图","以风砂石与风灵木锻造的风鹰器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_anger_stone",1,0.6f,false, Ing(("ore_fengsha",2),("wood_21",1),("bone_3",1)));
            c += Create("forging_yanyan","炎焰器图","以炎铜与火木灵锻造的炎焰器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_burning_blood",1,0.55f,false, Ing(("ore_yantong",1),("wood_19",1),("bone_13",1)));
            c += Create("forging_jiutian","九天器图","以九天陨铁与金丝楠锻造的九天器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_war_horn",1,0.35f,false, Ing(("ore_jiutianxie",1),("wood_16",1),("bone_10",1)));
            c += Create("forging_xukong","虚空器图","以虚空晶与建木心锻造的虚空器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_snake_ring",1,0.3f,false, Ing(("ore_xukong",1),("wood_32",1),("bone_38",1)));
            c += Create("forging_taiji","太极器图","以太极神金与太极木锻造的太极器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_gold_armor",1,0.2f,false, Ing(("ore_taiji",1),("wood_38",1),("bone_39",1)));
            c += Create("forging_hundun","混沌器图","以混沌原石与混沌木锻造的混沌器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_war_horn",1,0.2f,false, Ing(("ore_hundun",1),("wood_37",1),("bone_39",1)));
            c += Create("forging_xianjing","仙晶器图","以仙晶与蟠桃木锻造的仙晶器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_burning_blood",1,0.25f,false, Ing(("ore_xianjing",1),("wood_35",1),("bone_9",1)));
            c += Create("forging_shenfu","神符器图","以神符碎片与菩提木锻造的神符器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_anger_stone",1,0.3f,false, Ing(("frag_shenfu",1),("wood_36",1),("bone_30",1)));
            c += Create("forging_baihu","白虎器图","以白虎骨与阳精石锻造的白虎器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_gold_armor",1,0.35f,false, Ing(("bone_14",1),("ore_yangjing",1),("ore_xuantie",2)));
            c += Create("forging_qinglong","青龙器图","以青龙骨与青龙木锻造的青龙器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_snake_ring",1,0.35f,false, Ing(("bone_15",1),("wood_31",1),("ore_lingjing",2)));
            c += Create("forging_zhuque","朱雀器图","以朱雀骨与火木灵锻造的朱雀器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_burning_blood",1,0.35f,false, Ing(("bone_16",1),("wood_19",1),("ore_chiyan",1)));
            c += Create("forging_xuanwu","玄武器图","以玄武骨与冰木灵锻造的玄武器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_war_horn",1,0.35f,false, Ing(("bone_17",1),("wood_19",1),("ore_hanyu",1)));
            c += Create("forging_qilin","麒麟器图","以麒麟骨与麒麟木锻造的麒麟器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_gold_armor",1,0.3f,false, Ing(("bone_11",1),("wood_28",1),("ore_lingyin",1)));

            // ===== 祭道 Ritual (20) — 魂石+残片+天材地宝 → 药水 =====
            c += Create("ritual_huixue","回血玉笺","以散魂石与灵泉水献祭的回血玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_heal",1,0.9f,true, Ing(("soul_san",2),("water_quan",1),("frag_lingwen",1)));
            c += Create("ritual_liliang","力量玉笺","以聚魂玉与黄精献祭的力量玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_strength",1,0.8f,true, Ing(("soul_ju",1),("herb_huangjing",1),("frag_gujuan",1)));
            c += Create("ritual_xuruo","虚弱玉笺","以归元魂石与断肠草献祭的虚弱玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_weak",1,0.7f,false, Ing(("soul_guiyuan",1),("herb_duanhun",1),("frag_mowang",1)));
            c += Create("ritual_nengliang","能量玉笺","以灵灰石与灵晶石献祭的能量玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_energy",1,0.7f,false, Ing(("soul_linghui",1),("ore_lingjing",1),("frag_lingwen",1)));
            c += Create("ritual_gedi","格挡玉笺","以定神玉与玄铁矿献祭的格挡玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_block",1,0.7f,false, Ing(("soul_dingshen",1),("ore_xuantie",1),("frag_zhenfa",1)));
            c += Create("ritual_zhaohun","招魂玉笺","以招魂铃石与冥魄花献祭的招魂玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_heal",2,0.5f,false, Ing(("soul_zhaohun",1),("herb_guijiao",1),("frag_jianyi",1)));
            c += Create("ritual_fenghun","封魂玉笺","以封魂印与冥砂献祭的封魂玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_weak",2,0.45f,false, Ing(("soul_feng",1),("ore_mingsha",1),("frag_tianshu",1)));
            c += Create("ritual_duhun","渡魂玉笺","以渡魂石与冥河水献祭的渡魂玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_strength",2,0.4f,false, Ing(("soul_du",1),("water_minghe",1),("frag_danfang",1)));
            c += Create("ritual_lunhui","轮回玉笺","以轮回石与天书残卷献祭的轮回玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_heal",3,0.25f,false, Ing(("soul_lunhui",1),("frag_tianshu",1),("treasure_35",1)));
            c += Create("ritual_wanhun","万魂玉笺","以万魂幡石与冥魄玉献祭的万魂玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_energy",3,0.3f,false, Ing(("soul_wanhun",1),("soul_mingpo",1),("frag_shenfu",1)));
            c += Create("ritual_tianlei","天雷玉笺","以天雷引与雷击木献祭的天雷玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_strength",2,0.35f,false, Ing(("treasure_4",1),("wood_20",1),("frag_jianyi",1)));
            c += Create("ritual_tianhuo","天火玉笺","以天火种与扶桑枝献祭的天火玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_block",2,0.35f,false, Ing(("treasure_2",1),("wood_33",1),("frag_yaoshen",1)));
            c += Create("ritual_riyue","日月玉笺","以日月精与日华精露献祭的日月玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_heal",3,0.3f,false, Ing(("treasure_10",1),("water_rihua",1),("frag_hundun",1)));
            c += Create("ritual_xingyue","星月玉笺","以星月华与月华凝露献祭的星月玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_energy",3,0.3f,false, Ing(("treasure_11",1),("water_yuehua",1),("frag_xingchen",1)));
            c += Create("ritual_qiankun","乾坤玉笺","以乾坤气与乾坤铁献祭的乾坤玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_strength",3,0.25f,false, Ing(("treasure_13",1),("ore_qiankun",1),("frag_hundun",1)));
            c += Create("ritual_yinyang","阴阳玉笺","以阴阳石与太极碎片献祭的阴阳玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_block",3,0.25f,false, Ing(("treasure_14",1),("frag_taichi",1),("soul_qiankun",1)));
            c += Create("ritual_wuxing","五行玉笺","以五行精与五行种子碎片献祭的五行玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_heal",4,0.2f,false, Ing(("treasure_15",1),("frag_huoyin",1),("frag_shuiyin",1),("frag_muyin",1)));
            c += Create("ritual_bagua","八卦玉笺","以八卦玉与古卷残页献祭的八卦玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_energy",4,0.2f,false, Ing(("treasure_16",1),("frag_gujuan",1),("frag_zhenfa",1)));
            c += Create("ritual_tiandao","天道玉笺","以天道韵与天书金页献祭的天道玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_strength",4,0.15f,false, Ing(("treasure_38",1),("frag_tianshu",1),("soul_taixu",1)));
            c += Create("ritual_didao","地道玉笺","以地道韵与地脉残图献祭的地道玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_block",4,0.15f,false, Ing(("treasure_39",1),("frag_ditu",1),("soul_buxian",1)));

            // ===== 丹道补充40个 (总60) =====
            // 凡品丹方 (成功率85-95%)
            c += Create("alchemy_zhishe","止血丹方","以凤尾蕨与紫苏叶炼制的止血丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"5_heal_basic",1,0.9f,true, Ing(("herb_fern",2),("herb_zisu",1),("water_quan",1)));
            c += Create("alchemy_qingxin","清心丹方","以茯苓与玉露炼制的清心丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"4_draw_basic",1,0.85f,true, Ing(("herb_fuling",2),("water_lu",1)));
            c += Create("alchemy_qixue","气血丹方","以黄精与冬虫夏草炼制的气血丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"5_heal_basic",2,0.85f,false, Ing(("herb_huangjing",1),("herb_chongcao",1),("water_quan",1)));
            c += Create("alchemy_jiedu","解毒丹方","以凤尾蕨与灵泉水炼制的解毒丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"card_frail",1,0.9f,true, Ing(("herb_fern",3),("water_quan",1)));
            c += Create("alchemy_timo","提魔丹方","以麝香草与玉露炼制的提魔丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"card_weak",1,0.85f,false, Ing(("herb_shexiang",2),("water_lu",1)));
            c += Create("alchemy_baifu","白附丹方","以白附子与灵泉水炼制的白附丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"card_vulnerable",1,0.85f,false, Ing(("herb_baifu",2),("water_quan",1)));
            c += Create("alchemy_yexi","夜息丹方","以夜息婆与冥露炼制的夜息丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"body_su_breath",1,0.8f,false, Ing(("herb_yezipo",2),("water_minglu",1)));
            c += Create("alchemy_qingdeng","青灯丹方","以青灯草与冥露炼制的青灯丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"card_weak",1,0.8f,false, Ing(("herb_qingdeng",2),("water_minglu",1)));
            c += Create("alchemy_duwei","杜衡丹方","以杜衡与毒泉水炼制的杜衡丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"spr_db_poison",1,0.75f,false, Ing(("herb_duwei",2),("water_duquan",1)));
            c += Create("alchemy_duanhun","断肠丹方","以断肠草与毒泉水炼制的断肠丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"spr_db_poison",1,0.7f,false, Ing(("herb_duanhun",2),("water_duquan",1)));
            // 灵品丹方 (成功率55-70%)
            c += Create("alchemy_jingxue","精血丹方","以精血藤与蟠桃酒炼制的精血丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"body_su_spring",1,0.55f,false, Ing(("herb_jingxue",1),("water_pantao",1),("herb_lingzhi",1)));
            c += Create("alchemy_shemiao","蛇苗丹方","以蛇苗草与蛊域原液炼制的蛇苗丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"spr_db_bone",1,0.6f,false, Ing(("herb_shemiao",2),("water_guyu",1)));
            c += Create("alchemy_guijiao","鬼交丹方","以鬼交花与冥河水炼制的鬼交丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"spr_db_soul",1,0.55f,false, Ing(("herb_guijiao",2),("water_minghe",1)));
            c += Create("alchemy_moyu","魔芋丹方","以魔芋花与魔气液炼制的魔芋丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"jian_br_void",1,0.5f,false, Ing(("herb_moyu",2),("water_moqi",1)));
            c += Create("alchemy_yangxin","养心丹方","以养心莲与洗心泉炼制的养心丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"body_su_turtle",1,0.6f,false, Ing(("herb_yangxin",1),("water_shenxin",1)));
            c += Create("alchemy_bingxin","冰心丹方","以冰心草与冰晶水炼制的冰心丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"body_th_iron",1,0.6f,false, Ing(("herb_bingxin",2),("water_bingjing",1)));
            c += Create("alchemy_honglian","红莲丹方","以火红莲与炎泉水炼制的红莲丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"jian_br_charge",1,0.55f,false, Ing(("herb_honglian",1),("water_yanquan",1)));
            c += Create("alchemy_mingpo","冥魄丹方","以冥魄花与渡魄泉炼制的冥魄丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"spr_db_chaos",1,0.5f,false, Ing(("herb_mingpo",1),("water_dupo",1)));
            c += Create("alchemy_duobao","多宝丹方","以多宝花与三光神水炼制的多宝丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"body_su_aura",1,0.45f,false, Ing(("herb_duobao",1),("water_sanguang",1)));
            c += Create("alchemy_xuanhuang","玄黄丹方","以玄黄草与九转琼浆炼制的玄黄丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"body_su_gold",1,0.3f,false, Ing(("herb_xuanhuang",1),("water_jiuzhuan",1),("herb_taiyi",1)));
            // 妖丹系丹方
            c += Create("alchemy_ratswarm","鼠群丹方","以鼠群煞丹与黄精炼制的鼠群丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"2_attack_fast",1,0.75f,false, Ing(("core_ratswarm",1),("herb_huangjing",2),("water_quan",1)));
            c += Create("alchemy_batpoison","血蝠丹方","以血蝠毒丹与精血藤炼制的血蝠丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"9_attack_lifeSteal",1,0.6f,false, Ing(("core_batpoison",1),("herb_jingxue",1),("water_pantao",1)));
            c += Create("alchemy_mantisblade","螳螂丹方","以螳螂刃丹与龙涎草炼制的螳螂丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"jian_mh_gale",1,0.55f,false, Ing(("core_mantisblade",1),("herb_longxian",1),("water_longxian",1)));
            c += Create("alchemy_turtleshield","玄龟丹方","以玄龟盾丹与铁木心炼制的玄龟丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"body_th_bounce",1,0.6f,false, Ing(("core_turtleshield",1),("ore_xuantie",1),("water_lu",1)));
            c += Create("alchemy_boarcharge","野猪丹方","以野猪冲丹与雪参炼制的野猪丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"jian_br_focus",1,0.6f,false, Ing(("core_boarcharge",1),("herb_snowginseng",1),("water_lu",1)));
            c += Create("alchemy_skeleton","骷髅丹方","以骷髅骨丹与冥露炼制的骷髅丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"body_th_reflect",1,0.55f,false, Ing(("core_skeletonbone",1),("water_minglu",1),("herb_qingdeng",1)));
            c += Create("alchemy_zombie","僵尸丹方","以僵尸魔丹与冥魄花炼制的僵尸丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"body_su_long",1,0.5f,false, Ing(("core_zombiemo",1),("herb_mingpo",1),("water_minghe",1)));
            c += Create("alchemy_toadpoison","毒蟾丹方","以毒蟾蛊丹与杜衡炼制的毒蟾丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"spr_db_bog",1,0.55f,false, Ing(("core_toadpoison",1),("herb_duwei",1),("water_duquan",1)));
            c += Create("alchemy_eagleeye","鹰眼丹方","以鹰妖眼丹与天香果炼制的鹰眼丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"jian_mh_aura",1,0.5f,false, Ing(("core_eagleeye",1),("herb_tianxiang",1),("water_lu",1)));
            c += Create("alchemy_shimo","石魔丹方","以石魔将丹与玄铁精炼制的石魔丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"body_th_vajra",1,0.45f,false, Ing(("core_shimo",1),("ore_xuantie_jing",1),("water_lu",1)));
            c += Create("alchemy_yewangpo","妖王丹方","以妖王狂丹与龙涎香液炼制的妖王丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"jian_mh_hundred",1,0.4f,false, Ing(("core_yewangpo",1),("water_longxian",1),("herb_longya",1)));
            c += Create("alchemy_yinwang","幽冥丹方","以幽冥王丹与冥河水炼制的幽冥丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"spr_db_vein",1,0.35f,false, Ing(("core_yinwang",1),("water_minghe",1),("soul_mingpo",1)));
            c += Create("alchemy_duhuang","毒皇丹方","以毒皇毒丹与蛊域原液炼制的毒皇丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"spr_db_swarm",1,0.35f,false, Ing(("core_duhuang",1),("water_guyu",1),("herb_shemiao",2)));
            c += Create("alchemy_mozun","魔尊丹方","以魔尊心丹与魔气液炼制的魔尊丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"cap_sword_god",1,0.2f,false, Ing(("core_mozunmo",1),("water_moqi",1),("herb_niepan",1)));
            c += Create("alchemy_wandu","万毒丹方","以万毒老祖丹与九转琼浆炼制的万毒丹方。",RecipeType.Alchemy,RecipeOutputType.Card,"cap_body_saint",1,0.2f,false, Ing(("core_wandu",1),("water_jiuzhuan",1),("herb_taiyi",1)));

            // ===== 器道补充40个 (总60) =====
            // 凡品器图 (成功率80-90%)
            c += Create("forging_baitie","白铁器图","以白铁矿与桃木灵锻造的白铁器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_war_horn",1,0.85f,true, Ing(("ore_baitie",2),("wood_taomuling",1)));
            c += Create("forging_qingtong","青铜器图","以青铜精与松心木锻造的青铜器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_snake_ring",1,0.85f,false, Ing(("ore_qingtong",2),("wood_songxinmu",1)));
            c += Create("forging_bishi","碧石器图","以碧石与柳灵木锻造的碧石器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_anger_stone",1,0.8f,false, Ing(("ore_bishi",2),("wood_liulingmu",1)));
            c += Create("forging_huoshi","火石器图","以火石与枫灵木锻造的火石器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_burning_blood",1,0.8f,false, Ing(("ore_huoshi",2),("wood_fenglingmu",1)));
            c += Create("forging_fengsha","风砂器图","以风砂石与桦灵木锻造的风砂器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_war_horn",1,0.8f,false, Ing(("ore_fengsha",2),("wood_hualingmu",1)));
            c += Create("forging_xishui","吸水器图","以吸水石与杉灵木锻造的吸水器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_gold_armor",1,0.75f,false, Ing(("ore_xishui",2),("wood_shalingmu",1)));
            c += Create("forging_mingsha2","冥砂器图","以冥砂与柏香木锻造的冥砂器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_burning_blood",1,0.7f,false, Ing(("ore_mingsha",2),("wood_baixiangmu",1)));
            c += Create("forging_gutie","古铁器图","以古铁与楠灵木锻造的古铁器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_snake_ring",1,0.75f,false, Ing(("ore_gutie",2),("wood_nanlingmu",1)));
            c += Create("forging_yinsha","银砂器图","以银砂与桂灵木锻造的银砂器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_anger_stone",1,0.75f,false, Ing(("ore_yinsha",2),("wood_guilingmu",1)));
            c += Create("forging_heiyu","墨玉器图","以墨玉与檀灵木锻造的墨玉器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_gold_armor",1,0.7f,false, Ing(("ore_heiyu",2),("wood_tanlingmu",1)));
            // 灵品器图 (成功率50-65%)
            c += Create("forging_xuantie_jing","玄铁精器图","以玄铁精与铁木心锻造的玄铁精器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_war_horn",1,0.6f,false, Ing(("ore_xuantie_jing",1),("wood_tiemuxin",1),("bone_2",1)));
            c += Create("forging_lingyin","灵银器图","以灵银与金丝楠锻造的灵银器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_snake_ring",1,0.55f,false, Ing(("ore_lingyin",1),("wood_jinsinan",1),("bone_3",1)));
            c += Create("forging_guiyuan","归元器图","以归元石与梅灵木锻造的归元器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_gold_armor",1,0.55f,false, Ing(("ore_guiyuan",1),("wood_meilingmu",1)));
            c += Create("forging_pojun","破军器图","以破军石与铁木心锻造的破军器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_anger_stone",1,0.5f,false, Ing(("ore_pojun",1),("wood_tiemuxin",1),("bone_0",1)));
            c += Create("forging_bamo","拔魔器图","以拔魔石与檀灵木锻造的拔魔器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_burning_blood",1,0.5f,false, Ing(("ore_bamo",1),("wood_tanlingmu",1)));
            c += Create("forging_feixing","飞星器图","以飞星铁与梧桐灵锻造的飞星器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_war_horn",1,0.55f,false, Ing(("ore_feixing",1),("wood_wutongling",1)));
            c += Create("forging_dingshen","定神器图","以定神玉与菩提木锻造的定神器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_snake_ring",1,0.5f,false, Ing(("ore_dingshen",1),("wood_puttimu",1)));
            c += Create("forging_yangjing","阳精器图","以阳精石与白虎骨锻造的阳精器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_gold_armor",1,0.5f,false, Ing(("ore_yangjing",1),("bone_14",1)));
            c += Create("forging_yinjing","阴精器图","以阴精石与玄武骨锻造的阴精器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_snake_ring",1,0.5f,false, Ing(("ore_yinjing",1),("bone_17",1)));
            c += Create("forging_zhenhai","镇海器图","以镇海石与玄武甲锻造的镇海器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_war_horn",1,0.45f,false, Ing(("ore_zhenhai",1),("bone_19",1)));
            // 玄品/仙品器图
            c += Create("forging_qiankun2","乾坤器图","以乾坤铁与乾坤魂玉锻造的乾坤器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_burning_blood",1,0.3f,false, Ing(("ore_qiankun",1),("soul_qiankun",1)));
            c += Create("forging_shenhua","神化器图","以神化石与神魂玉锻造的神化器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_anger_stone",1,0.3f,false, Ing(("ore_shenhua",1),("soul_shenhun",1)));
            c += Create("forging_xianjing2","仙晶器图","以仙晶与仙桃心锻造的仙晶器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_gold_armor",1,0.25f,false, Ing(("ore_xianjing",1),("wood_xiantaoxin",1)));
            c += Create("forging_hunyuan","混元器图","以混元石与混元碎片锻造的混元器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_war_horn",1,0.2f,false, Ing(("ore_hunyuan",1),("frag_hundun",1)));
            // 灵兽骨系器图
            c += Create("forging_langsha2","贪狼器图","以贪狼骨与玄铁矿锻造的贪狼器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_war_horn",1,0.65f,false, Ing(("bone_20",1),("ore_xuantie",2)));
            c += Create("forging_snakepoison2","碧磷器图","以碧磷骨与冰魄银锻造的碧磷器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_snake_ring",1,0.55f,false, Ing(("bone_21",1),("ore_bingpo",1)));
            c += Create("forging_bearforce2","熊妖器图","以熊妖骨与赤焰铜锻造的熊妖器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_gold_armor",1,0.6f,false, Ing(("bone_22",1),("ore_chiyan",1)));
            c += Create("forging_eagleeye2","鹰妖器图","以鹰妖骨与天星铁锻造的鹰妖器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_anger_stone",1,0.55f,false, Ing(("bone_23",1),("ore_tianxingtie",1)));
            c += Create("forging_turtleshield2","玄龟器图","以玄龟甲与寒玉石锻造的玄龟器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_gold_armor",1,0.6f,false, Ing(("bone_24",1),("ore_hanyu",1)));
            c += Create("forging_tiger","虎妖器图","以虎妖骨与紫金砂锻造的虎妖器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_war_horn",1,0.55f,false, Ing(("bone_25",1),("ore_zijin",1)));
            c += Create("forging_jiaolong","蛟龙器图","以蛟龙骨与龙血木锻造的蛟龙器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_snake_ring",1,0.4f,false, Ing(("bone_26",1),("wood_26",1),("ore_zijin",1)));
            c += Create("forging_tianfeng","天凤器图","以天凤骨与凤栖木锻造的天凤器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_burning_blood",1,0.35f,false, Ing(("bone_27",1),("wood_27",1)));
            c += Create("forging_zhenlong","真龙器图","以真龙骨与青龙木锻造的真龙器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_war_horn",1,0.25f,false, Ing(("bone_28",1),("wood_31",1)));
            c += Create("forging_qilin2","麒麟器图","以麒麟骨与麒麟木锻造的麒麟器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_gold_armor",1,0.3f,false, Ing(("bone_29",1),("wood_28",1)));
            c += Create("forging_shanwang2","山魈器图","以山魈骨与松心木锻造的山魈器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_anger_stone",1,0.4f,false, Ing(("bone_31",1),("wood_songxinmu",1)));
            c += Create("forging_heifeng2","黑风器图","以黑风骨与玄铁矿锻造的黑风器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_burning_blood",1,0.35f,false, Ing(("bone_30",1),("ore_xuantie",2)));
            c += Create("forging_tianmo2","天魔器图","以天魔骨与虚空晶锻造的天魔器图。",RecipeType.Forging,RecipeOutputType.Relic,"relic_war_horn",1,0.2f,false, Ing(("bone_38",1),("ore_xukong",1)));

            // ===== 祭道补充40个 (总60) =====
            // 凡品玉笺 (成功率80-90%)
            c += Create("ritual_guiyuan","归元玉笺","以归元魂石与灵泉水献祭的归元玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_heal",1,0.85f,true, Ing(("soul_guiyuan",1),("water_quan",1),("frag_gujuan",1)));
            c += Create("ritual_pohun","破魂玉笺","以破魂石与断肠草献祭的破魂玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_weak",1,0.8f,false, Ing(("soul_pohun",1),("herb_duanhun",1),("frag_mowang",1)));
            c += Create("ritual_yingshi","影魂玉笺","以影魂石与暗晶魂石献祭的影魂玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_energy",1,0.8f,false, Ing(("soul_yingshi",1),("soul_anjing",1)));
            c += Create("ritual_qingguang","青光玉笺","以青光魂玉与冥露献祭的青光玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_block",1,0.8f,false, Ing(("soul_qingguang",1),("water_minglu",1)));
            c += Create("ritual_anjing","暗晶玉笺","以暗晶魂石与鬼脂玉献祭的暗晶玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_weak",1,0.75f,false, Ing(("soul_anjing",1),("soul_guizhi",1)));
            c += Create("ritual_yueshi","月魂玉笺","以月魂石与月华凝露献祭的月魂玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_heal",2,0.7f,false, Ing(("soul_yueshi",1),("water_yuehua",1)));
            c += Create("ritual_rishi","日魂玉笺","以日魂石与日华精露献祭的日魂玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_strength",2,0.7f,false, Ing(("soul_rishi",1),("water_rihua",1)));
            c += Create("ritual_xingshi","星魂玉笺","以星魂砂与星光碎片献祭的星魂玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_energy",2,0.65f,false, Ing(("soul_xingshi",1),("frag_xingchen",1)));
            c += Create("ritual_yinshen","隐神玉笺","以隐神石与灵纹残片献祭的隐神玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_block",2,0.65f,false, Ing(("soul_yinshen",1),("frag_lingwen",1)));
            c += Create("ritual_wangxiang","忘乡玉笺","以忘乡石与古卷残页献祭的忘乡玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_heal",2,0.6f,false, Ing(("soul_wangxiang",1),("frag_gujuan",1)));
            // 灵品玉笺 (成功率45-60%)
            c += Create("ritual_mingpo","冥魄玉笺","以冥魄玉与冥魄花献祭的冥魄玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_strength",2,0.5f,false, Ing(("soul_mingpo",1),("herb_mingpo",1),("water_minghe",1)));
            c += Create("ritual_zhaohun2","招魂玉笺","以招魂铃石与鬼交花献祭的招魂玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_energy",2,0.5f,false, Ing(("soul_zhaohun",1),("herb_guijiao",1)));
            c += Create("ritual_suohun","锁魂玉笺","以锁魂玉与古符残片献祭的锁魂玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_block",2,0.5f,false, Ing(("soul_suohun",1),("frag_gufu",1)));
            c += Create("ritual_linghui","灵灰玉笺","以灵灰石与道印碎片献祭的灵灰玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_heal",2,0.5f,false, Ing(("soul_linghui",1),("frag_daoyin",1)));
            c += Create("ritual_qianhun","千魂玉笺","以千魂石与佛印碎片献祭的千魂玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_weak",3,0.45f,false, Ing(("soul_qianhun",1),("frag_foyin",1)));
            c += Create("ritual_longhun","龙魂玉笺","以龙魂石与龙涎香液献祭的龙魂玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_strength",3,0.4f,false, Ing(("soul_longhun",1),("water_longxian",1)));
            c += Create("ritual_mohun","魔魂玉笺","以魔魂晶与魔道残章献祭的魔魂玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_block",3,0.4f,false, Ing(("soul_mohun",1),("frag_mowang",1)));
            c += Create("ritual_yaohun","妖魂玉笺","以妖魂玉与妖印碎片献祭的妖魂玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_heal",3,0.4f,false, Ing(("soul_yaohun",1),("frag_yaoyin",1)));
            c += Create("ritual_guizhi2","鬼脂玉笺","以鬼脂玉与鬼印碎片献祭的鬼脂玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_energy",3,0.4f,false, Ing(("soul_guizhi",1),("frag_guiyin",1)));
            c += Create("ritual_dinghai","定海玉笺","以定海魂珠与阵法残石献祭的定海玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_block",3,0.35f,false, Ing(("soul_dinghai",1),("frag_zhenfa",1)));
            // 玄品/仙品玉笺 (成功率15-30%)
            c += Create("ritual_pojie","破界玉笺","以破界魂晶与界石碎片献祭的破界玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_heal",4,0.3f,false, Ing(("soul_pojie",1),("frag_jieshi",1)));
            c += Create("ritual_shenhun","神魂玉笺","以神魂玉与神意碎片献祭的神魂玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_strength",4,0.25f,false, Ing(("soul_shenhun",1),("frag_shenyi",1)));
            c += Create("ritual_qiankun2","乾坤玉笺","以乾坤魂玉与太极碎片献祭的乾坤玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_block",4,0.25f,false, Ing(("soul_qiankun",1),("frag_taichi",1)));
            c += Create("ritual_fenghun2","凤魂玉笺","以凤魂玉与天书残卷献祭的凤魂玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_heal",5,0.2f,false, Ing(("soul_fenghun",1),("frag_tianshu",1)));
            c += Create("ritual_hundun2","混沌玉笺","以混沌魂晶与混沌碎片献祭的混沌玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_strength",5,0.15f,false, Ing(("soul_hundun",1),("frag_hundun",1)));
            c += Create("ritual_zhuansheng","转生玉笺","以转生石与命书残页献祭的转生玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_block",5,0.15f,false, Ing(("soul_zhuansheng",1),("frag_mingshu",1)));
            c += Create("ritual_buxian2","补天玉笺","以补天石与道书残卷献祭的补天玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_heal",5,0.15f,false, Ing(("soul_buxian",1),("frag_daoshu",1)));
            c += Create("ritual_qixing","七星玉笺","以七星魂玉与天书金页献祭的七星玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_energy",5,0.12f,false, Ing(("soul_qixing",1),("frag_tianshu",1)));
            // 天材地宝系玉笺
            c += Create("ritual_lingmai","灵脉玉笺","以灵脉晶与地脉精华献祭的灵脉玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_heal",3,0.35f,false, Ing(("treasure_0",1),("treasure_1",1)));
            c += Create("ritual_tianhuo2","天火玉笺","以天火种与地心焰献祭的天火玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_strength",3,0.3f,false, Ing(("treasure_2",1),("treasure_3",1)));
            c += Create("ritual_tianlei2","天雷玉笺","以天雷引与地煞气献祭的天雷玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_energy",3,0.3f,false, Ing(("treasure_4",1),("treasure_5",1)));
            c += Create("ritual_tianfeng","天风玉笺","以天风髓与地灵涎献祭的天风玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_block",3,0.3f,false, Ing(("treasure_6",1),("treasure_7",1)));
            c += Create("ritual_xingxing","星泪玉笺","以天星泪与地藏珠献祭的星泪玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_heal",4,0.25f,false, Ing(("treasure_8",1),("treasure_9",1)));
            c += Create("ritual_riyue2","日月玉笺","以日月精与星月华献祭的日月玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_strength",4,0.25f,false, Ing(("treasure_10",1),("treasure_11",1)));
            c += Create("ritual_tiandi","天地玉笺","以天地息与乾坤气献祭的天地玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_block",4,0.2f,false, Ing(("treasure_12",1),("treasure_13",1)));
            c += Create("ritual_yinyang2","阴阳玉笺","以阴阳石与五行精献祭的阴阳玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_heal",5,0.2f,false, Ing(("treasure_14",1),("treasure_15",1)));
            c += Create("ritual_bagua2","八卦玉笺","以八卦玉与九宫珠献祭的八卦玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_energy",5,0.18f,false, Ing(("treasure_16",1),("treasure_17",1)));
            c += Create("ritual_tianwang","天罡玉笺","以天罡玉与地阙金献祭的天罡玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_strength",5,0.18f,false, Ing(("treasure_18",1),("treasure_19",1)));
            c += Create("ritual_tianyi","天乙玉笺","以天乙贵与天河水献祭的天乙玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_block",5,0.15f,false, Ing(("treasure_20",1),("treasure_22",1)));
            c += Create("ritual_tianming","天命玉笺","以天命石与地缘玉献祭的天命玉笺。",RecipeType.Ritual,RecipeOutputType.Potion,"potion_heal",6,0.12f,false, Ing(("treasure_34",1),("treasure_35",1)));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"配方创建完成: {c}个 (丹道60+器道60+祭道60=180)");
        }

        static List<RecipeIngredient> Ing(params (string, int)[] items)
        {
            var list = new List<RecipeIngredient>();
            foreach (var (id, cnt) in items)
                list.Add(new RecipeIngredient { materialId = id, count = cnt });
            return list;
        }

        static int Create(string id, string name, string desc, RecipeType type, RecipeOutputType outType,
            string outId, int outCount, float success, bool unlock, List<RecipeIngredient> ings)
        {
            string path = $"Assets/NueGames/NueDeck/Data/Recipes/{id}.asset";
            if (AssetDatabase.LoadAssetAtPath<RecipeData>(path) != null) AssetDatabase.DeleteAsset(path);
            var r = ScriptableObject.CreateInstance<RecipeData>();
            r.recipeId = id; r.name = name; r.description = desc;
            r.recipeType = type; r.outputType = outType;
            r.outputItemId = outId; r.outputCount = outCount;
            r.successRate = success; r.unlockByDefault = unlock;
            r.ingredients = ings;
            AssetDatabase.CreateAsset(r, path);
            return 1;
        }
    }
}
