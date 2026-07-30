using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace CardGame
{
    /// <summary>
    /// 遗物处理器，反射发现所有 RelicBase 子类
    /// </summary>
    public static class RelicProcessor
    {
        private static readonly Dictionary<string, RelicBase> RelicDict =
            new Dictionary<string, RelicBase>();

        public static bool IsInitialized { get; private set; }

        public static void Initialize()
        {
            RelicDict.Clear();

            var allRelics = Assembly.GetAssembly(typeof(RelicBase)).GetTypes()
                .Where(t => typeof(RelicBase).IsAssignableFrom(t) && t.IsAbstract == false);

            foreach (var relicType in allRelics)
            {
                RelicBase relic = Activator.CreateInstance(relicType) as RelicBase;
                if (relic != null)
                {
                    RelicDict.Add(relic.RelicId, relic);
                    Debug.Log($"[RelicProcessor] Registered relic: {relic.RelicId} (trigger: {relic.TriggerType})");
                }
            }

            IsInitialized = true;
        }

        public static RelicBase GetRelic(string relicId) =>
            RelicDict.TryGetValue(relicId, out var relic) ? relic : null;

        public static bool HasRelic(string relicId) => RelicDict.ContainsKey(relicId);
    }
}
