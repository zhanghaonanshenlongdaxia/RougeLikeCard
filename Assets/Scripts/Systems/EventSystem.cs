using System.Collections.Generic;
using System.Linq;
using QFramework;
using UnityEngine;

namespace CardGame
{
    public class EventSystem : AbstractSystem, IEventSystem
    {
        protected IBattleModel BattleModel => this.GetModel<IBattleModel>();
        protected IRelicSystem RelicSystem => this.GetSystem<IRelicSystem>();
        protected IPotionSystem PotionSystem => this.GetSystem<IPotionSystem>();
        protected IInventorySystem InventorySystem => this.GetSystem<IInventorySystem>();
        protected IInventoryModel InventoryModel => this.GetModel<IInventoryModel>();
        protected ICraftSystem CraftSystem => this.GetSystem<ICraftSystem>();
        protected ILoadoutModel LoadoutModel => this.GetModel<ILoadoutModel>();
        protected IRelicModel RelicModel => this.GetModel<IRelicModel>();
        protected IPotionModel PotionModel => this.GetModel<IPotionModel>();

        private List<EventData> _eventPool;

        protected override void OnInit()
        {
        }

        public EventData GetRandomEvent()
        {
            LoadEventPool();
            if (_eventPool == null || _eventPool.Count == 0) return null;

            var currentRealm = this.GetModel<IRealmModel>().CurrentRealm.Value;
            var filtered = _eventPool.FindAll(e => (int)e.requiredRealm == currentRealm);
            if (filtered.Count == 0)
                return _eventPool[Random.Range(0, _eventPool.Count)];
            return filtered[Random.Range(0, filtered.Count)];
        }

