using System;
using System.Collections.Generic;
using System.Linq;
using CardGame.Audio;
using NueGames.NueDeck.Scripts.Data.Containers;
using NueGames.NueDeck.Scripts.Enums;
using UnityEngine;

namespace NueGames.NueDeck.Scripts.Characters
{
    public class StatusStats
    { 
        public StatusType StatusType { get; set; }
        public int StatusValue { get; set; }
        public bool DecreaseOverTurn { get; set; } // If true, decrease on turn end
        public bool IsPermanent { get; set; } // If true, status can not be cleared during combat
        public bool IsActive { get; set; }
        public bool CanNegativeStack { get; set; }
        public bool ClearAtNextTurn { get; set; }
        
        public Action OnTriggerAction;
        public StatusStats(StatusType statusType,int statusValue,bool decreaseOverTurn = false, bool isPermanent = false,bool isActive = false,bool canNegativeStack = false,bool clearAtNextTurn = false)
        {
            StatusType = statusType;
            StatusValue = statusValue;
            DecreaseOverTurn = decreaseOverTurn;
            IsPermanent = isPermanent;
            IsActive = isActive;
            CanNegativeStack = canNegativeStack;
            ClearAtNextTurn = clearAtNextTurn;
        }
    }
    public class CharacterStats
    { 
        public int MaxHealth { get; set; }
        public int CurrentHealth { get; set; }
        public bool IsStunned { get;  set; }
        public bool IsDeath { get; private set; }
       
        /// <summary>反伤来源：受到伤害时记录攻击者，用于Thorn反弹</summary>
        public CharacterStats LastAttacker { get; set; }
       
        public Action OnDeath;
        public Action<int, int> OnHealthChanged;
        private readonly Action<StatusType,int> OnStatusChanged;
        private readonly Action<StatusType, int> OnStatusApplied;
        private readonly Action<StatusType> OnStatusCleared;
        public Action OnHealAction;
        public Action OnTakeDamageAction;
        public Action OnShieldGained;
        
        public readonly Dictionary<StatusType, StatusStats> StatusDict = new Dictionary<StatusType, StatusStats>();
        
        #region Setup
        public CharacterStats(int maxHealth, CharacterCanvas characterCanvas)
        {
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
            SetAllStatus();
            
            OnHealthChanged += characterCanvas.UpdateHealthText;
            OnStatusChanged += characterCanvas.UpdateStatusText;
            OnStatusApplied += characterCanvas.ApplyStatus;
            OnStatusCleared += characterCanvas.ClearStatus;
        }
        
        private void SetAllStatus()
        {
            for (int i = 0; i < Enum.GetNames(typeof(StatusType)).Length; i++)
                StatusDict.Add((StatusType) i, new StatusStats((StatusType) i, 0));

            // 从 BuffDatabase 读取行为配置，数据驱动
            var db = BuffDatabase.Instance;
            if (db != null)
            {
                foreach (var buff in db.AllBuffs)
                {
                    if (buff == null) continue;
                    if (!StatusDict.ContainsKey(buff.StatusType)) continue;
                    var stats = StatusDict[buff.StatusType];
                    stats.DecreaseOverTurn = buff.DecreaseOverTurn;
                    stats.IsPermanent = buff.IsPermanent;
                    stats.CanNegativeStack = buff.CanNegativeStack;
                    stats.ClearAtNextTurn = buff.ClearAtNextTurn;

                    // 特殊效果挂载 OnTriggerAction
                    switch (buff.SpecialEffect)
                    {
                        case BuffSpecialEffect.Poison:
                            stats.OnTriggerAction += DamagePoison;
                            break;
                        case BuffSpecialEffect.Stun:
                            stats.OnTriggerAction += CheckStunStatus;
                            break;
                    }
                }
            }
            else
            {
                // Fallback: 如果 BuffDatabase 未初始化，使用旧硬编码值
                Debug.LogWarning("[CharacterStats] BuffDatabase.Instance is null, using fallback hardcoded values.");
                StatusDict[StatusType.Poison].DecreaseOverTurn = true;
                StatusDict[StatusType.Poison].OnTriggerAction += DamagePoison;
                StatusDict[StatusType.Block].ClearAtNextTurn = true;
                StatusDict[StatusType.Strength].CanNegativeStack = true;
                StatusDict[StatusType.Dexterity].CanNegativeStack = true;
                StatusDict[StatusType.Stun].DecreaseOverTurn = true;
                StatusDict[StatusType.Stun].OnTriggerAction += CheckStunStatus;
                StatusDict[StatusType.Weak].DecreaseOverTurn = true;
                StatusDict[StatusType.Frail].DecreaseOverTurn = true;
                StatusDict[StatusType.Vulnerable].DecreaseOverTurn = true;
                StatusDict[StatusType.Thorn].IsPermanent = true;
            }
        }
        #endregion
        
