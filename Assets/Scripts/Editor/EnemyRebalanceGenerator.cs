using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using NueGames.NueDeck.Scripts.Data.Characters;
using NueGames.NueDeck.Scripts.Data.Containers;
using NueGames.NueDeck.Scripts.Enums;

namespace CardGame.Editor
{
    /// <summary>
    /// 按 Slay the Spire 平衡规则重写所有敌人数据。
    /// 规则：
    /// - HP 按品阶×区域阶梯增长（Normal 10-65 / Elite 80-170 / Boss 180-340）
    /// - 普通怪伤害 4-14，精英 8-22，Boss 10-35（2阶段更高）
    /// - debuff 用小整数（1-3），不超过3
    /// - 每个敌人有独特技能组合（根据名字关键词匹配模板）
    /// - 精英/Boss 有技能轮换模式（followAbilityPattern=true）
    /// </summary>
    public static class EnemyRebalanceGenerator
    {
        // HP 范围表 [regionIndex, 0=min, 1=max]
        static readonly int[,] NormalHP = {
            { 10, 28 },  // Region 0 练气
            { 18, 38 },  // Region 1 筑基
            { 25, 48 },  // Region 2 金丹
            { 32, 58 },  // Region 3 元婴
        };
        static readonly int[,] EliteHP = {
            { 75, 100 },
            { 90, 120 },
            { 105, 140 },
            { 120, 160 },
        };
        static readonly int[,] BossHP = {
            { 180, 210 },
            { 210, 250 },
            { 240, 290 },
            { 270, 330 },
        };

        // 伤害基数（按区域递增）
        static readonly int[] NormalDmgBase = { 4, 5, 6, 7 };
        static readonly int[] EliteDmgBase = { 8, 9, 11, 13 };
        static readonly int[] BossP1DmgBase = { 10, 12, 14, 16 };
        static readonly int[] BossP2DmgBase = { 14, 16, 19, 22 };

        [MenuItem("Tools/Rebalance Enemies (StS Rules)")]
        public static void RebalanceAll()
        {
            var guids = AssetDatabase.FindAssets("t:EnemyCharacterData", new[] { "Assets/NueGames/NueDeck/Data/Enemies" });
            int processed = 0, errors = 0;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var enemy = AssetDatabase.LoadAssetAtPath<EnemyCharacterData>(path);
                if (enemy == null) { errors++; continue; }

                try
                {
                    RebalanceEnemy(enemy, path);
                    EditorUtility.SetDirty(enemy);
                    processed++;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[Rebalance] Failed on {enemy.name}: {e.Message}");
                    errors++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Rebalance] Done. {processed} enemies rebalanced, {errors} errors.");
        }

        static void RebalanceEnemy(EnemyCharacterData enemy, string assetPath)
        {
            int region = enemy.RegionId;
            if (region < 0 || region > 3) region = 0;
            var tier = enemy.EnemyTier;
            var nameLower = enemy.name.ToLower();

            var so = new SerializedObject(enemy);

            // 1. 设置 HP
            var hpProp = so.FindProperty("maxHealth");
            int newHP = tier switch
            {
                EnemyTier.Normal => Random.Range(NormalHP[region, 0], NormalHP[region, 1] + 1),
                EnemyTier.Elite => Random.Range(EliteHP[region, 0], EliteHP[region, 1] + 1),
                EnemyTier.Boss => Random.Range(BossHP[region, 0], BossHP[region, 1] + 1),
                _ => NormalHP[region, 0]
            };
            hpProp.intValue = newHP;

            // 2. 生成技能
            if (tier == EnemyTier.Boss)
            {
                RebalanceBoss(so, region, nameLower);
            }
            else if (tier == EnemyTier.Elite)
            {
                RebalanceElite(so, region, nameLower);
            }
            else
            {
                RebalanceNormal(so, region, nameLower);
            }

            // 3. 确保 followAbilityPattern = true（让敌人有可预测的技能轮换）
            so.FindProperty("followAbilityPattern").boolValue = true;

            so.ApplyModifiedProperties();
        }

        #region Normal Enemy Templates