        public void ExecuteChoice(EventData eventData, int choiceIndex)
        {
            if (eventData == null || choiceIndex >= eventData.choices.Count) return;

            var choice = eventData.choices[choiceIndex];
            var gm = NueGames.NueDeck.Scripts.Managers.GameManager.Instance;
            if (gm == null) return;

            var pd = gm.PersistentGameplayData;

            switch (choice.effectType)
            {
                // ── 血量相关（使用 PersistentGameplayData，不依赖 CombatManager）──
                case EventEffectType.Heal:
                    ModifyAllyHealth(pd, choice.effectValue, 0);
                    break;
                case EventEffectType.FullHeal:
                    FullHealAlly(pd);
                    break;
                case EventEffectType.TakeDamage:
                    ModifyAllyHealth(pd, -choice.effectValue, 0);
                    break;
                case EventEffectType.GainMaxHP:
                    ModifyAllyHealth(pd, choice.effectValue, choice.effectValue);
                    break;
                case EventEffectType.LoseMaxHP:
                    ModifyAllyHealth(pd, -choice.effectValue, -choice.effectValue);
                    break;

                // ── 资源相关 ──
                case EventEffectType.GainGold:
                    BattleModel.CurrentGold.Value += choice.effectValue;
                    pd.CurrentGold = BattleModel.CurrentGold.Value;
                    break;
                case EventEffectType.LoseGold:
                    var loss = Mathf.Min(BattleModel.CurrentGold.Value, choice.effectValue);
                    BattleModel.CurrentGold.Value -= loss;
                    pd.CurrentGold = BattleModel.CurrentGold.Value;
                    break;
                case EventEffectType.GainMaterial:
                    GrantRandomMaterial(choice.effectValue);
                    break;
                case EventEffectType.UnlockRecipe:
                    GrantRandomRecipe();
                    break;

                // ── 神识/法力 ──
                case EventEffectType.GainShenShi:
                    LoadoutModel.MaxShenShi.Value += choice.effectValue;
                    break;
                case EventEffectType.LoseShenShi:
                    LoadoutModel.MaxShenShi.Value = Mathf.Max(1, LoadoutModel.MaxShenShi.Value - choice.effectValue);
                    break;
                case EventEffectType.DrawBonus:
                    pd.DrawCount += choice.effectValue;
                    break;
                case EventEffectType.ManaBonus:
                    pd.MaxMana += choice.effectValue;
                    pd.CurrentMana = pd.MaxMana;
                    break;

                // ── 战斗状态（存储到 BattleModel，下场战斗生效）──
                case EventEffectType.GainStrength:
                    BattleModel.PendingStrengthBonus = choice.effectValue;
                    break;
                case EventEffectType.GainDexterity:
                    BattleModel.PendingDexterityBonus = choice.effectValue;
                    break;

                // ── 卡牌相关 ──
                case EventEffectType.GainCard:
                    var card = string.IsNullOrEmpty(choice.cardId)
                        ? gm.GameplayData.AllCardsList[Random.Range(0, gm.GameplayData.AllCardsList.Count)]
                        : gm.GameplayData.AllCardsList.Find(c => c.Id == choice.cardId);
                    if (card != null)
                        pd.CurrentCardsList.Add(card);
                    break;
                case EventEffectType.RemoveCard:
                    if (pd.CurrentCardsList.Count > 0)
                        pd.CurrentCardsList.RemoveAt(
                            Random.Range(0, pd.CurrentCardsList.Count));
                    break;
                case EventEffectType.UpgradeRandomCard:
                    {
                        var upgradeable = pd.CurrentCardsList.FindAll(c => c.HasUpgradeData && !c.IsUpgraded);
                        if (upgradeable.Count > 0)
                            upgradeable[Random.Range(0, upgradeable.Count)].Upgrade();
                    }
                    break;
                case EventEffectType.DowngradeRandomCard:
                    {
                        var upgraded = pd.CurrentCardsList.FindAll(c => c.IsUpgraded);
                        if (upgraded.Count > 0)
                            upgraded[Random.Range(0, upgraded.Count)].Downgrade();
                    }
                    break;

                // ── 道具 ──
                case EventEffectType.GainRelic:
                    {
                        var relicDir = "Assets/NueGames/NueDeck/Data/Relics";
                        var relicGuids = UnityEditor.AssetDatabase.FindAssets("t:RelicData", new[] { relicDir });
                        if (relicGuids.Length > 0)
                        {
                            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(relicGuids[Random.Range(0, relicGuids.Length)]);
                            var relic = UnityEditor.AssetDatabase.LoadAssetAtPath<RelicData>(path);
                            if (relic != null) RelicSystem.AddRelic(relic);
                        }
                    }
                    break;
                case EventEffectType.GainPotion:
                    {
                        var potionDir = "Assets/NueGames/NueDeck/Data/Potions";
                        var potionGuids = UnityEditor.AssetDatabase.FindAssets("t:PotionData", new[] { potionDir });
                        if (potionGuids.Length > 0)
                        {
                            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(potionGuids[Random.Range(0, potionGuids.Length)]);
                            var potion = UnityEditor.AssetDatabase.LoadAssetAtPath<PotionData>(path);
                            if (potion != null) PotionSystem.ObtainPotion(potion);
                        }
                    }
                    break;

                // ── 卡牌扩展 ──
                case EventEffectType.DuplicateCard:
                    {
                        if (pd.CurrentCardsList.Count > 0)
                        {
                            var src = pd.CurrentCardsList[Random.Range(0, pd.CurrentCardsList.Count)];
                            pd.CurrentCardsList.Add(src);
                            Debug.Log($"[Event] 复制卡牌: {src.CardName}");
                        }
                    }
                    break;

                // ── 道具扩展 ──
                case EventEffectType.LoseRelic:
                    {
                        if (RelicModel.OwnedRelics != null && RelicModel.OwnedRelics.Count > 0)
                        {
                            var idx = Random.Range(0, RelicModel.OwnedRelics.Count);
                            var lost = RelicModel.OwnedRelics[idx];
                            RelicModel.OwnedRelics.RemoveAt(idx);
                            Debug.Log($"[Event] 失去遗物: {lost?.relicId}");
                        }
                    }
                    break;
                case EventEffectType.LosePotion:
                    {
                        if (PotionModel.OwnedPotions != null && PotionModel.OwnedPotions.Count > 0)
                        {
                            // 找到非null的药水
                            var valid = PotionModel.OwnedPotions.FindAll(p => p != null);
                            if (valid.Count > 0)
                            {
                                var lost = valid[Random.Range(0, valid.Count)];
                                PotionModel.OwnedPotions.Remove(lost);
                                Debug.Log($"[Event] 失去药水: {lost?.name}");
                            }
                        }
                    }
                    break;

                // ── 赌博/随机 ──
                case EventEffectType.RandomGold:
                    {
                        int amount = Random.Range(1, choice.effectValue + 1);
                        BattleModel.CurrentGold.Value += amount;
                        pd.CurrentGold = BattleModel.CurrentGold.Value;
                        Debug.Log($"[Event] 随机灵石: +{amount}");
                    }
                    break;
                case EventEffectType.RandomDamage:
                    {
                        int amount = Random.Range(1, choice.effectValue + 1);
                        ModifyAllyHealth(pd, -amount, 0);
                        Debug.Log($"[Event] 随机受伤: -{amount}HP");
                    }
                    break;
                case EventEffectType.DoubleOrNothing:
                    {
                        // effectValue = 赌注灵石数
                        if (BattleModel.CurrentGold.Value < choice.effectValue) break;
                        if (Random.value < 0.5f)
                        {
                            BattleModel.CurrentGold.Value += choice.effectValue;
                            pd.CurrentGold = BattleModel.CurrentGold.Value;
                            Debug.Log($"[Event] 赌赢! +{choice.effectValue}灵石");
                        }
                        else
                        {
                            BattleModel.CurrentGold.Value -= choice.effectValue;
                            pd.CurrentGold = BattleModel.CurrentGold.Value;
                            Debug.Log($"[Event] 赌输! -{choice.effectValue}灵石");
                        }
                    }
                    break;

                // ── 神识/法力扩展 ──
                case EventEffectType.GainMaxMana:
                    pd.MaxMana += choice.effectValue;
                    pd.CurrentMana = pd.MaxMana;
                    break;
                case EventEffectType.LoseMaxMana:
                    pd.MaxMana = Mathf.Max(1, pd.MaxMana - choice.effectValue);
                    pd.CurrentMana = pd.MaxMana;
                    break;

                // ── 小游戏 ──
                case EventEffectType.MiniSlot:
                    MiniSlot(pd, choice.effectValue);
                    break;
                case EventEffectType.MiniDice:
                    MiniDice(pd, choice.effectValue);
                    break;
                case EventEffectType.MiniPinball:
                    MiniPinball(pd, choice.effectValue);
                    break;
                case EventEffectType.MiniRingToss:
                    MiniRingToss(pd, choice.effectValue);
                    break;
                case EventEffectType.MiniBalloon:
                    MiniBalloon(pd, choice.effectValue);
                    break;

                // ── 状态类（下场战斗生效）──
                case EventEffectType.GainWeak:
                    BattleModel.PendingEnemyWeak = choice.effectValue;
                    break;
                case EventEffectType.GainFrail:
                    BattleModel.PendingEnemyFrail = choice.effectValue;
                    break;
                case EventEffectType.GainVulnerable:
                    BattleModel.PendingEnemyVulnerable = choice.effectValue;
                    break;
                case EventEffectType.GainThorn:
                    BattleModel.PendingThorn = choice.effectValue;
                    break;
                case EventEffectType.GainBlockStart:
                    BattleModel.PendingBlockStart = choice.effectValue;
                    break;
                case EventEffectType.EnemyHpReduce:
                    BattleModel.PendingEnemyHpReduce = choice.effectValue;
                    break;
                case EventEffectType.CleanseAll:
                    // 清除所有pending负面
                    BattleModel.PendingEnemyWeak = 0;
                    BattleModel.PendingEnemyFrail = 0;
                    BattleModel.PendingEnemyVulnerable = 0;
                    // 也恢复HP
                    FullHealAlly(pd);
                    break;

                // ── 卡牌扩展 ──
                case EventEffectType.TransformCard:
                    {
                        if (pd.CurrentCardsList.Count > 0)
                        {
                            int idx = Random.Range(0, pd.CurrentCardsList.Count);
                            var oldCard = pd.CurrentCardsList[idx];
                            var newCard = gm.GameplayData.AllCardsList[Random.Range(0, gm.GameplayData.AllCardsList.Count)];
                            pd.CurrentCardsList[idx] = newCard;
                            Debug.Log($"[Event] 变形卡牌: {oldCard.CardName} → {newCard.CardName}");
                        }
                    }
                    break;
                case EventEffectType.GainPathCard:
                    {
                        // effectValue: 0=剑道 1=体道 2=灵道
                        var pathCards = gm.GameplayData.AllCardsList.FindAll(c =>
                        {
                            var id = c.Id.ToLower();
                            return (choice.effectValue == 0 && (id.Contains("jian") || id.Contains("attack") || id.Contains("fast"))) ||
                                   (choice.effectValue == 1 && (id.Contains("body") || id.Contains("block") || id.Contains("heal"))) ||
                                   (choice.effectValue == 2 && (id.Contains("spr") || id.Contains("mana") || id.Contains("draw")));
                        });
                        if (pathCards.Count > 0)
                            pd.CurrentCardsList.Add(pathCards[Random.Range(0, pathCards.Count)]);
                    }
                    break;
                case EventEffectType.GainRarityCard:
                    {
                        // effectValue: 0=Common 1=Uncommon 2=Rare 3=Legendary
                        var rarityCards = gm.GameplayData.AllCardsList.FindAll(c => (int)c.Rarity == choice.effectValue);
                        if (rarityCards.Count > 0)
                            pd.CurrentCardsList.Add(rarityCards[Random.Range(0, rarityCards.Count)]);
                    }
                    break;
                case EventEffectType.ExhaustCard:
                    {
                        if (pd.CurrentCardsList.Count > 0)
                        {
                            int idx = Random.Range(0, pd.CurrentCardsList.Count);
                            var lost = pd.CurrentCardsList[idx];
                            pd.CurrentCardsList.RemoveAt(idx);
                            Debug.Log($"[Event] 消耗卡牌: {lost.CardName}");
                        }
                    }
                    break;
                case EventEffectType.TradeCardForMaterial:
                    {
                        if (pd.CurrentCardsList.Count > 0)
                        {
                            int idx = Random.Range(0, pd.CurrentCardsList.Count);
                            pd.CurrentCardsList.RemoveAt(idx);
                            GrantRandomMaterial(0);
                            Debug.Log("[Event] 用一张卡换取灵材");
                        }
                    }
                    break;

                // ── 资源扩展 ──
                case EventEffectType.LoseMaterial:
                    {
                        var slots = InventoryModel?.Slots;
                        if (slots != null)
                        {
                            var nonEmpty = slots.FindAll(s => s != null && s.item != null);
                            if (nonEmpty.Count > 0)
                            {
                                var slot = nonEmpty[Random.Range(0, nonEmpty.Count)];
                                InventorySystem.RemoveItem(slot.item.ItemId, 1);
                                Debug.Log($"[Event] 失去灵材: {slot.item.ItemName}");
                            }
                        }
                    }
                    break;
                case EventEffectType.GainGoldByMaterial:
                    {
                        var slots = InventoryModel?.Slots;
                        int materialCount = slots?.FindAll(s => s != null && s.item != null).Count ?? 0;
                        int gold = materialCount * choice.effectValue;
                        BattleModel.CurrentGold.Value += gold;
                        pd.CurrentGold = BattleModel.CurrentGold.Value;
                        Debug.Log($"[Event] 按灵材获灵石: {materialCount}个灵材 → +{gold}灵石");
                    }
                    break;
                case EventEffectType.TradeHpForGold:
                    {
                        ModifyAllyHealth(pd, -choice.effectValue, 0);
                        int gold = choice.effectValue * 10;
                        BattleModel.CurrentGold.Value += gold;
                        pd.CurrentGold = BattleModel.CurrentGold.Value;
                        Debug.Log($"[Event] HP换灵石: -{choice.effectValue}HP → +{gold}灵石");
                    }
                    break;
                case EventEffectType.TradeGoldForHp:
                    {
                        if (BattleModel.CurrentGold.Value >= choice.effectValue)
                        {
                            BattleModel.CurrentGold.Value -= choice.effectValue;
                            pd.CurrentGold = BattleModel.CurrentGold.Value;
                            ModifyAllyHealth(pd, choice.effectValue / 5, 0);
                            Debug.Log($"[Event] 灵石回血: -{choice.effectValue}灵石 → +{choice.effectValue / 5}HP");
                        }
                    }
                    break;

                // ── 新小游戏 ──
                case EventEffectType.MiniLottery:
                    MiniLottery(pd, choice.effectValue);
                    break;
                case EventEffectType.MiniWheel:
                    MiniWheel(pd, choice.effectValue);
                    break;
                case EventEffectType.MiniCoinFlip:
                    MiniCoinFlip(pd, choice.effectValue);
                    break;
                case EventEffectType.MiniCardGuess:
                    MiniCardGuess(pd, choice.effectValue);
                    break;
                case EventEffectType.MiniTreasureHunt:
                    MiniTreasureHunt(pd, choice.effectValue);
                    break;

                case EventEffectType.Nothing:
                default:
                    break;
            }

            Debug.Log($"[Event] Executed choice: {choice.choiceText} ({choice.effectType}:{choice.effectValue})");
        }

