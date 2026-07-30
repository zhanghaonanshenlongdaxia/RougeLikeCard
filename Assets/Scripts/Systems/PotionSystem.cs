using QFramework;
using UnityEngine;

namespace CardGame
{
    public class PotionSystem : AbstractSystem, IPotionSystem
    {
        protected IPotionModel PotionModel => this.GetModel<IPotionModel>();
        protected IRelicSystem RelicSystem => this.GetSystem<IRelicSystem>();

        protected override void OnInit()
        {
        }

        public bool ObtainPotion(PotionData potion)
        {
            if (PotionModel.OwnedPotions.Count >= PotionModel.MaxPotionSlots)
            {
                Debug.Log("[PotionSystem] Potion slots full");
                return false;
            }

            PotionModel.OwnedPotions.Add(potion);
            PotionModel.PotionCount.Value = PotionModel.OwnedPotions.Count;
            Debug.Log($"[PotionSystem] Obtained potion: {potion.potionId}");
            return true;
        }

        public void UsePotion(int slotIndex, PotionUseContext context)
        {
            if (slotIndex < 0 || slotIndex >= PotionModel.OwnedPotions.Count) return;

            var potion = PotionModel.OwnedPotions[slotIndex];
            if (potion == null) return;

            if (!PotionProcessor.IsInitialized)
            {
                Debug.LogError("[PotionSystem] PotionProcessor not initialized");
                return;
            }

            var potionBase = PotionProcessor.GetPotion(potion.potionId);
            if (potionBase == null)
            {
                Debug.LogError($"[PotionSystem] Potion logic not found: {potion.potionId}");
                return;
            }

            potionBase.OnUse(potion, context);

            // 遗物触发：使用药水
            RelicSystem.TriggerRelics(RelicTriggerType.OnUsePotion,
                new RelicTriggerContext(player: context.Player, enemies: context.Enemies));

            // 使用后从槽位移除
            PotionModel.OwnedPotions.RemoveAt(slotIndex);
            PotionModel.PotionCount.Value = PotionModel.OwnedPotions.Count;

            Debug.Log($"[PotionSystem] Used potion: {potion.potionId}");
        }

        public void DiscardPotion(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= PotionModel.OwnedPotions.Count) return;

            PotionModel.OwnedPotions.RemoveAt(slotIndex);
            PotionModel.PotionCount.Value = PotionModel.OwnedPotions.Count;
            Debug.Log("[PotionSystem] Potion discarded");
        }
    }
}
