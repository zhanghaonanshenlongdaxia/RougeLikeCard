using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CardGame;
using CardGame.UI;
using NueGames.NueDeck.Scripts.Characters;
using NueGames.NueDeck.Scripts.Characters.Enemies;
using NueGames.NueDeck.Scripts.Data.Characters;
using NueGames.NueDeck.Scripts.Data.Containers;
using NueGames.NueDeck.Scripts.Enums;
using NueGames.NueDeck.Scripts.Utils.Background;
using QFramework;
using UnityEngine;

namespace NueGames.NueDeck.Scripts.Managers
{
    public class CombatManager : MonoBehaviour, IController
    {
        private CombatManager(){}
        public static CombatManager Instance { get; private set; }

        public IArchitecture GetArchitecture() => CardGameArchitecture.Interface;

        [Header("References")] 
        [SerializeField] private BackgroundContainer backgroundContainer;
        [SerializeField] private List<Transform> enemyPosList;
        [SerializeField] private List<Transform> allyPosList;
 
        
        #region Cache
        public List<EnemyBase> CurrentEnemiesList { get; private set; } = new List<EnemyBase>();
        public List<AllyBase> CurrentAlliesList { get; private set; }= new List<AllyBase>();

        public Action OnAllyTurnStarted;
        public Action OnEnemyTurnStarted;
        // 回合结束事件：玩家/敌人在各自回合结束时结算状态衰减（而不是回合开始）
        public Action OnAllyTurnEnded;
        public Action OnEnemyTurnEnded;
        public List<Transform> EnemyPosList => enemyPosList;

        public List<Transform> AllyPosList => allyPosList;

        public AllyBase CurrentMainAlly => CurrentAlliesList.Count>0 ? CurrentAlliesList[0] : null;

        public EnemyEncounter CurrentEncounter { get; private set; }
        public List<CardGame.MaterialData> LastDroppedMaterials { get; private set; } = new List<CardGame.MaterialData>();
        /// <summary>本场战斗所有敌人数据快照（死亡后仍可读取用于掉落）</summary>
        private List<EnemyCharacterData> _combatEnemySnapshot = new List<EnemyCharacterData>();
        
        public CombatStateType CurrentCombatStateType
        {
            get => _currentCombatStateType;
            private set
            {
                ExecuteCombatState(value);
                _currentCombatStateType = value;
            }
        }
        
        private CombatStateType _currentCombatStateType;
        protected FxManager FxManager => FxManager.Instance;
        protected AudioManager AudioManager => AudioManager.Instance;
        protected GameManager GameManager => GameManager.Instance;
        protected UIManager UIManager => UIManager.Instance;

        protected CollectionManager CollectionManager => CollectionManager.Instance;

