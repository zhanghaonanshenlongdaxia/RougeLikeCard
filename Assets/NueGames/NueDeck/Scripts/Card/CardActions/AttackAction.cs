using System.Collections;
using CardGame.Audio;
using NueGames.NueDeck.Scripts.Characters;
using NueGames.NueDeck.Scripts.Enums;
using NueGames.NueDeck.Scripts.Managers;
using UnityEngine;

namespace NueGames.NueDeck.Scripts.Card.CardActions
{
    public class AttackAction: CardActionBase
    {
        public override CardActionType ActionType => CardActionType.Attack;
        public override void DoAction(CardActionParameters actionParameters)
        {
            if (!actionParameters.TargetCharacter) return;
            
            var targetCharacter = actionParameters.TargetCharacter;
            var selfCharacter = actionParameters.SelfCharacter;
            
            var baseValue = actionParameters.Value + selfCharacter.CharacterStats.StatusDict[StatusType.Strength].StatusValue; 

            // 虚弱：攻击造成的伤害倍率从BuffDatabase读取
            var damageMult = CharacterStats.GetDamageDealtMult(selfCharacter.CharacterStats);
            if (!Mathf.Approximately(damageMult, 1f))
                baseValue = Mathf.RoundToInt(baseValue * damageMult);

            // 多段攻击：每次打击独立计算伤害
            var hitCount = actionParameters.HitCount > 0 ? actionParameters.HitCount : 1;
            var totalDamage = 0;
            for (int i = 0; i < hitCount; i++)
            {
                targetCharacter.CharacterStats.Damage(Mathf.RoundToInt(baseValue), false, selfCharacter.CharacterStats);
                totalDamage += Mathf.RoundToInt(baseValue);

                if (FxManager != null)
                    FxManager.PlayFx(targetCharacter.transform, FxType.Attack);
            }

            if (FxManager != null)
                FxManager.SpawnFloatingText(targetCharacter.TextSpawnRoot, totalDamage.ToString());
           
            if (AudioManager != null) 
                AudioManager.PlayOneShot(actionParameters.CardData.AudioType);
            
            if (GameAudioManager.Instance != null)
                GameAudioManager.Instance.PlaySFX(SFXType.SwordSlash);
        }
    }
}