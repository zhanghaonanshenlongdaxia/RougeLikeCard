using TMPro;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using Alchemy.Inspector;
using CardGame.Audio;

namespace CardGame.UI
{
    public class ShopUIController : MonoBehaviour, IController
    {
        [SerializeField] private Transform cardRowRoot;
        [SerializeField] private Transform relicRowRoot;
        [SerializeField] private Transform potionRowRoot;
        [SerializeField] private Button removeCardButton;
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private Button backButton;
        private TMP_FontAsset _font;

        private IShopSystem _system;
        private IBattleModel _battleModel;

        public IArchitecture GetArchitecture() => CardGameArchitecture.Interface;

        private void Awake()
        {
            _system = this.GetSystem<IShopSystem>();
            _battleModel = this.GetModel<IBattleModel>();
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
                canvas.sortingOrder = 40;
                var scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                gameObject.AddComponent<GraphicRaycaster>();
            }

            // 全屏遮罩背景
            var bgObj = new GameObject("Background");
            bgObj.transform.SetParent(transform, false);
            var bgRt = bgObj.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
            bgObj.AddComponent<Image>().color = new Color(0, 0, 0, 0.95f);

            Transform panel = transform.Find("Panel");
            if (panel == null)
            {
                var panelObj = new GameObject("Panel");
                panelObj.transform.SetParent(transform, false);
                var panelRt = panelObj.AddComponent<RectTransform>();
                panelRt.anchorMin = new Vector2(0.05f, 0.05f); panelRt.anchorMax = new Vector2(0.95f, 0.95f);
                panelRt.offsetMin = Vector2.zero; panelRt.offsetMax = Vector2.zero;
                panelObj.AddComponent<Image>().color = new Color(0.08f, 0.1f, 0.15f, 1f);
                panel = panelObj.transform;
            }

            // 标题+灵石
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
            titleTmp.text = "商店"; titleTmp.fontSize = 28; titleTmp.color = new Color(0.9f, 0.8f, 0.3f);
            titleTmp.alignment = TextAlignmentOptions.Left;
            if (_font) titleTmp.font = _font;

            var goldObj = new GameObject("GoldText");
            goldObj.transform.SetParent(header.transform, false);
            goldText = goldObj.AddComponent<TextMeshProUGUI>();
            goldText.fontSize = 22; goldText.color = new Color(1f, 0.85f, 0f);
            goldText.alignment = TextAlignmentOptions.Right;
            if (_font) goldText.font = _font;

            // 三个分类区域
            CreateShopSection(panel, "卡牌", 0.05f, 0.35f, out cardRowRoot);
            CreateShopSection(panel, "法宝", 0.37f, 0.62f, out relicRowRoot);
            CreateShopSection(panel, "丹药", 0.64f, 0.89f, out potionRowRoot);

            // 移除卡牌按钮
            var rmObj = new GameObject("RemoveCardButton");
            rmObj.transform.SetParent(panel, false);
            var rmRt = rmObj.AddComponent<RectTransform>();
            rmRt.anchorMin = new Vector2(0.3f, 0.05f); rmRt.anchorMax = new Vector2(0.7f, 0.12f);
            rmRt.offsetMin = Vector2.zero; rmRt.offsetMax = Vector2.zero;
            rmObj.AddComponent<Image>().color = new Color(0.5f, 0.2f, 0.2f, 1f);
            var rmTxt = new GameObject("Text");
            rmTxt.transform.SetParent(rmObj.transform, false);
            var rmTxtRt = rmTxt.AddComponent<RectTransform>();
            rmTxtRt.anchorMin = Vector2.zero; rmTxtRt.anchorMax = Vector2.one;
            rmTxtRt.offsetMin = Vector2.zero; rmTxtRt.offsetMax = Vector2.zero;
            var rmTmp = rmTxt.AddComponent<TextMeshProUGUI>();
            rmTmp.fontSize = 18; rmTmp.color = Color.white; rmTmp.alignment = TextAlignmentOptions.Center;
            if (_font) rmTmp.font = _font;
            removeCardButton = rmObj.AddComponent<Button>();
            removeCardButton.onClick.AddListener(OnRemoveCard);
        }

        private void CreateShopSection(Transform parent, string title, float yMin, float yMax, out Transform rowRoot)
        {
            var section = new GameObject($"Section_{title}");
            section.transform.SetParent(parent, false);
            var sRt = section.AddComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0.02f, yMin); sRt.anchorMax = new Vector2(0.98f, yMax);
            sRt.offsetMin = Vector2.zero; sRt.offsetMax = Vector2.zero;
            section.AddComponent<Image>().color = new Color(0.05f, 0.08f, 0.12f, 0.6f);

            var sLayout = section.AddComponent<VerticalLayoutGroup>();
            sLayout.spacing = 3; sLayout.padding = new RectOffset(5, 5, 5, 5);
            sLayout.childControlWidth = true; sLayout.childForceExpandHeight = false;

            // 标题
            var tObj = new GameObject("SectionTitle");
            tObj.transform.SetParent(section.transform, false);
            var tTmp = tObj.AddComponent<TextMeshProUGUI>();
            tTmp.text = title; tTmp.fontSize = 18; tTmp.color = new Color(0.9f, 0.8f, 0.3f);
            tTmp.alignment = TextAlignmentOptions.Left;
            if (_font) tTmp.font = _font;
            var tLe = tObj.AddComponent<LayoutElement>(); tLe.preferredHeight = 25;