        static void RebalanceNormal(SerializedObject so, int region, string nameLower)
        {
            var abListProp = so.FindProperty("enemyAbilityList");
            // 清除旧阶段
            var phaseProp = so.FindProperty("phaseList");
            if (phaseProp != null) phaseProp.ClearArray();

            abListProp.ClearArray();
            int dmg = NormalDmgBase[region];
            int dmgHigh = dmg + 4;
            int debuffVal = region < 2 ? 1 : 2;

            // 根据名字关键词选择模板
            var template = GetNormalTemplate(nameLower);

            for (int i = 0; i < template.Count; i++)
            {
                abListProp.InsertArrayElementAtIndex(i);
                var ab = abListProp.GetArrayElementAtIndex(i);
                var entry = template[i];
                ab.FindPropertyRelative("name").stringValue = entry.name;
                ab.FindPropertyRelative("hideActionValue").boolValue = entry.hideValue;
                ab.FindPropertyRelative("intention").objectReferenceValue = GetIntention(entry.intention);

                var actions = ab.FindPropertyRelative("actionList");
                actions.ClearArray();
                for (int j = 0; j < entry.actions.Count; j++)
                {
                    actions.InsertArrayElementAtIndex(j);
                    var act = actions.GetArrayElementAtIndex(j);
                    var (type, min, max) = entry.actions[j];
                    act.FindPropertyRelative("actionType").enumValueIndex = (int)type;
                    act.FindPropertyRelative("minActionValue").intValue = min;
                    act.FindPropertyRelative("maxActionValue").intValue = max;
                }
            }
        }

        class AbilityEntry
        {
            public string name;
            public EnemyIntentionType intention = EnemyIntentionType.Attack;
            public bool hideValue;
            public List<(EnemyActionType type, int min, int max)> actions = new List<(EnemyActionType, int, int)>();
        }

        static Dictionary<EnemyIntentionType, EnemyIntentionData> _intentions;

        static EnemyIntentionData GetIntention(EnemyIntentionType type)
        {
            if (_intentions == null)
            {
                _intentions = new Dictionary<EnemyIntentionType, EnemyIntentionData>();
                var guids = AssetDatabase.FindAssets("t:EnemyIntentionData", new[] { "Assets/NueGames/NueDeck/Data" });
                foreach (var g in guids)
                {
                    var p = AssetDatabase.GUIDToAssetPath(g);
                    var d = AssetDatabase.LoadAssetAtPath<EnemyIntentionData>(p);
                    if (d != null) _intentions[d.EnemyIntentionType] = d;
                }
            }
            return _intentions.TryGetValue(type, out var val) ? val : null;
        }

