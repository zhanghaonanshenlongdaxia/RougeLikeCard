using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace CardGame.Editor
{
    public static class StoryTreeBatchGenerator
    {
        [MenuItem("Tools/Generate Story Tree")]
        public static void GenerateAll()
        {
            string dir = "Assets/NueGames/NueDeck/Data/StoryTree";
            if (!AssetDatabase.IsValidFolder(dir))
                AssetDatabase.CreateFolder("Assets/NueGames/NueDeck/Data", "StoryTree");

            string path = $"{dir}/StoryTreeConfig.asset";
            if (AssetDatabase.LoadAssetAtPath<StoryTreeConfig>(path) != null)
                AssetDatabase.DeleteAsset(path);

            var config = ScriptableObject.CreateInstance<StoryTreeConfig>();
            var nodes = new List<StoryNodeData>();

            // === 第0层：根节点（自动解锁） ===
            nodes.Add(Node("root_start", "\u521D\u5165\u4ED9\u9014", "\u4F60\u8E0F\u4E0A\u4E86\u4FEE\u4ED9\u4E4B\u8DEF\uFF0C\u638C\u63E1\u4E86\u57FA\u7840\u529F\u6CD5\u3002",
                StoryNodeType.Root, StoryRewardType.None, 0, new Vector2(0, 0), "\u59CB", "#FFD700"));

            nodes.Add(Node("root_alchemy", "\u4E39\u9053\u5165\u95E8", "\u5B66\u4F1A\u57FA\u7840\u70BC\u4E39\u4E4B\u6CD5\uFF0C\u89E3\u95013\u4E2A\u51E1\u54C1\u4E39\u65B9\u3002",
                StoryNodeType.Story, StoryRewardType.RecipeUnlock, 0, new Vector2(-300, -150), "\u4E39", "#8BC34A",
                prereq: "root_start",
                rewards: new List<string>{"alchemy_huixue", "alchemy_gedi", "alchemy_zhishe"}));

            nodes.Add(Node("root_forge", "\u5668\u9053\u5165\u95E8", "\u5B66\u4F1A\u57FA\u7840\u953B\u9020\u4E4B\u6CD5\uFF0C\u89E3\u95012\u4E2A\u51E1\u54C1\u5668\u56FE\u3002",
                StoryNodeType.Story, StoryRewardType.RecipeUnlock, 0, new Vector2(0, -150), "\u5668", "#FF9800",
                prereq: "root_start",
                rewards: new List<string>{"forging_baitie", "forging_qingtong"}));

            nodes.Add(Node("root_ritual", "\u796D\u9053\u5165\u95E8", "\u5B66\u4F1A\u57FA\u7840\u796D\u7940\u4E4B\u6CD5\uFF0C\u89E3\u95011\u4E2A\u51E1\u54C1\u7389\u7B3A\u3002",
                StoryNodeType.Story, StoryRewardType.RecipeUnlock, 0, new Vector2(300, -150), "\u796D", "#9C27B0",
                prereq: "root_start",
                rewards: new List<string>{"ritual_huixue"}));

            // === 第1层：三道传承（3次冒险后） ===
            nodes.Add(Node("ch1_sword", "\u5251\u9053\u4F20\u627F", "\u83B7\u5F97\u5251\u9053\u529F\u6CD5\u4F20\u627F\uFF0C\u89E3\u9501\u5251\u9053\u5361\u724C10\u5F20\u3002",
                StoryNodeType.Story, StoryRewardType.CardUnlock, 1, new Vector2(-400, -350), "\u5251", "#42A5F5",
                prereq: "root_start",
                rewards: new List<string>{"sw_mh_twin", "sw_mh_triple", "sw_mh_whirl", "sw_mh_flurry", "sw_br_gather", "sw_br_release", "sw_sword_qi", "sw_sword_block", "sw_sword_rush", "sw_sword_meditate"}));

            nodes.Add(Node("ch1_body", "\u4F53\u9053\u4F20\u627F", "\u83B7\u5F97\u4F53\u9053\u529F\u6CD5\u4F20\u627F\uFF0C\u89E3\u9501\u4F53\u9053\u5361\u724C10\u5F20\u3002",
                StoryNodeType.Story, StoryRewardType.CardUnlock, 1, new Vector2(0, -350), "\u4F53", "#66BB6A",
                prereq: "root_start",
                rewards: new List<string>{"bd_th_spike", "bd_th_counter", "bd_su_regen", "bd_su_shield2", "bd_su_meditate", "bd_su_breathe", "bd_block_basic", "bd_heal_basic", "bd_str_body", "bd_attack_body"}));

            nodes.Add(Node("ch1_spirit", "\u7075\u9053\u4F20\u627F", "\u83B7\u5F97\u7075\u9053\u529F\u6CD5\u4F20\u627F\uFF0C\u89E3\u9501\u7075\u9053\u5361\u724C10\u5F20\u3002",
                StoryNodeType.Story, StoryRewardType.CardUnlock, 1, new Vector2(400, -350), "\u7075", "#AB47BC",
                prereq: "root_start",
                rewards: new List<string>{"sp_db_curse", "sp_db_shatter", "sp_db_expose", "sp_db_weakpoint", "sp_mn_bolt2", "sp_mn_charge", "sp_mn_free", "sp_heal_basic", "sp_block_basic", "sp_draw_basic"}));

            // === 第2层：药水+法宝（6次冒险后） ===
            nodes.Add(Node("ch2_potion", "\u4E39\u836F\u7CBE\u8FDB", "\u70BC\u5236\u6280\u6CD5\u7CBE\u8FDB\uFF0C\u89E3\u9501\u7075\u54C1\u836F\u6C3410\u4E2A\u3002",
                StoryNodeType.Story, StoryRewardType.PotionUnlock, 2, new Vector2(-200, -550), "\u836F", "#26A69A",
                prereq: "root_alchemy",
                rewards: new List<string>{"potion_heal_l", "potion_block_l", "potion_str_l", "potion_dex_l", "potion_energy_l", "potion_draw_l", "potion_str_dex", "potion_block_str", "potion_maxhp_s", "potion_cleanse"}));

            nodes.Add(Node("ch2_relic", "\u6CD5\u5B9D\u5165\u95E8", "\u83B7\u5F97\u7075\u54C1\u6CD5\u5B9D\uFF0C\u89E3\u950110\u4E2A\u3002",
                StoryNodeType.Story, StoryRewardType.RelicUnlock, 2, new Vector2(200, -550), "\u5B9D", "#EF5350",
                prereq: "root_forge",
                rewards: new List<string>{"relic_attack_str2", "relic_turn_heal3", "relic_turn_block5", "relic_draw2", "relic_energy2", "relic_enemy_death_gold10", "relic_player_dmg_str2", "relic_card_block2", "relic_enemy_dmg_heal2", "relic_turn_end_draw2"}));

            // === 第3层：玄品（9次冒险后） ===
            nodes.Add(Node("ch3_rare_card", "\u7384\u54C1\u529F\u6CD5", "\u7384\u54C1\u529F\u6CD5\u4F20\u627F\uFF0C\u89E3\u9501\u7384\u54C1\u5361\u724C15\u5F20\u3002",
                StoryNodeType.Story, StoryRewardType.CardUnlock, 3, new Vector2(-200, -750), "\u7384", "#7E57C2",
                prereq: "ch1_sword",
                rewards: new List<string>{"sw_mh_storm", "sw_mh_eclipse", "sw_br_quake", "sw_br_annihilate", "sw_br_execute", "bd_th_fortress", "bd_th_needle", "bd_su_recover", "bd_su_stoneskin", "bd_su_immortal", "sp_db_hex", "sp_db_devour", "sp_mn_overload", "sp_mn_storm2", "sp_mn_annihilate"}));

            nodes.Add(Node("ch3_rare_potion", "\u7384\u54C1\u4E39\u836F", "\u7384\u54C1\u4E39\u836F\u7CBE\u5236\uFF0C\u89E3\u95015\u4E2A\u3002",
                StoryNodeType.Story, StoryRewardType.PotionUnlock, 3, new Vector2(200, -750), "\u7384", "#7E57C2",
                prereq: "ch2_potion",
                rewards: new List<string>{"potion_heal_full_r", "potion_block_huge", "potion_str_huge", "potion_dex_huge", "potion_omni"}));

            // === 第4层：传奇（12次冒险后） ===
            nodes.Add(Node("ch4_legend", "\u4ED9\u54C1\u4F20\u627F", "\u4ED9\u54C1\u529F\u6CD5\u964D\u4E16\uFF0C\u89E3\u9501\u4F20\u5947\u5361\u724C5\u5F20\u3002",
                StoryNodeType.Story, StoryRewardType.CardUnlock, 4, new Vector2(0, -950), "\u4ED9", "#FFD700",
                prereq: "ch3_rare_card",
                rewards: new List<string>{"leg_sword_unity", "leg_body_immortal", "leg_spirit_chaos", "leg_gen_omni", "leg_gen_judgment"}));

            nodes.Add(Node("ch4_boss_relic", "\u9B54\u5B9D\u964D\u4E16", "\u51FB\u8D25\u5F3A\u654C\u540E\u83B7\u5F97\u7684\u6781\u54C1\u6CD5\u5B9D\uFF0C\u89E3\u95015\u4E2A\u3002",
                StoryNodeType.Story, StoryRewardType.RelicUnlock, 4, new Vector2(300, -950), "\u9B54", "#D32F2F",
                prereq: "ch2_relic",
                rewards: new List<string>{"relic_attack_str3", "relic_turn_heal5", "relic_turn_block15", "relic_draw3", "relic_combat_str5"}));

            // === 第5层：终极（15次冒险后） ===
            nodes.Add(Node("ch5_final", "\u5927\u9053\u5706\u6EE1", "\u4FEE\u4ED9\u4E4B\u8DEF\u5706\u6EE1\uFF0C\u83B7\u5F97\u6700\u7EC8\u4F20\u627F\u548C\u5927\u91CF\u7075\u77F3\u3002",
                StoryNodeType.Story, StoryRewardType.MultiReward, 5, new Vector2(0, -1150), "\u9053", "#FFFFFF",
                prereq: "ch4_legend",
                rewards: new List<string>{"card:leg_sword_god2", "card:leg_body_saint2", "card:leg_spirit_void", "card:leg_gen_rebirth", "card:leg_gen_eternal"},
                gold: 500));

            config.nodes = nodes;
            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Story Tree\u751F\u6210\u5B8C\u6210: {nodes.Count}\u4E2A\u8282\u70B9 at {path}");
        }

        static StoryNodeData Node(string id, string name, string desc,
            StoryNodeType nodeType, StoryRewardType rewardType, int chapter,
            Vector2 pos, string icon, string color,
            string prereq = null, List<string> rewards = null, int gold = 0)
        {
            var node = new StoryNodeData
            {
                nodeId = id,
                nodeName = name,
                description = desc,
                nodeType = nodeType,
                rewardType = rewardType,
                chapter = chapter,
                position = pos,
                iconText = icon,
                colorHex = color,
                goldReward = gold,
                prerequisites = prereq != null ? new List<string> { prereq } : new List<string>(),
                rewardIds = rewards != null ? new List<string>(rewards) : new List<string>()
            };
            return node;
        }
    }
}
