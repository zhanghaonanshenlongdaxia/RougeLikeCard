using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using NueGames.NueDeck.Scripts.Data.Collection;
using NueGames.NueDeck.Scripts.Enums;
using NueGames.NueDeck.Scripts.Managers;
using Alchemy.Inspector;
using CardGame.Audio;

namespace CardGame.UI
{
    /// <summary>
    /// 卡组重构UI：三栏布局
    /// 左=基础卡组(首神通7张) | 中=附加卡组(选中的非基础卡牌) | 右=所有卡牌(当前功法拥有的未选中非基础卡牌)
    /// 右→中添加，中→右移除，确认后保存。
    /// </summary>
    public class LoadoutUIController : MonoBehaviour, IController
    {
        [FoldoutGroup("References")]
        [SerializeField] private Transform baseDeckRoot;
        [SerializeField] private Transform extraDeckRoot;
        [SerializeField] private Transform allCardsRoot;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button backButton;
        [SerializeField] private TextMeshProUGUI infoText;

        private TMP_FontAsset _font;
        private ILoadoutModel _model;

        // 当前功法首神通的卡牌ID列表
        private List<string> _baseCardIds = new List<string>();
        // 当前功法所有已解锁卡牌ID列表（含基础+额外）
        private List<string> _allUnlockedCardIds = new List<string>();
        // 待确认的附加卡牌（从所有卡牌移到附加卡组的）
        private List<string> _pendingExtra = new List<string>();

        public IArchitecture GetArchitecture() => CardGameArchitecture.Interface;

        private void Awake()
        {
            _model = this.GetModel<ILoadoutModel>();
            _font = UnityEngine.Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            AutoBindReferences();
            UIHelper.EnsureCloseButton(this, OnBackButton);
        }

        private void OnEnable()
        {
            CacheCardData();
            _pendingExtra = new List<string>(_model.SelectedCardIds);
            RefreshAll();
        }

        private void CacheCardData()
        {
            var cultSystem = this.GetSystem<ICultivationSystem>();
            var activeMethod = cultSystem.GetMethodConfig(this.GetModel<ICultivationModel>().ActiveMethodId.Value ?? "");

            // 基础卡牌 = 首神通的7张
            _baseCardIds.Clear();
            if (activeMethod?.Nodes != null)
            {
                var firstNode = activeMethod.Nodes
                    .Where(n => n.Realm == RealmLevel.LianQi)
                    .OrderBy(n => n.GridIndex.y)
                    .FirstOrDefault();
                if (firstNode?.RewardIds != null)
                    _baseCardIds.AddRange(firstNode.RewardIds);
            }

            // 所有已解锁卡牌
            _allUnlockedCardIds = cultSystem.GetActiveMethodCards();
        }

        private void RefreshAll()
        {
            RefreshBaseDeck();
            RefreshExtraDeck();
            RefreshAllCards();
            UpdateInfo();
        }

        /// <summary>左侧：基础卡组（首神通7张，按品质排序）</summary>
        private void RefreshBaseDeck()
        {
            if (baseDeckRoot == null) return;
            ClearChildren(baseDeckRoot);

            // 按品质排序
            var sorted = _baseCardIds
                .Select(id => ResourceCache.GetCardsFromAllList()?.Find(c => c.Id == id))
                .Where(c => c != null)
                .OrderByDescending(c => (int)c.Rarity)
                .ToList();

            foreach (var card in sorted)
                CreateCardItem(baseDeckRoot, $"{card.CardName} [{GetRarityName(card.Rarity)}]", card.Id, false, 0, false);
        }

        /// <summary>中间：附加卡组（选中的非基础卡牌，可移除）</summary>
        private void RefreshExtraDeck()
        {
            if (extraDeckRoot == null) return;
            ClearChildren(extraDeckRoot);

            if (_pendingExtra.Count == 0)
            {
                CreateText(extraDeckRoot, "从右侧添加卡牌", 14, new Color(0.5f, 0.5f, 0.55f));
                return;
            }

            // 逐张显示
            foreach (var cardId in _pendingExtra)
            {
                CreateCardItem(extraDeckRoot, GetCardName(cardId), cardId, true, 0, true);
            }
        }

        /// <summary>右侧：所有卡牌（当前功法拥有的、未选中的非基础卡牌），已解锁排最上+按品质排序</summary>
        private void RefreshAllCards()
        {
            if (allCardsRoot == null) return;
            ClearChildren(allCardsRoot);

            // 排除基础卡牌 + 已在附加卡组中的
            var available = _allUnlockedCardIds
                .Where(id => !_baseCardIds.Contains(id))
                .Where(id => !_pendingExtra.Contains(id))
                .ToList();

            if (available.Count == 0)
            {
                CreateText(allCardsRoot, "解锁更多神通获得额外卡牌", 14, new Color(0.5f, 0.5f, 0.55f));
                return;
            }

            // 获取卡牌数据并按品质排序（Legendary > Rare > Uncommon > Common）
            var cardDataList = available
                .Select(id => {
                    var cards = ResourceCache.GetCardsFromAllList();
                    return cards?.Find(c => c.Id == id);
                })
                .Where(c => c != null)
                .OrderByDescending(c => (int)c.Rarity)
                .ToList();

            foreach (var card in cardDataList)
            {
                CreateCardItem(allCardsRoot, $"{card.CardName} [{GetRarityName(card.Rarity)}]", card.Id, false, 0, true);
            }
        }

