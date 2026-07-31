using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using NueGames.NueDeck.Scripts.Data.Collection;
using NueGames.NueDeck.Scripts.Managers;

namespace CardGame.UI
{
    public class LoadoutUIController : MonoBehaviour, IController
    {
        [Header("References")]
        [SerializeField] private Transform basicCardRoot;
        [SerializeField] private Transform selectedCardRoot;
        [SerializeField] private Transform cardPoolRoot;
        [SerializeField] private GameObject cardItemPrefab;
        [SerializeField] private TextMeshProUGUI shenShiText;
        [SerializeField] private Button startButton;
        [SerializeField] private Button backButton;

        private ILoadoutModel _model;
        private ILoadoutSystem _system;

        public IArchitecture GetArchitecture() => CardGameArchitecture.Interface;

        private void Awake()
        {
            _model = this.GetModel<ILoadoutModel>();
            _system = this.GetSystem<ILoadoutSystem>();
        }

        private void OnEnable()
        {
            _model.CurrentShenShi.RegisterWithInitValue(UpdateShenShi).UnRegisterWhenGameObjectDestroyed(gameObject);
            RefreshAll();
        }

        private void UpdateShenShi(int value)
        {
            if (shenShiText) shenShiText.text = $"神识: {value}/{_model.MaxShenShi.Value}";
        }

        private void RefreshAll()
        {
            RefreshBasicCards();
            RefreshSelectedCards();
            RefreshCardPool();
        }

        private void RefreshBasicCards()
        {
            if (basicCardRoot == null) return;
            ClearChildren(basicCardRoot);
            foreach (var cardId in _model.BasicCardIds)
            {
                var card = FindCard(cardId);
                if (card != null) CreateCardItem(basicCardRoot, card, false, 0, false);
            }
        }

        private void RefreshSelectedCards()
        {
            if (selectedCardRoot == null) return;
            ClearChildren(selectedCardRoot);
            var gm = GameManager.Instance;
            if (gm == null) return;

            foreach (var cardId in _model.SelectedCardIds)
            {
                var card = FindCard(cardId);
                if (card != null) CreateCardItem(selectedCardRoot, card, true, GetCardShenShi(cardId), true);
            }
        }

        private void RefreshCardPool()
        {
            if (cardPoolRoot == null) return;
            ClearChildren(cardPoolRoot);
            var gm = GameManager.Instance;
            if (gm == null) return;

            foreach (var card in gm.GameplayData.AllCardsList)
            {
                if (_model.BasicCardIds.Contains(card.Id)) continue;
                if (_model.SelectedCardIds.Contains(card.Id)) continue;
                int cost = GetCardShenShi(card.Id);
                CreateCardItem(cardPoolRoot, card, false, cost, true);
            }
        }

        private void CreateCardItem(Transform parent, CardData card, bool isSelected, int shenShiCost, bool canInteract)
        {
            var go = new GameObject("Card_" + card.Id);
            go.transform.SetParent(parent);
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(160, 100);
            go.AddComponent<Image>().color = new Color(0.1f, 0.15f, 0.25f, 1);

            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 3;
            layout.padding = new RectOffset(5, 5, 5, 5);
            layout.childAlignment = TextAnchor.MiddleCenter;

            CreateText(go, card.CardName, 18, Color.white);
            CreateText(go, shenShiCost > 0 ? $"绁炶瘑:{shenShiCost}" : "鏈懡", 14, shenShiCost > 0 ? new Color(0.3f, 0.6f, 0.9f) : new Color(0.6f, 0.6f, 0.6f));

            if (canInteract)
            {
                var btn = go.AddComponent<Button>();
                var cardId = card.Id;
                var cost = shenShiCost;
                if (isSelected)
                {
                    btn.onClick.AddListener(() =>
                    {
                        _system.DeselectCard(cardId, cost);
                        UpdateShenShi(_model.CurrentShenShi.Value);
                        RefreshAll();
                    });
                }
                else
                {
                    btn.onClick.AddListener(() =>
                    {
                        if (_system.SelectCard(cardId, cost))
                        {
                            UpdateShenShi(_model.CurrentShenShi.Value);
                            RefreshAll();
                        }
                    });
                }
            }
        }

        private int GetCardShenShi(string cardId)
        {
            return cardId switch
            {
                "1_attack_basic" => 0,
                "3_block_basic" => 0,
                "2_attack_fast" => 2,
                "4_draw_basic" => 1,
                "5_heal_basic" => 1,
                "6_power_maxhealth" => 2,
                "7_power_strength" => 2,
                "8_skill_earnMana" => 2,
                "9_attack_lifeSteal" => 3,
                "card_weak" => 1,
                "card_vulnerable" => 1,
                "card_frail" => 1,
                "card_weak_strike" => 2,
                _ => 1
            };
        }

        private CardData FindCard(string cardId)
        {
            var gm = GameManager.Instance;
            return gm?.GameplayData.AllCardsList.Find(c => c.Id == cardId);
        }

        private void ClearChildren(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
                Destroy(t.GetChild(i).gameObject);
        }

        private void CreateText(GameObject parent, string text, float size, Color color)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent.transform);
            go.AddComponent<RectTransform>();
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = size;
            tmp.color = color;
            var libSans = UnityEngine.Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (libSans) tmp.font = libSans;
        }

        public void OnStartAdventure()
        {
            _system.StartAdventure();
        }

        public void OnBackButton()
        {
            gameObject.SetActive(false);
        }
    }
}
