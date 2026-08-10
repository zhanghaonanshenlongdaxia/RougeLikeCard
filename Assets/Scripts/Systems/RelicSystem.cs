using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace CardGame
{
    public class RelicSystem : AbstractSystem, IRelicSystem
    {
        protected IRelicModel RelicModel => this.GetModel<IRelicModel>();

        protected override void OnInit()
        {
        }

        public void AddRelic(RelicData relicData)
        {
            if (HasRelic(relicData.relicId))
            {
                Debug.Log($"[RelicSystem] Already has relic {relicData.relicId}");
                return;
            }

            var instance = RelicInstance.FromData(relicData);
            RelicModel.OwnedRelics.Add(instance);
            RelicModel.RelicCount.Value = RelicModel.OwnedRelics.Count;

            Debug.Log($"[RelicSystem] Added relic: {relicData.relicId}");

            // 战斗开始时立即触发被动效果
            if (relicData.triggerType == RelicTriggerType.Passive)
            {
                var relicBase = RelicProcessor.GetRelic(relicData.relicId);
                relicBase?.OnTrigger(relicData, new RelicTriggerContext());
            }
        }

        public void RemoveRelic(string relicId)
        {
            var relic = RelicModel.OwnedRelics.Find(r => r.relicId == relicId);
            if (relic != null)
            {
                RelicModel.OwnedRelics.Remove(relic);
                RelicModel.RelicCount.Value = RelicModel.OwnedRelics.Count;
            }
        }

        public void TriggerRelics(RelicTriggerType triggerType, RelicTriggerContext context)
        {
            if (!RelicProcessor.IsInitialized) return;

            bool anyChanged = false;
            foreach (var instance in RelicModel.OwnedRelics)
            {
                if (instance.isUsed || instance.IsBroken) continue;

                var relicBase = RelicProcessor.GetRelic(instance.relicId);
                if (relicBase == null) continue;

                if (relicBase.TriggerType == triggerType)
                {
                    relicBase.OnTrigger(instance.data, context);

                    int cost = instance.data.durabilityCost > 0 ? instance.data.durabilityCost : 1;
                    instance.currentDurability -= cost;
                    anyChanged = true;

                    if (instance.data.oneTimeUse || instance.currentDurability <= 0)
                    {
                        instance.isUsed = true;
                        if (instance.currentDurability <= 0)
                            Debug.Log($"[RelicSystem] {instance.relicId} 已损坏 (耐久归零)");
                    }

                    Debug.Log($"[RelicSystem] {instance.relicId} triggered, durability {instance.currentDurability}/{instance.data.maxDurability}");
                }
            }

            // 触发UI刷新
            if (anyChanged)
            {
                RelicModel.RelicCount.Value = RelicModel.OwnedRelics.Count;
            }
        }

        public bool HasRelic(string relicId)
        {
            return RelicModel.OwnedRelics.Exists(r => r.relicId == relicId);
        }

        /// <summary>移除所有耐久归零的法宝（战斗结束后调用）</summary>
        public void RemoveBrokenRelics()
        {
            var broken = RelicModel.OwnedRelics.FindAll(r => r.IsBroken);
            if (broken.Count == 0) return;

            foreach (var r in broken)
            {
                RelicModel.OwnedRelics.Remove(r);
                Debug.Log($"[RelicSystem] 法宝报废: {r.relicId} (耐久归零，已从背包移除)");
            }
            RelicModel.RelicCount.Value = RelicModel.OwnedRelics.Count;
        }
    }
}