        private string GetRarityName(RarityType rarity)
        {
            return rarity switch
            {
                RarityType.Common => "黄",
                RarityType.Uncommon => "玄",
                RarityType.Rare => "地",
                RarityType.Legendary => "天",
                _ => "?"
            };
        }

        /// <summary>创建单个卡牌item，背景色按品质区分</summary>
        private void CreateCardItem(Transform parent, string name, string cardId, bool isExtra, int count, bool interactable)
        {
            // 获取卡牌品质
            var cards = ResourceCache.GetCardsFromAllList();
            var cardData = cards?.Find(c => c.Id == cardId);
            var rarity = cardData?.Rarity ?? RarityType.Common;
            
            Color bgColor;
            if (isExtra)
                bgColor = new Color(0.15f, 0.25f, 0.15f, 1f); // 附加卡组绿色
            else
                bgColor = rarity switch
                {
                    RarityType.Common => new Color(0.1f, 0.12f, 0.16f, 0.95f),
                    RarityType.Uncommon => new Color(0.08f, 0.18f, 0.1f, 0.95f),
                    RarityType.Rare => new Color(0.15f, 0.08f, 0.18f, 0.95f),
                    RarityType.Legendary => new Color(0.2f, 0.14f, 0.04f, 0.95f),
                    _ => new Color(0.06f, 0.1f, 0.15f, 0.9f)
                };

            var go = new GameObject("Card_" + cardId);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            go.AddComponent<Image>().color = bgColor;
            go.AddComponent<LayoutElement>().preferredHeight = 40;

            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 5; hlg.padding = new RectOffset(5, 5, 3, 3);
            hlg.childControlWidth = true; hlg.childForceExpandWidth = false;

            var nameTmp = CreateText(go.transform, name, 14, Color.white);
            var le = nameTmp.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;

            if (interactable)
            {
                if (isExtra)
                {
                    // 附加卡组：显示"移除"按钮
                    var btn = CreateButton(go.transform, "移除", new Color(0.5f, 0.2f, 0.2f));
                    var capturedId = cardId;
                    btn.onClick.AddListener(() =>
                    {
                        if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick);
                        _pendingExtra.Remove(capturedId);
                        RefreshAll();
                    });
                }
                else
                {
                    // 所有卡牌：显示"添加"按钮
                    var btn = CreateButton(go.transform, "添加", new Color(0.2f, 0.5f, 0.2f));
                    var capturedId = cardId;
                    btn.onClick.AddListener(() =>
                    {
                        if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick);
                        _pendingExtra.Add(capturedId);
                        RefreshAll();
                    });
                }
            }
        }

        private void UpdateInfo()
        {
            if (infoText == null) return;
            int total = _baseCardIds.Count + _pendingExtra.Count;
            infoText.text = $"卡组总数: {total}张 (基础{_baseCardIds.Count} + 附加{_pendingExtra.Count})";
        }

        private void OnConfirm()
        {
            if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick);

            _model.SelectedCardIds.Clear();
            foreach (var id in _pendingExtra)
                _model.SelectedCardIds.Add(id);

            int total = _baseCardIds.Count + _pendingExtra.Count;
            FloatingTip.ShowSuccess($"卡组已确认，共{total}张");
            RefreshAll();
        }

        // === UI Helpers ===

        private TextMeshProUGUI CreateText(Transform parent, string text, float size, Color color)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = size; tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            if (_font) tmp.font = _font;
            return tmp;
        }

        private Button CreateButton(Transform parent, string label, Color color)
        {
            var go = new GameObject("Btn_" + label);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            go.AddComponent<Image>().color = color;
            go.AddComponent<LayoutElement>().preferredWidth = 50;
            CreateText(go.transform, label, 13, Color.white);
            var btn = go.AddComponent<Button>();
            return btn;
        }

        private string GetCardName(string cardId)
        {
            var cards = ResourceCache.GetCardsFromAllList();
            if (cards == null) return "未知";
            var card = cards.Find(c => c.Id == cardId);
            return card != null ? card.CardName : "未知";
        }

        private void ClearChildren(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
                Destroy(t.GetChild(i).gameObject);
        }

        public void OnBackButton()
        {
            gameObject.SetActive(false);
        }

        private void AutoBindReferences()
        {
            var panel = transform.Find("Panel");
            if (panel == null) return;

            if (baseDeckRoot == null) baseDeckRoot = panel.Find("Section_当前卡组/ScrollView/Viewport/Content");
            if (extraDeckRoot == null) extraDeckRoot = panel.Find("Section_待确认/ScrollView/Viewport/Content");
            if (allCardsRoot == null) allCardsRoot = panel.Find("Section_可选卡牌/ScrollView/Viewport/Content");
            if (infoText == null) infoText = panel.Find("Header/InfoText")?.GetComponent<TextMeshProUGUI>();
            if (confirmButton == null) confirmButton = panel.Find("ConfirmButton")?.GetComponent<Button>();
            if (backButton == null) backButton = panel.Find("CloseButton")?.GetComponent<Button>();

            if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirm);
        }
    }
}
