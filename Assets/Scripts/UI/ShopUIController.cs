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
            AutoBindReferences();
            UIHelper.EnsureCloseButton(this, OnBackButton);
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
            sLayout.childControlWidth = true; sLayout.childControlHeight = true;
            sLayout.childForceExpandHeight = false;

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
            var scLe = scrollObj.AddComponent<LayoutElement>();
            scLe.preferredHeight = 60;
            scLe.flexibleHeight = 1;

            var vp = new GameObject("Viewport");
            vp.transform.SetParent(scrollObj.transform, false);
            var vpRt = vp.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero; vpRt.offsetMax = Vector2.zero;
            var vpImg = vp.AddComponent<Image>(); vpImg.color = new Color(0.05f, 0.08f, 0.12f, 1f);
            vp.AddComponent<UnityEngine.UI.Mask>();
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
                    bool success = false;
                    string itemName = name;
                    if (type == "card") success = _system.BuyCard(index);
                    else if (type == "relic") success = _system.BuyRelic(index);
                    else if (type == "potion")
                    {
                        success = _system.BuyPotion(index);
                        if (!success && _battleModel.CurrentGold.Value >= CurrentShopPrice(type, index))
                            FloatingTip.ShowWarning("药水槽已满!");
                        else if (!success)
                            FloatingTip.ShowInsufficientGold();
                        else
                            FloatingTip.ShowPurchaseSuccess(itemName);
                        RefreshShop();
                        return;
                    }
                    
                    if (success) FloatingTip.ShowPurchaseSuccess(itemName);
                    else FloatingTip.ShowInsufficientGold();
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
                if (_system.RemoveCard(card))
                    FloatingTip.ShowSuccess($"移除卡牌: {card.CardName}");
                else
                    FloatingTip.ShowInsufficientGold();
            }
            else
            {
                FloatingTip.ShowWarning("卡牌不足3张!");
            }
            RefreshShop();
        }

        public void OnBackButton()
        {
            gameObject.SetActive(false);
            NotifyMapRefresh();
        }

        private int CurrentShopPrice(string type, int index)
        {
            var shop = _system.CurrentShop;
            if (shop == null) return int.MaxValue;
            if (type == "card" && index < shop.cardPrices.Count) return shop.cardPrices[index];
            if (type == "relic" && index < shop.relicPrices.Count) return shop.relicPrices[index];
            if (type == "potion" && index < shop.potionPrices.Count) return shop.potionPrices[index];
            return int.MaxValue;
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
            tmp.text = text; tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = size; tmp.color = color;
            if (_font) tmp.font = _font;
        }
        private void AutoBindReferences()
        {
            var panel = transform.Find("Panel");
            if (panel == null) return;
            if (goldText == null) goldText = panel.Find("Header/GoldText")?.GetComponent<TMPro.TextMeshProUGUI>();
            var s1 = panel.Find("Section_卡牌");
            var s2 = panel.Find("Section_法宝");
            var s3 = panel.Find("Section_丹药");
            if (cardRowRoot == null && s1 != null) cardRowRoot = s1.Find("Scroll/Viewport/Content");
            if (relicRowRoot == null && s2 != null) relicRowRoot = s2.Find("Scroll/Viewport/Content");
            if (potionRowRoot == null && s3 != null) potionRowRoot = s3.Find("Scroll/Viewport/Content");
            if (removeCardButton == null) removeCardButton = panel.Find("RemoveCardButton")?.GetComponent<UnityEngine.UI.Button>();
        }

    }

}
