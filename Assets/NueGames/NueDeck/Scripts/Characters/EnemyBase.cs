using System.Collections;
using NueGames.NueDeck.Scripts.Data.Characters;
using NueGames.NueDeck.Scripts.Data.Containers;
using NueGames.NueDeck.Scripts.EnemyBehaviour;
using NueGames.NueDeck.Scripts.Enums;
using NueGames.NueDeck.Scripts.Interfaces;
using NueGames.NueDeck.Scripts.Managers;
using NueGames.NueDeck.Scripts.NueExtentions;
using UnityEngine;

namespace NueGames.NueDeck.Scripts.Characters
{
    public class EnemyBase : CharacterBase, IEnemy
    {
        [Header("Enemy Base References")]
        [SerializeField] protected EnemyCharacterData enemyCharacterData;
        [SerializeField] protected EnemyCanvas enemyCanvas;
        [SerializeField] protected SoundProfileData deathSoundProfileData;
        protected EnemyAbilityData NextAbility;
        
        // Multi-phase support
        private int _currentPhaseIndex = -1; // -1 = no phases or not yet triggered
        private bool _phaseChangedThisTurn;
        
        public EnemyCharacterData EnemyCharacterData => enemyCharacterData;
        public EnemyCanvas EnemyCanvas => enemyCanvas;
        public SoundProfileData DeathSoundProfileData => deathSoundProfileData;
        public int CurrentPhaseIndex => _currentPhaseIndex;

        #region Setup
        public override void BuildCharacter()
        {
            base.BuildCharacter();
            EnemyCanvas.InitCanvas();
            CharacterStats = new CharacterStats(EnemyCharacterData.MaxHealth,EnemyCanvas);
            CharacterStats.OnDeath += OnDeath;
            CharacterStats.SetCurrentHealth(CharacterStats.CurrentHealth);
            CombatManager.OnAllyTurnStarted += ShowNextAbility;
            CombatManager.OnEnemyTurnStarted += CharacterStats.TriggerAllStatus;
            
            // Subscribe to health changes for phase switching
            if (EnemyCharacterData.HasPhases)
                CharacterStats.OnHealthChanged += OnHealthChanged;
        }
        protected override void OnDeath()
        {
            base.OnDeath();
            CombatManager.OnAllyTurnStarted -= ShowNextAbility;
            CombatManager.OnEnemyTurnStarted -= CharacterStats.TriggerAllStatus;
            if (EnemyCharacterData.HasPhases)
                CharacterStats.OnHealthChanged -= OnHealthChanged;
           
            CombatManager.OnEnemyDeath(this);
            AudioManager.PlayOneShot(DeathSoundProfileData.GetRandomClip());
            Destroy(gameObject);
        }
        #endregion
        
        #region Private Methods

        private int _usedAbilityCount;
        private void ShowNextAbility()
        {
            // Get active ability list based on current phase
            var abilityList = GetActiveAbilityList();
            NextAbility = GetAbilityFromList(abilityList, _usedAbilityCount);
            EnemyCanvas.IntentImage.sprite = NextAbility.Intention.IntentionSprite;
            
            if (NextAbility.HideActionValue)
            {
                EnemyCanvas.NextActionValueText.gameObject.SetActive(false);
            }
            else
            {
                EnemyCanvas.NextActionValueText.gameObject.SetActive(true);
                EnemyCanvas.NextActionValueText.text = NextAbility.ActionList[0].ActionValue.ToString();
            }

            _usedAbilityCount++;
            EnemyCanvas.IntentImage.gameObject.SetActive(true);
            _phaseChangedThisTurn = false;
        }
        
        private System.Collections.Generic.List<EnemyAbilityData> GetActiveAbilityList()
        {
            if (!EnemyCharacterData.HasPhases)
                return EnemyCharacterData.EnemyAbilityList;
            return EnemyCharacterData.GetActiveAbilityList((float)CharacterStats.CurrentHealth / CharacterStats.MaxHealth);
        }
        
        private EnemyAbilityData GetAbilityFromList(System.Collections.Generic.List<EnemyAbilityData> list, int usedCount)
        {
            if (EnemyCharacterData.FollowAbilityPatternCheck())
            {
                var index = usedCount % list.Count;
                return list[index];
            }
            return list.RandomItem();
        }
        