        // ========== 小游戏实现 ==========

        /// <summary>仙缘抽签：消耗灵石抽签，不同签获不同奖励</summary>
        void MiniLottery(NueGames.NueDeck.Scripts.Data.Settings.PersistentGameplayData pd, int cost)
        {
            if (BattleModel.CurrentGold.Value < cost) { Debug.Log("[MiniLottery] 灵石不足"); return; }
            BattleModel.CurrentGold.Value -= cost;
            pd.CurrentGold = BattleModel.CurrentGold.Value;

            int stick = Random.Range(0, 100);
            string result;
            if (stick < 15) { result = "下签 无奖励"; }
            else if (stick < 40) { int g = cost; BattleModel.CurrentGold.Value += g; pd.CurrentGold = BattleModel.CurrentGold.Value; result = $"中签 +{g}灵石"; }
            else if (stick < 65) { GrantRandomMaterial(0); result = "中签 获得凡品灵材"; }
            else if (stick < 82) { GrantRandomMaterial(1); result = "上签 获得灵品灵材"; }
            else if (stick < 92) { var c = LoadRandomCard(pd); if (c != null) pd.CurrentCardsList.Add(c); result = $"上签 获得卡牌 {c?.CardName}"; }
            else if (stick < 98) { var p = LoadRandomPotion(); if (p != null) PotionSystem.ObtainPotion(p); result = "上上签 获得药水"; }
            else { var r = LoadRandomRelic(); if (r != null) RelicSystem.AddRelic(r); result = $"上上签 获得遗物 {r?.name}!"; }

            Debug.Log($"[MiniLottery] 消耗{cost}灵石 → {result}");
        }

