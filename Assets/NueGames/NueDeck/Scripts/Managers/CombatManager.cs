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

            // 图鉴解锁 + 战前对话
            StartCoroutine(StartCombatWithDialogue());
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
            CurrentCombatStateType = CombatStateType.EnemyTurn;
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
            CurrentEncounter = GameManager.EncounterData.GetEnemyEncounter(
                GameManager.PersistentGameplayData.CurrentStageId,
                GameManager.PersistentGameplayData.CurrentEncounterId,
                GameManager.PersistentGameplayData.IsFinalEncounter);
            
            var enemyList = CurrentEncounter.EnemyList;
            for (var i = 0; i < enemyList.Count; i++)
            {
                var clone = Instantiate(enemyList[i].EnemyPrefab, EnemyPosList.Count >= i ? EnemyPosList[i] : EnemyPosList[0]);
                // Override enemyCharacterData with the correct SO from encounter list
                var dataField = typeof(EnemyBase).GetField("enemyCharacterData", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (dataField != null) dataField.SetValue(clone, enemyList[i]);
                clone.BuildCharacter();
                
                // 应用难度倍率
                ApplyDifficultyMultiplier(clone);
                
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
                
                // 品阶提升
                var baseRarity = hasBoss ? MaterialRarity.XuanPin : hasElite ? MaterialRarity.LingPin : MaterialRarity.FanPin;
                var targetRarity = baseRarity;
                for (int i = 0; i < rarityBonus; i++)
                {
                    if (targetRarity == MaterialRarity.FanPin) targetRarity = MaterialRarity.LingPin;
                    else if (targetRarity == MaterialRarity.LingPin) targetRarity = MaterialRarity.XuanPin;
                    else if (targetRarity == MaterialRarity.XuanPin) targetRarity = MaterialRarity.XianPin;
                }
                
                var candidateMats = allMaterials.Where(m => m.rarity == targetRarity && (m.regionId == regionId || m.regionId == -1)).ToList();
                if (candidateMats.Count == 0) // 没找到指定品阶，降级
                    candidateMats = allMaterials.Where(m => m.regionId == regionId || m.regionId == -1).ToList();
                
                for (int i = 0; i < matCount && candidateMats.Count > 0; i++)
                {
                    var mat = candidateMats[UnityEngine.Random.Range(0, candidateMats.Count)];
                    inventorySystem.AddItem(mat, 1);
                    Debug.Log($"[掉落] 材料: {mat.name} ×1 (品阶:{targetRarity}, 难度倍率:×{lootMult})");
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

            foreach (var currentEnemy in CurrentEnemiesList)
            {
                yield return currentEnemy.StartCoroutine(nameof(EnemyExample.ActionRoutine));
                yield return waitDelay;
            }

            if (CurrentCombatStateType != CombatStateType.EndCombat)
                CurrentCombatStateType = CombatStateType.AllyTurn;
        }
        #endregion
    }
}