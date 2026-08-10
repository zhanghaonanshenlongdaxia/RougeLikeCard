using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace CardGame
{
    /// <summary>
    /// 遗物系统 — 管理遗物触发
    /// </summary>
    public interface IRelicSystem : ISystem
    {
        void AddRelic(RelicData relicData);
        void RemoveRelic(string relicId);
        void TriggerRelics(RelicTriggerType triggerType, RelicTriggerContext context);
        bool HasRelic(string relicId);
        /// <summary>移除所有耐久归零的法宝（战斗结束后调用）</summary>
        void RemoveBrokenRelics();
    }
}
