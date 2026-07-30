using System;
using System.Collections.Generic;
using NueGames.NueDeck.Scripts.Characters;
using NueGames.NueDeck.Scripts.Managers;
using UnityEngine;

namespace CardGame
{
    /// <summary>
    /// 遗物触发上下文
    /// </summary>
    public class RelicTriggerContext
    {
        public CharacterBase Player;
        public List<EnemyBase> Enemies;
        public CharacterBase TargetCharacter;
        public int Value;
        public string CardId;

        public RelicTriggerContext(CharacterBase player = null, List<EnemyBase> enemies = null,
            CharacterBase target = null, int value = 0, string cardId = "")
        {
            Player = player;
            Enemies = enemies;
            TargetCharacter = target;
            Value = value;
            CardId = cardId;
        }
    }

    /// <summary>
    /// 遗物效果基类，子类通过反射自动发现
    /// </summary>
    public abstract class RelicBase
    {
        public abstract string RelicId { get; }
        public abstract RelicTriggerType TriggerType { get; }

        /// <summary>是否已使用过（一次性遗物）</summary>
        public bool IsUsed { get; set; }

        /// <summary>当前叠数/计数</summary>
        public int Counter { get; set; }

        public abstract void OnTrigger(RelicData data, RelicTriggerContext context);
    }
}