        #region Public Methods
        public void ApplyStatus(StatusType targetStatus,int value)
        {
            if (StatusDict[targetStatus].IsActive)
            {
                StatusDict[targetStatus].StatusValue += value;
                OnStatusChanged?.Invoke(targetStatus, StatusDict[targetStatus].StatusValue);
                
            }
            else
            {
                StatusDict[targetStatus].StatusValue = value;
                StatusDict[targetStatus].IsActive = true;
                OnStatusApplied?.Invoke(targetStatus, StatusDict[targetStatus].StatusValue);
            }
        }
        public void TriggerAllStatus()
        {
            for (int i = 0; i < Enum.GetNames(typeof(StatusType)).Length; i++)
                TriggerStatus((StatusType) i);
        }
        
        public void SetCurrentHealth(int targetCurrentHealth)
        {
            CurrentHealth = targetCurrentHealth <=0 ? 1 : targetCurrentHealth;
            OnHealthChanged?.Invoke(CurrentHealth,MaxHealth);
        } 
        
        public void Heal(int value)
        {
            CurrentHealth += value;
            if (CurrentHealth>MaxHealth)  CurrentHealth = MaxHealth;
            OnHealthChanged?.Invoke(CurrentHealth,MaxHealth);
        }
        
        public void Damage(int value, bool canPierceArmor = false, CharacterStats attacker = null)
        {
            if (IsDeath) return;
            if (value <= 0) return; // 负数或0不造成伤害，避免加血bug
            OnTakeDamageAction?.Invoke();
            var remainingDamage = value;

            // 易伤：从 BuffDatabase 读取 damageTakenMult
            if (StatusDict[StatusType.Vulnerable].IsActive)
            {
                var mult = GetMultiplier(StatusType.Vulnerable, BuffField.DamageTakenMult);
                if (!Mathf.Approximately(mult, 1f))
                    remainingDamage = Mathf.RoundToInt(remainingDamage * mult);
            }

            if (!canPierceArmor)
            {
                if (StatusDict[StatusType.Block].IsActive)
                {
                    ApplyStatus(StatusType.Block,-remainingDamage);

                    remainingDamage = 0;
                    if (StatusDict[StatusType.Block].StatusValue <= 0)
                    {
                        remainingDamage = StatusDict[StatusType.Block].StatusValue * -1;
                        ClearStatus(StatusType.Block);
                    }
                }
            }
            
            CurrentHealth -= remainingDamage;
            
            // 受击音效
            if (remainingDamage > 0 && GameAudioManager.Instance != null)
                GameAudioManager.Instance.PlaySFX(SFXType.TakeDamage);
            
            // 反伤(Thorn)：如果受到实际伤害且有攻击者，反弹Thorn值的伤害给攻击者
            if (remainingDamage > 0 && attacker != null && StatusDict[StatusType.Thorn].IsActive)
            {
                var thornValue = StatusDict[StatusType.Thorn].StatusValue;
                if (thornValue > 0)
                {
                    attacker.Damage(thornValue, true);
                }
            }
            
            if (CurrentHealth <= 0)
            {
                CurrentHealth = 0;
                OnDeath?.Invoke();
                IsDeath = true;
                if (GameAudioManager.Instance != null)
                    GameAudioManager.Instance.PlaySFX(SFXType.Death);
            }
            OnHealthChanged?.Invoke(CurrentHealth,MaxHealth);
        }
        