        /// <summary>命运转盘：消耗灵石转盘，不同区域不同奖励</summary>
        void MiniWheel(NueGames.NueDeck.Scripts.Data.Settings.PersistentGameplayData pd, int cost)
        {
            if (BattleModel.CurrentGold.Value < cost) { Debug.Log("[MiniWheel] 灵石不足"); return; }
            BattleModel.CurrentGold.Value -= cost;
            pd.CurrentGold = BattleModel.CurrentGold.Value;

            int wheel = Random.Range(0, 360);
            string result;
            if (wheel < 90) { result = "空区"; }
            else if (wheel < 150) { int g = cost; BattleModel.CurrentGold.Value += g; pd.CurrentGold = BattleModel.CurrentGold.Value; result = $"灵石区 +{g}灵石"; }
            else if (wheel < 210) { GrantRandomMaterial(0); result = "凡品灵材区"; }
            else if (wheel < 260) { int g = cost * 2; BattleModel.CurrentGold.Value += g; pd.CurrentGold = BattleModel.CurrentGold.Value; result = $"双倍灵石区 +{g}灵石"; }
            else if (wheel < 300) { GrantRandomMaterial(1); result = "灵品灵材区"; }
            else if (wheel < 335) { var c = LoadRandomCard(pd); if (c != null) pd.CurrentCardsList.Add(c); result = $"卡牌区 {c?.CardName}"; }
            else if (wheel < 350) { var p = LoadRandomPotion(); if (p != null) PotionSystem.ObtainPotion(p); result = "药水区"; }
            else { var r = LoadRandomRelic(); if (r != null) RelicSystem.AddRelic(r); result = $"遗物区 {r?.name}!"; }

            Debug.Log($"[MiniWheel] 消耗{cost}灵石 → 转盘{wheel}° → {result}");
        }

