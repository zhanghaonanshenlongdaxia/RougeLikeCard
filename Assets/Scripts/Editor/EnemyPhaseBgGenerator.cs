using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using NueGames.NueDeck.Scripts.Data.Characters;
using NueGames.NueDeck.Scripts.Data.Containers;
using NueGames.NueDeck.Scripts.Enums;

namespace CardGame.Editor
{
    /// <summary>
    /// 为9个未配置阶段的Boss和所有Elite配置多阶段。
    /// 同时为所有敌人设置战斗背景类型。
    /// </summary>
    public static class EnemyPhaseBgGenerator
    {
        [MenuItem("Tools/Generate Boss Phases & Backgrounds")]
        public static void GenerateAll()
        {
            int phaseCount = ConfigureBossPhases();
            int elitePhaseCount = ConfigureElitePhases();
            int bgCount = ConfigureBackgrounds();
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"多阶段+背景配置完成: Boss阶段{phaseCount} + Elite阶段{elitePhaseCount} + 背景设置{bgCount}");
        }

        static int ConfigureBossPhases()
        {
            var guids = AssetDatabase.FindAssets("t:EnemyCharacterData", new[] { "Assets/NueGames/NueDeck/Data/Enemies" });
            int count = 0;

            foreach (var g in guids)
            {
                var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(g));
                if (so == null) continue;

                var tierField = so.GetType().GetField("enemyTier", BindingFlags.NonPublic | BindingFlags.Instance);
                var tier = tierField?.GetValue(so)?.ToString();
                if (tier != "Boss") continue;

                var phaseField = so.GetType().GetField("phaseList", BindingFlags.NonPublic | BindingFlags.Instance);
                var phases = phaseField?.GetValue(so) as IList;
                if (phases != null && phases.Count > 0) continue; // 已有阶段，跳过

                var nameProp = so.GetType().GetProperty("CharacterName");
                var bossName = (string)nameProp?.GetValue(so) ?? "Unknown";

                // 根据敌人名分配不同阶段配置
                var phaseConfigs = GetBossPhaseConfigs(bossName);
                
                var newPhases = new List<EnemyPhaseData>();
                foreach (var (threshold, phaseName, abilities) in phaseConfigs)
                {
                    var phase = new EnemyPhaseData();
                    phase.EditHealthThreshold(threshold);
                    phase.EditPhaseEnterName(phaseName);
                    phase.EditPhaseAbilityList(abilities);
                    newPhases.Add(phase);
                }

                phaseField.SetValue(so, newPhases);
                EditorUtility.SetDirty(so);
                count++;
                Debug.Log($"  Boss {bossName}: configured {newPhases.Count} phases");
            }
            return count;
        }

        static int ConfigureElitePhases()
        {
            var guids = AssetDatabase.FindAssets("t:EnemyCharacterData", new[] { "Assets/NueGames/NueDeck/Data/Enemies" });
            int count = 0;

            foreach (var g in guids)
            {
                var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(g));
                if (so == null) continue;

                var tierField = so.GetType().GetField("enemyTier", BindingFlags.NonPublic | BindingFlags.Instance);
                var tier = tierField?.GetValue(so)?.ToString();
                if (tier != "Elite") continue;

                var phaseField = so.GetType().GetField("phaseList", BindingFlags.NonPublic | BindingFlags.Instance);
                var phases = phaseField?.GetValue(so) as IList;
                if (phases != null && phases.Count > 0) continue;

                // 精英怪配置2阶段：50%血量时进入狂暴
                var phaseConfigs = GetElitePhaseConfigs();
                
                var newPhases = new List<EnemyPhaseData>();
                foreach (var (threshold, phaseName, abilities) in phaseConfigs)
                {
                    var phase = new EnemyPhaseData();
                    phase.EditHealthThreshold(threshold);
                    phase.EditPhaseEnterName(phaseName);
                    phase.EditPhaseAbilityList(abilities);
                    newPhases.Add(phase);
                }

                phaseField.SetValue(so, newPhases);
                EditorUtility.SetDirty(so);
                count++;
            }
            return count;
        }

        // Boss阶段配置生成器
        static List<(float, string, List<EnemyAbilityData>)> GetBossPhaseConfigs(string bossName)
        {
            // 通用Boss：3阶段（60%→狂暴, 30%→末日）
            // 技能从敌人已有的enemyAbilityList复制并增强
            
            var result = new List<(float, string, List<EnemyAbilityData>)>();

            // 阶段1: 60%血量 → 狂暴（攻击力提升）
            result.Add((0.6f, "\u72C2\u66B4", CreateAbilities(
                ("Attack", 18, 25, EnemyIntentionType.Attack),
                ("Attack", 12, 18, EnemyIntentionType.Attack),
                ("Heal", 8, 12, EnemyIntentionType.Heal)
            )));

            // 阶段2: 30%血量 → 末日（全屏攻击+debuff）
            result.Add((0.3f, "\u672B\u65E5\u964D\u4E34", CreateAbilities(
                ("Attack", 25, 35, EnemyIntentionType.Attack),
                ("ApplyWeak", 3, 3, EnemyIntentionType.Debuff),
                ("ApplyVulnerable", 3, 3, EnemyIntentionType.Debuff),
                ("Block", 10, 15, EnemyIntentionType.Defend)
            )));

            return result;
        }

        // 精英怪阶段配置
        static List<(float, string, List<EnemyAbilityData>)> GetElitePhaseConfigs()
        {
            var result = new List<(float, string, List<EnemyAbilityData>)>();

            // 阶段1: 50%血量 → 狂暴
            result.Add((0.5f, "\u72C2\u66B4", CreateAbilities(
                ("Attack", 12, 18, EnemyIntentionType.Attack),
                ("Attack", 8, 12, EnemyIntentionType.Attack),
                ("ApplyWeak", 2, 2, EnemyIntentionType.Debuff)
            )));

            return result;
        }

        // 创建EnemyAbilityData列表
        static List<EnemyAbilityData> CreateAbilities(params (string, int, int, EnemyIntentionType)[] abilitySpecs)
        {
            var list = new List<EnemyAbilityData>();
            foreach (var (actionTypeName, minVal, maxVal, intentionType) in abilitySpecs)
            {
                var actionType = (EnemyActionType)System.Enum.Parse(typeof(EnemyActionType), actionTypeName);
                
                var action = new EnemyActionData();
                // 用反射设置actionType和min/max
                var atField = action.GetType().GetField("actionType", BindingFlags.NonPublic | BindingFlags.Instance);
                atField?.SetValue(action, actionType);
                var minField = action.GetType().GetField("minActionValue", BindingFlags.NonPublic | BindingFlags.Instance);
                minField?.SetValue(action, minVal);
                var maxField = action.GetType().GetField("maxActionValue", BindingFlags.NonPublic | BindingFlags.Instance);
                maxField?.SetValue(action, maxVal);

                // 创建IntentionData
                var intention = new EnemyIntentionData();
                var intTypeField = intention.GetType().GetField("enemyIntentionType", BindingFlags.NonPublic | BindingFlags.Instance);
                intTypeField?.SetValue(intention, intentionType);

                var ability = new EnemyAbilityData();
                var nameField = ability.GetType().GetField("name", BindingFlags.NonPublic | BindingFlags.Instance);
                nameField?.SetValue(ability, $"{actionTypeName}_{minVal}_{maxVal}");
                var intField = ability.GetType().GetField("intention", BindingFlags.NonPublic | BindingFlags.Instance);
                intField?.SetValue(ability, intention);
                var actListField = ability.GetType().GetField("actionList", BindingFlags.NonPublic | BindingFlags.Instance);
                actListField?.SetValue(ability, new List<EnemyActionData> { action });

                list.Add(ability);
            }
            return list;
        }

        // 为所有敌人设置战斗背景
        static int ConfigureBackgrounds()
        {
            var guids = AssetDatabase.FindAssets("t:EnemyCharacterData", new[] { "Assets/NueGames/NueDeck/Data/Enemies" });
            int count = 0;

            foreach (var g in guids)
            {
                var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(g));
                if (so == null) continue;

                // 获取regionId
                var regionField = so.GetType().GetField("regionId", BindingFlags.NonPublic | BindingFlags.Instance);
                var regionId = (int)(regionField?.GetValue(so) ?? 0);

                // 获取tier
                var tierField = so.GetType().GetField("enemyTier", BindingFlags.NonPublic | BindingFlags.Instance);
                var tier = tierField?.GetValue(so)?.ToString();

                // 设置backgroundType
                // 普通怪用地图对应背景: region0→Profile1, region1→Profile2, region2→Profile3, region3→Profile1
                // Boss/Elite用特殊背景: Boss→Profile3, Elite→Profile2
                BackgroundTypes bgType;
                if (tier == "Boss")
                    bgType = BackgroundTypes.Profile3; // Boss用最特殊的背景
                else if (tier == "Elite")
                    bgType = BackgroundTypes.Profile2; // 精英用次特殊背景
                else
                    bgType = regionId switch { 0 => BackgroundTypes.Profile1, 1 => BackgroundTypes.Profile2, 2 => BackgroundTypes.Profile3, 3 => BackgroundTypes.Profile1, _ => BackgroundTypes.Profile1 };

                // 设置到EncounterBase.targetBackgroundType
                var bgField = so.GetType().BaseType.GetField("targetBackgroundType", BindingFlags.NonPublic | BindingFlags.Instance);
                if (bgField != null)
                {
                    bgField.SetValue(so, bgType);
                    EditorUtility.SetDirty(so);
                    count++;
                }
            }
            return count;
        }
    }
}