        public void IncreaseMaxHealth(int value)
        {
            MaxHealth += value;
            OnHealthChanged?.Invoke(CurrentHealth,MaxHealth);
        }

        public void ClearAllStatus()
        {
            foreach (var status in StatusDict)
                ClearStatus(status.Key);
        }
           
        public void ClearStatus(StatusType targetStatus)
        {
            StatusDict[targetStatus].IsActive = false;
            StatusDict[targetStatus].StatusValue = 0;
            OnStatusCleared?.Invoke(targetStatus);
        }

        #endregion

        #region Private Methods
        private void TriggerStatus(StatusType targetStatus)
        {
            StatusDict[targetStatus].OnTriggerAction?.Invoke();
            
            //One turn only statuses
            if (StatusDict[targetStatus].ClearAtNextTurn)
            {
                ClearStatus(targetStatus);
                OnStatusChanged?.Invoke(targetStatus, StatusDict[targetStatus].StatusValue);
                return;
            }
            
            //Check status
            if (StatusDict[targetStatus].StatusValue <= 0)
            {
                if (StatusDict[targetStatus].CanNegativeStack)
                {
                    if (StatusDict[targetStatus].StatusValue == 0 && !StatusDict[targetStatus].IsPermanent)
                        ClearStatus(targetStatus);
                }
                else
                {
                    if (!StatusDict[targetStatus].IsPermanent)
                        ClearStatus(targetStatus);
                }
            }
            
            if (StatusDict[targetStatus].DecreaseOverTurn) 
                StatusDict[targetStatus].StatusValue--;
            
            if (StatusDict[targetStatus].StatusValue == 0)
                if (!StatusDict[targetStatus].IsPermanent)
                    ClearStatus(targetStatus);
            
            OnStatusChanged?.Invoke(targetStatus, StatusDict[targetStatus].StatusValue);
        }
        
     
        private void DamagePoison()
        {
            if (StatusDict[StatusType.Poison].StatusValue<=0) return;
            Damage(StatusDict[StatusType.Poison].StatusValue,true);
        }
        
        public void CheckStunStatus()
        {
            if (StatusDict[StatusType.Stun].StatusValue <= 0)
            {
                IsStunned = false;
                return;
            }
            
            IsStunned = true;
        }

        #endregion

        #region BuffDatabase Helpers

        private enum BuffField
        {
            DamageTakenMult,
            DamageDealtMult,
            BlockMult
        }

        /// <summary>从 BuffDatabase 读取指定buff的倍率，找不到时返回1f</summary>
        private static float GetMultiplier(StatusType type, BuffField field)
        {
            var buff = BuffDatabase.Instance?.GetBuff(type);
            if (buff == null) return 1f;
            return field switch
            {
                BuffField.DamageTakenMult => buff.DamageTakenMult,
                BuffField.DamageDealtMult => buff.DamageDealtMult,
                BuffField.BlockMult => buff.BlockMult,
                _ => 1f
            };
        }

        /// <summary>静态helper：获取攻击伤害倍率（Weak用），供AttackAction等调用</summary>
        public static float GetDamageDealtMult(CharacterStats stats)
        {
            if (stats.StatusDict[StatusType.Weak].IsActive)
                return GetMultiplier(StatusType.Weak, BuffField.DamageDealtMult);
            return 1f;
        }

        /// <summary>静态helper：获取格挡倍率（Frail用），供BlockAction等调用</summary>
        public static float GetBlockMult(CharacterStats stats)
        {
            if (stats.StatusDict[StatusType.Frail].IsActive)
                return GetMultiplier(StatusType.Frail, BuffField.BlockMult);
            return 1f;
        }

        #endregion
    }
}