        /// <summary>灵币翻面：消耗灵石猜正反，猜对翻倍</summary>
        void MiniCoinFlip(NueGames.NueDeck.Scripts.Data.Settings.PersistentGameplayData pd, int cost)
        {
            if (BattleModel.CurrentGold.Value < cost) { Debug.Log("[MiniCoinFlip] 灵石不足"); return; }
            BattleModel.CurrentGold.Value -= cost;
            pd.CurrentGold = BattleModel.CurrentGold.Value;

            bool heads = Random.value < 0.5f;
            string side = heads ? "正" : "反";
            // 50% chance to win
            if (Random.value < 0.5f)
            {
                int g = cost * 2;
                BattleModel.CurrentGold.Value += g;
                pd.CurrentGold = BattleModel.CurrentGold.Value;
                Debug.Log($"[MiniCoinFlip] 消耗{cost}灵石 → 翻面{side} → 猜对! +{g}灵石");
            }
            else
            {
                Debug.Log($"[MiniCoinFlip] 消耗{cost}灵石 → 翻面{side} → 猜错! -{cost}灵石");
            }
        }

        /// <summary>猜牌大小：消耗灵石，连续猜大小，每次猜对奖励翻倍，可中途退出</summary>
        void MiniCardGuess(NueGames.NueDeck.Scripts.Data.Settings.PersistentGameplayData pd, int cost)
        {
            if (BattleModel.CurrentGold.Value < cost) { Debug.Log("[MiniCardGuess] 灵石不足"); return; }
            BattleModel.CurrentGold.Value -= cost;
            pd.CurrentGold = BattleModel.CurrentGold.Value;

            int reward = cost;
            int round = 0;
            while (round < 5) // 最多5轮
            {
                round++;
                // 50% chance to "guess correctly"
                if (Random.value < 0.5f)
                {
                    reward *= 2;
                }
                else
                {
                    Debug.Log($"[MiniCardGuess] 第{round}轮猜错，损失全部。最终: 0灵石");
                    return;
                }
            }
            // 猜对5轮
            BattleModel.CurrentGold.Value += reward;
            pd.CurrentGold = BattleModel.CurrentGold.Value;
            Debug.Log($"[MiniCardGuess] 连猜{round}轮全对! 获得灵石×{reward / cost} = +{reward}灵石");
        }

        /// <summary>寻宝迷踪：消耗灵石三选一开宝箱，有陷阱</summary>
        void MiniTreasureHunt(NueGames.NueDeck.Scripts.Data.Settings.PersistentGameplayData pd, int cost)
        {
            if (BattleModel.CurrentGold.Value < cost) { Debug.Log("[MiniTreasureHunt] 灵石不足"); return; }
            BattleModel.CurrentGold.Value -= cost;
            pd.CurrentGold = BattleModel.CurrentGold.Value;

            int chest = Random.Range(0, 3);
            string result;
            if (chest == 0)
            {
                // 陷阱箱
                ModifyAllyHealth(pd, -cost / 3, 0);
                result = $"陷阱箱! -{cost / 3}HP";
            }
            else if (chest == 1)
            {
                // 普通宝箱
                int g = cost * 2;
                BattleModel.CurrentGold.Value += g;
                pd.CurrentGold = BattleModel.CurrentGold.Value;
                result = $"普通宝箱 +{g}灵石";
            }
            else
            {
                // 稀有宝箱
                int roll = Random.Range(0, 3);
                if (roll == 0) { GrantRandomMaterial(1); result = "稀有宝箱 获得灵品灵材"; }
                else if (roll == 1) { var c = LoadRandomCard(pd); if (c != null) pd.CurrentCardsList.Add(c); result = $"稀有宝箱 获得卡牌 {c?.CardName}"; }
                else { var p = LoadRandomPotion(); if (p != null) PotionSystem.ObtainPotion(p); result = "稀有宝箱 获得药水"; }
            }

            Debug.Log($"[MiniTreasureHunt] 消耗{cost}灵石 → {result}");
        }

