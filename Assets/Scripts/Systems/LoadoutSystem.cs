using QFramework;
using UnityEngine;

namespace CardGame
{
    public class LoadoutSystem : AbstractSystem, ILoadoutSystem
    {
        protected ILoadoutModel LoadoutModel => this.GetModel<ILoadoutModel>();

        protected override void OnInit()
        {
        }

        public bool SelectCard(string cardId, int shenShiCost)
        {
            var model = LoadoutModel;
            if (model.SelectedCardIds.Contains(cardId)) return false;

            if (model.CurrentShenShi.Value + shenShiCost > model.MaxShenShi.Value)
            {
                Debug.Log("[Loadout] 神识不足");
                return false;
            }

            model.SelectedCardIds.Add(cardId);
            model.CurrentShenShi.Value += shenShiCost;
            return true;
        }

        public bool DeselectCard(string cardId, int shenShiCost)
        {
            var model = LoadoutModel;
            if (!model.SelectedCardIds.Contains(cardId)) return false;

            model.SelectedCardIds.Remove(cardId);
            model.CurrentShenShi.Value -= shenShiCost;
            return true;
        }

        public bool ValidateLoadout()
        {
            var model = LoadoutModel;
            if (model.CurrentShenShi.Value < model.MinShenShiRequired)
            {
                Debug.Log($"[Loadout] 神识不足: {model.CurrentShenShi.Value} < {model.MinShenShiRequired}");
                return false;
            }
            return true;
        }

        public void StartAdventure()
        {
            var model = LoadoutModel;
            var gm = NueGames.NueDeck.Scripts.Managers.GameManager.Instance;
            if (gm == null) return;

            // 出征卡组 = 功法基础卡 + 功法自带神通卡 + 自选神通卡
            gm.PersistentGameplayData.CurrentCardsList.Clear();

            // 1. 功法基础卡 + 功法自带神通卡 (从当前装备功法的已解锁节点获取)
            var cultSystem = this.GetSystem<ICultivationSystem>();
            var methodCards = cultSystem.GetActiveMethodCards();
            foreach (var cardId in methodCards)
            {
                var card = gm.GameplayData.AllCardsList.Find(c => c.Id == cardId);
                if (card != null) gm.PersistentGameplayData.CurrentCardsList.Add(card);
            }

            // 2. 自选神通卡 (玩家从已学神通中选配的，受能量上限限制)
            var equippedAbilities = cultSystem.GetEquippedAbilities();
            foreach (var ability in equippedAbilities)
            {
                var card = gm.GameplayData.AllCardsList.Find(c => c.Id == ability.CardId);
                if (card != null) gm.PersistentGameplayData.CurrentCardsList.Add(card);
            }

            Debug.Log($"[Loadout] 出征! 功法{methodCards.Count} + 神通{equippedAbilities.Count} = {gm.PersistentGameplayData.CurrentCardsList.Count}张");
        }
    }
}
