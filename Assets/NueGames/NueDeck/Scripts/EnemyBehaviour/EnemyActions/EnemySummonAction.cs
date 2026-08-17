using System.Collections.Generic;
using System.Reflection;
using NueGames.NueDeck.Scripts.Characters;
using NueGames.NueDeck.Scripts.Data.Characters;
using NueGames.NueDeck.Scripts.Enums;
using UnityEngine;

namespace NueGames.NueDeck.Scripts.EnemyBehaviour.EnemyActions
{
    /// <summary>
    /// 召唤从属敌人。主将执行此 Action 时在可用位置生成从属。
    /// 从属敌人数据来自主将的 subordinateId 字段，通过 ResourceCache 查找。
    /// </summary>
    public class EnemySummonAction : EnemyActionBase
    {
        public override EnemyActionType ActionType => EnemyActionType.Summon;

        public override void DoAction(EnemyActionParameters actionParameters)
        {
            var self = actionParameters.SelfCharacter as EnemyBase;
            if (self == null || self.EnemyCharacterData == null) return;

            var data = self.EnemyCharacterData;
            if (string.IsNullOrEmpty(data.SubordinateId))
            {
                Debug.LogWarning($"[EnemySummonAction] {data.name} has no subordinateId set!");
                return;
            }

            // 通过 ResourceCache 查找从属敌人数据
            var subordinateData = FindEnemyData(data.SubordinateId);
            if (subordinateData == null)
            {
                Debug.LogWarning($"[EnemySummonAction] Cannot find enemy with id={data.SubordinateId}");
                return;
            }

            int summonCount = data.SummonCount;
            var availablePositions = GetAvailablePositions();

            for (int i = 0; i < summonCount && i < availablePositions.Count; i++)
            {
                SpawnEnemy(subordinateData, availablePositions[i]);
                Debug.Log($"[EnemySummonAction] {data.name} summoned {subordinateData.name} at pos {i}");
            }
        }

        private EnemyCharacterData FindEnemyData(string enemyId)
        {
#if UNITY_EDITOR
            var guids = UnityEditor.AssetDatabase.FindAssets("t:EnemyCharacterData");
            foreach (var g in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                var ed = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyCharacterData>(path);
                if (ed != null && ed.name == enemyId) return ed;
            }
            return null;
#else
            // 打包后从 Resources 加载
            var allEnemies = Resources.LoadAll<EnemyCharacterData>("Data/Enemies");
            foreach (var ed in allEnemies)
                if (ed.name == enemyId) return ed;
            return null;
#endif
        }

        private List<Transform> GetAvailablePositions()
        {
            var result = new List<Transform>();
            var occupied = new HashSet<int>();

            // 标记已占用位置
            foreach (var enemy in CombatManager.CurrentEnemiesList)
            {
                for (int i = 0; i < CombatManager.EnemyPosList.Count; i++)
                {
                    if (CombatManager.EnemyPosList[i] == enemy.transform.parent)
                    {
                        occupied.Add(i);
                        break;
                    }
                }
            }

            // 收集可用位置
            for (int i = 0; i < CombatManager.EnemyPosList.Count; i++)
            {
                if (!occupied.Contains(i))
                    result.Add(CombatManager.EnemyPosList[i]);
            }

            return result;
        }

        private void SpawnEnemy(EnemyCharacterData enemyData, Transform pos)
        {
            if (enemyData.EnemyPrefab == null)
            {
                Debug.LogWarning($"[EnemySummonAction] {enemyData.name} has no EnemyPrefab!");
                return;
            }

            var clone = Object.Instantiate(enemyData.EnemyPrefab, pos);
            var dataField = typeof(EnemyBase).GetField("enemyCharacterData",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (dataField != null) dataField.SetValue(clone, enemyData);
            clone.BuildCharacter();

            // 应用动态缩放
            var enemyCount = CombatManager.CurrentEnemiesList.Count + 1;
            ApplyDynamicScaling(clone, enemyCount);

            // 应用难度倍率
            ApplyDifficultyMultiplier(clone);

            // 应用立绘
            ApplyEnemySprite(clone, enemyData);

            CombatManager.CurrentEnemiesList.Add(clone);
        }

        private void ApplyDynamicScaling(EnemyBase enemy, int totalEnemies)
        {
            float hpMult = totalEnemies switch
            {
                1 => 1f,
                2 => 0.75f,
                3 => 0.6f,
                _ => 0.5f
            };
            var stats = enemy.CharacterStats;
            var newMax = Mathf.RoundToInt(stats.MaxHealth * hpMult);
            var maxHealthField = stats.GetType().GetField("maxHealth",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (maxHealthField != null)
            {
                maxHealthField.SetValue(stats, newMax);
            }
            stats.SetCurrentHealth(newMax);
        }

        private void ApplyDifficultyMultiplier(EnemyBase enemy)
        {
            // 简化：召唤的从属不应用难度倍率，因为主将已经应用过
        }

        private void ApplyEnemySprite(EnemyBase enemy, EnemyCharacterData data)
        {
            if (data.EnemyPortrait == null) return;
            var spriteRoot = enemy.transform.Find("SpriteRoot");
            if (spriteRoot == null) return;
            var sr = spriteRoot.Find("p3_stand")?.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = data.EnemyPortrait;
                var scale = sr.transform.localScale;
                sr.transform.localScale = new Vector3(-Mathf.Abs(scale.x), scale.y, scale.z);
            }
        }
    }
}