        /// <summary>灵石机（老虎机）：消耗灵石，随机摇奖</summary>
        void MiniSlot(NueGames.NueDeck.Scripts.Data.Settings.PersistentGameplayData pd, int cost)
        {
            if (BattleModel.CurrentGold.Value < cost) { Debug.Log("[MiniSlot] 灵石不足"); return; }
            BattleModel.CurrentGold.Value -= cost;
            pd.CurrentGold = BattleModel.CurrentGold.Value;

            int roll = Random.Range(0, 100);
            string result;
            if (roll < 30) { result = "空奖"; }
            else if (roll < 50) { BattleModel.CurrentGold.Value += cost; pd.CurrentGold = BattleModel.CurrentGold.Value; result = $"回本 +{cost}灵石"; }
            else if (roll < 70) { int g = cost * 2; BattleModel.CurrentGold.Value += g; pd.CurrentGold = BattleModel.CurrentGold.Value; result = $"小奖 +{g}灵石"; }
            else if (roll < 85) { int g = cost * 3; BattleModel.CurrentGold.Value += g; pd.CurrentGold = BattleModel.CurrentGold.Value; result = $"中奖 +{g}灵石"; }
            else if (roll < 92) { GrantRandomMaterial(0); result = "获得凡品灵材"; }
            else if (roll < 96) { GrantRandomMaterial(1); result = "获得灵品灵材"; }
            else if (roll < 99) { var r = LoadRandomRelic(); if (r != null) RelicSystem.AddRelic(r); result = $"获得遗物 {r?.name}"; }
            else { int g = cost * 10; BattleModel.CurrentGold.Value += g; pd.CurrentGold = BattleModel.CurrentGold.Value; result = $"大奖 +{g}灵石!"; }

            Debug.Log($"[MiniSlot] 消耗{cost}灵石 → {result}");
        }

        /// <summary>掷骰问运：消耗灵石掷骰，点数决定奖励倍率</summary>
        void MiniDice(NueGames.NueDeck.Scripts.Data.Settings.PersistentGameplayData pd, int cost)
        {
            if (BattleModel.CurrentGold.Value < cost) { Debug.Log("[MiniDice] 灵石不足"); return; }
            BattleModel.CurrentGold.Value -= cost;
            pd.CurrentGold = BattleModel.CurrentGold.Value;

            int dice = Random.Range(1, 7);
            string result;
            if (dice <= 2) { result = $"{dice}点 空奖"; }
            else if (dice <= 4) { int g = cost; BattleModel.CurrentGold.Value += g; pd.CurrentGold = BattleModel.CurrentGold.Value; result = $"{dice}点 回本 +{g}灵石"; }
            else if (dice == 5) { int g = cost * 2; BattleModel.CurrentGold.Value += g; pd.CurrentGold = BattleModel.CurrentGold.Value; result = $"5点 双倍 +{g}灵石"; }
            else { int g = cost * 5; BattleModel.CurrentGold.Value += g; pd.CurrentGold = BattleModel.CurrentGold.Value; result = $"6点 五倍 +{g}灵石!"; }

            Debug.Log($"[MiniDice] 消耗{cost}灵石 → {result}");
        }

        /// <summary>灵珠弹射：消耗灵石，弹珠随机落入不同区域</summary>
        void MiniPinball(NueGames.NueDeck.Scripts.Data.Settings.PersistentGameplayData pd, int cost)
        {
            if (BattleModel.CurrentGold.Value < cost) { Debug.Log("[MiniPinball] 灵石不足"); return; }
            BattleModel.CurrentGold.Value -= cost;
            pd.CurrentGold = BattleModel.CurrentGold.Value;

            int zone = Random.Range(0, 100);
            string result;
            if (zone < 25) { result = "落入空区"; }
            else if (zone < 50) { int g = cost; BattleModel.CurrentGold.Value += g; pd.CurrentGold = BattleModel.CurrentGold.Value; result = $"灵石区 +{g}灵石"; }
            else if (zone < 70) { GrantRandomMaterial(0); result = "灵材区 获得凡品灵材"; }
            else if (zone < 85) { GrantRandomMaterial(1); result = "灵材区 获得灵品灵材"; }
            else if (zone < 95) { var p = LoadRandomPotion(); if (p != null) PotionSystem.ObtainPotion(p); result = "药水区 获得药水"; }
            else { var c = LoadRandomCard(pd); if (c != null) pd.CurrentCardsList.Add(c); result = $"卡牌区 获得卡牌 {c?.CardName}"; }

            Debug.Log($"[MiniPinball] 消耗{cost}灵石 → {result}");
        }

