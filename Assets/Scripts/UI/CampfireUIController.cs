using TMPro;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using System.Collections.Generic;
using NueGames.NueDeck.Scripts.Data.Collection;
using Alchemy.Inspector;
using CardGame.Audio;

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

            // 运行时自动绑定按钮
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                var name = btn.gameObject.name;
                if (name == "Button_0") { restButton = btn; btn.onClick.AddListener(OnRest); }
                else if (name == "Button_1") { upgradeButton = btn; btn.onClick.AddListener(ShowUpgradeCards); }
            }
            // 查找或创建返回按钮
            backButton = null;
            foreach (var btn in buttons)
            {
                if (btn.gameObject.name.Contains("Back") || btn.gameObject.name.Contains("Close")) { backButton = btn; break; }
            }
            if (backButton == null)
            {
                var go = new GameObject("CloseButton");
                var panel = transform.GetChild(0);
                go.transform.SetParent(panel);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(1, 1); rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(1, 1);
                rt.anchoredPosition = new Vector2(-10, -10);
                rt.sizeDelta = new Vector2(80, 40);
                go.AddComponent<Image>().color = new Color(0.6f, 0.15f, 0.15f, 1);
                var txtObj = new GameObject("Text");
                txtObj.transform.SetParent(go.transform);
                var txtRt = txtObj.AddComponent<RectTransform>();
                txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
                txtRt.offsetMin = Vector2.zero; txtRt.offsetMax = Vector2.zero;
                var tmp = txtObj.AddComponent<TextMeshProUGUI>();
                tmp.text = "离开";
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 18; tmp.color = Color.white;
                var libSans = UnityEngine.Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                if (libSans) tmp.font = libSans;
                backButton = go.AddComponent<Button>();
            }
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(() => { if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick); OnBack(); });
        }

        private void OnEnable()
        {
        }

        private void OnRest()
        {
            if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick);
            _system.Rest();
            gameObject.SetActive(false);
            NotifyMapRefresh();
        }

        private void ShowUpgradeCards()
        {
            if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick);
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
                    NotifyMapRefresh();
                });
            }
        }

        public void OnBack()
        {
            gameObject.SetActive(false);
            NotifyMapRefresh();
        }

        private void NotifyMapRefresh()
        {
            var mapMgr = FindObjectOfType<NueGames.NueDeck.Scripts.Managers.MapManager>();
            if (mapMgr != null) mapMgr.SendMessage("BuildMap");
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
