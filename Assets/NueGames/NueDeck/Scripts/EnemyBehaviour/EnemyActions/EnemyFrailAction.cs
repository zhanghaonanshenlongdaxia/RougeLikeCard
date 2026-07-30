using NueGames.NueDeck.Scripts.Enums;
using NueGames.NueDeck.Scripts.Managers;
using UnityEngine;

namespace NueGames.NueDeck.Scripts.EnemyBehaviour.EnemyActions
{
    public class EnemyFrailAction : EnemyActionBase
    {
        public override EnemyActionType ActionType => EnemyActionType.ApplyFrail;

        public override void DoAction(EnemyActionParameters actionParameters)
        {
            if (!actionParameters.TargetCharacter) return;

            actionParameters.TargetCharacter.CharacterStats.ApplyStatus(StatusType.Frail, Mathf.RoundToInt(actionParameters.Value));

            if (FxManager != null)
                FxManager.PlayFx(actionParameters.TargetCharacter.transform, FxType.Debuff);

            if (AudioManager != null)
                AudioManager.PlayOneShot(AudioActionType.Attack);
        }
    }
}
