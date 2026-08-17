using System;
using System.Collections.Generic;
using NueGames.NueDeck.Scripts.Characters;
using NueGames.NueDeck.Scripts.Data.Containers;
using NueGames.NueDeck.Scripts.Enums;
using NueGames.NueDeck.Scripts.NueExtentions;
using UnityEngine;
using Random = UnityEngine.Random;

namespace NueGames.NueDeck.Scripts.Data.Characters
{
    [CreateAssetMenu(fileName = "Enemy Character Data",menuName = "NueDeck/Characters/Enemy",order = 1)]
    public class EnemyCharacterData : CharacterDataBase
    {
        [Header("Enemy Defaults")] 
        [SerializeField] private EnemyBase enemyPrefab;
        [SerializeField] private bool followAbilityPattern;
        [SerializeField] private List<EnemyAbilityData> enemyAbilityList;
        public List<EnemyAbilityData> EnemyAbilityList => enemyAbilityList;

        public EnemyBase EnemyPrefab => enemyPrefab;

        [Header("Enemy Identity")]
        [Tooltip("品阶：普通/精英/Boss")]
        [SerializeField] private EnemyTier enemyTier = EnemyTier.Normal;
        [Tooltip("区域序号：0=山野荒原 1=幽冥秘境 2=万蛊沼泽 3=天魔裂隙")]
        [SerializeField] private int regionId;
        [Tooltip("背景故事（图鉴用，长文本）")]
        [SerializeField] [TextArea(3, 8)] private string enemyDescription;
        [Tooltip("立绘大图（图鉴用）")]
        [SerializeField] private Sprite enemyPortrait;

        [Header("Dialogue")]
        [Tooltip("战前对话（战斗开始时显示，用\\n换行表示多行）")]
        [SerializeField] [TextArea(1, 6)] private string encounterDialogue;
        [Tooltip("战胜后对话（击杀后显示，用\\n换行）")]
        [SerializeField] [TextArea(1, 6)] private string victoryDialogue;

        [Header("Multi-Phase")]
        [Tooltip("多阶段配置。留空=单阶段（使用 enemyAbilityList）。按顺序填血量阈值，如 [0.6, 0.3]")]
        [SerializeField] private List<EnemyPhaseData> phaseList;

        [Header("Spawn Settings")]
        [Tooltip("生成模式：单独/复数/主将/从属")]
        [SerializeField] private EnemySpawnType spawnType = EnemySpawnType.Solo;
        [Tooltip("复数类型：生成数量")]
        [SerializeField] private int spawnCount = 2;
        [Tooltip("从属类型：所属主将的敌人ID")]
        [SerializeField] private string commanderId;
        [Tooltip("从属类型：生成模式")]
        [SerializeField] private SubordinateSpawnMode subordinateMode = SubordinateSpawnMode.WithCommander;
        [Tooltip("主将类型：召唤的从属敌人ID（ResourceCache中查找）")]
        [SerializeField] private string subordinateId;
        [Tooltip("召唤模式：每隔N回合召唤一次（通过技能轮转实现）")]
        [SerializeField] private int summonInterval = 3;
        [Tooltip("召唤模式：每次召唤数量")]
        [SerializeField] private int summonCount = 1;

        public EnemyTier EnemyTier => enemyTier;
        public int RegionId => regionId;
        public string EnemyDescription => enemyDescription;
        public Sprite EnemyPortrait => enemyPortrait;
        public string EncounterDialogue => encounterDialogue;
        public string VictoryDialogue => victoryDialogue;
        public bool HasPhases => phaseList != null && phaseList.Count > 0;
        public List<EnemyPhaseData> PhaseList => phaseList;

        public EnemySpawnType SpawnType => spawnType;
        public int SpawnCount => spawnCount;
        public string CommanderId => commanderId;
        public SubordinateSpawnMode SubordinateMode => subordinateMode;
        public string SubordinateId => subordinateId;
        public int SummonInterval => summonInterval;
        public int SummonCount => summonCount;

        /// <summary>Expose followAbilityPattern for phase-aware EnemyBase</summary>
        public bool FollowAbilityPatternCheck() => followAbilityPattern;

