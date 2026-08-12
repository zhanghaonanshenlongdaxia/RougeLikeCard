using System;
using System.Collections.Generic;
using CardGame;
using NueGames.NueDeck.Scripts.Data.Cultivation;
using NueGames.NueDeck.Scripts.Enums;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardGame.UI.Cultivation
{
    /// <summary>
    /// 功法节点 Item：绑定在 CultivationAbilityItem.prefab 上。
    /// 运行时只刷新数据与视觉状态，不创建布局。
    /// </summary>
    public class AbilityItemController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image _frameImage;
        [SerializeField] private Image _iconImage;
        [SerializeField] private GameObject _elementTagObj;
        [SerializeField] private Image _elementTagBg;
        [SerializeField] private TextMeshProUGUI _elementTagText;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private GameObject _starsRowObj;
        [SerializeField] private List<Image> _starImages = new List<Image>();
        [SerializeField] private Button _button;

        [Header("Data")]
        [SerializeField] private int _maxStars = 5;

        private List<CultivationNodeData> _mergedNodes;
        private DivineAbilityData _abilityData;
        private ElementType _element;
        private bool _isUnlocked;
        private bool _canUnlock;
        private int _currentAdvLevel;
        private Action<AbilityItemController> _onClick;

        public List<CultivationNodeData> MergedNodes => _mergedNodes;
        public CultivationNodeData PrimaryNode => _mergedNodes != null && _mergedNodes.Count > 0 ? _mergedNodes[0] : null;
        public ElementType Element => _element;

        private void Awake()
        {
            AutoBindReferences();
            if (_button == null) _button = GetComponent<Button>();
            if (_button != null) _button.onClick.AddListener(() => _onClick?.Invoke(this));
        }

        private void AutoBindReferences()
        {
            if (_frameImage == null) _frameImage = GetComponent<Image>();
            if (_button == null) _button = GetComponent<Button>();

            var iconBox = transform.Find("IconBox");
            if (iconBox != null)
            {
                if (_iconImage == null) _iconImage = iconBox.Find("IconImage")?.GetComponent<Image>();
                if (_elementTagObj == null)
                {
                    var tag = iconBox.Find("ElementBadge");
                    if (tag != null) _elementTagObj = tag.gameObject;
                }
                if (_elementTagBg == null) _elementTagBg = iconBox.Find("ElementBadge")?.GetComponent<Image>();
                if (_elementTagText == null) _elementTagText = iconBox.Find("ElementBadge/BadgeText")?.GetComponent<TextMeshProUGUI>();
                if (_nameText == null) _nameText = iconBox.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            }

            var starsRow = transform.Find("StarsRow");
            if (starsRow != null)
            {
                if (_starsRowObj == null) _starsRowObj = starsRow.gameObject;
                if (_starImages == null || _starImages.Count == 0)
                {
                    _starImages = new List<Image>();
                    foreach (Transform child in starsRow)
                    {
                        var img = child.GetComponent<Image>();
                        if (img != null) _starImages.Add(img);
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveAllListeners();
        }

        public void Init(List<CultivationNodeData> mergedNodes, DivineAbilityData ability,
            ElementType element, bool isUnlocked, bool canUnlock,
            int currentAdvLevel, Action<AbilityItemController> onClick)
        {
            _mergedNodes = mergedNodes;
            _abilityData = ability;
            _element = element;
            _isUnlocked = isUnlocked;
            _canUnlock = canUnlock;
            _currentAdvLevel = currentAdvLevel;
            _onClick = onClick;

            UpdateVisual();
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
                    _iconImage.sprite = null;
                }
            }

            Color frameColor, nameColor;
            if (_isUnlocked) { frameColor = new Color(0.7f, 0.55f, 0.1f, 0.95f); nameColor = new Color(1f, 0.95f, 0.85f); }
            else if (_canUnlock) { frameColor = new Color(0.08f, 0.5f, 0.12f, 0.9f); nameColor = new Color(0.85f, 1f, 0.85f); }
            else { frameColor = new Color(0.15f, 0.15f, 0.2f, 0.85f); nameColor = new Color(0.75f, 0.75f, 0.8f, 0.85f); }

            if (_frameImage) _frameImage.color = frameColor;
            if (_iconImage && _abilityData?.Icon == null) _iconImage.color = frameColor;
            if (_nameText) _nameText.color = nameColor;

            // Stars
            if (_starsRowObj)
            {
                bool hasStars = _mergedNodes != null && _mergedNodes.Count > 1;
                _starsRowObj.SetActive(hasStars);
                int starCount = hasStars ? _mergedNodes.Count : 0;
                for (int i = 0; i < _starImages.Count; i++)
                {
                    if (_starImages[i] == null) continue;
                    bool visible = i < starCount;
                    _starImages[i].gameObject.SetActive(visible);
                    if (visible)
                    {
                        _starImages[i].color = i < _currentAdvLevel
                            ? new Color(1f, 0.8f, 0.15f, 1f)
                            : new Color(0.2f, 0.2f, 0.25f, 0.5f);
                    }
                }
            }

            // Element badge
            if (_elementTagObj) _elementTagObj.SetActive(_element != ElementType.None);
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
