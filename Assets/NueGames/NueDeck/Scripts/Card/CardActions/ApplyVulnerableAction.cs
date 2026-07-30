using NueGames.NueDeck.Scripts.Enums;
using NueGames.NueDeck.Scripts.Managers;
using UnityEngine;

namespace NueGames.NueDeck.Scripts.Card.CardActions
{
    public class ApplyVulnerableAction : CardActionBase
    {
        public override CardActionType ActionType => CardActionType.ApplyVulnerable;

        public override void DoAction(CardActionParameters actionParameters)
        {
            var target = actionParameters.TargetCharacter
                ? actionParameters.TargetCharacter
                : actionParameters.SelfCharacter;

            if (!target) return;

            target.CharacterStats.ApplyStatus(StatusType.Vulnerable, Mathf.RoundToInt(actionParameters.Value));

            if (FxManager != null)
                FxManager.PlayFx(target.transform, FxType.Debuff);

            if (AudioManager != null)
                AudioManager.PlayOneShot(actionParameters.CardData.AudioType);
        }
    }
}
