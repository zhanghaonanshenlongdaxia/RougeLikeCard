using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NueGames.NueDeck.Scripts.Characters;
using UnityEngine;

namespace CardGame
{
    /// <summary>
    /// 药水使用上下文
    /// </summary>
    public class PotionUseContext
    {
        public CharacterBase Player;
        public List<EnemyBase> Enemies;
        public CharacterBase TargetEnemy;

        public PotionUseContext(CharacterBase player = null, List<EnemyBase> enemies = null, CharacterBase target = null)
        {
            Player = player;
            Enemies = enemies;
            TargetEnemy = target;
        }
    }

    /// <summary>
    /// 药水效果基类，子类通过反射自动发现
    /// </summary>
    public abstract class PotionBase
    {
        public abstract string PotionId { get; }
        public abstract void OnUse(PotionData data, PotionUseContext context);
    }

    /// <summary>
    /// 药水处理器，反射发现所有 PotionBase 子类
    /// </summary>
    public static class PotionProcessor
    {
        private static readonly Dictionary<string, PotionBase> PotionDict =
            new Dictionary<string, PotionBase>();

        public static bool IsInitialized { get; private set; }

        public static void Initialize()
        {
            PotionDict.Clear();

            var allPotions = Assembly.GetAssembly(typeof(PotionBase)).GetTypes()
                .Where(t => typeof(PotionBase).IsAssignableFrom(t) && t.IsAbstract == false);

            foreach (var potionType in allPotions)
            {
                PotionBase potion = Activator.CreateInstance(potionType) as PotionBase;
                if (potion != null)
                {
                    PotionDict.Add(potion.PotionId, potion);
                    Debug.Log($"[PotionProcessor] Registered potion: {potion.PotionId}");
                }
            }

            IsInitialized = true;
        }

        public static PotionBase GetPotion(string potionId) =>
            PotionDict.TryGetValue(potionId, out var potion) ? potion : null;
    }
}
