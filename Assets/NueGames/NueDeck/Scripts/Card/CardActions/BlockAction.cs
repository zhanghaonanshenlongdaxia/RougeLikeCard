using CardGame.Audio;
using NueGames.NueDeck.Scripts.Enums;
using NueGames.NueDeck.Scripts.Managers;
using UnityEngine;

namespace NueGames.NueDeck.Scripts.Card.CardActions
{
    public class BlockAction : CardActionBase
    {
        public override CardActionType ActionType => CardActionType.Block;
        public override void DoAction(CardActionParameters actionParameters)
        {
            var newTarget = actionParameters.TargetCharacter
                ? actionParameters.TargetCharacter
                : actionParameters.SelfCharacter;
            
            if (!newTarget) return;

            var blockValue = Mathf.RoundToInt(actionParameters.Value + actionParameters.SelfCharacter.CharacterStats
                    .StatusDict[StatusType.Dexterity].StatusValue);

            // 脆弱：获得的格挡减少25%
            if (actionParameters.SelfCharacter.CharacterStats.StatusDict[StatusType.Frail].IsActive)
                blockValue = Mathf.RoundToInt(blockValue * 0.75f);

            newTarget.CharacterStats.ApplyStatus(StatusType.Block, blockValue);

            if (FxManager != null) 
                FxManager.PlayFx(newTarget.transform, FxType.Block);
            
            if (AudioManager != null) 
                AudioManager.PlayOneShot(actionParameters.CardData.AudioType);
            
            if (GameAudioManager.Instance != null)
                GameAudioManager.Instance.PlaySFX(SFXType.ShieldBlock);
        }
    }
}