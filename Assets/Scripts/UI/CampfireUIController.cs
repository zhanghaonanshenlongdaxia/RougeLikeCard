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
    /// <summary>
    /// 休息处面板。Prefab 驱动，运行时只处理事件和数据。
    /// </summary>
    public class CampfireUIController : MonoBehaviour, IController
    {
        [Header("Buttons")]
        [SerializeField] private Button _restButton;
        [SerializeField] private Button _upgradeButton;
        [SerializeField] private Button _closeButton;

        [Header("Content")]
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private Transform _upgradeCardListRoot;

        private ICampfireSystem _system;

        public IArchitecture GetArchitecture() => CardGameArchitecture.Interface;

        private void Awake()
        {
            AutoBindReferences();
            _system = this.GetSystem<ICampfireSystem>();

            if (_restButton) _restButton.onClick.AddListener(OnRest);
            if (_upgradeButton) _upgradeButton.onClick.AddListener(ShowUpgradeCards);
            if (_closeButton) _closeButton.onClick.AddListener(() => {
                if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick);
                OnBack();
            });
        }

        private void AutoBindReferences()
        {
            var panel = transform.Find("Panel");
            if (panel == null) return;

            if (_restButton == null) _restButton = panel.Find("ButtonRow/RestButton")?.GetComponent<Button>();
            if (_upgradeButton == null) _upgradeButton = panel.Find("ButtonRow/UpgradeButton")?.GetComponent<Button>();
            if (_closeButton == null) _closeButton = panel.Find("CloseButton")?.GetComponent<Button>();
            if (_statusText == null) _statusText = panel.Find("StatusText")?.GetComponent<TextMeshProUGUI>();
            if (_upgradeCardListRoot == null) _upgradeCardListRoot = panel.Find("UpgradeCardList");
        }

        private void OnRest()
        {
            if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick);
            _system.Rest();
            FloatingTip.ShowSuccess("休息恢复30%生命");
            gameObject.SetActive(false);
            NotifyMapRefresh();
        }

        private void ShowUpgradeCards()
        {
            if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick);

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
                if (_statusText) _statusText.text = "没有可升级的卡牌";
                if (_upgradeCardListRoot) _upgradeCardListRoot.gameObject.SetActive(false);
                FloatingTip.ShowWarning("没有可升级的卡牌");
                return;
            }

            for (int i = _upgradeCardListRoot.childCount - 1; i >= 0; i--)
                Destroy(_upgradeCardListRoot.GetChild(i).gameObject);

            if (_statusText) _statusText.text = "选择一张卡牌进行升级：";
            _upgradeCardListRoot.gameObject.SetActive(true);

            var font = UnityEngine.Resources.Load<TMPro.TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            foreach (var card in upgradeable)
            {
                var go = new GameObject("Card_" + card.Id);
                go.transform.SetParent(_upgradeCardListRoot, false);
                var hlg = go.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = 10; hlg.padding = new RectOffset(5, 5, 5, 5);
                hlg.childControlWidth = true; hlg.childForceExpandWidth = false;
                go.AddComponent<Image>().color = new Color(0.1f, 0.15f, 0.25f, 1f);
                go.AddComponent<LayoutElement>().preferredHeight = 50;

                CreateText(go.transform, card.CardName, 16, Color.white, font);
                CreateText(go.transform, "→ " + card.CardName + "+", 16, new Color(0.3f, 0.8f, 0.3f), font);

                var btn = go.AddComponent<Button>();
                var captured = card;
                btn.onClick.AddListener(() =>
                {
                    if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick);
                    _system.UpgradeCard(captured);
                    FloatingTip.ShowSuccess($"升级卡牌: {captured.CardName} → {captured.CardName}+");
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

        private static TextMeshProUGUI CreateText(Transform parent, string text, float size, Color color, TMP_FontAsset font)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = size; tmp.color = color;
            if (font) tmp.font = font;
            return tmp;
        }
    }
}
