using TMPro;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using System.Collections.Generic;
using NueGames.NueDeck.Scripts.Data.Collection;

namespace CardGame.UI
{
    public class CampfireUIController : MonoBehaviour, IController
    {
        [SerializeField] private Button restButton;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Transform upgradeCardListRoot;
        [SerializeField] private GameObject cardItemPrefab;
        [SerializeField] private Button backButton;

        private ICampfireSystem _system;

        public IArchitecture GetArchitecture() => CardGameArchitecture.Interface;

        private void Awake()
        {
            _system = this.GetSystem<ICampfireSystem>();
        }

        private void OnEnable()
        {
            if (restButton) restButton.onClick.AddListener(OnRest);
            if (upgradeButton) upgradeButton.onClick.AddListener(ShowUpgradeCards);
            if (backButton) backButton.onClick.AddListener(OnBack);
        }

        private void OnRest()
        {
            _system.Rest();
            gameObject.SetActive(false);
        }

        private void ShowUpgradeCards()
        {
            if (upgradeCardListRoot == null) { gameObject.SetActive(false); return; }

            for (int i = upgradeCardListRoot.childCount - 1; i >= 0; i--)
                Destroy(upgradeCardListRoot.GetChild(i).gameObject);

            var gm = NueGames.NueDeck.Scripts.Managers.GameManager.Instance;
            if (gm == null) return;

            var upgradeable = new List<CardData>();
            foreach (var card in gm.PersistentGameplayData.CurrentCardsList)
            {
                if (card.HasUpgradeData && !card.IsUpgraded && !upgradeable.Contains(card))
                    upgradeable.Add(card);
            }

            if (upgradeable.Count == 0)
            {
                gameObject.SetActive(false);
                return;
            }

            upgradeCardListRoot.gameObject.SetActive(true);
            foreach (var card in upgradeable)
            {
                var go = new GameObject("Card_" + card.Id);
                go.transform.SetParent(upgradeCardListRoot);
                go.AddComponent<RectTransform>().sizeDelta = new Vector2(160, 80);
                go.AddComponent<Image>().color = new Color(0.1f, 0.15f, 0.25f, 1);
                CreateText(go, card.CardName, 18, Color.white);
                CreateText(go, "鈫?" + (string.IsNullOrEmpty(card.name) ? card.CardName + "+" : card.CardName + "+"), 16, new Color(0.3f, 0.8f, 0.3f));

                var btn = go.AddComponent<Button>();
                var captured = card;
                btn.onClick.AddListener(() =>
                {
                    _system.UpgradeCard(captured);
                    gameObject.SetActive(false);
                });
            }
        }

        public void OnBack()
        {
            gameObject.SetActive(false);
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
    }
}
