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
        /// 应用功法被动属性 (MaxHP/ShenShi/Strength/Dexterity/DrawCount/MaxMana/BlockStart)
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
                    case NueGames.NueDeck.Scripts.Enums.PassiveStatType.Strength:
                        player.CharacterStats.ApplyStatus(NueGames.NueDeck.Scripts.Enums.StatusType.Strength, kv.Value);
                        break;
                    case NueGames.NueDeck.Scripts.Enums.PassiveStatType.Dexterity:
                        player.CharacterStats.ApplyStatus(NueGames.NueDeck.Scripts.Enums.StatusType.Dexterity, kv.Value);
                        break;
                    case NueGames.NueDeck.Scripts.Enums.PassiveStatType.DrawCount:
                        if (bm != null) bm.DrawCount.Value += kv.Value;
                        if (gm != null) gm.PersistentGameplayData.DrawCount += kv.Value;
                        break;
                    case NueGames.NueDeck.Scripts.Enums.PassiveStatType.MaxMana:
                        if (bm != null) bm.MaxMana.Value += kv.Value;
                        if (gm != null) gm.PersistentGameplayData.MaxMana += kv.Value;
                        break;
                    case NueGames.NueDeck.Scripts.Enums.PassiveStatType.BlockStart:
                        player.CharacterStats.ApplyStatus(NueGames.NueDeck.Scripts.Enums.StatusType.Block, kv.Value);
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

                // 测试模式没有随机遭遇 → 用当前区域的第一个普通遭遇兜底，供背景/胜利对话使用
                try
                {
                    if (GameManager.EncounterData != null &&
                        GameManager.EncounterData.EnemyEncounterList != null &&
                        GameManager.EncounterData.EnemyEncounterList.Count > 0)
                    {
                        var stage = GameManager.EncounterData.EnemyEncounterList
                            .Find(s => s.StageId == GameManager.PersistentGameplayData.CurrentStageId) ??
                            GameManager.EncounterData.EnemyEncounterList[0];
                        if (stage != null && stage.EnemyEncounterList != null && stage.EnemyEncounterList.Count > 0)
                            CurrentEncounter = stage.EnemyEncounterList[0];
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[CombatManager] Test mode fallback encounter failed: {e.Message}");
                }
                return;
            }

            CurrentEncounter = GameManager.EncounterData.GetEnemyEncounter(
                GameManager.PersistentGameplayData.CurrentStageId,
                GameManager.PersistentGameplayData.CurrentEncounterId,
                GameManager.PersistentGameplayData.IsFinalEncounter);
            
            var enemyList = CurrentEncounter.EnemyList;
            var typeCounter2 = new System.Collections.Generic.Dictionary<string, int>();
            for (var i = 0; i < enemyList.Count; i++)
            {
                var clone = Instantiate(enemyList[i].EnemyPrefab, EnemyPosList.Count >= i ? EnemyPosList[i] : EnemyPosList[0]);
                // Override enemyCharacterData with the correct SO from encounter list
                var dataField = typeof(EnemyBase).GetField("enemyCharacterData", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (dataField != null) dataField.SetValue(clone, enemyList[i]);
                clone.BuildCharacter();
                
                // 相同敌人分配不同技能起始偏移
                AssignAbilityOffset(clone, enemyList[i], typeCounter2);
                // 应用动态难度缩放（基于敌人数量 + 楼层）
                ApplyDynamicScaling(clone, enemyList.Count);
                ApplyDifficultyMultiplier(clone);
                ApplyEnemySprite(clone, enemyList[i]);
                
                CurrentEnemiesList.Add(clone);
            }
        }
        private void BuildAllies()
        {
            for (var i = 0; i < GameManager.PersistentGameplayData.AllyList.Count; i++)
            {
                var clone = Instantiate(GameManager.PersistentGameplayData.AllyList[i], AllyPosList.Count >= i ? AllyPosList[i] : AllyPosList[0]);
                clone.BuildCharacter();
                CurrentAlliesList.Add(clone);
            }
        }
        private void LoseCombat()
        {
            if (CurrentCombatStateType == CombatStateType.EndCombat) return;
            
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
                GameManager.PersistentGameplayData.CurrentEncounterId++;
                UIManager.CombatCanvas.gameObject.SetActive(false);
                UIManager.RewardCanvas.gameObject.SetActive(true);
                UIManager.RewardCanvas.PrepareCanvas();
                UIManager.RewardCanvas.BuildReward(RewardType.Gold);
                UIManager.RewardCanvas.BuildReward(RewardType.Card);

                // 战斗胜利后掉落材料+配方
                DropLoot();

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
                // HP倍率
                int baseMaxHp = enemy.CharacterStats.MaxHealth;
                int newMaxHp = Mathf.RoundToInt(baseMaxHp * diff.enemyHpMultiplier);
                enemy.CharacterStats.IncreaseMaxHealth(newMaxHp - baseMaxHp);
                enemy.CharacterStats.Damage(0); // 触发更新
                
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

                // 调整 scale：原始敌人 64x68px PPU=100 → 0.64x0.68 世界单位
                // 立绘 256x341px PPU=100 → 2.56x3.41 世界单位，scale=0.25 → 0.64x0.85
                var t = sr.transform;
                float targetHeight = 1.0f; // 接近原始敌人高度
                float spriteHeight = enemyData.EnemyPortrait.rect.height / enemyData.EnemyPortrait.pixelsPerUnit;
                float scale = targetHeight / spriteHeight;
                // 朝左（敌人面对玩家，玩家在左边）
                t.localScale = new Vector3(-scale, scale, 1f);

                Debug.Log($"[EnemySprite] {enemyData.CharacterName}: scale={scale}, height={spriteHeight}, facing left");
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
                int rarityBonus = diff?.lootRarityBonus ?? 0;
                int goldMult = diff?.goldRewardMultiplier ?? 1;

                var allMaterials = CardGame.ResourceCache.GetMaterials();
                var allRecipes = CardGame.ResourceCache.GetRecipes().FindAll(r => !r.unlockByDefault && !craftSystem.IsRecipeUnlocked(r.recipeId));

                // 根据敌人品阶决定掉落
                bool hasElite = false;
                bool hasBoss = false;
                int regionId = 0;
                foreach (var enemy in CurrentEnemiesList)
                {
                    if (enemy == null || enemy.EnemyCharacterData == null) continue;
                    if (enemy.EnemyCharacterData.EnemyTier == EnemyTier.Elite) hasElite = true;
                    if (enemy.EnemyCharacterData.EnemyTier == EnemyTier.Boss) hasBoss = true;
                    regionId = enemy.EnemyCharacterData.RegionId;
                }

                // 材料掉落 — 应用难度倍率
                int baseMatCount = hasBoss ? 3 : hasElite ? 2 : 1;
                int matCount = Mathf.RoundToInt(baseMatCount * lootMult);
                
                // 品质计算：根据敌人品阶决定基础品质，rarityBonus提升品阶
                int baseQualityVal = hasBoss ? (int)NueGames.NueDeck.Scripts.Enums.ItemQuality.JinDan_T1
                                   : hasElite ? (int)NueGames.NueDeck.Scripts.Enums.ItemQuality.ZhuJi_T1
                                   : (int)NueGames.NueDeck.Scripts.Enums.ItemQuality.LianQi_T1;
                int targetQualityVal = Mathf.Min(19, baseQualityVal + rarityBonus);
                var targetQuality = (NueGames.NueDeck.Scripts.Enums.ItemQuality)targetQualityVal;
                
                var candidateMats = allMaterials.Where(m => m.quality == targetQuality && (m.regionId == regionId || m.regionId == -1)).ToList();
                if (candidateMats.Count == 0) // 没找到指定品质，按旧rarity降级
                    candidateMats = allMaterials.Where(m => m.regionId == regionId || m.regionId == -1).ToList();
                
                for (int i = 0; i < matCount && candidateMats.Count > 0; i++)
                {
                    var mat = candidateMats[UnityEngine.Random.Range(0, candidateMats.Count)];
                    inventorySystem.AddItem(mat, 1);
                    Debug.Log($"[掉落] 材料: {mat.name} ×1 (品质:{NueGames.NueDeck.Scripts.Enums.ItemQualityHelper.GetDisplayName(targetQuality)}, 难度倍率:×{lootMult})");
                }

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