using NueGames.NueDeck.Scripts.Enums;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardGame.UI.Common
{
    /// <summary>
    /// 通用物品展示组件 — 统一展示卡牌/材料/丹药/法宝/配方。
    /// 布局：[图标] [名称(品质色)] [品质标签] [×数量]
    /// Prefab驱动，AutoBindReferences自动绑定。
    /// </summary>
    public class ItemView : MonoBehaviour
    {
        [SerializeField] private Image _bgImage;
        [SerializeField] private Image _iconImage;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private GameObject _qualityObj;
        [SerializeField] private Image _qualityBg;
        [SerializeField] private TextMeshProUGUI _qualityText;
        [SerializeField] private GameObject _countObj;
        [SerializeField] private TextMeshProUGUI _countText;

        private bool _initialized;

        private void Awake()
        {
            AutoBindReferences();
        }

        private void AutoBindReferences()
        {
            if (_initialized) return;
            _initialized = true;

            var bg = transform.Find("BG");
            if (bg != null && _bgImage == null) _bgImage = bg.GetComponent<Image>();

            var container = transform.Find("Container");
            if (container != null)
            {
                if (_iconImage == null) _iconImage = container.Find("Icon")?.GetComponent<Image>();
                if (_nameText == null) _nameText = container.Find("name")?.GetComponent<TextMeshProUGUI>();

                var quality = container.Find("Quality");
                if (quality != null)
                {
                    if (_qualityObj == null) _qualityObj = quality.gameObject;
                    if (_qualityBg == null) _qualityBg = quality.GetComponent<Image>();
                    if (_qualityText == null) _qualityText = quality.Find("Text")?.GetComponent<TextMeshProUGUI>();
                }

                var count = container.Find("Count");
                if (count != null)
                {
                    if (_countObj == null) _countObj = count.gameObject;
                    if (_countText == null) _countText = count.Find("Text")?.GetComponent<TextMeshProUGUI>();
                }
            }
        }

        /// <summary>
        /// 设置物品数据并刷新显示。
        /// </summary>
        public void SetData(Sprite icon, string name, ItemQuality quality, int count = 0)
        {
            if (!_initialized) AutoBindReferences();

            var qColor = ItemQualityHelper.GetColor(quality);

            // Icon
            if (_iconImage != null)
            {
                if (icon != null)
                {
                    _iconImage.sprite = icon;
                    _iconImage.color = Color.white;
                }
                else
                {
                    _iconImage.sprite = null;
                    _iconImage.color = qColor * 0.5f;
                }
            }

            // Name
            if (_nameText != null)
            {
                _nameText.text = name;
                _nameText.color = qColor;
            }

            // Quality tag
            if (_qualityText != null)
            {
                _qualityText.text = ItemQualityHelper.GetDisplayName(quality);
                _qualityText.color = qColor;
            }
            if (_qualityBg != null)
            {
                _qualityBg.color = qColor * 0.25f + new Color(0.1f, 0.1f, 0.15f, 0.9f);
            }

            // Background
            if (_bgImage != null)
            {
                _bgImage.color = qColor * 0.15f + new Color(0.08f, 0.1f, 0.14f, 0.95f);
            }

            // Count
            if (_countObj != null)
            {
                bool showCount = count > 1;
                _countObj.SetActive(showCount);
                if (showCount && _countText != null)
                    _countText.text = $"×{count}";
            }
        }

        private static GameObject _prefab;

        /// <summary>创建一个带ItemView的GameObject，从预制体加载</summary>
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
                go.AddComponent<RectTransform>();
                go.AddComponent<ItemView>();
            }
            return go.GetComponent<ItemView>();
        }
    }
}
