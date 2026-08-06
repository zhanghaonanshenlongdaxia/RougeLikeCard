using NueGames.NueDeck.Scripts.Enums;
using UnityEngine;

namespace NueGames.NueDeck.Scripts.Card.CardActions
{
    /// <summary>
    /// 施加反伤状态：受击时对攻击者反弹N点伤害。
    /// Thorn为永久状态（Power类型），不随回合递减。
    /// </summary>
    public class ThornAction : CardActionBase
    {
        public override CardActionType ActionType => CardActionType.Thorn;
        
        public override void DoAction(CardActionParameters actionParameters)
        {
            var newTarget = actionParameters.TargetCharacter
                ? actionParameters.TargetCharacter
                : actionParameters.SelfCharacter;
            
            if (!newTarget) return;
            
            newTarget.CharacterStats.ApplyStatus(StatusType.Thorn, Mathf.RoundToInt(actionParameters.Value));
            
            if (FxManager != null)
                FxManager.PlayFx(newTarget.transform, FxType.Debuff);
            
            if (AudioManager != null)
                AudioManager.PlayOneShot(actionParameters.CardData.AudioType);
        }
    }
}