        static List<AbilityEntry> GetNormalTemplate(string nameLower)
        {
            // 根据敌人名关键词返回独特技能组合
            if (nameLower.Contains("archer") || nameLower.Contains("bow") || nameLower.Contains("arrow"))
            {
                return new List<AbilityEntry> {
                    new AbilityEntry { name = "穿甲箭", intention = EnemyIntentionType.Attack, actions = { (EnemyActionType.Attack, 5, 8) } },
                    new AbilityEntry { name = "弱点射击", intention = EnemyIntentionType.Debuff, actions = { (EnemyActionType.ApplyVulnerable, 1, 1) } },
                    new AbilityEntry { name = "连射", intention = EnemyIntentionType.Attack, actions = { (EnemyActionType.Attack, 3, 3), (EnemyActionType.Attack, 3, 3) } },
                };
            }
            if (nameLower.Contains("bandit") || nameLower.Contains("spear") || nameLower.Contains("gujiang"))
            {
                return new List<AbilityEntry> {
                    new AbilityEntry { name = "横劈", intention = EnemyIntentionType.Attack, actions = { (EnemyActionType.Attack, 6, 9) } },
                    new AbilityEntry { name = "双连斩", intention = EnemyIntentionType.Attack, actions = { (EnemyActionType.Attack, 3, 3), (EnemyActionType.Attack, 3, 3) } },
                    new AbilityEntry { name = "蓄力格挡", intention = EnemyIntentionType.Defend, actions = { (EnemyActionType.Block, 4, 4) } },
                };
            }
            if (nameLower.Contains("bat") || nameLower.Contains("vampire") || nameLower.Contains("leech"))
            {
                return new List<AbilityEntry> {
                    new AbilityEntry { name = "吸血", intention = EnemyIntentionType.Attack, actions = { (EnemyActionType.Attack, 4, 6), (EnemyActionType.Heal, 2, 2) } },
                    new AbilityEntry { name = "扑击", intention = EnemyIntentionType.Attack, actions = { (EnemyActionType.Attack, 5, 7) } },
                    new AbilityEntry { name = "疫毒", intention = EnemyIntentionType.Debuff, actions = { (EnemyActionType.Poison, 2, 2) } },
                };
            }
            if (nameLower.Contains("ant") || nameLower.Contains("insect") || nameLower.Contains("guchong"))
            {
                return new List<AbilityEntry> {
                    new AbilityEntry { name = "撕咬", intention = EnemyIntentionType.Attack, actions = { (EnemyActionType.Attack, 3, 5) } },
                    new AbilityEntry { name = "虫群冲锋", intention = EnemyIntentionType.Attack, actions = { (EnemyActionType.Attack, 2, 2), (EnemyActionType.Attack, 2, 2) } },
                    new AbilityEntry { name = "虫甲硬化", intention = EnemyIntentionType.Defend, actions = { (EnemyActionType.Block, 3, 3), (EnemyActionType.ApplyFrail, 1, 1) } },
                };
            }
            if (nameLower.Contains("fox") || nameLower.Contains("huli") || nameLower.Contains("foxspirit"))
            {
                return new List<AbilityEntry> {
                    new AbilityEntry { name = "魅惑", intention = EnemyIntentionType.Debuff, actions = { (EnemyActionType.ApplyWeak, 2, 2) } },
                    new AbilityEntry { name = "妖爪", intention = EnemyIntentionType.Attack, actions = { (EnemyActionType.Attack, 5, 7) } },
                    new AbilityEntry { name = "幻影", intention = EnemyIntentionType.Defend, actions = { (EnemyActionType.Block, 3, 3) } },
                };
            }
            if (nameLower.Contains("skeleton") || nameLower.Contains("bone") || nameLower.Contains("corpse") || nameLower.Contains("leichen"))
            {
                return new List<AbilityEntry> {
                    new AbilityEntry { name = "骨爪", intention = EnemyIntentionType.Attack, actions = { (EnemyActionType.Attack, 6, 8) } },
                    new AbilityEntry { name = "骨铠", intention = EnemyIntentionType.Defend, actions = { (EnemyActionType.Block, 5, 5) } },
                    new AbilityEntry { name = "双爪", intention = EnemyIntentionType.Attack, actions = { (EnemyActionType.Attack, 4, 4), (EnemyActionType.Attack, 4, 4) } },
                };
            }
            if (nameLower.Contains("banshee") || nameLower.Contains("ghost") || nameLower.Contains("spirit") || nameLower.Contains("gumu"))
            {
                return new List<AbilityEntry> {
                    new AbilityEntry { name = "怨嚎", intention = EnemyIntentionType.Debuff, actions = { (EnemyActionType.ApplyWeak, 2, 2) } },
                    new AbilityEntry { name = "怨爪", intention = EnemyIntentionType.Attack, actions = { (EnemyActionType.Attack, 5, 7) } },
                    new AbilityEntry { name = "破绽", intention = EnemyIntentionType.Debuff, actions = { (EnemyActionType.ApplyVulnerable, 1, 1) } },
                    new AbilityEntry { name = "虚化", intention = EnemyIntentionType.Defend, actions = { (EnemyActionType.Block, 4, 4) } },
                };
            }
            if (nameLower.Contains("slime") || nameLower.Contains("duhuang") || nameLower.Contains("ooze"))
            {
                return new List<AbilityEntry> {
                    new AbilityEntry { name = "腐蚀液", intention = EnemyIntentionType.Debuff, actions = { (EnemyActionType.Poison, 2, 3) } },
                    new AbilityEntry { name = "撞击", intention = EnemyIntentionType.Attack, actions = { (EnemyActionType.Attack, 4, 6) } },
                    new AbilityEntry { name = "分裂格挡", intention = EnemyIntentionType.Defend, actions = { (EnemyActionType.Block, 3, 3), (EnemyActionType.ApplyFrail, 1, 1) } },
                };
            }
            if (nameLower.Contains("ansha") || nameLower.Contains("assassin") || nameLower.Contains("shadow"))
            {
                return new List<AbilityEntry> {
                    new AbilityEntry { name = "蓄力暗杀", intention = EnemyIntentionType.Attack, actions = { (EnemyActionType.Attack, 10, 14) } },
                    new AbilityEntry { name = "暗刃", intention = EnemyIntentionType.Attack, actions = { (EnemyActionType.Attack, 5, 7) } },
                    new AbilityEntry { name = "致盲", intention = EnemyIntentionType.Debuff, actions = { (EnemyActionType.ApplyWeak, 2, 2) } },
                };
            }
            if (nameLower.Contains("duo") || nameLower.Contains("demon") || nameLower.Contains("mogui"))
            {
                return new List<AbilityEntry> {
                    new AbilityEntry { name = "魔诀", intention = EnemyIntentionType.Attack, actions = { (EnemyActionType.Attack, 7, 10) } },
                    new AbilityEntry { name = "聚魔气", intention = EnemyIntentionType.Debuff, actions = { (EnemyActionType.Heal, 5, 5) } },
                    new AbilityEntry { name = "双魔诀", intention = EnemyIntentionType.Attack, actions = { (EnemyActionType.Attack, 5, 5), (EnemyActionType.Attack, 5, 5) } },
                };
            }

            // 默认模板：通用近战
            return new List<AbilityEntry> {
                new AbilityEntry { name = "攻击", intention = EnemyIntentionType.Attack, actions = { (EnemyActionType.Attack, 5, 8) } },
                new AbilityEntry { name = "格挡", intention = EnemyIntentionType.Defend, actions = { (EnemyActionType.Block, 4, 4) } },
                new AbilityEntry { name = "猛击", intention = EnemyIntentionType.Attack, actions = { (EnemyActionType.Attack, 7, 10) } },
            };
        }

