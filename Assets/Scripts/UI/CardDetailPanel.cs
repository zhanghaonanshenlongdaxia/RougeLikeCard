using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NueGames.NueDeck.Scripts.Data.Collection;
using NueGames.NueDeck.Scripts.Enums;

namespace CardGame.UI
{
    /// <summary>
    /// 卡牌详情弹窗：右键点击卡牌时弹出，显示完整信息（消耗/描述/图片/稀有度/道途等）。
    /// </summary>
    public class CardDetailPanel : MonoBehaviour
    {
        private static CardDetailPanel _instance;
        private TMP_FontAsset _font;

        public static void Show(CardData cardData)
        {
            if (cardData == null) return;

            if (_instance == null)
            {
                var go = new GameObject("CardDetailPanel");
                var panel = go.AddComponent<CardDetailPanel>();
                panel.Init();
            }

            _instance.DisplayCard(cardData);
        }

        private Image _cardImage;
        private TextMeshProUGUI _nameText;
        private TextMeshProUGUI _costText;
        private TextMeshProUGUI _rarityText;
        private TextMeshProUGUI _descText;
        private TextMeshProUGUI _pathText;
        private TextMeshProUGUI _keywordText;
        private Button _closeBtn;
        private GameObject _panel;

        private void Init()
        {
            _instance = this;
            _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 400;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            gameObject.AddComponent<GraphicRaycaster>();
            DontDestroyOnLoad(gameObject);

            // 半透明背景（点击关闭）
            var bg = new GameObject("BG");
            bg.transform.SetParent(transform, false);
            var bgRt = bg.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.6f);
            var bgBtn = bg.AddComponent<Button>();
            bgBtn.onClick.AddListener(Close);

            // 主面板
            _panel = new GameObject("Panel");
            _panel.transform.SetParent(transform, false);
            var pRt = _panel.AddComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0.3f, 0.15f); pRt.anchorMax = new Vector2(0.7f, 0.85f);
            pRt.offsetMin = Vector2.zero; pRt.offsetMax = Vector2.zero;
            var pImg = _panel.AddComponent<Image>();
            pImg.color = new Color(0.06f, 0.08f, 0.12f, 0.98f);

            var vlg = _panel.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8; vlg.padding = new RectOffset(20, 20, 20, 20);
            vlg.childControlWidth = true; vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperCenter;

            // 卡牌图片
            var imgObj = CreateTextObject(_panel.transform, "");
            _cardImage = imgObj.AddComponent<Image>();
            var imgLe = imgObj.AddComponent<LayoutElement>();
            imgLe.preferredHeight = 250;

            // 卡牌名
            _nameText = CreateTMP(_panel.transform, "", 28, new Color(0.9f, 0.8f, 0.3f));
            _nameText.alignment = TextAlignmentOptions.Center;
            _nameText.gameObject.AddComponent<LayoutElement>().preferredHeight = 35;

            // 消耗 + 稀有度
            var infoRow = new GameObject("InfoRow");
            infoRow.transform.SetParent(_panel.transform, false);
            infoRow.AddComponent<RectTransform>();
            var hlg = infoRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 30; hlg.childAlignment = TextAnchor.MiddleCenter;
            _costText = CreateTMP(infoRow.transform, "", 20, new Color(0.4f, 0.8f, 1f));
            _rarityText = CreateTMP(infoRow.transform, "", 20, Color.white);
            _pathText = CreateTMP(infoRow.transform, "", 20, new Color(0.7f, 0.7f, 0.7f));

            // 完整描述
            var descLabel = CreateTMP(_panel.transform, "完整描述", 18, new Color(0.6f, 0.6f, 0.6f));
            descLabel.alignment = TextAlignmentOptions.Left;
            _descText = CreateTMP(_panel.transform, "", 16, Color.white);
            _descText.alignment = TextAlignmentOptions.Left;
            _descText.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;

