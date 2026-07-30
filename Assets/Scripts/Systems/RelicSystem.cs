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

            foreach (var instance in RelicModel.OwnedRelics)
            {
                if (instance.isUsed) continue;

                var relicBase = RelicProcessor.GetRelic(instance.relicId);
                if (relicBase == null) continue;

                if (relicBase.TriggerType == triggerType)
                {
                    relicBase.OnTrigger(instance.data, context);

                    if (instance.data.oneTimeUse)
                        instance.isUsed = true;
                }
            }
        }

        public bool HasRelic(string relicId)
        {
            return RelicModel.OwnedRelics.Exists(r => r.relicId == relicId);
        }
    }
}
