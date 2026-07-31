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
            if (!ValidateLoadout()) return;

            var model = LoadoutModel;
            var gm = NueGames.NueDeck.Scripts.Managers.GameManager.Instance;
            if (gm == null) return;

            // 设置当前卡组 = 基础卡 + 已选卡
            gm.PersistentGameplayData.CurrentCardsList.Clear();
            foreach (var cardId in model.BasicCardIds)
            {
                var card = gm.GameplayData.AllCardsList.Find(c => c.Id == cardId);
                if (card != null) gm.PersistentGameplayData.CurrentCardsList.Add(card);
            }
            foreach (var cardId in model.SelectedCardIds)
            {
                var card = gm.GameplayData.AllCardsList.Find(c => c.Id == cardId);
                if (card != null) gm.PersistentGameplayData.CurrentCardsList.Add(card);
            }

            Debug.Log($"[Loadout] 出征! 基础卡{model.BasicCardIds.Count}张 + 自选{model.SelectedCardIds.Count}张 = {gm.PersistentGameplayData.CurrentCardsList.Count}张");
        }
    }
}