            // 物品行（水平滚动）
            var scrollObj = new GameObject("Scroll");
            scrollObj.transform.SetParent(section.transform, false);
            var scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.horizontal = true; scroll.vertical = false;
            var scRt = scrollObj.GetComponent<RectTransform>();

            var vp = new GameObject("Viewport");
            vp.transform.SetParent(scrollObj.transform, false);
            var vpRt = vp.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero; vpRt.offsetMax = Vector2.zero;
            vp.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
                vp.AddComponent<RectMask2D>();
            scroll.viewport = vpRt;

            var content = new GameObject("Content");
            content.transform.SetParent(vp.transform, false);
            var cRt = content.AddComponent<RectTransform>();
            cRt.anchorMin = Vector2.zero; cRt.anchorMax = new Vector2(0, 1);
            cRt.pivot = new Vector2(0, 0.5f);
            cRt.offsetMin = Vector2.zero; cRt.offsetMax = Vector2.zero;
            var hlg = content.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 5; hlg.childControlWidth = true; hlg.childForceExpandWidth = false;
            var csf = content.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = cRt;
            rowRoot = content.transform;
        }

        private void OnEnable()
        {
            if (_battleModel != null)
                _battleModel.CurrentGold.RegisterWithInitValue(UpdateGold).UnRegisterWhenGameObjectDestroyed(gameObject);
            RefreshShop();
        }

        private void UpdateGold(int gold)
        {
            if (goldText) goldText.text = "灵石: " + gold;
        }

        private void RefreshShop()
        {
            var shop = _system.CurrentShop ?? _system.GenerateShop();
            PopulateRow(cardRowRoot, shop.cardSlots, shop.cardPrices, "card");
            PopulateRow(relicRowRoot, shop.relicSlots, shop.relicPrices, "relic");
            PopulateRow(potionRowRoot, shop.potionSlots, shop.potionPrices, "potion");

            if (removeCardButton)
            {
                var tmp = removeCardButton.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp) tmp.text = "移除卡牌 价格:" + shop.removeCardPrice + "灵石";
                removeCardButton.onClick.RemoveAllListeners();
                removeCardButton.onClick.AddListener(OnRemoveCard);
            }
        }

        private void PopulateRow(Transform root, System.Collections.IList slots, System.Collections.Generic.List<int> prices, string type)
        {
            if (root == null) return;
            for (int i = root.childCount - 1; i >= 0; i--)
                Destroy(root.GetChild(i).gameObject);

            for (int i = 0; i < slots.Count; i++)
            {
                var item = slots[i];
                if (item == null) continue;

                string name = "";
                var itemType = item.GetType();
                if (itemType == typeof(NueGames.NueDeck.Scripts.Data.Collection.CardData))
                    name = ((NueGames.NueDeck.Scripts.Data.Collection.CardData)item).CardName;
                else if (itemType == typeof(RelicData))
                    name = ((RelicData)item).name;
                else if (itemType == typeof(PotionData))
                    name = ((PotionData)item).name;

                var go = CreateShopItem(root, name, prices[i]);
                var btn = go.GetComponentInChildren<Button>();
                if (btn == null) continue;
                var index = i;
                btn.onClick.AddListener(() =>
                {
                    if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick);
                    if (type == "card") _system.BuyCard(index);
                    else if (type == "relic") _system.BuyRelic(index);
                    else if (type == "potion") _system.BuyPotion(index);
                    RefreshShop();
                });
            }
        }

        private GameObject CreateShopItem(Transform parent, string name, int price)
        {
            var go = new GameObject("ShopItem");
            go.transform.SetParent(parent);
            go.AddComponent<RectTransform>();
            go.AddComponent<Image>().color = new Color(0.1f, 0.15f, 0.25f, 1f);
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 140; le.preferredHeight = 80;

            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 3; layout.padding = new RectOffset(5, 5, 5, 5);

            CreateText(go, name, 14, Color.white);
            CreateText(go, price + "灵石", 14, new Color(1f, 0.85f, 0f));

            var buyBtn = new GameObject("BuyButton");
            buyBtn.transform.SetParent(go.transform);
            buyBtn.AddComponent<RectTransform>();
            var bLe = buyBtn.AddComponent<LayoutElement>(); bLe.preferredHeight = 25;
            buyBtn.AddComponent<Image>().color = new Color(0.15f, 0.3f, 0.5f, 1f);
            buyBtn.AddComponent<Button>();
            CreateText(buyBtn, "购买", 14, Color.white);

            return go;
        }

        private void OnRemoveCard()
        {
            var gm = NueGames.NueDeck.Scripts.Managers.GameManager.Instance;
            if (gm == null) return;
            if (gm.PersistentGameplayData.CurrentCardsList.Count > 2)
            {
                var card = gm.PersistentGameplayData.CurrentCardsList[2];
                _system.RemoveCard(card);
            }
            RefreshShop();
        }

        public void OnBackButton()
        {
            gameObject.SetActive(false);
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
    }
}
