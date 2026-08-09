using CardGame.Audio;
using NueGames.NueDeck.Scripts.Characters;
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

            // 脆弱：获得的格挡倍率从BuffDatabase读取
            var blockMult = CharacterStats.GetBlockMult(actionParameters.SelfCharacter.CharacterStats);
            if (!Mathf.Approximately(blockMult, 1f))
                blockValue = Mathf.RoundToInt(blockValue * blockMult);

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