        /// <summary>套灵兽：消耗灵石投掷，套中不同灵兽获不同奖励</summary>
        void MiniRingToss(NueGames.NueDeck.Scripts.Data.Settings.PersistentGameplayData pd, int cost)
        {
            if (BattleModel.CurrentGold.Value < cost) { Debug.Log("[MiniRingToss] 灵石不足"); return; }
            BattleModel.CurrentGold.Value -= cost;
            pd.CurrentGold = BattleModel.CurrentGold.Value;

            int hit = Random.Range(0, 100);
            string result;
            if (hit < 35) { result = "套空"; }
            else if (hit < 60) { int g = cost; BattleModel.CurrentGold.Value += g; pd.CurrentGold = BattleModel.CurrentGold.Value; result = $"套中小灵兽 +{g}灵石"; }
            else if (hit < 80) { GrantRandomMaterial(0); result = "套中灵兔 获得凡品灵材"; }
            else if (hit < 92) { GrantRandomMaterial(1); result = "套中灵狐 获得灵品灵材"; }
            else if (hit < 98) { var p = LoadRandomPotion(); if (p != null) PotionSystem.ObtainPotion(p); result = "套中灵鹿 获得药水"; }
            else { var r = LoadRandomRelic(); if (r != null) RelicSystem.AddRelic(r); result = $"套中灵龙 获得遗物 {r?.name}!"; }

            Debug.Log($"[MiniRingToss] 消耗{cost}灵石 → {result}");
        }

        /// <summary>灵气球：消耗灵石射击，命中不同气球获不同奖励</summary>
        void MiniBalloon(NueGames.NueDeck.Scripts.Data.Settings.PersistentGameplayData pd, int cost)
        {
            if (BattleModel.CurrentGold.Value < cost) { Debug.Log("[MiniBalloon] 灵石不足"); return; }
            BattleModel.CurrentGold.Value -= cost;
            pd.CurrentGold = BattleModel.CurrentGold.Value;

            int shot = Random.Range(0, 100);
            string result;
            if (shot < 30) { result = "射偏"; }
            else if (shot < 55) { int g = cost; BattleModel.CurrentGold.Value += g; pd.CurrentGold = BattleModel.CurrentGold.Value; result = $"击中蓝气球 +{g}灵石"; }
            else if (shot < 75) { GrantRandomMaterial(0); result = "击中绿气球 获得凡品灵材"; }
            else if (shot < 88) { int g = cost * 2; BattleModel.CurrentGold.Value += g; pd.CurrentGold = BattleModel.CurrentGold.Value; result = $"击中黄气球 +{g}灵石"; }
            else if (shot < 96) { GrantRandomMaterial(1); result = "击中紫气球 获得灵品灵材"; }
            else { int g = cost * 5; BattleModel.CurrentGold.Value += g; pd.CurrentGold = BattleModel.CurrentGold.Value; result = $"击中金气球 +{g}灵石!"; }

            Debug.Log($"[MiniBalloon] 消耗{cost}灵石 → {result}");
        }

        // ========== 小游戏工具方法 ==========
        RelicData LoadRandomRelic()
        {
            var guids = UnityEditor.AssetDatabase.FindAssets("t:RelicData", new[] { "Assets/NueGames/NueDeck/Data/Relics" });
            if (guids.Length == 0) return null;
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[Random.Range(0, guids.Length)]);
            return UnityEditor.AssetDatabase.LoadAssetAtPath<RelicData>(path);
        }

