using NueGames.NueDeck.Scripts.Enums;
using UnityEngine;

namespace CardGame
{
    /// <summary>
    /// 燃烧之血：每回合结束时回复5点HP（StS中燃烧之血是回6）
    /// </summary>
    public class BurningBloodRelic : RelicBase
    {
        public override string RelicId => "relic_burning_blood";
        public override RelicTriggerType TriggerType => RelicTriggerType.OnTurnEnd;

        public override void OnTrigger(RelicData data, RelicTriggerContext context)
        {
            if (context.Player == null) return;

            context.Player.CharacterStats.Heal(data.effectValue > 0 ? data.effectValue : 5);

            if (NueGames.NueDeck.Scripts.Managers.FxManager.Instance)
                NueGames.NueDeck.Scripts.Managers.FxManager.Instance.PlayFx(context.Player.transform, FxType.Heal);

            Debug.Log($"[Relic] BurningBlood healed {data.effectValue} HP");
        }
    }

    /// <summary>
    /// 蛇眼：每回合开始时获得1点能量（被动效果在StartCombat时用OnCombatStart代替）
    /// </summary>
    public class SnakeRingRelic : RelicBase
    {
        public override string RelicId => "relic_snake_ring";
        public override RelicTriggerType TriggerType => RelicTriggerType.OnTurnStart;

        public override void OnTrigger(RelicData data, RelicTriggerContext context)
        {
            if (context.Player == null) return;

            // 通过CombatManager增加能量
            if (NueGames.NueDeck.Scripts.Managers.CombatManager.Instance)
            {
                NueGames.NueDeck.Scripts.Managers.CombatManager.Instance.IncreaseMana(data.effectValue > 0 ? data.effectValue : 1);
            }

            Debug.Log($"[Relic] SnakeRing gained {data.effectValue} energy");
        }
    }

    /// <summary>
    /// 愤怒之石：每打出一张攻击牌时，获得1点力量（仅本回合）
    /// </summary>
    public class AngerStoneRelic : RelicBase
    {
        public override string RelicId => "relic_anger_stone";
        public override RelicTriggerType TriggerType => RelicTriggerType.OnAttackPlayed;

        public override void OnTrigger(RelicData data, RelicTriggerContext context)
        {
            if (context.Player == null) return;

            context.Player.CharacterStats.ApplyStatus(StatusType.Strength, data.effectValue > 0 ? data.effectValue : 1);

            Debug.Log($"[Relic] AngerStone granted {data.effectValue} Strength");
        }
    }

    /// <summary>
    /// 战争号角：战斗开始时获得2点力量
    /// </summary>
    public class WarHornRelic : RelicBase
    {
        public override string RelicId => "relic_war_horn";
        public override RelicTriggerType TriggerType => RelicTriggerType.OnCombatStart;

        public override void OnTrigger(RelicData data, RelicTriggerContext context)
        {
            if (context.Player == null) return;

            context.Player.CharacterStats.ApplyStatus(StatusType.Strength, data.effectValue > 0 ? data.effectValue : 2);

            Debug.Log($"[Relic] WarHorn granted {data.effectValue} Strength at combat start");
        }
    }

    /// <summary>
    /// 黄金甲：敌人死亡时获得10金币
    /// </summary>
    public class GoldArmorRelic : RelicBase
    {
        public override string RelicId => "relic_gold_armor";
        public override RelicTriggerType TriggerType => RelicTriggerType.OnEnemyDeath;

        public override void OnTrigger(RelicData data, RelicTriggerContext context)
        {
            if (NueGames.NueDeck.Scripts.Managers.GameManager.Instance == null) return;

            var gold = data.effectValue > 0 ? data.effectValue : 10;
            NueGames.NueDeck.Scripts.Managers.GameManager.Instance.PersistentGameplayData.CurrentGold += gold;

            Debug.Log($"[Relic] GoldArmor gained {gold} gold on enemy death");
        }
    }
}
