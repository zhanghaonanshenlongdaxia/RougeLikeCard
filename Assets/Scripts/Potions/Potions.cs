using NueGames.NueDeck.Scripts.Enums;
using UnityEngine;

namespace CardGame
{
    /// <summary>
    /// 回血药水：回复20点HP
    /// </summary>
    public class HealPotion : PotionBase
    {
        public override string PotionId => "potion_heal";

        public override void OnUse(PotionData data, PotionUseContext context)
        {
            if (context.Player == null) return;

            var heal = data.effectValue > 0 ? data.effectValue : 20;
            context.Player.CharacterStats.Heal(heal);

            if (NueGames.NueDeck.Scripts.Managers.FxManager.Instance)
                NueGames.NueDeck.Scripts.Managers.FxManager.Instance.PlayFx(context.Player.transform, FxType.Heal);

            Debug.Log($"[Potion] Healed {heal} HP");
        }
    }

    /// <summary>
    /// 力量药水：获得2点力量
    /// </summary>
    public class StrengthPotion : PotionBase
    {
        public override string PotionId => "potion_strength";

        public override void OnUse(PotionData data, PotionUseContext context)
        {
            if (context.Player == null) return;

            var str = data.effectValue > 0 ? data.effectValue : 2;
            context.Player.CharacterStats.ApplyStatus(StatusType.Strength, str);

            if (NueGames.NueDeck.Scripts.Managers.FxManager.Instance)
                NueGames.NueDeck.Scripts.Managers.FxManager.Instance.PlayFx(context.Player.transform, FxType.Str);

            Debug.Log($"[Potion] Gained {str} Strength");
        }
    }

    /// <summary>
    /// 虚弱药水：对所有敌人施加2层虚弱
    /// </summary>
    public class WeakPotion : PotionBase
    {
        public override string PotionId => "potion_weak";

        public override void OnUse(PotionData data, PotionUseContext context)
        {
            if (context.Enemies == null) return;

            var stacks = data.effectValue > 0 ? data.effectValue : 2;
            foreach (var enemy in context.Enemies)
            {
                enemy.CharacterStats.ApplyStatus(StatusType.Weak, stacks);

                if (NueGames.NueDeck.Scripts.Managers.FxManager.Instance)
                    NueGames.NueDeck.Scripts.Managers.FxManager.Instance.PlayFx(enemy.transform, FxType.Debuff);
            }

            Debug.Log($"[Potion] Applied {stacks} Weak to all enemies");
        }
    }

    /// <summary>
    /// 能量药水：获得2点能量
    /// </summary>
    public class EnergyPotion : PotionBase
    {
        public override string PotionId => "potion_energy";

        public override void OnUse(PotionData data, PotionUseContext context)
        {
            var energy = data.effectValue > 0 ? data.effectValue : 2;
            if (NueGames.NueDeck.Scripts.Managers.CombatManager.Instance)
                NueGames.NueDeck.Scripts.Managers.CombatManager.Instance.IncreaseMana(energy);

            Debug.Log($"[Potion] Gained {energy} Energy");
        }
    }

    /// <summary>
    /// 格挡药水：获得12点格挡
    /// </summary>
    public class BlockPotion : PotionBase
    {
        public override string PotionId => "potion_block";

        public override void OnUse(PotionData data, PotionUseContext context)
        {
            if (context.Player == null) return;

            var block = data.effectValue > 0 ? data.effectValue : 12;
            context.Player.CharacterStats.ApplyStatus(StatusType.Block, block);

            if (NueGames.NueDeck.Scripts.Managers.FxManager.Instance)
                NueGames.NueDeck.Scripts.Managers.FxManager.Instance.PlayFx(context.Player.transform, FxType.Block);

            Debug.Log($"[Potion] Gained {block} Block");
        }
    }
}
