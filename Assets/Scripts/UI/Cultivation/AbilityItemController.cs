using System;
using System.Collections.Generic;
using CardGame;
using CardGame.Audio;
using NueGames.NueDeck.Scripts.Data.Cultivation;
using NueGames.NueDeck.Scripts.Enums;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardGame.UI.Cultivation
{
    /// <summary>
    /// 神通Item组件 — 参考参考图的竖版结构。
    /// 布局（从上到下）：
    ///   [方形图标框]  ← 左上角有圆形属性徽章(火/水/金...)半重叠
    ///     名字叠加在图标底部
    ///   [星星行]      ← box 下方，只有多层时显示
    /// </summary>
    public class AbilityItemController : MonoBehaviour
    {
        private Image _frameImage;
        private Image _iconImage;
        private GameObject _elementTagObj;
        private Image _elementTagBg;
        private TextMeshProUGUI _elementTagText;
        private TextMeshProUGUI _nameText;
        private GameObject _starsRowObj;
        private List<Image> _starImages = new List<Image>();
        private Button _button;
        private TMP_FontAsset _font;

        private List<CultivationNodeData> _mergedNodes;
        private DivineAbilityData _abilityData;
        private ElementType _element;
        private bool _isUnlocked;
        private bool _canUnlock;
        private int _currentAdvLevel;
        private Action<AbilityItemController> _onClick;

        // 圆形 sprite 缓存
        private static Sprite _circleSprite;

        public void Init(List<CultivationNodeData> mergedNodes, DivineAbilityData ability,
            ElementType element, bool isUnlocked, bool canUnlock,
            int currentAdvLevel, TMP_FontAsset font,
            Action<AbilityItemController> onClick)
        {
            _mergedNodes = mergedNodes;
            _abilityData = ability;
            _element = element;
            _isUnlocked = isUnlocked;
            _canUnlock = canUnlock;
            _currentAdvLevel = currentAdvLevel;
            _font = font;
            _onClick = onClick;

            BuildInternal();
            UpdateVisual();
        }

        public List<CultivationNodeData> MergedNodes => _mergedNodes;
        public CultivationNodeData PrimaryNode => _mergedNodes != null && _mergedNodes.Count > 0 ? _mergedNodes[0] : null;
        public ElementType Element => _element;

        private static Sprite GetCircleSprite()
        {
            if (_circleSprite != null) return _circleSprite;
            // 运行时生成圆形 sprite
            var tex = new Texture2D(32, 32);
            var pixels = new Color32[32 * 32];
            float center = 15.5f;
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    pixels[y * 32 + x] = dist <= 15f ? Color.white : new Color(0, 0, 0, 0);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            _circleSprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
            return _circleSprite;
        }

        private void BuildInternal()
        {
            int starCount = _mergedNodes?.Count ?? 0;
            bool hasStars = starCount > 1;

            // 整体尺寸：图标框 + 星星行（如果有）
            float boxSize = 70f;
            float starRowHeight = hasStars ? 16f : 0f;
            float totalHeight = boxSize + starRowHeight + (hasStars ? 2f : 0f);

            _frameImage = gameObject.GetComponent<Image>();
            if (_frameImage == null) _frameImage = gameObject.AddComponent<Image>();
            var rt = GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(90, totalHeight);

            // === 图标框（方形，占上方） ===
            var iconGo = new GameObject("IconBox");
            iconGo.transform.SetParent(transform, false);
            var iconRt = iconGo.AddComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 1f);
            iconRt.anchorMax = new Vector2(0.5f, 1f);
            iconRt.pivot = new Vector2(0.5f, 1f);
            iconRt.sizeDelta = new Vector2(boxSize, boxSize);
            iconRt.anchoredPosition = new Vector2(0, 0);
            _iconImage = iconGo.AddComponent<Image>();
            _iconImage.color = new Color(0.12f, 0.14f, 0.18f, 1f);

            // 名字 — 叠加在图标底部
            var nameGo = new GameObject("NameText");
            nameGo.transform.SetParent(iconGo.transform, false);
            var nameRt = nameGo.AddComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0, 0);
            nameRt.anchorMax = new Vector2(1, 0);
            nameRt.pivot = new Vector2(0.5f, 0);
            nameRt.sizeDelta = new Vector2(0, 18);
            nameRt.anchoredPosition = new Vector2(0, 2);
            _nameText = nameGo.AddComponent<TextMeshProUGUI>();
            _nameText.fontSize = 12;
            _nameText.alignment = TextAlignmentOptions.Bottom;
            _nameText.color = Color.white;
            _nameText.enableWordWrapping = false;
            if (_font) _nameText.font = _font;

            // 属性徽章 — 圆形，左上角半重叠
            _elementTagObj = new GameObject("ElementBadge");
            _elementTagObj.transform.SetParent(iconGo.transform, false);
            var tagRt = _elementTagObj.AddComponent<RectTransform>();
            tagRt.anchorMin = new Vector2(0, 1);
            tagRt.anchorMax = new Vector2(0, 1);
            tagRt.pivot = new Vector2(0.5f, 0.5f);
            tagRt.sizeDelta = new Vector2(22, 22);
            tagRt.anchoredPosition = new Vector2(0, 0); // 半重叠在左上角
            _elementTagBg = _elementTagObj.AddComponent<Image>();
            _elementTagBg.sprite = GetCircleSprite();
            _elementTagBg.type = Image.Type.Simple;
            _elementTagBg.color = GetElementColor(_element);

            var tagTextGo = new GameObject("BadgeText");
            tagTextGo.transform.SetParent(_elementTagObj.transform, false);
            var tagTextRt = tagTextGo.AddComponent<RectTransform>();
            tagTextRt.anchorMin = Vector2.zero; tagTextRt.anchorMax = Vector2.one;
            tagTextRt.offsetMin = Vector2.zero; tagTextRt.offsetMax = Vector2.zero;
            _elementTagText = tagTextGo.AddComponent<TextMeshProUGUI>();
            _elementTagText.fontSize = 12;
            _elementTagText.alignment = TextAlignmentOptions.Center;
            _elementTagText.color = Color.white;
            _elementTagText.enableWordWrapping = false;
            if (_font) _elementTagText.font = _font;

            // === 星星行（box 下方，只有多层时显示） ===
            if (hasStars)
            {
                _starsRowObj = new GameObject("StarsRow");
                _starsRowObj.transform.SetParent(transform, false);
                var starsRt = _starsRowObj.AddComponent<RectTransform>();
                starsRt.anchorMin = new Vector2(0.5f, 0);
                starsRt.anchorMax = new Vector2(0.5f, 0);
                starsRt.pivot = new Vector2(0.5f, 0);
                starsRt.sizeDelta = new Vector2(90, 14);
                starsRt.anchoredPosition = new Vector2(0, 0);

                var hlg = _starsRowObj.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = 3;
                hlg.childControlWidth = true;
                hlg.childControlHeight = true;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = false;
                hlg.childAlignment = TextAnchor.MiddleCenter;

                for (int i = 0; i < starCount; i++)
                {
                    var starGo = new GameObject($"Star_{i}");
                    starGo.transform.SetParent(_starsRowObj.transform, false);
                    var starRt = starGo.AddComponent<RectTransform>();
                    starRt.sizeDelta = new Vector2(12, 12);
                    var starImg = starGo.AddComponent<Image>();
                    // 用圆形 sprite 做星星
                    starImg.sprite = GetCircleSprite();
                    starImg.type = Image.Type.Simple;
                    _starImages.Add(starImg);
                }
            }

            // 按钮
            _button = gameObject.GetComponent<Button>();
            if (_button == null) _button = gameObject.AddComponent<Button>();
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() =>
            {
                if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick);
                _onClick?.Invoke(this);
            });
        }

        public void UpdateVisual()
        {
            string displayName = GetDisplayName();
            if (_nameText) _nameText.text = displayName;

            if (_iconImage)
            {
                if (_abilityData?.Icon != null)
                {
                    _iconImage.sprite = _abilityData.Icon;
                    _iconImage.color = Color.white;
                }
                else
                {
                    _iconImage.color = new Color(0.12f, 0.14f, 0.18f, 1f);
                }
            }

            // 边框颜色（用 frameImage = iconImage 的背景色）
            Color frameColor, nameColor;
            if (_isUnlocked) { frameColor = new Color(0.7f, 0.55f, 0.1f, 0.95f); nameColor = new Color(1f, 0.9f, 0.6f); }
            else if (_canUnlock) { frameColor = new Color(0.08f, 0.5f, 0.12f, 0.9f); nameColor = new Color(0.7f, 0.9f, 0.7f); }
            else { frameColor = new Color(0.15f, 0.15f, 0.2f, 0.85f); nameColor = new Color(0.5f, 0.5f, 0.55f); }

            // 如果没有 sprite，用边框色做背景
            if (_iconImage && _abilityData?.Icon == null)
                _iconImage.color = frameColor;
            if (_nameText) _nameText.color = nameColor;

            // 星星填充
            for (int i = 0; i < _starImages.Count; i++)
            {
                _starImages[i].color = i < _currentAdvLevel
                    ? new Color(1f, 0.8f, 0.15f, 1f)   // 实心金色
                    : new Color(0.2f, 0.2f, 0.25f, 0.5f); // 空心暗色
            }

            // 属性徽章
            if (_elementTagObj)
                _elementTagObj.SetActive(_element != ElementType.None);
            if (_elementTagBg) _elementTagBg.color = GetElementColor(_element);
            if (_elementTagText) _elementTagText.text = ElementChar(_element);

            if (_button) _button.interactable = true;
        }

        private string GetDisplayName()
        {
            string name = _abilityData?.AbilityName ?? PrimaryNode?.NodeName ?? "???";
            if (name.Length > 2 && (name.StartsWith("壹·") || name.StartsWith("贰·") || name.StartsWith("叁·") || name.StartsWith("肆·")))
                return name.Substring(2);
            return name;
        }

        private static string ElementChar(ElementType el) => el switch
        {
            ElementType.Metal => "金", ElementType.Wood => "木", ElementType.Water => "水",
            ElementType.Fire => "火", ElementType.Earth => "土", ElementType.Sword => "剑",
            ElementType.Wind => "风", ElementType.Thunder => "雷", ElementType.Ghost => "鬼",
            _ => ""
        };

        private static Color GetElementColor(ElementType el) => el switch
        {
            ElementType.Metal => new Color(0.9f, 0.8f, 0.2f, 0.95f),
            ElementType.Wood => new Color(0.15f, 0.7f, 0.15f, 0.95f),
            ElementType.Water => new Color(0.15f, 0.4f, 0.85f, 0.95f),
            ElementType.Fire => new Color(0.85f, 0.2f, 0.15f, 0.95f),
            ElementType.Earth => new Color(0.6f, 0.4f, 0.2f, 0.95f),
            ElementType.Sword => new Color(0.65f, 0.65f, 0.8f, 0.95f),
            ElementType.Wind => new Color(0.25f, 0.75f, 0.5f, 0.95f),
            ElementType.Thunder => new Color(0.5f, 0.25f, 0.85f, 0.95f),
            ElementType.Ghost => new Color(0.35f, 0.2f, 0.5f, 0.95f),
            _ => new Color(0.5f, 0.5f, 0.5f, 0.5f)
        };
    }
}
