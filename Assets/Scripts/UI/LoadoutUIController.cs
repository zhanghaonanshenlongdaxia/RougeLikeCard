using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using NueGames.NueDeck.Scripts.Data.Collection;
using NueGames.NueDeck.Scripts.Managers;
using Alchemy.Inspector;
using CardGame.Audio;

namespace CardGame.UI
{
    public class LoadoutUIController : MonoBehaviour, IController
    {
        [FoldoutGroup("References")]
        [SerializeField] private Transform basicCardRoot;
        [SerializeField] private Transform selectedCardRoot;
        [SerializeField] private Transform cardPoolRoot;
        [SerializeField] private GameObject cardItemPrefab;
        [SerializeField] private TextMeshProUGUI shenShiText;
        [SerializeField] private Button startButton;
        [SerializeField] private Button backButton;
        private TMP_FontAsset _font;

        private ILoadoutModel _model;
        private ILoadoutSystem _system;

        public IArchitecture GetArchitecture() => CardGameArchitecture.Interface;

        private void Awake()
        {
            _model = this.GetModel<ILoadoutModel>();
            _system = this.GetSystem<ILoadoutSystem>();
            _font = UnityEngine.Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            BuildUI();
            UIHelper.EnsureCloseButton(this, OnBackButton);
        }

        private void BuildUI()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 60;
                var scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                gameObject.AddComponent<GraphicRaycaster>();
            }
            Transform panel = transform.Find("Panel");
            if (panel == null)
            {
                var panelObj = new GameObject("Panel");
                panelObj.transform.SetParent(transform, false);
                var panelRt = panelObj.AddComponent<RectTransform>();
                panelRt.anchorMin = new Vector2(0.05f, 0.05f); panelRt.anchorMax = new Vector2(0.95f, 0.95f);
                panelRt.offsetMin = Vector2.zero; panelRt.offsetMax = Vector2.zero;
                panelObj.AddComponent<Image>().color = new Color(0.08f, 0.1f, 0.15f, 0.95f);
                panel = panelObj.transform;
            }