        #endregion

        #region Elite Templates

        static void RebalanceElite(SerializedObject so, int region, string nameLower)
        {
            var phaseProp = so.FindProperty("phaseList");
            // 精英保留单阶段（50%血量触发狂暴）
            phaseProp.ClearArray();

            var abListProp = so.FindProperty("enemyAbilityList");
            abListProp.ClearArray();

            int dmgBase = EliteDmgBase[region];
            int dmgHigh = dmgBase + 6;
            int debuffVal = 2;

            // 精英模板：3-4个技能，有特色机制
            var template = GetEliteTemplate(nameLower, dmgBase, dmgHigh, debuffVal);

            for (int i = 0; i < template.Count; i++)
            {
                abListProp.InsertArrayElementAtIndex(i);
                var ab = abListProp.GetArrayElementAtIndex(i);
                var entry = template[i];
                ab.FindPropertyRelative("name").stringValue = entry.name;
                ab.FindPropertyRelative("hideActionValue").boolValue = entry.hideValue;
                ab.FindPropertyRelative("intention").objectReferenceValue = GetIntention(entry.intention);

                var actions = ab.FindPropertyRelative("actionList");
                actions.ClearArray();
                for (int j = 0; j < entry.actions.Count; j++)
                {
                    actions.InsertArrayElementAtIndex(j);
                    var act = actions.GetArrayElementAtIndex(j);
                    var (type, min, max) = entry.actions[j];
                    act.FindPropertyRelative("actionType").enumValueIndex = (int)type;
                    act.FindPropertyRelative("minActionValue").intValue = min;
                    act.FindPropertyRelative("maxActionValue").intValue = max;
                }
            }
        }

