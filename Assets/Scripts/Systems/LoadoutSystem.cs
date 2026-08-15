using System.Linq;
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

            // 出征卡组 = 功法基础卡 + 玩家自选附加卡 + 装备神通卡
            gm.PersistentGameplayData.CurrentCardsList.Clear();

            var cultSystem = this.GetSystem<ICultivationSystem>();
            var cultModel = this.GetModel<ICultivationModel>();
            var activeMethod = cultSystem.GetMethodConfig(cultModel.ActiveMethodId.Value ?? "");
            var allCards = ResourceCache.GetCardsFromAllList();

            // 1. 功法基础卡：当前功法首神通的卡牌（LoadoutUI左侧固定显示的那批）
            var baseCardIds = new System.Collections.Generic.List<string>();
            if (activeMethod?.Nodes != null)
            {
                var firstNode = activeMethod.Nodes
                    .FindAll(n => n.Realm == RealmLevel.LianQi)
                    .OrderBy(n => n.GridIndex.y)
                    .FirstOrDefault();
                if (firstNode?.RewardIds != null)
                    baseCardIds.AddRange(firstNode.RewardIds);
            }
            foreach (var cardId in baseCardIds)
            {
                var card = allCards.Find(c => c.Id == cardId);
                if (card != null) gm.PersistentGameplayData.CurrentCardsList.Add(card);
            }

            // 2. 玩家自选附加卡（LoadoutUI中间栏，从已解锁卡牌中选配的）
            foreach (var cardId in model.SelectedCardIds)
            {
                var card = allCards.Find(c => c.Id == cardId);
                if (card != null) gm.PersistentGameplayData.CurrentCardsList.Add(card);
            }

            // 3. 装备的神通卡
            var equippedAbilities = cultSystem.GetEquippedAbilities();
            foreach (var ability in equippedAbilities)
            {
                var card = allCards.Find(c => c.Id == ability.CardId);
                if (card != null) gm.PersistentGameplayData.CurrentCardsList.Add(card);
            }

            // 兜底：如果什么都没有，使用初始牌组
            if (gm.PersistentGameplayData.CurrentCardsList.Count == 0)
            {
                foreach (var card in gm.GameplayData.InitalDeck.CardList)
                    gm.PersistentGameplayData.CurrentCardsList.Add(card);
                Debug.Log($"[Loadout] 无功法无选卡，使用初始牌组兜底: {gm.PersistentGameplayData.CurrentCardsList.Count}张");
            }
            else
            {
                Debug.Log($"[Loadout] 出征! 基础{baseCardIds.Count} + 自选{model.SelectedCardIds.Count} + 神通{equippedAbilities.Count} = {gm.PersistentGameplayData.CurrentCardsList.Count}张");
            }
        }
    }
}
