using NueGames.NueDeck.Scripts.Enums;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardGame.UI.Common
{
    /// <summary>
    /// 通用物品展示组件 — 统一展示卡牌/材料/丹药/法宝/配方。
    /// 布局：[图标64×64] [名称(品质色)+缩略描述] [品质标签] [消耗标记] [×数量]
    /// 挂在任意GameObject上，调用SetData即可。
    /// </summary>
    public class ItemView : MonoBehaviour
    {
        private Image _iconImage;
        private Image _bgImage;
        private TextMeshProUGUI _nameText;
        private TextMeshProUGUI _descText;
        private TextMeshProUGUI _qualityText;
        private TextMeshProUGUI _countText;
        private GameObject _exhaustTag;
        private bool _initialized;

        private static TMP_FontAsset _font;

        private void Init()
        {
            if (_initialized) return;
            _initialized = true;
            _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            BuildLayout();
        }

        private void BuildLayout()
        {
            // Background (quality-colored)
            var bgGo = new GameObject("BG");
            bgGo.transform.SetParent(transform, false);
            _bgImage = bgGo.AddComponent<Image>();
            _bgImage.color = new Color(0.1f, 0.14f, 0.2f, 0.95f);
            Stretch(bgGo);

            // HLG container
            var hlgGo = new GameObject("Container");
            hlgGo.transform.SetParent(transform, false);
            var hlgRect = hlgGo.AddComponent<RectTransform>();
            Stretch(hlgGo);
            var hlg = hlgGo.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            hlg.padding = new RectOffset(6, 6, 4, 4);
            hlg.childControlWidth = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            // Icon (64×64)
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(hlgGo.transform, false);
            var iconRect = iconGo.AddComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(48, 48);
            _iconImage = iconGo.AddComponent<Image>();
            _iconImage.preserveAspect = true;
            _iconImage.raycastTarget = false;
            var iconLe = iconGo.AddComponent<LayoutElement>();
            iconLe.preferredWidth = 48;
            iconLe.preferredHeight = 48;

            // Name + Desc (vertical)
            var textColGo = new GameObject("TextCol");
            textColGo.transform.SetParent(hlgGo.transform, false);
            textColGo.AddComponent<RectTransform>();
            var vlg = textColGo.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 1;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperLeft;
            var colLe = textColGo.AddComponent<LayoutElement>();
            colLe.flexibleWidth = 1;
            colLe.minWidth = 100;

            _nameText = CreateText(textColGo.transform, "", 15, Color.white, TextAlignmentOptions.Left);
            var nameLe = _nameText.gameObject.AddComponent<LayoutElement>();
            nameLe.preferredHeight = 20;

            _descText = CreateText(textColGo.transform, "", 12, new Color(0.65f, 0.68f, 0.72f), TextAlignmentOptions.Left);
            var descLe = _descText.gameObject.AddComponent<LayoutElement>();
            descLe.preferredHeight = 16;

            // Quality tag
            var qualityGo = new GameObject("Quality");
            qualityGo.transform.SetParent(hlgGo.transform, false);
            qualityGo.AddComponent<RectTransform>();
            _qualityText = CreateText(qualityGo.transform, "", 11, Color.white, TextAlignmentOptions.Center);
            var qBg = qualityGo.AddComponent<Image>();
            qBg.color = new Color(0.2f, 0.2f, 0.25f, 0.8f);
            _qualityText.transform.SetAsFirstSibling();
            var qLe = qualityGo.AddComponent<LayoutElement>();
            qLe.preferredWidth = 55;
            qLe.preferredHeight = 18;

            // Exhaust tag
            var exhaustGo = new GameObject("ExhaustTag");
            exhaustGo.transform.SetParent(hlgGo.transform, false);
            exhaustGo.AddComponent<RectTransform>();
            var exhaustImg = exhaustGo.AddComponent<Image>();
            exhaustImg.color = new Color(0.5f, 0.1f, 0.1f, 0.9f);
            _exhaustTag = exhaustGo;
            var exhaustText = CreateText(exhaustGo.transform, "消耗", 11, new Color(1f, 0.8f, 0.8f), TextAlignmentOptions.Center);
            var exLe = exhaustGo.AddComponent<LayoutElement>();
            exLe.preferredWidth = 32;
            exLe.preferredHeight = 18;
            _exhaustTag.SetActive(false);

            // Count
            var countGo = new GameObject("Count");
            countGo.transform.SetParent(hlgGo.transform, false);
            countGo.AddComponent<RectTransform>();
            _countText = CreateText(countGo.transform, "", 15, new Color(1f, 0.85f, 0.3f), TextAlignmentOptions.Right);
            var countLe = countGo.AddComponent<LayoutElement>();
            countLe.preferredWidth = 40;
            countLe.preferredHeight = 20;
            _countText.gameObject.SetActive(false);
        }

        /// <summary>
        /// 设置物品数据并刷新显示。
        /// </summary>
        public void SetData(Sprite icon, string name, string desc, ItemQuality quality, int count = 0, bool isExhaust = false)
        {
            Init();

            // Icon
            if (icon != null)
            {
                _iconImage.sprite = icon;
                _iconImage.color = Color.white;
            }
            else
            {
                _iconImage.sprite = null;
                _iconImage.color = ItemQualityHelper.GetColor(quality) * 0.5f;
            }

            // Name (quality-colored)
            var qColor = ItemQualityHelper.GetColor(quality);
            _nameText.text = name;
            _nameText.color = qColor;

            // Description (truncate)
            string shortDesc = desc ?? "";
            if (shortDesc.Length > 35) shortDesc = shortDesc.Substring(0, 33) + "…";
            _descText.text = shortDesc;

            // Quality tag
            _qualityText.text = ItemQualityHelper.GetDisplayName(quality);
            _qualityText.color = qColor;
            _bgImage.GetComponent<Image>().color = qColor * 0.15f + new Color(0.08f, 0.1f, 0.14f, 0.95f);

            // Exhaust tag
            _exhaustTag.SetActive(isExhaust);

            // Count
            if (count > 1)
            {
                _countText.gameObject.SetActive(true);
                _countText.text = $"×{count}";
            }
            else
            {
                _countText.gameObject.SetActive(false);
            }
        }

        private TextMeshProUGUI CreateText(Transform parent, string text, float size, Color color, TextAlignmentOptions alignment)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;
            if (_font) tmp.font = _font;
            return tmp;
        }

        private void Stretch(GameObject go)
        {
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static GameObject _prefab;

        /// <summary>创建一个带ItemView的GameObject，优先从预制体加载</summary>
        public static ItemView Create(Transform parent, string name = "ItemView")
        {
            if (_prefab == null)
                _prefab = Resources.Load<GameObject>("Prefabs/Common/ItemView");
            
            GameObject go;
            if (_prefab != null)
            {
                go = Instantiate(_prefab, parent, false);
                go.name = name;
            }
            else
            {
                go = new GameObject(name);
                go.transform.SetParent(parent, false);
                var rt = go.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(0, 56);
                go.AddComponent<LayoutElement>().preferredHeight = 56;
                go.AddComponent<ItemView>();
            }
            return go.GetComponent<ItemView>();
        }
    }
}