        #endregion
        
        
        #region Setup
        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            } 
            else
            {
                Instance = this;
                CurrentCombatStateType = CombatStateType.PrepareCombat;
            }
        }

        private void Start()
        {
            StartCombat();
        }

        public void StartCombat()
        {
            // 重置战斗状态（防止上一场战斗的 EndCombat 残留）
            var battleModel = this.GetModel<IBattleModel>();
            battleModel.State.Value = CombatStateType.PrepareCombat;
            battleModel.CanUseCards.Value = true;
            battleModel.CanSelectCards.Value = true;

            BuildEnemies();
            BuildAllies();
            backgroundContainer.OpenSelectedBackground();
          
            this.GetSystem<ICardSystem>().SetGameDeck();
           
            UIManager.CombatCanvas.gameObject.SetActive(true);
            UIManager.InformationCanvas.gameObject.SetActive(true);

            // 遗物触发：战斗开始
            this.GetSystem<IRelicSystem>().TriggerRelics(RelicTriggerType.OnCombatStart,
                new RelicTriggerContext(player: CurrentMainAlly, enemies: CurrentEnemiesList));

            // 应用事件预存的状态加成
            ApplyPendingBuffs();

            // 应用功法被动属性
            ApplyCultivationPassives();

            // 图鉴解锁 + 战前对话
            StartCoroutine(StartCombatWithDialogue());
        }

        /// <summary>
        /// 应用功法被动属性 (MaxHP/神识上限/灵力上限)
        /// </summary>
        private void ApplyCultivationPassives()
        {
            var cultSystem = this.GetSystem<ICultivationSystem>();
            if (cultSystem == null) return;

            var passives = cultSystem.GetActivePassiveStats();
            if (passives == null || passives.Count == 0) return;

            var player = CurrentMainAlly;
            if (player == null || player.CharacterStats == null) return;

            var bm = this.GetModel<IBattleModel>();
            var gm = GameManager.Instance;

            foreach (var kv in passives)
            {
                switch (kv.Key)
                {
                    case NueGames.NueDeck.Scripts.Enums.PassiveStatType.MaxHP:
                        player.CharacterStats.IncreaseMaxHealth(kv.Value);
                        player.CharacterStats.Heal(kv.Value);
                        break;
                    case NueGames.NueDeck.Scripts.Enums.PassiveStatType.ShenShi:
                        var loadout = this.GetModel<ILoadoutModel>();
                        loadout.MaxShenShi.Value += kv.Value;
                        break;
                    case NueGames.NueDeck.Scripts.Enums.PassiveStatType.MaxMana:
                        if (bm != null) bm.MaxMana.Value += kv.Value;
                        if (gm != null) gm.PersistentGameplayData.MaxMana += kv.Value;
                        break;
                }
                Debug.Log($"[Combat] Cultivation passive: {kv.Key} +{kv.Value}");
            }
        }

        /// <summary>
        /// 应用事件预存的Pending状态（来自事件/篝火等非战斗场景），应用后清零
        /// </summary>
        private void ApplyPendingBuffs()
        {
            var bm = this.GetModel<IBattleModel>();
            if (bm == null) return;

            var player = CurrentMainAlly;
            if (player != null && player.CharacterStats != null)
            {
                if (bm.PendingStrengthBonus > 0)
                {
                    player.CharacterStats.ApplyStatus(NueGames.NueDeck.Scripts.Enums.StatusType.Strength, bm.PendingStrengthBonus);
                    Debug.Log($"[Combat] Applied PendingStrength: +{bm.PendingStrengthBonus}");
                }
                if (bm.PendingDexterityBonus > 0)
                {
                    player.CharacterStats.ApplyStatus(NueGames.NueDeck.Scripts.Enums.StatusType.Dexterity, bm.PendingDexterityBonus);
                    Debug.Log($"[Combat] Applied PendingDexterity: +{bm.PendingDexterityBonus}");
                }
                if (bm.PendingThorn > 0)
                {
                    player.CharacterStats.ApplyStatus(NueGames.NueDeck.Scripts.Enums.StatusType.Thorn, bm.PendingThorn);
                    Debug.Log($"[Combat] Applied PendingThorn: +{bm.PendingThorn}");
                }
                if (bm.PendingBlockStart > 0)
                {
                    player.CharacterStats.ApplyStatus(NueGames.NueDeck.Scripts.Enums.StatusType.Block, bm.PendingBlockStart);
                    Debug.Log($"[Combat] Applied PendingBlockStart: +{bm.PendingBlockStart}");
                }
            }

            // 给敌人施加减益
            foreach (var enemy in CurrentEnemiesList)
            {
                if (enemy == null || enemy.CharacterStats == null) continue;

                if (bm.PendingEnemyWeak > 0)
                {
                    enemy.CharacterStats.ApplyStatus(NueGames.NueDeck.Scripts.Enums.StatusType.Weak, bm.PendingEnemyWeak);
                    Debug.Log($"[Combat] Applied PendingEnemyWeak: +{bm.PendingEnemyWeak} to {enemy.EnemyCharacterData?.CharacterName}");
                }
                if (bm.PendingEnemyFrail > 0)
                {
                    enemy.CharacterStats.ApplyStatus(NueGames.NueDeck.Scripts.Enums.StatusType.Frail, bm.PendingEnemyFrail);
                    Debug.Log($"[Combat] Applied PendingEnemyFrail: +{bm.PendingEnemyFrail}");
                }
                if (bm.PendingEnemyVulnerable > 0)
                {
                    enemy.CharacterStats.ApplyStatus(NueGames.NueDeck.Scripts.Enums.StatusType.Vulnerable, bm.PendingEnemyVulnerable);
                    Debug.Log($"[Combat] Applied PendingEnemyVulnerable: +{bm.PendingEnemyVulnerable}");
                }
                if (bm.PendingEnemyHpReduce > 0)
                {
                    var reduceAmount = Mathf.RoundToInt(enemy.CharacterStats.MaxHealth * bm.PendingEnemyHpReduce / 100f);
                    enemy.CharacterStats.Damage(reduceAmount);
                    Debug.Log($"[Combat] Applied PendingEnemyHpReduce: -{reduceAmount}HP ({bm.PendingEnemyHpReduce}%) to {enemy.EnemyCharacterData?.CharacterName}");
                }
            }

            // 清零所有Pending
            bm.PendingStrengthBonus = 0;
            bm.PendingDexterityBonus = 0;
            bm.PendingEnemyWeak = 0;
            bm.PendingEnemyFrail = 0;
            bm.PendingEnemyVulnerable = 0;
            bm.PendingThorn = 0;
            bm.PendingBlockStart = 0;
            bm.PendingEnemyHpReduce = 0;
        }

        private IEnumerator StartCombatWithDialogue()
        {
            // 图鉴解锁所有遭遇敌人
            try
            {
                var codex = this.GetSystem<IEnemyCodexSystem>();
                foreach (var enemy in CurrentEnemiesList)
                {
                    if (enemy.EnemyCharacterData != null)
                        codex.OnEncounter(enemy.EnemyCharacterData.CharacterID);
                }
            }
            catch { /* QFramework not ready */ }

            // 显示战前对话（取第一个有对话的敌人）
            string dialogue = null;
            string enemyName = null;
            foreach (var enemy in CurrentEnemiesList)
            {
                if (enemy.EnemyCharacterData != null && !string.IsNullOrEmpty(enemy.EnemyCharacterData.EncounterDialogue))
                {
                    dialogue = enemy.EnemyCharacterData.EncounterDialogue;
                    enemyName = enemy.EnemyCharacterData.CharacterName;
                    break;
                }
            }

            if (!string.IsNullOrEmpty(dialogue))
            {
                // 创建对话面板
                var panelObj = new GameObject("EncounterDialoguePanel");
                var panel = panelObj.AddComponent<EncounterDialoguePanel>();
                bool dialogueComplete = false;
                panel.Init(enemyName, dialogue, "开战", () => { dialogueComplete = true; });

                // 等待玩家点击继续
                while (!dialogueComplete)
                    yield return null;
            }

            // 开始战斗
            CurrentCombatStateType = CombatStateType.AllyTurn;
        }
        
        private void ExecuteCombatState(CombatStateType targetStateType)
        {
            var battleModel = this.GetModel<IBattleModel>();
            switch (targetStateType)
            {
                case CombatStateType.PrepareCombat:
                    break;
                case CombatStateType.AllyTurn:

                    OnAllyTurnStarted?.Invoke();
                    
                    if (CurrentMainAlly.CharacterStats.IsStunned)
                    {
                        EndTurn();
                        return;
                    }
                    
                    battleModel.CurrentMana.Value = battleModel.MaxMana.Value;
                   
                    this.GetSystem<ICardSystem>().DrawCards(battleModel.DrawCount.Value);
                    
                    battleModel.CanSelectCards.Value = true;
                    UIManager.CombatCanvas.SetPileTexts();

                    // 遗物触发：回合开始
                    this.GetSystem<IRelicSystem>().TriggerRelics(RelicTriggerType.OnTurnStart,
                        new RelicTriggerContext(player: CurrentMainAlly, enemies: CurrentEnemiesList));
                    
                    // 刷新手牌卡牌数值（反映当前buff加成）
                    RefreshHandCardDisplay();
                    
                    break;
                case CombatStateType.EnemyTurn:

                    OnEnemyTurnStarted?.Invoke();
                    
                    this.GetSystem<ICardSystem>().DiscardHand();

                    // 遗物触发：回合结束（玩家回合结束时）
                    this.GetSystem<IRelicSystem>().TriggerRelics(RelicTriggerType.OnTurnEnd,
                        new RelicTriggerContext(player: CurrentMainAlly, enemies: CurrentEnemiesList));
                    
                    StartCoroutine(nameof(EnemyTurnRoutine));
                    
                    battleModel.CanSelectCards.Value = false;
                    UIManager.CombatCanvas.SetPileTexts();
                    
                    break;
                case CombatStateType.EndCombat:
                    
                    battleModel.CanSelectCards.Value = false;
                    battleModel.CanUseCards.Value = false;
                    
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(targetStateType), targetStateType, null);
            }
        }
        #endregion

        #region Public Methods
        public void EndTurn()
        {
            // 玩家回合结束：结算玩家身上的状态衰减（中毒扣血/虚弱递减等）
            OnAllyTurnEnded?.Invoke();
            // buff变化后刷新卡牌数值
            RefreshHandCardDisplay();
            CurrentCombatStateType = CombatStateType.EnemyTurn;
        }

        /// <summary>刷新手牌中所有卡牌的显示数值（反映当前buff加成）</summary>
        public void RefreshHandCardDisplay()
        {
            if (CollectionManager == null || CollectionManager.HandController == null) return;
            foreach (var card in CollectionManager.HandController.hand)
            {
                if (card != null) card.UpdateCardText();
            }
        }
        public void OnAllyDeath(AllyBase targetAlly)
        {
            var targetAllyData = GameManager.PersistentGameplayData.AllyList.Find(x =>
                x.AllyCharacterData.CharacterID == targetAlly.AllyCharacterData.CharacterID);
            if (GameManager.PersistentGameplayData.AllyList.Count>1)
                GameManager.PersistentGameplayData.AllyList.Remove(targetAllyData);
            CurrentAlliesList.Remove(targetAlly);
            UIManager.InformationCanvas.ResetCanvas();
            if (CurrentAlliesList.Count<=0)
                LoseCombat();
        }
        public void OnEnemyDeath(EnemyBase targetEnemy)
        {
            CurrentEnemiesList.Remove(targetEnemy);

            // 遗物触发：敌人死亡
            this.GetSystem<IRelicSystem>().TriggerRelics(RelicTriggerType.OnEnemyDeath,
                new RelicTriggerContext(player: CurrentMainAlly, enemies: CurrentEnemiesList));

            if (CurrentEnemiesList.Count<=0)
                WinCombat();
        }
        public void DeactivateCardHighlights()
        {
            foreach (var currentEnemy in CurrentEnemiesList)
                currentEnemy.EnemyCanvas.SetHighlight(false);

            foreach (var currentAlly in CurrentAlliesList)
                currentAlly.AllyCanvas.SetHighlight(false);
        }
        public void IncreaseMana(int target)
        {
            this.GetModel<IBattleModel>().CurrentMana.Value += target;
            UIManager.CombatCanvas.SetPileTexts();
        }
        public void HighlightCardTarget(ActionTargetType targetTypeTargetType)
        {
            switch (targetTypeTargetType)
            {
                case ActionTargetType.Enemy:
                    foreach (var currentEnemy in CurrentEnemiesList)
                        currentEnemy.EnemyCanvas.SetHighlight(true);
                    break;
                case ActionTargetType.Ally:
                    foreach (var currentAlly in CurrentAlliesList)
                        currentAlly.AllyCanvas.SetHighlight(true);
                    break;
                case ActionTargetType.AllEnemies:
                    foreach (var currentEnemy in CurrentEnemiesList)
                        currentEnemy.EnemyCanvas.SetHighlight(true);
                    break;
                case ActionTargetType.AllAllies:
                    foreach (var currentAlly in CurrentAlliesList)
                        currentAlly.AllyCanvas.SetHighlight(true);
                    break;
                case ActionTargetType.RandomEnemy:
                    foreach (var currentEnemy in CurrentEnemiesList)
                        currentEnemy.EnemyCanvas.SetHighlight(true);
                    break;
                case ActionTargetType.RandomAlly:
                    foreach (var currentAlly in CurrentAlliesList)
                        currentAlly.AllyCanvas.SetHighlight(true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(targetTypeTargetType), targetTypeTargetType, null);
            }
        }
        #endregion
        
        #region Private Methods
        private void BuildEnemies()
        {
            // 测试模式：使用强制指定的敌人（Boss 关卡不使用测试模式）
            if (CardGame.EnemyTestMode.Enabled && CardGame.EnemyTestMode.ForcedEnemies.Count > 0
                && !GameManager.PersistentGameplayData.IsFinalEncounter)
            {
                Debug.Log($"[CombatManager] Test mode active — spawning {CardGame.EnemyTestMode.ForcedEnemies.Count} forced enemies.");
                var forcedList = CardGame.EnemyTestMode.ForcedEnemies;
                var typeCounter = new System.Collections.Generic.Dictionary<string, int>();
                for (var i = 0; i < forcedList.Count && i < EnemyPosList.Count; i++)
                {
                    var enemyData = forcedList[i];
                    if (enemyData == null || enemyData.EnemyPrefab == null) continue;
                    var clone = Instantiate(enemyData.EnemyPrefab, EnemyPosList[i]);
                    var dataField = typeof(EnemyBase).GetField("enemyCharacterData", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (dataField != null) dataField.SetValue(clone, enemyData);
                    clone.BuildCharacter();
                    // 相同敌人分配不同技能起始偏移
                    AssignAbilityOffset(clone, enemyData, typeCounter);
                    ApplyDynamicScaling(clone, forcedList.Count);
                    ApplyDifficultyMultiplier(clone);
                    ApplyEnemySprite(clone, enemyData);
                    CurrentEnemiesList.Add(clone);
                }

                // 测试模式没有随机遭遇 → 用强制敌人列表构建CurrentEncounter，供胜利对话使用
                CurrentEncounter = new EnemyEncounter();
                var encounterField = typeof(EnemyEncounter).GetField("enemyList", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (encounterField != null)
                    encounterField.SetValue(CurrentEncounter, new List<EnemyCharacterData>(forcedList));
                return;
            }

            // 按节点类型从对应池子抽取遭遇（不重复）
            var encounterModel = this.GetModel<IEncounterModel>();
            var battleModel = this.GetModel<IBattleModel>();
            var currentCombatNodeType = battleModel.CurrentCombatNodeType;
            var encounterPool = GameManager.EncounterData.GetEncounterPool(
                GameManager.PersistentGameplayData.CurrentStageId,
                currentCombatNodeType);

            if (encounterPool == null || encounterPool.Count == 0)
            {
                // 兜底：旧的随机逻辑
                CurrentEncounter = GameManager.EncounterData.GetEnemyEncounter(
                    GameManager.PersistentGameplayData.CurrentStageId,
                    GameManager.PersistentGameplayData.CurrentEncounterId,
                    GameManager.PersistentGameplayData.IsFinalEncounter);
            }
            else
            {
                bool isElite = currentCombatNodeType == MapNodeType.Elite;
                bool isBoss = currentCombatNodeType == MapNodeType.Boss;
                var usedSet = isElite ? encounterModel.UsedEliteEncounters : encounterModel.UsedNormalEncounters;

                // 过滤未用遭遇
                var available = new List<int>();
                for (int idx = 0; idx < encounterPool.Count; idx++)
                {
                    if (!usedSet.Contains(idx))
                        available.Add(idx);
                }

                // 全部用完则重置
                if (available.Count == 0)
                {
                    usedSet.Clear();
                    for (int idx = 0; idx < encounterPool.Count; idx++)
                        available.Add(idx);
                }

                int chosenIdx = available[UnityEngine.Random.Range(0, available.Count)];
                CurrentEncounter = encounterPool[chosenIdx];

                // 标记已用（Boss不标记，可重复）
                if (!isBoss)
                    usedSet.Add(chosenIdx);

                Debug.Log($"[CombatManager] 遭遇池={currentCombatNodeType}, 选中#{chosenIdx}, 剩余{available.Count - 1}");
            }
            
            var enemyList = CurrentEncounter.EnemyList;
            var typeCounter2 = new System.Collections.Generic.Dictionary<string, int>();

            // 按生成模式展开敌人列表
            var spawnList = new List<EnemyCharacterData>();
            foreach (var enemyData in enemyList)
            {
                if (enemyData == null) continue;
                switch (enemyData.SpawnType)
                {
                    case EnemySpawnType.Solo:
                        spawnList.Add(enemyData);
                        break;
                    case EnemySpawnType.Multiple:
                        for (int j = 0; j < enemyData.SpawnCount; j++)
                            spawnList.Add(enemyData);
                        break;
                    case EnemySpawnType.Commander:
                        spawnList.Add(enemyData);
                        // 从属(WithCommander模式)也加入生成列表
                        if (!string.IsNullOrEmpty(enemyData.SubordinateId))
                        {
                            var subData = FindEnemyDataById(enemyData.SubordinateId);
                            if (subData != null && subData.SpawnType == EnemySpawnType.Subordinate
                                && subData.SubordinateMode == SubordinateSpawnMode.WithCommander)
                            {
                                for (int j = 0; j < subData.SpawnCount; j++)
                                    spawnList.Add(subData);
                            }
                        }
                        break;
                    case EnemySpawnType.Subordinate:
                        // SummonByCommander 的从属跳过（由主将召唤）
                        // WithCommander 的从属已由上面的 Commander 逻辑处理
                        // 如果从属在遭遇表中单独出现（没有对应主将），按 Solo 处理
                        spawnList.Add(enemyData);
                        break;
                }
            }

            int totalSpawnCount = spawnList.Count;

            // 强度递增倍率
            int combatCount = encounterModel.CombatCount;
            float hpScale = 1f + combatCount * 0.08f;
            float dmgScale = 1f + combatCount * 0.05f;

            // 多怪平衡系数
            float groupHpMult = totalSpawnCount switch
            {
                1 => 1f,
                2 => 0.75f,
                3 => 0.6f,
                _ => 0.5f
            };

            Debug.Log($"[CombatManager] 敌人数={totalSpawnCount}, 战斗次数={combatCount}, HP×{hpScale}×{groupHpMult}(组), 伤害×{dmgScale}");

            for (var i = 0; i < totalSpawnCount; i++)
            {
                if (i >= EnemyPosList.Count) break;
                var clone = Instantiate(spawnList[i].EnemyPrefab, EnemyPosList[i]);
                // Override enemyCharacterData with the correct SO from encounter list
                var dataField = typeof(EnemyBase).GetField("enemyCharacterData", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (dataField != null) dataField.SetValue(clone, spawnList[i]);
                clone.BuildCharacter();
                
                // 赋值死亡音效
                if (spawnList[i].DeathSoundProfile != null)
                {
                    var deathField = typeof(EnemyBase).GetField("deathSoundProfileData", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    deathField?.SetValue(clone, spawnList[i].DeathSoundProfile);
                }
                
                // 相同敌人分配不同技能起始偏移
                AssignAbilityOffset(clone, spawnList[i], typeCounter2);
                // 应用动态难度缩放（基于敌人数量 + 楼层）
                ApplyDynamicScaling(clone, totalSpawnCount);
                // 应用强度递增 + 多怪平衡
                ApplyPowerScaling(clone, hpScale * groupHpMult, dmgScale);
                // 应用选图难度倍率
                ApplyDifficultyMultiplier(clone);
                ApplyEnemySprite(clone, spawnList[i]);
                
                CurrentEnemiesList.Add(clone);
            }

            // 快照：记录所有敌人数据，供掉落使用（敌人死亡后从CurrentEnemiesList移除）
            _combatEnemySnapshot.Clear();
            foreach (var e in CurrentEnemiesList)
                if (e != null && e.EnemyCharacterData != null)
                    _combatEnemySnapshot.Add(e.EnemyCharacterData);
        }

        /// <summary>应用强度递增+多怪平衡倍率</summary>
        private void ApplyPowerScaling(EnemyBase enemy, float hpMult, float dmgMult)
        {
            if (enemy.CharacterStats == null) return;
            int baseMaxHp = enemy.CharacterStats.MaxHealth;
            int newMaxHp = Mathf.RoundToInt(baseMaxHp * hpMult);
            enemy.CharacterStats.IncreaseMaxHealth(newMaxHp - baseMaxHp);
            enemy.CharacterStats.SetCurrentHealth(newMaxHp);
            // 伤害倍率通过修改敌人技能值实现比较复杂，暂存在 enemy 上供后续使用
            // 当前先只改HP，伤害递增待后续实现
        }

        /// <summary>根据敌人ID查找EnemyCharacterData（编辑器和打包都可用）</summary>
        private EnemyCharacterData FindEnemyDataById(string enemyId)
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
            var allEnemies = Resources.LoadAll<EnemyCharacterData>("Data/Enemies");
            foreach (var ed in allEnemies)
                if (ed.name == enemyId) return ed;
            return null;
#endif
        }
        private void BuildAllies()
        {
            for (var i = 0; i < GameManager.PersistentGameplayData.AllyList.Count; i++)
            {
                var clone = Instantiate(GameManager.PersistentGameplayData.AllyList[i], AllyPosList.Count >= i ? AllyPosList[i] : AllyPosList[0]);
                clone.BuildCharacter();

                // 替换玩家立绘为新游戏时选择的形象
                ApplyPlayerPortrait(clone);

                CurrentAlliesList.Add(clone);
            }
        }

        /// <summary>用新游戏时选择的立绘替换玩家sprite</summary>
        private void ApplyPlayerPortrait(AllyBase ally)
        {
            if (!PlayerPrefs.HasKey("SelectedPortraitIndex")) return;
            int index = PlayerPrefs.GetInt("SelectedPortraitIndex", -1);
            if (index < 0) return;

            // 加载玩家立绘
            Sprite portrait = null;
#if UNITY_EDITOR
            var guids = UnityEditor.AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/NueGames/NueDeck/Sprites/PlayerPortraits" });
            if (index < guids.Length)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[index]);
                portrait = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
#else
            var sprites = Resources.LoadAll<Sprite>("PlayerPortraits");
            if (index < sprites.Length) portrait = sprites[index];
#endif
            if (portrait == null) return;

            // 找到玩家SpriteRenderer并替换
            var sr = ally.GetComponentInChildren<SpriteRenderer>(true);
            if (sr != null)
            {
                sr.sprite = portrait;
                var texW = portrait.texture.width;
                var texH = portrait.texture.height;
                float targetHeight = 1.0f; // 缩小到原来的2/3
                float scale = targetHeight / (texH / 100f);
                sr.transform.localScale = new Vector3(scale, scale, 1);
                sr.transform.localPosition = new Vector3(0, 0, -1);
                Debug.Log($"[PlayerPortrait] Replaced with {portrait.name}, scale={scale}");
            }
        }
        private void LoseCombat()
        {
            if (CurrentCombatStateType == CombatStateType.EndCombat) return;

            // BattleLauncher格子战斗失败：清除待处理状态（死亡流程照常走）
            CardGame.BattleLauncher.HandleBattleLose();

            this.GetSystem<IBattleSystem>().LoseCombat();
            CurrentCombatStateType = CombatStateType.EndCombat;
            
            this.GetSystem<ICardSystem>().DiscardHand();
            this.GetSystem<ICardSystem>().ClearPiles();

            // 触发死亡系统
            this.GetSystem<CardGame.IEvacuateSystem>().OnDeath();

            // 打开死亡UI
            var existingDeath = GameObject.Find("DeathCanvas");
            if (existingDeath != null)
            {
                var deathCtrl = existingDeath.GetComponent<CardGame.UI.DeathUIController>();
                if (deathCtrl != null) deathCtrl.ShowDeath();
                existingDeath.SetActive(true);
            }
            else
            {
                // 从 Prefab 加载
                var prefab = Resources.Load<GameObject>("UI/DeathCanvas");
                if (prefab == null)
                    prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                        "Assets/NueGames/NueDeck/Prefabs/UI/DeathCanvas.prefab");
                if (prefab != null)
                {
                    var instance = Instantiate(prefab);
                    instance.name = "DeathCanvas";
                    var deathCtrl = instance.GetComponent<CardGame.UI.DeathUIController>();
                    if (deathCtrl != null) deathCtrl.ShowDeath();
                    Debug.Log("[CombatManager] Death UI shown");
                }
                else
                {
                    // fallback: 使用原有的 CombatLosePanel
                    UIManager.CombatCanvas.gameObject.SetActive(true);
                    UIManager.CombatCanvas.CombatLosePanel.SetActive(true);
                }
            }
        }
        private void WinCombat()
        {
            if (CurrentCombatStateType == CombatStateType.EndCombat) return;

            this.GetSystem<IBattleSystem>().WinCombat();
            CurrentCombatStateType = CombatStateType.EndCombat;

            // 战斗次数+1（用于后续敌人强度递增）——格子地图战斗不计入StS冒险进度
            if (!CardGame.BattleLauncher.HasPendingBattle)
            {
                var encounterModel = this.GetModel<IEncounterModel>();
                encounterModel.CombatCount++;
            }
           
            foreach (var allyBase in CurrentAlliesList)
            {
                GameManager.PersistentGameplayData.SetAllyHealthData(allyBase.AllyCharacterData.CharacterID,
                    allyBase.CharacterStats.CurrentHealth, allyBase.CharacterStats.MaxHealth);
            }
            
            this.GetSystem<ICardSystem>().ClearPiles();

            // 战斗结束：移除耐久归零的法宝
            this.GetSystem<IRelicSystem>().RemoveBrokenRelics();

            // 显示战胜对话
            StartCoroutine(ShowVictoryDialogueThenReward());
        }

        private IEnumerator ShowVictoryDialogueThenReward()
        {
            // 销毁可能残留的战前对话面板，避免遮挡胜利面板
            var encounterPanel = GameObject.Find("EncounterDialoguePanel");
            if (encounterPanel != null) Destroy(encounterPanel);

            // 查找有战胜对话的敌人（已死亡的敌人数据仍保留在CurrentEncounter中）
            string dialogue = null;
            string enemyName = null;
            if (CurrentEncounter != null)
            {
                foreach (var enemyData in CurrentEncounter.EnemyList)
                {
                    if (enemyData != null && !string.IsNullOrEmpty(enemyData.VictoryDialogue))
                    {
                        dialogue = enemyData.VictoryDialogue;
                        enemyName = enemyData.CharacterName;
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(dialogue))
            {
                var panelObj = new GameObject("VictoryDialoguePanel");
                var panel = panelObj.AddComponent<EncounterDialoguePanel>();
                bool dialogueComplete = false;
                panel.Init(enemyName, dialogue, "继续", () => { dialogueComplete = true; });

                while (!dialogueComplete)
                    yield return null;
            }

            // 继续原有的胜利流程
            CurrentMainAlly.CharacterStats.ClearAllStatus();
           
            if (GameManager.PersistentGameplayData.IsFinalEncounter)
            {
                UIManager.CombatCanvas.CombatWinPanel.SetActive(true);
            }
            else
            {
                // 格子地图战斗不推进StS遭遇进度
                if (!CardGame.BattleLauncher.HasPendingBattle)
                    GameManager.PersistentGameplayData.CurrentEncounterId++;
                UIManager.CombatCanvas.gameObject.SetActive(false);
                UIManager.RewardCanvas.gameObject.SetActive(true);
                UIManager.RewardCanvas.PrepareCanvas();
                UIManager.RewardCanvas.BuildReward(RewardType.Gold);
                UIManager.RewardCanvas.BuildReward(RewardType.Card);

                // 战斗胜利后掉落材料+配方
                DropLoot();

                // 在奖励界面显示材料掉落
                UIManager.RewardCanvas.BuildReward(RewardType.Material);

                // 自动存档
                CardGame.SaveSystem.Save();
            }
        }

        /// <summary>
        /// <summary>
        /// 获取当前选中的难度配置
        /// </summary>
        private AdventureDifficulty GetCurrentDifficulty()
        {
            try
            {
                var advModel = this.GetModel<IAdventureModel>();
                if (string.IsNullOrEmpty(advModel.SelectedMapId)) return null;
#if UNITY_EDITOR
                var config = UnityEditor.AssetDatabase.LoadAssetAtPath<AdventureMapConfig>(
                    "Assets/NueGames/NueDeck/Data/AdventureMaps/AdventureMapConfig.asset");
#else
                var config = Resources.Load<AdventureMapConfig>("AdventureMapConfig");
#endif
                if (config == null) return null;
                var mapData = config.GetMap(advModel.SelectedMapId);
                if (mapData == null) return null;
                return mapData.difficulties.Find(d => d.difficultyType == advModel.SelectedDifficulty);
            }
            catch { return null; }
        }

        /// <summary>
        /// 应用难度倍率到敌人
        /// </summary>
        private void ApplyDifficultyMultiplier(EnemyBase enemy)
        {
            var diff = GetCurrentDifficulty();
            if (diff == null) return;

            if (enemy.CharacterStats != null)
            {
                int baseMaxHp = enemy.CharacterStats.MaxHealth;
                int baseCurrentHp = enemy.CharacterStats.CurrentHealth;
                int newMaxHp = Mathf.RoundToInt(baseMaxHp * diff.enemyHpMultiplier);
                int newCurrentHp = Mathf.RoundToInt(baseCurrentHp * diff.enemyHpMultiplier);
                enemy.CharacterStats.IncreaseMaxHealth(newMaxHp - baseMaxHp);
                enemy.CharacterStats.SetCurrentHealth(newCurrentHp);
                
                Debug.Log($"[Difficulty] {enemy.EnemyCharacterData?.CharacterName}: HP {baseMaxHp}→{newMaxHp} (×{diff.enemyHpMultiplier})");
            }
        }

        /// <summary>
        /// 动态难度缩放（StS 规则）：
        /// 1. 多敌人时降低个体 HP（避免血量总和过高）
        /// 2. 楼层越高 HP 小幅增长
        /// 3. 伤害不变（多敌人 = 更多行动 = 更高威胁，靠数量而非数值）
        /// </summary>
        private void ApplyDynamicScaling(EnemyBase enemy, int enemyCount)
        {
            if (enemy.CharacterStats == null) return;

            int baseHP = enemy.CharacterStats.MaxHealth;
            float hpMult = 1f;

            // StS 规则：多敌人时个体 HP 降低
            // 1个: 100%, 2个: 75%, 3个: 60%, 4+: 50%
            switch (enemyCount)
            {
                case 1: hpMult = 1.0f; break;
                case 2: hpMult = 0.75f; break;
                case 3: hpMult = 0.60f; break;
                default: hpMult = 0.50f; break;
            }

            // 楼层加成：每5层 HP +5%（上限 +50%）
            int floor = GameManager.PersistentGameplayData.CurrentStageId;
            float floorBonus = Mathf.Min(floor * 0.05f, 0.5f);
            hpMult += floorBonus;

            int newHP = Mathf.RoundToInt(baseHP * hpMult);
            if (newHP < 1) newHP = 1;
            enemy.CharacterStats.IncreaseMaxHealth(newHP - baseHP);
            enemy.CharacterStats.SetCurrentHealth(newHP);

            Debug.Log($"[Dynamic] {enemy.EnemyCharacterData?.CharacterName}: count={enemyCount}, floor={floor}, HP {baseHP}→{newHP} (×{hpMult:F2})");
        }

        /// <summary>
        /// 为相同类型的敌人分配不同的技能起始偏移。
        /// 例：3个相同敌人各有3个技能，分别从技能0/1/2开始轮转，
        /// 这样同一回合不会3个敌人同时放同一个技能。
        /// </summary>
        private void AssignAbilityOffset(EnemyBase enemy, EnemyCharacterData enemyData,
            System.Collections.Generic.Dictionary<string, int> typeCounter)
        {
            if (enemyData == null) return;
            string key = enemyData.CharacterID ?? enemyData.name;

            if (!typeCounter.ContainsKey(key))
                typeCounter[key] = 0;

            int offset = typeCounter[key];
            typeCounter[key]++;

            if (offset > 0)
            {
                enemy.SetAbilityStartOffset(offset);
                Debug.Log($"[AbilityOffset] {enemyData.CharacterName} #{offset + 1}: starts from ability index {offset}");
            }
        }

        /// <summary>
        /// 替换敌人战斗精灵为 enemyPortrait（如果有），调整大小和朝向
        /// </summary>
        private void ApplyEnemySprite(EnemyBase enemy, EnemyCharacterData enemyData)
        {
            if (enemyData == null || enemyData.EnemyPortrait == null) return;

            var sr = enemy.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = enemyData.EnemyPortrait;

                var t = sr.transform;
                float targetHeight = 1.0f;
                float spriteHeight = enemyData.EnemyPortrait.rect.height / enemyData.EnemyPortrait.pixelsPerUnit;
                float scale = targetHeight / spriteHeight;
                // 应用体型缩放
                scale *= enemyData.BodyScale;
                // 朝左
                t.localScale = new Vector3(-scale, scale, 1f);

                Debug.Log($"[EnemySprite] {enemyData.CharacterName}: scale={scale}, bodyScale={enemyData.BodyScale}, height={spriteHeight}");
            }
        }

        /// <summary>
        /// 战斗胜利后根据敌人品阶掉落材料+配方
        /// </summary>
        private void DropLoot()
        {
            try
            {
                var craftSystem = this.GetSystem<ICraftSystem>();
                var inventorySystem = this.GetSystem<IInventorySystem>();
                if (craftSystem == null || inventorySystem == null) return;

                // 获取难度倍率
                var diff = GetCurrentDifficulty();
                float lootMult = diff?.lootMultiplier ?? 1f;
                int goldMult = diff?.goldRewardMultiplier ?? 1;

                var allMaterials = CardGame.ResourceCache.GetMaterials();
                var allRecipes = CardGame.ResourceCache.GetRecipes().FindAll(r => !r.unlockByDefault && !craftSystem.IsRecipeUnlocked(r.recipeId));

                LastDroppedMaterials.Clear();

                // 每个敌人掉自己的妖兽材料（仿怪物猎人掉率）
                foreach (var enemyData in _combatEnemySnapshot)
                {
                    var enemyId = enemyData.name; // SO资产名

                    // 找这个敌人的妖兽材料
                    var beastMats = allMaterials.FindAll(m => m.sourceEnemyId == enemyId);
                    if (beastMats.Count == 0) continue;

                    // 掉落次数：Normal 3次, Elite 5次, Boss 8次
                    int rollCount = enemyData.EnemyTier == EnemyTier.Boss ? 8
                                  : enemyData.EnemyTier == EnemyTier.Elite ? 5 : 3;
                    rollCount = Mathf.RoundToInt(rollCount * lootMult);

                    // 已掉落的部位记录（同部位重复掉率递减）
                    var droppedCounts = new System.Collections.Generic.Dictionary<string, int>();

                    for (int i = 0; i < rollCount; i++)
                    {
                        // 加权随机选部位
                        var weightedMats = new System.Collections.Generic.List<(MaterialData mat, float weight)>();
                        float totalWeight = 0;
                        foreach (var bm in beastMats)
                        {
                            // 基础权重
                            float w = bm.dropWeight;
                            // 已掉落过的部位权重递减：每掉1次权重×0.6
                            string key = bm.materialType.ToString();
                            if (droppedCounts.ContainsKey(key))
                                w *= Mathf.Pow(0.6f, droppedCounts[key]);
                            weightedMats.Add((bm, w));
                            totalWeight += w;
                        }

                        if (totalWeight <= 0) break;

                        // 随机选
                        float roll = UnityEngine.Random.Range(0f, totalWeight);
                        float accum = 0;
                        MaterialData chosen = null;
                        foreach (var (mat2, w2) in weightedMats)
                        {
                            accum += w2;
                            if (roll <= accum) { chosen = mat2; break; }
                        }
                        if (chosen == null) continue;

                        // 掉落数量
                        int count = UnityEngine.Random.Range(chosen.minDropCount, chosen.maxDropCount + 1);

                        inventorySystem.AddItem(chosen, count);
                        for (int j = 0; j < count; j++) LastDroppedMaterials.Add(chosen);

                        // 记录该部位掉落次数
                        string typeKey = chosen.materialType.ToString();
                        if (!droppedCounts.ContainsKey(typeKey)) droppedCounts[typeKey] = 0;
                        droppedCounts[typeKey]++;

                        Debug.Log($"[掉落] {enemyData.CharacterName} → {chosen.name} ×{count} (weight={chosen.dropWeight}, 第{droppedCounts[typeKey]}次)");
                    }
                }

                // 非妖兽材料掉落：少量额外材料
                bool hasElite2 = false;
                bool hasBoss2 = false;
                foreach (var enemyData in _combatEnemySnapshot)
                {
                    if (enemyData.EnemyTier == EnemyTier.Elite) hasElite2 = true;
                    if (enemyData.EnemyTier == EnemyTier.Boss) hasBoss2 = true;
                }
                int extraMats = hasBoss2 ? 2 : hasElite2 ? 1 : 0;
                extraMats = Mathf.RoundToInt(extraMats * lootMult);
                if (extraMats > 0)
                {
                    var nonBeastMats = allMaterials.FindAll(m => string.IsNullOrEmpty(m.sourceEnemyId));
                    for (int i = 0; i < extraMats && nonBeastMats.Count > 0; i++)
                    {
                        var mat = nonBeastMats[UnityEngine.Random.Range(0, nonBeastMats.Count)];
                        inventorySystem.AddItem(mat, 1);
                        LastDroppedMaterials.Add(mat);
                        Debug.Log($"[掉落] 材料: {mat.name} ×1");
                    }
                }

                // 临时变量用于后续逻辑
                bool hasElite = hasElite2;
                bool hasBoss = hasBoss2;

                // 配方掉落：精英50%概率，Boss必掉 — 高难度增加概率
                if (allRecipes.Count > 0)
                {
                    float recipeChance = hasBoss ? 1f : (hasElite ? 0.5f : 0f);
                    // 高难度增加配方掉率
                    if (diff != null && diff.difficultyType >= DifficultyType.Hard)
                        recipeChance = Mathf.Min(1f, recipeChance + 0.2f);
                    
                    bool dropRecipe = UnityEngine.Random.value < recipeChance;
                    if (dropRecipe)
                    {
                        var recipe = allRecipes[UnityEngine.Random.Range(0, allRecipes.Count)];
                        craftSystem.UnlockRecipe(recipe.recipeId);
                        Debug.Log($"[掉落] 配方: {recipe.name} 已解锁!");
                    }
                }

                // 金币奖励倍率应用到BattleModel
                if (diff != null && goldMult > 1)
                {
                    var battleModel = this.GetModel<IBattleModel>();
                    int bonusGold = (10 + (hasBoss ? 50 : hasElite ? 30 : 0)) * (goldMult - 1);
                    battleModel.CurrentGold.Value += bonusGold;
                    GameManager.PersistentGameplayData.CurrentGold = battleModel.CurrentGold.Value;
                    Debug.Log($"[掉落] 难度金币奖励: +{bonusGold} (×{goldMult})");
                }

                // 参悟点掉落: Normal 1-2, Elite 3-5, Boss 8-10 (×难度倍率)
                var cultSystem2 = this.GetSystem<ICultivationSystem>();
                if (cultSystem2 != null)
                {
                    int basePoints = hasBoss ? UnityEngine.Random.Range(8, 11) : hasElite ? UnityEngine.Random.Range(3, 6) : UnityEngine.Random.Range(1, 3);
                    int points = Mathf.RoundToInt(basePoints * lootMult);
                    if (points > 0)
                    {
                        cultSystem2.AddComprehensionPoints(points);
                        Debug.Log($"[掉落] 参悟点: +{points} (base={basePoints}, ×{lootMult})");
                    }

                    // 神通书籍掉落: Boss 2%, Elite 0.5%, Normal 0%
                    float abilityChance = hasBoss ? 0.02f : hasElite ? 0.005f : 0f;
                    if (UnityEngine.Random.value < abilityChance)
                    {
                        var allAbilities = cultSystem2.GetAllMethodConfigs(); // not this - need ability list
                        // Pick a random unacquired ability
                        var allAbilityConfigs = UnityEngine.Resources.LoadAll<NueGames.NueDeck.Scripts.Data.Cultivation.DivineAbilityData>("");
                        if (allAbilityConfigs != null && allAbilityConfigs.Length > 0)
                        {
                            var unacquired = new System.Collections.Generic.List<NueGames.NueDeck.Scripts.Data.Cultivation.DivineAbilityData>();
                            foreach (var a in allAbilityConfigs)
                            {
                                if (!cultSystem2.GetLearnedAbilities().Exists(x => x.AbilityId == a.AbilityId))
                                    unacquired.Add(a);
                            }
                            if (unacquired.Count > 0)
                            {
                                var drop = unacquired[UnityEngine.Random.Range(0, unacquired.Count)];
                                cultSystem2.TryAcquireAbilityBook(drop.AbilityId);
                                Debug.Log($"[掉落] 神通书籍: {drop.AbilityName}!");
                            }
                        }
                    }

                    // 功法残篇掉落: Boss 1%, Elite 0.3%, Normal 0%
                    float methodChance = hasBoss ? 0.01f : hasElite ? 0.003f : 0f;
                    if (UnityEngine.Random.value < methodChance)
                    {
                        var allMethods = cultSystem2.GetAllMethodConfigs();
                        var unlearned = allMethods.FindAll(m => !cultSystem2.GetLearnedMethods().Contains(m));
                        if (unlearned.Count > 0)
                        {
                            var drop = unlearned[UnityEngine.Random.Range(0, unlearned.Count)];
                            cultSystem2.TryAcquireMethodFragment(drop.MethodId);
                            Debug.Log($"[掉落] 功法残篇: {drop.MethodName}!");
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[掉落] 异常: {e.Message}");
            }
        }
        #endregion
        
        #region Routines
        private IEnumerator EnemyTurnRoutine()
        {
            var waitDelay = new WaitForSeconds(0.1f);

            // 用索引遍历避免敌人死亡时修改集合导致异常
            for (int i = 0; i < CurrentEnemiesList.Count; i++)
            {
                if (CurrentEnemiesList[i] == null) continue;
                yield return CurrentEnemiesList[i].StartCoroutine(nameof(EnemyExample.ActionRoutine));
                yield return waitDelay;
            }

            if (CurrentCombatStateType != CombatStateType.EndCombat)
            {
                // 敌方回合结束：结算敌人身上的状态衰减（玩家施加的虚弱/易伤等递减）
                OnEnemyTurnEnded?.Invoke();
                CurrentCombatStateType = CombatStateType.AllyTurn;
            }
        }
        #endregion
    }
}