        private void OnHealthChanged(int currentHealth, int maxHealth)
        {
            if (!EnemyCharacterData.HasPhases) return;
            float ratio = (float)currentHealth / maxHealth;
            
            // Check if we should advance to a new phase
            var phases = EnemyCharacterData.PhaseList;
            int newPhaseIndex = -1;
            for (int i = phases.Count - 1; i >= 0; i--)
            {
                if (ratio <= phases[i].HealthThreshold)
                {
                    newPhaseIndex = i;
                    break;
                }
            }
            
            if (newPhaseIndex > _currentPhaseIndex)
            {
                _currentPhaseIndex = newPhaseIndex;
                _usedAbilityCount = 0; // Reset pattern counter for new phase
                _phaseChangedThisTurn = true;
                var phaseName = phases[newPhaseIndex].PhaseEnterName;
                if (!string.IsNullOrEmpty(phaseName))
                    Debug.Log($"[{EnemyCharacterData.name}] 进入阶段: {phaseName}");
            }
        }
        #endregion
        
        #region Action Routines
        public virtual IEnumerator ActionRoutine()
        {
            if (CharacterStats.IsStunned)
                yield break;
            
            EnemyCanvas.IntentImage.gameObject.SetActive(false);
            if (NextAbility.Intention.EnemyIntentionType == EnemyIntentionType.Attack || NextAbility.Intention.EnemyIntentionType == EnemyIntentionType.Debuff)
            {
                yield return StartCoroutine(AttackRoutine(NextAbility));
            }
            else
            {
                yield return StartCoroutine(BuffRoutine(NextAbility));
            }
        }
        
        protected virtual IEnumerator AttackRoutine(EnemyAbilityData targetAbility)
        {
            var waitFrame = new WaitForEndOfFrame();

            if (CombatManager == null) yield break;
            
            var target = CombatManager.CurrentAlliesList.RandomItem();
            
            var startPos = transform.position;
            var endPos = target.transform.position;

            var startRot = transform.localRotation;
            var endRot = Quaternion.Euler(60, 0, 60);
            
            yield return StartCoroutine(MoveToTargetRoutine(waitFrame, startPos, endPos, startRot, endRot, 5));
          
            targetAbility.ActionList.ForEach(x=>EnemyActionProcessor.GetAction(x.ActionType).DoAction(new EnemyActionParameters(x.ActionValue,target,this)));
            
            yield return StartCoroutine(MoveToTargetRoutine(waitFrame, endPos, startPos, endRot, startRot, 5));
        }
        
        protected virtual IEnumerator BuffRoutine(EnemyAbilityData targetAbility)
        {
            var waitFrame = new WaitForEndOfFrame();
            
            var target = CombatManager.CurrentEnemiesList.RandomItem();
            
            var startPos = transform.position;
            var endPos = startPos+new Vector3(0,0.2f,0);
            
            var startRot = transform.localRotation;
            var endRot = transform.localRotation;
            
            yield return StartCoroutine(MoveToTargetRoutine(waitFrame, startPos, endPos, startRot, endRot, 5));
            
            targetAbility.ActionList.ForEach(x=>EnemyActionProcessor.GetAction(x.ActionType).DoAction(new EnemyActionParameters(x.ActionValue,target,this)));
            
            yield return StartCoroutine(MoveToTargetRoutine(waitFrame, endPos, startPos, endRot, startRot, 5));
        }
        #endregion
        
        #region Other Routines
        private IEnumerator MoveToTargetRoutine(WaitForEndOfFrame waitFrame,Vector3 startPos, Vector3 endPos, Quaternion startRot, Quaternion endRot, float speed)
        {
            var timer = 0f;
            while (true)
            {
                timer += Time.deltaTime*speed;

                transform.position = Vector3.Lerp(startPos, endPos, timer);
                transform.localRotation = Quaternion.Lerp(startRot,endRot,timer);
                if (timer>=1f)
                {
                    break;
                }

                yield return waitFrame;
            }
        }

        #endregion
    }
}