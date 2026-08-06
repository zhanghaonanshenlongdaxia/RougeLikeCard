using TMPro;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using Alchemy.Inspector;

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

        private IShopSystem _system;
        private IBattleModel _battleModel;

        public IArchitecture GetArchitecture() => CardGameArchitecture.Interface;

        private void Awake()
        {
            _system = this.GetSystem<IShopSystem>();
            _battleModel = this.GetModel<IBattleModel>();

            // 运行时自动绑定返回按钮
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                if (btn.gameObject.name.Contains("Back"))
                {
                    backButton = btn;
                    btn.onClick.AddListener(OnBackButton);
                    break;
                }
            }

            // 绑定移除卡牌按钮
            foreach (var btn in buttons)
            {
                if (btn.gameObject.name.Contains("Remove"))
                {
                    removeCardButton = btn;
                    btn.onClick.AddListener(OnRemoveCard);
                    break;
                }
            }

            // 查找 TextMeshPro 引用
            var tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var tmp in tmps)
            {
                if (tmp.text != null && tmp.text.Contains("灵石")) { goldText = tmp; break; }
            }
            UIHelper.EnsureCloseButton(this, OnBackButton);
        }

        private void OnEnable()
        {
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
            go.AddComponent<Image>().color = new Color(0.1f, 0.15f, 0.25f, 1);

            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 5;
            layout.padding = new RectOffset(10, 10, 10, 10);

            CreateText(go, name, 20, Color.white);
            CreateText(go, price + "灵石", 18, new Color(1f, 0.85f, 0f));

            var buyBtn = new GameObject("BuyButton");
            buyBtn.transform.SetParent(go.transform);
            buyBtn.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 35);
            buyBtn.AddComponent<Image>().color = new Color(0.15f, 0.3f, 0.5f, 1);
            buyBtn.AddComponent<Button>();
            CreateText(buyBtn, "购买", 18, Color.white);

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
