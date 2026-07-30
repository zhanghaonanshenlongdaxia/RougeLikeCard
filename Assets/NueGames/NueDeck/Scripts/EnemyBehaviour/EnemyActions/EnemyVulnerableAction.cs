using NueGames.NueDeck.Scripts.Enums;
using NueGames.NueDeck.Scripts.Managers;
using UnityEngine;

namespace NueGames.NueDeck.Scripts.EnemyBehaviour.EnemyActions
{
    public class EnemyVulnerableAction : EnemyActionBase
    {
        public override EnemyActionType ActionType => EnemyActionType.ApplyVulnerable;

        public override void DoAction(EnemyActionParameters actionParameters)
        {
            if (!actionParameters.TargetCharacter) return;

            actionParameters.TargetCharacter.CharacterStats.ApplyStatus(StatusType.Vulnerable, Mathf.RoundToInt(actionParameters.Value));

            if (FxManager != null)
                FxManager.PlayFx(actionParameters.TargetCharacter.transform, FxType.Debuff);

            if (AudioManager != null)
                AudioManager.PlayOneShot(AudioActionType.Attack);
        }
    }
}