        PotionData LoadRandomPotion()
        {
            var guids = UnityEditor.AssetDatabase.FindAssets("t:PotionData", new[] { "Assets/NueGames/NueDeck/Data/Potions" });
            if (guids.Length == 0) return null;
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[Random.Range(0, guids.Length)]);
            return UnityEditor.AssetDatabase.LoadAssetAtPath<PotionData>(path);
        }

        NueGames.NueDeck.Scripts.Data.Collection.CardData LoadRandomCard(NueGames.NueDeck.Scripts.Data.Settings.PersistentGameplayData pd)
        {
            var all = pd != null ? pd.CurrentCardsList : null;
            if (all == null || all.Count == 0)
            {
                var gm = NueGames.NueDeck.Scripts.Managers.GameManager.Instance;
                if (gm == null) return null;
                all = gm.GameplayData.AllCardsList;
            }
            return all.Count > 0 ? all[Random.Range(0, all.Count)] : null;
        }

        /// <summary>
        /// 修改角色血量（通过 PersistentGameplayData，不依赖 CombatManager）
        /// </summary>
        private void ModifyAllyHealth(NueGames.NueDeck.Scripts.Data.Settings.PersistentGameplayData pd, int healthDelta, int maxHealthDelta)
        {
            if (pd.AllyHealthDataList == null || pd.AllyHealthDataList.Count == 0)
            {
                // 没有存档数据，尝试直接修改 allyList 中的角色
                if (pd.AllyList != null && pd.AllyList.Count > 0)
                {
                    var ally = pd.AllyList[0];
                    var stats = ally.CharacterStats;
                    if (stats != null)
                    {
                        if (maxHealthDelta != 0)
                            stats.IncreaseMaxHealth(maxHealthDelta);
                        if (healthDelta > 0)
                            stats.Heal(healthDelta);
                        else if (healthDelta < 0)
                            stats.Damage(-healthDelta);
                    }
                }
                return;
            }

            var hd = pd.AllyHealthDataList[0];
            if (maxHealthDelta != 0)
                hd.MaxHealth = Mathf.Max(1, hd.MaxHealth + maxHealthDelta);

            if (healthDelta > 0)
                hd.CurrentHealth = Mathf.Min(hd.MaxHealth, hd.CurrentHealth + healthDelta);
            else if (healthDelta < 0)
                hd.CurrentHealth = Mathf.Max(0, hd.CurrentHealth + healthDelta);

            pd.SetAllyHealthData(hd.CharacterId, hd.CurrentHealth, hd.MaxHealth);
        }

        private void FullHealAlly(NueGames.NueDeck.Scripts.Data.Settings.PersistentGameplayData pd)
        {
            if (pd.AllyHealthDataList == null || pd.AllyHealthDataList.Count == 0)
            {
                if (pd.AllyList != null && pd.AllyList.Count > 0)
                {
                    var ally = pd.AllyList[0];
                    ally.CharacterStats?.Heal(ally.CharacterStats.MaxHealth);
                }
                return;
            }
            var hd = pd.AllyHealthDataList[0];
            hd.CurrentHealth = hd.MaxHealth;
            pd.SetAllyHealthData(hd.CharacterId, hd.MaxHealth, hd.MaxHealth);
        }

        /// <summary>
        /// 随机给予灵材（按品阶筛选）
        /// </summary>
        private void GrantRandomMaterial(int rarityIndex)
        {
            var matDir = "Assets/NueGames/NueDeck/Data/Materials";
            var guids = UnityEditor.AssetDatabase.FindAssets("t:MaterialData", new[] { matDir });
            if (guids.Length == 0) return;

            var matRarities = new[] { "FanPin", "LingPin", "XuanPin", "XianPin" };
            var targetRarity = rarityIndex >= 0 && rarityIndex < matRarities.Length
                ? matRarities[rarityIndex]
                : matRarities[0];

            var validPaths = new List<string>();
            foreach (var g in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<MaterialData>(path);
                if (mat != null && mat.rarity.ToString() == targetRarity)
                    validPaths.Add(path);
            }
            if (validPaths.Count == 0)
            {
                // 没有指定品阶的，取任意
                foreach (var g in guids)
                    validPaths.Add(UnityEditor.AssetDatabase.GUIDToAssetPath(g));
            }
            if (validPaths.Count == 0) return;

            var picked = UnityEditor.AssetDatabase.LoadAssetAtPath<MaterialData>(
                validPaths[Random.Range(0, validPaths.Count)]);
            if (picked != null)
            {
                InventorySystem.AddItem(picked, 1);
                Debug.Log($"[Event] 获得灵材: {picked.name}");
            }
        }

        /// <summary>
        /// 随机解锁一个未解锁的配方
        /// </summary>
        private void GrantRandomRecipe()
        {
            var recipeDir = "Assets/NueGames/NueDeck/Data/Recipes";
            var guids = UnityEditor.AssetDatabase.FindAssets("t:RecipeData", new[] { recipeDir });
            if (guids.Length == 0) return;

            var locked = new List<RecipeData>();
            foreach (var g in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                var recipe = UnityEditor.AssetDatabase.LoadAssetAtPath<RecipeData>(path);
                if (recipe != null && !recipe.unlockByDefault && !CraftSystem.IsRecipeUnlocked(recipe.recipeId))
                    locked.Add(recipe);
            }
            if (locked.Count == 0) return;

            var picked = locked[Random.Range(0, locked.Count)];
            CraftSystem.UnlockRecipe(picked.recipeId);
            Debug.Log($"[Event] 解锁配方: {picked.name}");
        }

        void LoadEventPool()
        {
            if (_eventPool != null) return;
            _eventPool = new List<EventData>();
            var guids = UnityEditor.AssetDatabase.FindAssets("t:EventData", new[] { "Assets/NueGames/NueDeck/Data/Events" });
            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var evt = UnityEditor.AssetDatabase.LoadAssetAtPath<EventData>(path);
                if (evt != null) _eventPool.Add(evt);
            }
            Debug.Log($"[EventSystem] Loaded {_eventPool.Count} events from Events folder");
            if (_eventPool.Count == 0)
                Debug.LogWarning("[EventSystem] No events found in Assets/NueGames/NueDeck/Data/Events");
        }
    }
}
