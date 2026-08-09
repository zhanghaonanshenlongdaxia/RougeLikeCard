using System.Collections.Generic;
using QFramework;
using UnityEngine;
using NueGames.NueDeck.Scripts.Data.Collection;
using NueGames.NueDeck.Scripts.Managers;

namespace CardGame
{
    public class ShopSystem : AbstractSystem, IShopSystem
    {
        protected IBattleModel BattleModel => this.GetModel<IBattleModel>();
        protected IRelicModel RelicModel => this.GetModel<IRelicModel>();
        protected IPotionModel PotionModel => this.GetModel<IPotionModel>();

        public ShopData CurrentShop { get; private set; }

        protected override void OnInit()
        {
        }

        public ShopData GenerateShop()
        {
            CurrentShop = new ShopData();

            var gameData = GameManager.Instance?.GameplayData;
            if (gameData == null) return CurrentShop;

            // 3张卡牌（随机选）
            var availableCards = gameData.AllCardsList;
            for (int i = 0; i < 3 && availableCards.Count > 0; i++)
            {
                var card = availableCards[Random.Range(0, availableCards.Count)];
                CurrentShop.cardSlots.Add(card);
                CurrentShop.cardPrices.Add(Random.Range(45, 75));
            }

            // 2个遗物（随机选）
            var allRelics = ResourceCache.GetRelics();
            var availableRelics = allRelics.FindAll(r => !RelicModel.OwnedRelics.Exists(own => own.relicId == r.relicId));
            for (int i = 0; i < 2 && availableRelics.Count > 0; i++)
            {
                var relic = availableRelics[Random.Range(0, availableRelics.Count)];
                if (relic != null)
                {
                    CurrentShop.relicSlots.Add(relic);
                    CurrentShop.relicPrices.Add(Random.Range(150, 250));
                }
            }

            // 2个药水
            var allPotions = ResourceCache.GetPotions();
            for (int i = 0; i < 2 && allPotions.Count > 0; i++)
            {
                var potion = allPotions[Random.Range(0, allPotions.Count)];
                if (potion != null)
                {
                    CurrentShop.potionSlots.Add(potion);
                    CurrentShop.potionPrices.Add(Random.Range(50, 80));
                }
            }

            return CurrentShop;
        }

        public bool BuyCard(int slotIndex)
        {
            if (CurrentShop == null || slotIndex >= CurrentShop.cardSlots.Count) return false;
            var price = CurrentShop.cardPrices[slotIndex];
            if (BattleModel.CurrentGold.Value < price) return false;

            BattleModel.CurrentGold.Value -= price;
            GameManager.Instance.PersistentGameplayData.CurrentGold = BattleModel.CurrentGold.Value;
            GameManager.Instance.PersistentGameplayData.CurrentCardsList.Add(CurrentShop.cardSlots[slotIndex]);
            CurrentShop.cardSlots[slotIndex] = null;
            return true;
        }

        public bool BuyRelic(int slotIndex)
        {
            if (CurrentShop == null || slotIndex >= CurrentShop.relicSlots.Count) return false;
            var price = CurrentShop.relicPrices[slotIndex];
            if (BattleModel.CurrentGold.Value < price) return false;

            BattleModel.CurrentGold.Value -= price;
            GameManager.Instance.PersistentGameplayData.CurrentGold = BattleModel.CurrentGold.Value;
            this.GetSystem<IRelicSystem>().AddRelic(CurrentShop.relicSlots[slotIndex]);
            CurrentShop.relicSlots[slotIndex] = null;
            return true;
        }

        public bool BuyPotion(int slotIndex)
        {
            if (CurrentShop == null || slotIndex >= CurrentShop.potionSlots.Count) return false;
            if (PotionModel.OwnedPotions.Count >= PotionModel.MaxPotionSlots) return false;
            var price = CurrentShop.potionPrices[slotIndex];
            if (BattleModel.CurrentGold.Value < price) return false;

            BattleModel.CurrentGold.Value -= price;
            GameManager.Instance.PersistentGameplayData.CurrentGold = BattleModel.CurrentGold.Value;
            this.GetSystem<IPotionSystem>().ObtainPotion(CurrentShop.potionSlots[slotIndex]);
            CurrentShop.potionSlots[slotIndex] = null;
            return true;
        }

        public bool RemoveCard(CardData card)
        {
            if (BattleModel.CurrentGold.Value < CurrentShop.removeCardPrice) return false;
            if (!GameManager.Instance.PersistentGameplayData.CurrentCardsList.Remove(card)) return false;

            BattleModel.CurrentGold.Value -= CurrentShop.removeCardPrice;
            GameManager.Instance.PersistentGameplayData.CurrentGold = BattleModel.CurrentGold.Value;
            CurrentShop.removeCardPrice += 25; // 每次移除涨价
            return true;
        }
    }
}
