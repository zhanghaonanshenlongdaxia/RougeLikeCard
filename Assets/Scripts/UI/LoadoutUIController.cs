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
    public class LoadoutUIController : MonoBehaviour, IController, LoopScrollDataSource
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
        private LoopVerticalScrollRect _cardPoolLoopScroll;
        private LoopScrollPrefabSourceImpl _cardPoolPrefabSource;
        private GameObject _cardItemTemplate;
        private List<CardData> _cardPoolData = new List<CardData>();

        private ILoadoutModel _model;
        private ILoadoutSystem _system;

        public IArchitecture GetArchitecture() => CardGameArchitecture.Interface;

        private void Awake()
        {
            _model = this.GetModel<ILoadoutModel>();
            _system = this.GetSystem<ILoadoutSystem>();
            _font = UnityEngine.Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            AutoBindReferences();
            UIHelper.EnsureCloseButton(this, OnBackButton);
        }


        private void CreateSection(Transform parent, string title, float xMin, float yMin, float xMax, float yMax, out Transform contentRoot, bool scrollable = true, bool useLoopScroll = false)
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
                
                if (useLoopScroll)
                {
                    // 先 inactive 再 AddComponent，避免 LoopVerticalScrollRect.Awake 编辑态断言
                    scrollObj.SetActive(false);
                    _cardPoolLoopScroll = scrollObj.AddComponent<LoopVerticalScrollRect>();
                    _cardPoolLoopScroll.horizontal = false;
                    _cardPoolLoopScroll.vertical = true;
                }
                else
                {
                    var scroll = scrollObj.AddComponent<ScrollRect>();
                    scroll.horizontal = false; scroll.vertical = true;
                }

                var vp = new GameObject("Viewport");
                vp.transform.SetParent(scrollObj.transform, false);
                var vpRt = vp.AddComponent<RectTransform>();
                vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
                vpRt.pivot = new Vector2(0, 1);
                vpRt.offsetMin = Vector2.zero; vpRt.offsetMax = Vector2.zero;
                var vpImg = vp.AddComponent<Image>(); vpImg.color = new Color(0.05f, 0.08f, 0.12f, 1f);
                vp.AddComponent<UnityEngine.UI.Mask>();

                var content = new GameObject("Content");
                content.transform.SetParent(vp.transform, false);
                var cRt = content.AddComponent<RectTransform>();
                cRt.anchorMin = new Vector2(0, 1); cRt.anchorMax = new Vector2(1, 1);
                cRt.pivot = new Vector2(0, 1);
                cRt.offsetMin = Vector2.zero; cRt.offsetMax = Vector2.zero;
                var cvlg = content.AddComponent<VerticalLayoutGroup>();
                cvlg.spacing = 3; cvlg.childControlWidth = true; cvlg.childForceExpandHeight = false;

                if (useLoopScroll)
                {
                    _cardPoolLoopScroll.viewport = vpRt;
                    _cardPoolLoopScroll.content = cRt;
                    // 创建模板 + PrefabSource
                    _cardItemTemplate = CreateCardItemTemplate();
                    _cardItemTemplate.SetActive(false);
                    _cardPoolPrefabSource = new LoopScrollPrefabSourceImpl(_cardItemTemplate, scrollObj.transform);
                    _cardPoolLoopScroll.prefabSource = _cardPoolPrefabSource;
                    _cardPoolLoopScroll.dataSource = this;
                    // 配置完成后激活，Awake 断言通过
                    scrollObj.SetActive(true);
                }
                else
                {
                    var csf = content.AddComponent<ContentSizeFitter>();
                    csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                    scrollObj.GetComponent<ScrollRect>().viewport = vpRt;
                    scrollObj.GetComponent<ScrollRect>().content = cRt;
                }
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
            var gm = GameManager.Instance;
            if (gm == null) return;

            _cardPoolData.Clear();
            foreach (var card in gm.GameplayData.AllCardsList)
            {
                if (_model.BasicCardIds.Contains(card.Id)) continue;
                if (_model.SelectedCardIds.Contains(card.Id)) continue;
                _cardPoolData.Add(card);
            }

            if (_cardPoolLoopScroll != null)
            {
                _cardPoolLoopScroll.totalCount = _cardPoolData.Count;
                _cardPoolLoopScroll.RefillCells();
            }
        }

        public void ProvideData(Transform transform, int idx)
        {
            if (idx < 0 || idx >= _cardPoolData.Count) return;
            var card = _cardPoolData[idx];
            int shenShiCost = GetCardShenShi(card.Id);

            var img = transform.GetComponent<Image>();
            if (img) img.color = new Color(0.1f, 0.15f, 0.25f, 1f);

            var tmps = transform.GetComponentsInChildren<TextMeshProUGUI>();
            if (tmps.Length >= 1) tmps[0].text = card.CardName;
            if (tmps.Length >= 2) tmps[1].text = shenShiCost > 0 ? $"神识:{shenShiCost}" : "本命";

            var btn = transform.GetComponent<Button>();
            if (btn == null) btn = transform.gameObject.AddComponent<Button>();
            btn.onClick.RemoveAllListeners();
            var cardId = card.Id;
            var cost = shenShiCost;
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

        private GameObject CreateCardItemTemplate()
        {
            var go = new GameObject("CardItem");
            go.AddComponent<RectTransform>();
            go.AddComponent<Image>().color = new Color(0.1f, 0.15f, 0.25f, 1f);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 55;

            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 2; layout.padding = new RectOffset(5, 5, 3, 3);

            var nameObj = new GameObject("NameText");
            nameObj.transform.SetParent(go.transform, false);
            nameObj.AddComponent<RectTransform>();
            var nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
            nameTmp.alignment = TextAlignmentOptions.Center;
            nameTmp.fontSize = 16; nameTmp.color = Color.white;
            if (_font) nameTmp.font = _font;

            var costObj = new GameObject("CostText");
            costObj.transform.SetParent(go.transform, false);
            costObj.AddComponent<RectTransform>();
            var costTmp = costObj.AddComponent<TextMeshProUGUI>();
            costTmp.alignment = TextAlignmentOptions.Center;
            costTmp.fontSize = 12; costTmp.color = new Color(0.3f, 0.6f, 0.9f);
            if (_font) costTmp.font = _font;

            go.AddComponent<Button>();
            return go;
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
        private void AutoBindReferences()
        {
            var panel = transform.Find("Panel");
            if (panel == null) return;
            if (basicCardRoot == null) basicCardRoot = panel.Find("Section_本命功法/ScrollView/Viewport/Content");
            if (selectedCardRoot == null) selectedCardRoot = panel.Find("Section_已选卡牌/ScrollView/Viewport/Content");
            if (cardPoolRoot == null) cardPoolRoot = panel.Find("Section_可选卡牌池/ScrollView/Viewport/Content");
            if (shenShiText == null) shenShiText = panel.Find("Header/ShenShiText")?.GetComponent<TMPro.TextMeshProUGUI>();
        
            // Setup LoopScrollRect on cardPool ScrollView
            var poolSection = panel.Find("Section_可选卡牌池");
            if (poolSection != null && _cardPoolLoopScroll == null)
            {
                var scrollObj = poolSection.Find("ScrollView");
                if (scrollObj != null)
                {
                    scrollObj.gameObject.SetActive(false);
                    var oldSR2 = scrollObj.GetComponent<ScrollRect>(); if (oldSR2 != null) DestroyImmediate(oldSR2);
                    _cardPoolLoopScroll = scrollObj.gameObject.AddComponent<LoopVerticalScrollRect>();
                    _cardPoolLoopScroll.horizontal = false;
                    _cardPoolLoopScroll.vertical = true;
                    // _cardPoolPrefabSource created after template below
                    scrollObj.gameObject.SetActive(true);
                                        _cardPoolLoopScroll.dataSource = this;
                    _cardPoolLoopScroll.viewport = scrollObj.Find("Viewport")?.GetComponent<RectTransform>();
                    _cardPoolLoopScroll.content = cardPoolRoot?.GetComponent<RectTransform>();
                    _cardItemTemplate = new GameObject("CardItem");
                    _cardItemTemplate.transform.SetParent(transform, false);
                    _cardItemTemplate.SetActive(false);
                    var rt = _cardItemTemplate.AddComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(0, 50);
                    _cardItemTemplate.AddComponent<Image>().color = new Color(0.1f, 0.15f, 0.25f, 1f);
                    var nameGO = new GameObject("NameText"); nameGO.transform.SetParent(_cardItemTemplate.transform, false);
                    nameGO.AddComponent<RectTransform>();
                    var nt = nameGO.AddComponent<TMPro.TextMeshProUGUI>(); nt.fontSize = 16; nt.color = Color.white; nt.alignment = TextAlignmentOptions.Left;
                    _cardPoolPrefabSource = new LoopScrollPrefabSourceImpl(_cardItemTemplate, scrollObj);
                    _cardPoolLoopScroll.prefabSource = _cardPoolPrefabSource;
                }
            }

}

    }

}