        /// <summary>当前生效的技能池（带阶段支持）</summary>
        public List<EnemyAbilityData> GetActiveAbilityList(float healthRatio)
        {
            if (!HasPhases) return enemyAbilityList;
            // 找到最后一个 currentHP/maxHP <= threshold 的阶段
            for (int i = phaseList.Count - 1; i >= 0; i--)
            {
                if (healthRatio <= phaseList[i].HealthThreshold)
                    return phaseList[i].PhaseAbilityList;
            }
            // 未到任何阈值 → 阶段0（第一个phase的阈值最高，指进入条件）
            // 若列表第一个阈值是0.6, 当前HP>60%则为阶段0（在列表里最后一个满足或第一个不满足）
            // 简化：HP低于最小阈值用最后一个，否则用阶段0对应的"未触发"技能。此处返回阶段0列表。
            return phaseList[0].PhaseAbilityList;
        }

        /// <summary>获取当前阶段名（用于阶段切换提示）</summary>
        public string GetPhaseName(float healthRatio)
        {
            if (!HasPhases) return "";
            for (int i = phaseList.Count - 1; i >= 0; i--)
            {
                if (healthRatio <= phaseList[i].HealthThreshold)
                    return phaseList[i].PhaseEnterName;
            }
            return phaseList[0].PhaseEnterName;
        }

        public EnemyAbilityData GetAbility()
        {
            return EnemyAbilityList.RandomItem();
        }
        
        public EnemyAbilityData GetAbility(int usedAbilityCount)
        {
            if (followAbilityPattern)
            {
                var index = usedAbilityCount % EnemyAbilityList.Count;
                return EnemyAbilityList[index];
            }

            return GetAbility();
        }

        #region Editor
#if UNITY_EDITOR
        public void EditEnemyTier(EnemyTier tier) => enemyTier = tier;
        public void EditRegionId(int id) => regionId = id;
        public void EditEnemyDescription(string desc) => enemyDescription = desc;
        public void EditEnemyPortrait(Sprite s) => enemyPortrait = s;
        public void EditEncounterDialogue(string d) => encounterDialogue = d;
        public void EditVictoryDialogue(string d) => victoryDialogue = d;
        public void EditSpawnType(EnemySpawnType t) => spawnType = t;
        public void EditSpawnCount(int c) => spawnCount = c;
        public void EditCommanderId(string id) => commanderId = id;
        public void EditSubordinateMode(SubordinateSpawnMode m) => subordinateMode = m;
        public void EditSubordinateId(string id) => subordinateId = id;
        public void EditSummonInterval(int n) => summonInterval = n;
        public void EditSummonCount(int c) => summonCount = c;
#endif
        #endregion
    }

    /// <summary>多阶段数据：血量阈值 + 该阶段技能池</summary>
    [Serializable]
    public class EnemyPhaseData
    {
        [Tooltip("触发本阶段的血量比例阈值（如 0.6 = HP低于60%时进入本阶段）")]
        [SerializeField] private float healthThreshold = 0.5f;
        [Tooltip("阶段名称（进入时提示，如 狂暴/觉醒/末日）")]
        [SerializeField] private string phaseEnterName = "";
        [SerializeField] private List<EnemyAbilityData> phaseAbilityList = new List<EnemyAbilityData>();

        public float HealthThreshold => healthThreshold;
        public string PhaseEnterName => phaseEnterName;
        public List<EnemyAbilityData> PhaseAbilityList => phaseAbilityList;

#if UNITY_EDITOR
        public void EditHealthThreshold(float v) => healthThreshold = v;
        public void EditPhaseEnterName(string n) => phaseEnterName = n;
        public void EditPhaseAbilityList(List<EnemyAbilityData> l) => phaseAbilityList = l;
#endif
    }
    
    [Serializable]
    public class EnemyAbilityData
    {
        [Header("Settings")]
        [SerializeField] private string name;
        [SerializeField] private EnemyIntentionData intention;
        [SerializeField] private bool hideActionValue;
        [SerializeField] private List<EnemyActionData> actionList;
        public string Name => name;
        public EnemyIntentionData Intention => intention;
        public List<EnemyActionData> ActionList => actionList;
        public bool HideActionValue => hideActionValue;
    }
    
    [Serializable]
    public class EnemyActionData
    {
        [SerializeField] private EnemyActionType actionType;
        [SerializeField] private int minActionValue;
        [SerializeField] private int maxActionValue;
        public EnemyActionType ActionType => actionType;
        public int ActionValue => Random.Range(minActionValue,maxActionValue);
        
    }
    
    
    
}