        static List<AbilityEntry> GetEliteTemplate(string nameLower, int dmgBase, int dmgHigh, int debuffVal)
        {
            if (nameLower.Contains("dujiao") || nameLower.Contains("horn"))
            {
                return new List<AbilityEntry> {
                    new AbilityEntry { name = "狂角冲撞", intention = EnemyIntentionType.Attack, actions = { (EnemyActionType.Attack, dmgBase, dmgBase + 3) } },
                    new AbilityEntry { name = "裂地踏", intention = EnemyIntentionType.Attack, actions = { (EnemyActionType.Attack, dmgBase - 2, dmgBase), (EnemyActionType.ApplyVulnerable, debuffVal, debuffVal) } },
                    new AbilityEntry { name = "铜皮", intention = EnemyIntentionType.Defend, actions = { (EnemyActionType.Block, dmgBase + 2, dmgBase + 2) } },
                    new AbilityEntry { name = "震荡", intention = EnemyIntentionType.Debuff, actions = { (EnemyActionType.ApplyFrail, debuffVal, debuffVal), (EnemyActionType.ApplyWeak, debuffVal, debuffVal) } },
                };
            }
            if (nameLower.Contains("anwang") || nameLower.Contains("dark") || nameLower.Contains("king"))
            {
                return new List<AbilityEntry> {
                    new AbilityEntry { name = "暗影斩", intention = EnemyIntentionType.Attack, actions = { (EnemyActionType.Attack, dmgBase, dmgHigh) } },
                    new AbilityEntry { name = "黑暗侵蚀", intention = EnemyIntentionType.Debuff, actions = { (EnemyActionType.ApplyWeak, debuffVal, debuffVal), (EnemyActionType.ApplyFrail, debuffVal, debuffVal) } },
                    new AbilityEntry { name = "暗影护盾", intention = EnemyIntentionType.Defend, actions = { (EnemyActionType.Block, dmgBase + 4, dmgBase + 4) } },
                    new AbilityEntry { name = "死亡之握", intention = EnemyIntentionType.Attack, actions = { (EnemyActionType.Attack, dmgBase - 2, dmgBase - 2), (EnemyActionType.ApplyVulnerable, debuffVal, debuffVal) } },
                };
            }
            // 默认精英：攻击+debuff+格挡+大招
            return new List<AbilityEntry> {
                new AbilityEntry { name = "重击", intention = EnemyIntentionType.Attack, actions = { (EnemyActionType.Attack, dmgBase, dmgHigh) } },
                new AbilityEntry { name = "压制", intention = EnemyIntentionType.Attack, actions = { (EnemyActionType.Attack, dmgBase - 3, dmgBase - 1), (EnemyActionType.ApplyWeak, debuffVal, debuffVal) } },
                new AbilityEntry { name = "铁壁", intention = EnemyIntentionType.Defend, actions = { (EnemyActionType.Block, dmgBase + 2, dmgBase + 2) } },
                new AbilityEntry { name = "破甲突袭", intention = EnemyIntentionType.Attack, actions = { (EnemyActionType.Attack, dmgBase - 1, dmgBase + 1), (EnemyActionType.ApplyVulnerable, debuffVal, debuffVal) } },
            };
        }

        #endregion

        #region Boss Templates

        static void RebalanceBoss(SerializedObject so, int region, string nameLower)
        {
            int p1Dmg = BossP1DmgBase[region];
            int p2Dmg = BossP2DmgBase[region];
            int debuffVal = 2;

            var phaseProp = so.FindProperty("phaseList");
            var abListProp = so.FindProperty("enemyAbilityList");
            abListProp.ClearArray();

            // 确保2阶段
            while (phaseProp.arraySize < 2)
                phaseProp.InsertArrayElementAtIndex(phaseProp.arraySize);

            // 阶段1：60% HP阈值
            var phase0 = phaseProp.GetArrayElementAtIndex(0);
            phase0.FindPropertyRelative("healthThreshold").floatValue = 0.6f;
            phase0.FindPropertyRelative("phaseEnterName").stringValue = "狂暴";
            var p0Abilities = phase0.FindPropertyRelative("phaseAbilityList");

            // 阶段2：30% HP阈值
            var phase1 = phaseProp.GetArrayElementAtIndex(1);
            phase1.FindPropertyRelative("healthThreshold").floatValue = 0.3f;
            phase1.FindPropertyRelative("phaseEnterName").stringValue = "末日降临";
            var p1Abilities = phase1.FindPropertyRelative("phaseAbilityList");

            // Boss模板
            var (p0Template, p1Template) = GetBossTemplate(nameLower, p1Dmg, p2Dmg, debuffVal);

            FillAbilityList(p0Abilities, p0Template);
            FillAbilityList(p1Abilities, p1Template);
        }

