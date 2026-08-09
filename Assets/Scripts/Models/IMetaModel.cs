using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace CardGame
{
    /// <summary>
    /// Meta进度模型 — 跨冒险持久化，记录已解锁内容。
    /// 类似杀戮尖塔2的meta进度：每次冒险后解锁新卡牌/药水/法宝。
    /// </summary>
    public interface IMetaModel : IModel
    {
        /// <summary>当前解锁章节（0=初始，每次冒险完成+1，上限5）</summary>
        BindableProperty<int> UnlockedChapter { get; }
        /// <summary>已完成冒险次数</summary>
        BindableProperty<int> AdventureCount { get; }
        /// <summary>已解锁的卡牌ID集合</summary>
        HashSet<string> UnlockedCardIds { get; }
        /// <summary>已解锁的药水ID集合</summary>
        HashSet<string> UnlockedPotionIds { get; }
        /// <summary>已解锁的法宝ID集合</summary>
        HashSet<string> UnlockedRelicIds { get; }
    }

    public class MetaModel : AbstractModel, IMetaModel
    {
        public BindableProperty<int> UnlockedChapter { get; } = new BindableProperty<int>(0);
        public BindableProperty<int> AdventureCount { get; } = new BindableProperty<int>(0);
        public HashSet<string> UnlockedCardIds { get; } = new HashSet<string>();
        public HashSet<string> UnlockedPotionIds { get; } = new HashSet<string>();
        public HashSet<string> UnlockedRelicIds { get; } = new HashSet<string>();

        protected override void OnInit()
        {
        }
    }

    /// <summary>
    /// Meta解锁系统 — 冒险完成后解锁新内容
    /// </summary>
    public interface IMetaSystem : ISystem
    {
        /// <summary>冒险完成时调用，解锁新内容</summary>
        void OnAdventureComplete();
        /// <summary>检查卡牌是否已解锁</summary>
        bool IsCardUnlocked(string cardId, int cardChapter);
        /// <summary>检查药水是否已解锁</summary>
        bool IsPotionUnlocked(string potionId, int potionChapter);
        /// <summary>检查法宝是否已解锁</summary>
        bool IsRelicUnlocked(string relicId, int relicChapter);
        /// <summary>获取当前已解锁的卡牌列表</summary>
        List<NueGames.NueDeck.Scripts.Data.Collection.CardData> GetUnlockedCards();
        /// <summary>获取当前已解锁的药水列表</summary>
        List<PotionData> GetUnlockedPotions();
        /// <summary>获取当前已解锁的法宝列表</summary>
        List<RelicData> GetUnlockedRelics();
        /// <summary>获取本次冒险将解锁的内容预览</summary>
        string GetNextUnlockPreview();
        /// <summary>加载存档中的meta数据</summary>
        void LoadFromSave(int chapter, int adventureCount, List<string> cardIds, List<string> potionIds, List<string> relicIds);
        /// <summary>保存meta数据到存档格式</summary>
        (int chapter, int adventureCount, List<string> cardIds, List<string> potionIds, List<string> relicIds) GetSaveData();
    }

    public class MetaSystem : AbstractSystem, IMetaSystem
    {
        private IMetaModel _model;

        protected override void OnInit()
        {
            _model = this.GetModel<IMetaModel>();
        }

        public void OnAdventureComplete()
        {
            _model.AdventureCount.Value++;
            
            int oldChapter = _model.UnlockedChapter.Value;
            // 每3次冒险解锁一个新章节，上限5
            int newChapter = Mathf.Min(5, _model.AdventureCount.Value / 3);
            
            if (newChapter > oldChapter)
            {
                _model.UnlockedChapter.Value = newChapter;
                UnlockNewContent(newChapter);
                Debug.Log($"[Meta] 解锁章节 {newChapter}！冒险次数: {_model.AdventureCount.Value}");
            }
            else
            {
                // 即使没解锁新章节，也随机解锁1-2个当前章节的锁定内容
                UnlockRandomContent(oldChapter);
                Debug.Log($"[Meta] 冒险完成，随机解锁内容。冒险次数: {_model.AdventureCount.Value}");
            }
        }

        void UnlockNewContent(int chapter)
        {
            // 解锁该章节的所有卡牌/药水/法宝
            int cardUnlock = 0, potionUnlock = 0, relicUnlock = 0;

            // 卡牌
            foreach (var card in CardGame.ResourceCache.GetCardsFromAllList())
            {
                if (card.UnlockChapter == chapter && !_model.UnlockedCardIds.Contains(card.Id))
                {
                    _model.UnlockedCardIds.Add(card.Id);
                    cardUnlock++;
                }
            }

            // 药水
            foreach (var potion in CardGame.ResourceCache.GetPotions())
            {
                if (potion.unlockChapter == chapter && !_model.UnlockedPotionIds.Contains(potion.potionId))
                {
                    _model.UnlockedPotionIds.Add(potion.potionId);
                    potionUnlock++;
                }
            }

            // 法宝
            foreach (var relic in CardGame.ResourceCache.GetRelics())
            {
                if (relic.unlockChapter == chapter && !_model.UnlockedRelicIds.Contains(relic.relicId))
                {
                    _model.UnlockedRelicIds.Add(relic.relicId);
                    relicUnlock++;
                }
            }

            Debug.Log($"[Meta] 章节解锁: 卡牌+{cardUnlock}, 药水+{potionUnlock}, 法宝+{relicUnlock}");
        }

        void UnlockRandomContent(int maxChapter)
        {
            // 随机解锁1个卡牌+1个药水或法宝
            var lockedCards = new List<NueGames.NueDeck.Scripts.Data.Collection.CardData>();
            foreach (var card in CardGame.ResourceCache.GetCardsFromAllList())
            {
                if (card.UnlockChapter > 0 && card.UnlockChapter <= maxChapter + 1 && !_model.UnlockedCardIds.Contains(card.Id))
                    lockedCards.Add(card);
            }
            if (lockedCards.Count > 0)
            {
                var picked = lockedCards[Random.Range(0, lockedCards.Count)];
                _model.UnlockedCardIds.Add(picked.Id);
                Debug.Log($"[Meta] 随机解锁卡牌: {picked.CardName}");
            }
        }

        public bool IsCardUnlocked(string cardId, int cardChapter)
        {
            if (cardChapter == 0) return true; // chapter=0 始终可用
            return _model.UnlockedCardIds.Contains(cardId);
        }

        public bool IsPotionUnlocked(string potionId, int potionChapter)
        {
            if (potionChapter == 0) return true;
            return _model.UnlockedPotionIds.Contains(potionId);
        }

        public bool IsRelicUnlocked(string relicId, int relicChapter)
        {
            if (relicChapter == 0) return true;
            return _model.UnlockedRelicIds.Contains(relicId);
        }

        public List<NueGames.NueDeck.Scripts.Data.Collection.CardData> GetUnlockedCards()
        {
            var result = new List<NueGames.NueDeck.Scripts.Data.Collection.CardData>();
            foreach (var card in CardGame.ResourceCache.GetCardsFromAllList())
            {
                if (IsCardUnlocked(card.Id, card.UnlockChapter))
                    result.Add(card);
            }
            return result;
        }

        public List<PotionData> GetUnlockedPotions()
        {
            var result = new List<PotionData>();
            foreach (var potion in CardGame.ResourceCache.GetPotions())
            {
                if (IsPotionUnlocked(potion.potionId, potion.unlockChapter))
                    result.Add(potion);
            }
            return result;
        }

        public List<RelicData> GetUnlockedRelics()
        {
            var result = new List<RelicData>();
            foreach (var relic in CardGame.ResourceCache.GetRelics())
            {
                if (IsRelicUnlocked(relic.relicId, relic.unlockChapter))
                    result.Add(relic);
            }
            return result;
        }

        public string GetNextUnlockPreview()
        {
            int currentChapter = _model.UnlockedChapter.Value;
            int nextChapter = currentChapter + 1;
            int adventuresNeeded = nextChapter * 3 - _model.AdventureCount.Value;
            
            if (nextChapter > 5) return "已达最高解锁章节";

            // 统计下一章将解锁的内容
            int cards = 0, potions = 0, relics = 0;
            foreach (var card in CardGame.ResourceCache.GetCardsFromAllList())
                if (card.UnlockChapter == nextChapter) cards++;
            foreach (var potion in CardGame.ResourceCache.GetPotions())
                if (potion.unlockChapter == nextChapter) potions++;
            foreach (var relic in CardGame.ResourceCache.GetRelics())
                if (relic.unlockChapter == nextChapter) relics++;

            return $"下一解锁章节: 第{nextChapter}章\n还需冒险 {adventuresNeeded} 次\n将解锁: 卡牌{cards}张, 药水{potions}个, 法宝{relics}个";
        }

        public void LoadFromSave(int chapter, int adventureCount, List<string> cardIds, List<string> potionIds, List<string> relicIds)
        {
            _model.UnlockedChapter.Value = chapter;
            _model.AdventureCount.Value = adventureCount;
            _model.UnlockedCardIds.Clear();
            _model.UnlockedPotionIds.Clear();
            _model.UnlockedRelicIds.Clear();
            if (cardIds != null) foreach (var id in cardIds) _model.UnlockedCardIds.Add(id);
            if (potionIds != null) foreach (var id in potionIds) _model.UnlockedPotionIds.Add(id);
            if (relicIds != null) foreach (var id in relicIds) _model.UnlockedRelicIds.Add(id);
        }

        public (int, int, List<string>, List<string>, List<string>) GetSaveData()
        {
            return (
                _model.UnlockedChapter.Value,
                _model.AdventureCount.Value,
                new List<string>(_model.UnlockedCardIds),
                new List<string>(_model.UnlockedPotionIds),
                new List<string>(_model.UnlockedRelicIds)
            );
        }
    }
}
