using NueGames.NueDeck.Scripts.Enums;
using UnityEngine;

namespace NueGames.NueDeck.Scripts.Card.CardActions
{
    public class IncreaseDexterityAction : CardActionBase
    {
        public override CardActionType ActionType => CardActionType.IncreaseDexterity;
        
        public override void DoAction(CardActionParameters actionParameters)
        {
            var newTarget = actionParameters.TargetCharacter
                ? actionParameters.TargetCharacter
                : actionParameters.SelfCharacter;
            
            if (!newTarget) return;
            
            newTarget.CharacterStats.ApplyStatus(StatusType.Dexterity, Mathf.RoundToInt(actionParameters.Value));
            
            if (FxManager != null)
                FxManager.PlayFx(newTarget.transform, FxType.Debuff);
            
            if (AudioManager != null)
                AudioManager.PlayOneShot(actionParameters.CardData.AudioType);
        }
    }
}