            // 关键词
            _keywordText = CreateTMP(_panel.transform, "", 14, new Color(0.5f, 0.8f, 0.5f));
            _keywordText.alignment = TextAlignmentOptions.Left;

            // 关闭按钮
            var btnObj = new GameObject("CloseButton");
            btnObj.transform.SetParent(_panel.transform, false);
            btnObj.AddComponent<RectTransform>();
            btnObj.AddComponent<Image>().color = new Color(0.3f, 0.2f, 0.1f, 1f);
            var btnLe = btnObj.AddComponent<LayoutElement>();
            btnLe.preferredHeight = 40;
            _closeBtn = btnObj.AddComponent<Button>();
            _closeBtn.onClick.AddListener(Close);
            var btnTxt = CreateTMP(btnObj.transform, "关闭", 20, Color.white);
            btnTxt.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            btnTxt.GetComponent<RectTransform>().anchorMax = Vector2.one;
            btnTxt.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            btnTxt.GetComponent<RectTransform>().offsetMax = Vector2.zero;

            gameObject.SetActive(false);
        }

        private void DisplayCard(CardData cardData)
        {
            if (cardData.CardSprite != null)
            {
                _cardImage.sprite = cardData.CardSprite;
                _cardImage.color = Color.white;
                _cardImage.preserveAspect = true;
            }
            else
            {
                _cardImage.color = new Color(0.15f, 0.15f, 0.2f, 1f);
            }

            _nameText.text = cardData.CardName;
            _costText.text = $"消耗: {cardData.ManaCost}";
            _rarityText.text = $"品质: {ItemQualityHelper.GetDisplayName(cardData.Quality)}";

            string pathStr = cardData.PathType != PathType.None ? $"道途: {GetPathName(cardData.PathType)}" : "";
            if (cardData.BuildTag != BuildTag.None)
                pathStr += $" ({GetBuildName(cardData.BuildTag)})";
            _pathText.text = pathStr;

            // 完整描述
            cardData.UpdateDescription();
            _descText.text = cardData.MyDescription;

            // 关键词
            if (cardData.KeywordsList != null && cardData.KeywordsList.Count > 0)
            {
                var kwStr = "关键词: ";
                foreach (var kw in cardData.KeywordsList)
                    kwStr += kw.ToString() + " ";
                _keywordText.text = kwStr.Trim();
                _keywordText.gameObject.SetActive(true);
            }
            else
            {
                _keywordText.gameObject.SetActive(false);
            }

            gameObject.SetActive(true);
        }

        private void Close()
        {
            gameObject.SetActive(false);
        }

        private string GetRarityName(RarityType rarity)
        {
            return rarity switch
            {
                RarityType.Common => "凡品",
                RarityType.Uncommon => "灵品",
                RarityType.Rare => "玄品",
                RarityType.Legendary => "仙品",
                _ => rarity.ToString()
            };
        }

        private string GetPathName(PathType path)
        {
            return path switch
            {
                PathType.Sword => "剑道",
                PathType.Body => "体道",
                PathType.Spirit => "灵道",
                _ => path.ToString()
            };
        }

        private string GetBuildName(BuildTag build)
        {
            return build switch
            {
                BuildTag.MultiHit => "多段",
                BuildTag.Burst => "爆发",
                BuildTag.Thorn => "反伤",
                BuildTag.Sustain => "续战",
                BuildTag.Debuff => "减益",
                BuildTag.ManaBurst => "灵力爆发",
                _ => build.ToString()
            };
        }

        private TextMeshProUGUI CreateTMP(Transform parent, string text, float size, Color color)
        {
            var go = CreateTextObject(parent, text);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = size; tmp.color = color;
            if (_font) tmp.font = _font;
            return tmp;
        }

        private GameObject CreateTextObject(Transform parent, string name)
        {
            var go = new GameObject(string.IsNullOrEmpty(name) ? "Text" : name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }
    }
}
