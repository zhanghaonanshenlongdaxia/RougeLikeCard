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

        private List<EventData> _eventPool;

        protected override void OnInit()
        {
        }

        public EventData GetRandomEvent()
        {
            LoadEventPool();
            if (_eventPool == null || _eventPool.Count == 0) return null;
            return _eventPool[Random.Range(0, _eventPool.Count)];
        }

        public void ExecuteChoice(EventData eventData, int choiceIndex)
        {
            if (eventData == null || choiceIndex >= eventData.choices.Count) return;

            var choice = eventData.choices[choiceIndex];
            var gm = NueGames.NueDeck.Scripts.Managers.GameManager.Instance;
            if (gm == null) return;

            var player = NueGames.NueDeck.Scripts.Managers.CombatManager.Instance?.CurrentMainAlly;

            switch (choice.effectType)
            {
                case EventEffectType.Heal:
                    player?.CharacterStats.Heal(choice.effectValue);
                    break;
                case EventEffectType.TakeDamage:
                    player?.CharacterStats.Damage(choice.effectValue);
                    break;
                case EventEffectType.GainGold:
                    BattleModel.CurrentGold.Value += choice.effectValue;
                    gm.PersistentGameplayData.CurrentGold = BattleModel.CurrentGold.Value;
                    break;
                case EventEffectType.LoseGold:
                    var loss = Mathf.Min(BattleModel.CurrentGold.Value, choice.effectValue);
                    BattleModel.CurrentGold.Value -= loss;
                    gm.PersistentGameplayData.CurrentGold = BattleModel.CurrentGold.Value;
                    break;
                case EventEffectType.GainMaxHP:
                    if (player != null)
                        player.CharacterStats.IncreaseMaxHealth(choice.effectValue);
                    break;
                case EventEffectType.LoseMaxHP:
                    if (player != null)
                    {
                        player.CharacterStats.IncreaseMaxHealth(-choice.effectValue);
                        player.CharacterStats.Damage(choice.effectValue);
                    }
                    break;
                case EventEffectType.GainStrength:
                    player?.CharacterStats.ApplyStatus(NueGames.NueDeck.Scripts.Enums.StatusType.Strength, choice.effectValue);
                    break;
                case EventEffectType.GainCard:
                    var card = string.IsNullOrEmpty(choice.cardId)
                        ? gm.GameplayData.AllCardsList[Random.Range(0, gm.GameplayData.AllCardsList.Count)]
                        : gm.GameplayData.AllCardsList.Find(c => c.Id == choice.cardId);
                    if (card != null)
                        gm.PersistentGameplayData.CurrentCardsList.Add(card);
                    break;
                case EventEffectType.RemoveCard:
                    if (gm.PersistentGameplayData.CurrentCardsList.Count > 0)
                        gm.PersistentGameplayData.CurrentCardsList.RemoveAt(
                            Random.Range(0, gm.PersistentGameplayData.CurrentCardsList.Count));
                    break;
                case EventEffectType.GainRelic:
                    var relicDir = "Assets/NueGames/NueDeck/Data/Relics";
                    var relicGuids = UnityEditor.AssetDatabase.FindAssets("t:RelicData", new[] { relicDir });
                    if (relicGuids.Length > 0)
                    {
                        var path = UnityEditor.AssetDatabase.GUIDToAssetPath(relicGuids[Random.Range(0, relicGuids.Length)]);
                        var relic = UnityEditor.AssetDatabase.LoadAssetAtPath<RelicData>(path);
                        if (relic != null) RelicSystem.AddRelic(relic);
                    }
                    break;
                case EventEffectType.GainPotion:
                    var potionDir = "Assets/NueGames/NueDeck/Data/Potions";
                    var potionGuids = UnityEditor.AssetDatabase.FindAssets("t:PotionData", new[] { potionDir });
                    if (potionGuids.Length > 0)
                    {
                        var path = UnityEditor.AssetDatabase.GUIDToAssetPath(potionGuids[Random.Range(0, potionGuids.Length)]);
                        var potion = UnityEditor.AssetDatabase.LoadAssetAtPath<PotionData>(path);
                        if (potion != null) PotionSystem.ObtainPotion(potion);
                    }
                    break;
                case EventEffectType.UpgradeRandomCard:
                    var upgradeable = gm.PersistentGameplayData.CurrentCardsList.FindAll(c => c.HasUpgradeData && !c.IsUpgraded);
                    if (upgradeable.Count > 0)
                        upgradeable[Random.Range(0, upgradeable.Count)].Upgrade();
                    break;
                case EventEffectType.Nothing:
                default:
                    break;
            }

            Debug.Log($"[Event] Executed choice: {choice.choiceText}");
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
            if (_eventPool.Count == 0)
                Debug.LogWarning("[EventSystem] No events found in Assets/NueGames/NueDeck/Data/Events");
        }
    }
}