        static (List<AbilityEntry>, List<AbilityEntry>) GetBossTemplate(string nameLower, int p1Dmg, int p2Dmg, int debuffVal)
        {
            if (nameLower.Contains("heifeng") || nameLower.Contains("blackwind"))
            {
                return (
                    new List<AbilityEntry> {
                        new AbilityEntry { name = "黑风掌", intention = EnemyIntentionType.Attack, actions = { (EnemyActionType.Attack, p1Dmg, p1Dmg + 2) } },
                        new AbilityEntry { name = "妖气波", intention = EnemyIntentionType.Attack, actions = { (EnemyActionType.Attack, p1Dmg - 3, p1Dmg - 1), (EnemyActionType.ApplyWeak, debuffVal, debuffVal) } },
                        new AbilityEntry { name = "妖气回元", intention = EnemyIntentionType.Debuff, actions = { (EnemyActionType.Heal, 8, 8) } },
                    },
                    new List<AbilityEntry> {
                        new AbilityEntry { name = "末日·三连爪", intention = EnemyIntentionType.Attack, actions = { (EnemyActionType.Attack, p2Dmg - 4, p2Dmg - 4), (EnemyActionType.Attack, p2Dmg - 4, p2Dmg - 4), (EnemyActionType.Attack, p2Dmg - 4, p2Dmg - 4) } },
                        new AbilityEntry { name = "末日·妖啸", intention = EnemyIntentionType.Debuff, actions = { (EnemyActionType.ApplyVulnerable, debuffVal, debuffVal), (EnemyActionType.ApplyWeak, debuffVal, debuffVal) } },
                        new AbilityEntry { name = "末日·黑风掌", intention = EnemyIntentionType.Attack, actions = { (EnemyActionType.Attack, p2Dmg, p2Dmg + 3) } },
                        new AbilityEntry { name = "末日·护体", intention = EnemyIntentionType.Defend, actions = { (EnemyActionType.Block, 12, 12) } },
                    }
                );
            }

            if (nameLower.Contains("guishen") || nameLower.Contains("shangxian") || nameLower.Contains("ghost"))
            {
                return (
                    new List<AbilityEntry> {
                        new AbilityEntry { name = "鬼手", intention = EnemyIntentionType.Attack, actions = { (EnemyActionType.Attack, p1Dmg, p1Dmg + 3) } },
                        new AbilityEntry { name = "亡灵之息", intention = EnemyIntentionType.Debuff, actions = { (EnemyActionType.ApplyWeak, debuffVal, debuffVal), (EnemyActionType.ApplyFrail, debuffVal, debuffVal) } },
                        new AbilityEntry { name = "怨灵回复", intention = EnemyIntentionType.Debuff, actions = { (EnemyActionType.Heal, 10, 10) } },
                        new AbilityEntry { name = "骨盾", intention = EnemyIntentionType.Defend, actions = { (EnemyActionType.Block, 10, 10) } },
                    },
                    new List<AbilityEntry> {
                        new AbilityEntry { name = "末日·冥火", intention = EnemyIntentionType.Attack, actions = { (EnemyActionType.Attack, p2Dmg, p2Dmg + 4) } },
                        new AbilityEntry { name = "末日·万怨", intention = EnemyIntentionType.Debuff, actions = { (EnemyActionType.ApplyVulnerable, debuffVal, debuffVal), (EnemyActionType.ApplyWeak, debuffVal, debuffVal), (EnemyActionType.Poison, debuffVal, debuffVal) } },
                        new AbilityEntry { name = "末日·噬魂", intention = EnemyIntentionType.Attack, actions = { (EnemyActionType.Attack, p2Dmg - 5, p2Dmg - 5), (EnemyActionType.Attack, p2Dmg - 5, p2Dmg - 5) } },
                        new AbilityEntry { name = "末日·冥铠", intention = EnemyIntentionType.Defend, actions = { (EnemyActionType.Block, 15, 15) } },
                    }
                );
            }

            if (nameLower.Contains("duhuang") || nameLower.Contains("poison") || nameLower.Contains("swamp"))
            {
                return (
                    new List<AbilityEntry> {
                        new AbilityEntry { name = "毒息", intention = EnemyIntentionType.Debuff, actions = { (EnemyActionType.Poison, 3, 3), (EnemyActionType.ApplyWeak, debuffVal, debuffVal) } },
                        new AbilityEntry { name = "沼泽冲撞", intention = EnemyIntentionType.Attack, actions = { (EnemyActionType.Attack, p1Dmg, p1Dmg + 2) } },
                        new AbilityEntry { name = "再生", intention = EnemyIntentionType.Debuff, actions = { (EnemyActionType.Heal, 12, 12) } },
                    },
                    new List<AbilityEntry> {
                        new AbilityEntry { name = "末日·万毒", intention = EnemyIntentionType.Debuff, actions = { (EnemyActionType.Poison, 4, 4), (EnemyActionType.ApplyVulnerable, debuffVal, debuffVal) } },
                        new AbilityEntry { name = "末日·毒爆", intention = EnemyIntentionType.Attack, actions = { (EnemyActionType.Attack, p2Dmg, p2Dmg + 3) } },
                        new AbilityEntry { name = "末日·吞噬", intention = EnemyIntentionType.Attack, actions = { (EnemyActionType.Attack, p2Dmg - 6, p2Dmg - 6), (EnemyActionType.Heal, 8, 8) } },
                        new AbilityEntry { name = "末日·毒壁", intention = EnemyIntentionType.Defend, actions = { (EnemyActionType.Block, 12, 12), (EnemyActionType.Poison, 2, 2) } },
                    }
                );
            }

            // 默认Boss模板（天魔类）
            return (
                new List<AbilityEntry> {
                    new AbilityEntry { name = "天魔斩", intention = EnemyIntentionType.Attack, actions = { (EnemyActionType.Attack, p1Dmg, p1Dmg + 3) } },
                    new AbilityEntry { name = "魔气侵蚀", intention = EnemyIntentionType.Debuff, actions = { (EnemyActionType.ApplyWeak, debuffVal, debuffVal), (EnemyActionType.ApplyVulnerable, debuffVal, debuffVal) } },
                    new AbilityEntry { name = "魔气回元", intention = EnemyIntentionType.Debuff, actions = { (EnemyActionType.Heal, 10, 10) } },
                    new AbilityEntry { name = "魔甲", intention = EnemyIntentionType.Defend, actions = { (EnemyActionType.Block, 10, 10) } },
                },
                new List<AbilityEntry> {
                    new AbilityEntry { name = "末日·天魔崩", intention = EnemyIntentionType.Attack, actions = { (EnemyActionType.Attack, p2Dmg, p2Dmg + 5) } },
                    new AbilityEntry { name = "末日·万魔蚀", intention = EnemyIntentionType.Debuff, actions = { (EnemyActionType.ApplyWeak, debuffVal, debuffVal), (EnemyActionType.ApplyFrail, debuffVal, debuffVal), (EnemyActionType.ApplyVulnerable, debuffVal, debuffVal) } },
                    new AbilityEntry { name = "末日·魔连斩", intention = EnemyIntentionType.Attack, actions = { (EnemyActionType.Attack, p2Dmg - 8, p2Dmg - 8), (EnemyActionType.Attack, p2Dmg - 8, p2Dmg - 8) } },
                    new AbilityEntry { name = "末日·魔铠", intention = EnemyIntentionType.Defend, actions = { (EnemyActionType.Block, 15, 15) } },
                }
            );
        }

        #endregion

        static void FillAbilityList(SerializedProperty listProp, List<AbilityEntry> template)
        {
            listProp.ClearArray();
            for (int i = 0; i < template.Count; i++)
            {
                listProp.InsertArrayElementAtIndex(i);
                var ab = listProp.GetArrayElementAtIndex(i);
                var entry = template[i];
                ab.FindPropertyRelative("name").stringValue = entry.name;
                ab.FindPropertyRelative("hideActionValue").boolValue = entry.hideValue;
                ab.FindPropertyRelative("intention").objectReferenceValue = GetIntention(entry.intention);

                var actions = ab.FindPropertyRelative("actionList");
                actions.ClearArray();
                for (int j = 0; j < entry.actions.Count; j++)
                {
                    actions.InsertArrayElementAtIndex(j);
                    var act = actions.GetArrayElementAtIndex(j);
                    var (type, min, max) = entry.actions[j];
                    act.FindPropertyRelative("actionType").enumValueIndex = (int)type;
                    act.FindPropertyRelative("minActionValue").intValue = min;
                    act.FindPropertyRelative("maxActionValue").intValue = max;
                }
            }
        }
    }
}