            // 标题+神识
            var header = new GameObject("Header");
            header.transform.SetParent(panel, false);
            var hRt = header.AddComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0.02f, 0.93f); hRt.anchorMax = new Vector2(0.98f, 1f);
            hRt.offsetMin = Vector2.zero; hRt.offsetMax = Vector2.zero;
            var hLayout = header.AddComponent<HorizontalLayoutGroup>();
            hLayout.childControlWidth = true; hLayout.childForceExpandWidth = true;

            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(header.transform, false);
            var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "编队"; titleTmp.fontSize = 28; titleTmp.color = new Color(0.9f, 0.8f, 0.3f);
            titleTmp.alignment = TextAlignmentOptions.Left;
            if (_font) titleTmp.font = _font;

            var ssObj = new GameObject("ShenShiText");
            ssObj.transform.SetParent(header.transform, false);
            shenShiText = ssObj.AddComponent<TextMeshProUGUI>();
            shenShiText.fontSize = 22; shenShiText.color = new Color(0.3f, 0.6f, 0.9f);
            shenShiText.alignment = TextAlignmentOptions.Right;
            if (_font) shenShiText.font = _font;

            // 三栏布局
            CreateSection(panel, "本命功法", 0.02f, 0.05f, 0.3f, 0.88f, out basicCardRoot);
            CreateSection(panel, "已选卡牌", 0.35f, 0.05f, 0.63f, 0.88f, out selectedCardRoot);
            CreateSection(panel, "可选卡牌池", 0.68f, 0.05f, 0.98f, 0.88f, out cardPoolRoot, true);
        }

        private void CreateSection(Transform parent, string title, float xMin, float yMin, float xMax, float yMax, out Transform contentRoot, bool scrollable = true)
        {
            var section = new GameObject($"Section_{title}");
            section.transform.SetParent(parent, false);
            var sRt = section.AddComponent<RectTransform>();
            sRt.anchorMin = new Vector2(xMin, yMin); sRt.anchorMax = new Vector2(xMax, yMax);
            sRt.offsetMin = Vector2.zero; sRt.offsetMax = Vector2.zero;
            section.AddComponent<Image>().color = new Color(0.05f, 0.08f, 0.12f, 0.8f);

            var vlg = section.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 3; vlg.padding = new RectOffset(5, 5, 5, 5);
            vlg.childControlWidth = true; vlg.childForceExpandHeight = false;

            var tObj = new GameObject("SectionTitle");
            tObj.transform.SetParent(section.transform, false);
            var tTmp = tObj.AddComponent<TextMeshProUGUI>();
            tTmp.text = title; tTmp.fontSize = 18; tTmp.color = new Color(0.9f, 0.8f, 0.3f);
            tTmp.alignment = TextAlignmentOptions.Left;
            if (_font) tTmp.font = _font;
            var tLe = tObj.AddComponent<LayoutElement>(); tLe.preferredHeight = 25;

            if (scrollable)
            {
                var scrollObj = new GameObject("Scroll");
                scrollObj.transform.SetParent(section.transform, false);
                var scroll = scrollObj.AddComponent<ScrollRect>();
                scroll.horizontal = false; scroll.vertical = true;

                var vp = new GameObject("Viewport");
                vp.transform.SetParent(scrollObj.transform, false);
                var vpRt = vp.AddComponent<RectTransform>();
                vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
                vpRt.pivot = new Vector2(0, 1);
                vpRt.offsetMin = Vector2.zero; vpRt.offsetMax = Vector2.zero;
                var vpImg = vp.AddComponent<Image>(); vpImg.color = new Color(0.05f, 0.08f, 0.12f, 1f);
                vp.AddComponent<UnityEngine.UI.Mask>();
                scroll.viewport = vpRt;

                var content = new GameObject("Content");
                content.transform.SetParent(vp.transform, false);
                var cRt = content.AddComponent<RectTransform>();
                cRt.anchorMin = new Vector2(0, 1); cRt.anchorMax = new Vector2(1, 1);
                cRt.pivot = new Vector2(0, 1);
                cRt.offsetMin = Vector2.zero; cRt.offsetMax = Vector2.zero;
                var cvlg = content.AddComponent<VerticalLayoutGroup>();
                cvlg.spacing = 3; cvlg.childControlWidth = true; cvlg.childForceExpandHeight = false;
                var csf = content.AddComponent<ContentSizeFitter>();
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                scroll.content = cRt;
                contentRoot = content.transform;
            }
            else
            {
                var content = new GameObject("Content");
                content.transform.SetParent(section.transform, false);
                var cRt = content.AddComponent<RectTransform>();
                content.AddComponent<VerticalLayoutGroup>().spacing = 3;
                contentRoot = content.transform;
            }
        }

        private void OnEnable()
        {
            if (_model != null)
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
            go.AddComponent<RectTransform>();
            go.AddComponent<Image>().color = isSelected ? new Color(0.15f, 0.25f, 0.15f, 1f) : new Color(0.1f, 0.15f, 0.25f, 1f);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 55;

            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 2; layout.padding = new RectOffset(5, 5, 3, 3);

            CreateText(go, card.CardName, 16, Color.white);
            string costStr = shenShiCost > 0 ? $"神识:{shenShiCost}" : "本命";
            CreateText(go, costStr, 12, shenShiCost > 0 ? new Color(0.3f, 0.6f, 0.9f) : new Color(0.6f, 0.6f, 0.6f));

            if (canInteract)
            {
                var btn = go.AddComponent<Button>();
                var cardId = card.Id;
                var cost = shenShiCost;
                if (isSelected)
                {
                    btn.onClick.AddListener(() =>
                    {
                        if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick);
                        _system.DeselectCard(cardId, cost);
                        UpdateShenShi(_model.CurrentShenShi.Value);
                        RefreshAll();
                    });
                }
                else
                {
                    btn.onClick.AddListener(() =>
                    {
                        if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick);
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
            var card = FindCard(cardId);
            if (card == null) return 1;
            return (int)card.Rarity + 1 + card.PowerTier;
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
            tmp.text = text; tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = size; tmp.color = color;
            if (_font) tmp.font = _font;
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
