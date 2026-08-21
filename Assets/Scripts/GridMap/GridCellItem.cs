using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CardGame
{
    /// <summary>
    /// 格子地图单个格子（item预制体）
    /// 背景色 + 物体图标 + 标签 + 玩家标记 + 可点击高亮
    /// </summary>
    public class GridCellItem : MonoBehaviour
    {
        [SerializeField] private Image bgImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image highlightImage;
        [SerializeField] private Image playerMark;
        [SerializeField] private TextMeshProUGUI labelText;

        public GridCell Cell { get; private set; }

        System.Action<GridCellItem> _onClick;
        Button _button;

        void Awake()
        {
            AutoBindReferences();
            _button = GetComponent<Button>();
            if (_button != null)
                _button.onClick.AddListener(HandleClick);
        }

        void AutoBindReferences()
        {
            if (bgImage == null) bgImage = GetComponent<Image>();
            if (iconImage == null) { var t = transform.Find("IconImage"); if (t != null) iconImage = t.GetComponent<Image>(); }
            if (highlightImage == null) { var t = transform.Find("Highlight"); if (t != null) highlightImage = t.GetComponent<Image>(); }
            if (playerMark == null) { var t = transform.Find("PlayerMark"); if (t != null) playerMark = t.GetComponent<Image>(); }
            if (labelText == null) { var t = transform.Find("LabelText"); if (t != null) labelText = t.GetComponent<TextMeshProUGUI>(); }
        }

        void HandleClick() => _onClick?.Invoke(this);

        /// <summary>初始化格子数据与点击回调</summary>
        public void Init(GridCell cell, System.Action<GridCellItem> onClick)
        {
            Cell = cell;
            _onClick = onClick;
        }

        /// <summary>设置视觉：背景色 + 图标 + 标签</summary>
        public void SetVisual(Color bgColor, Sprite icon, string label)
        {
            if (bgImage != null) bgImage.color = bgColor;
            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.gameObject.SetActive(icon != null);
            }
            if (labelText != null)
            {
                labelText.text = label ?? string.Empty;
                labelText.gameObject.SetActive(!string.IsNullOrEmpty(label));
            }
        }

        /// <summary>可点击高亮</summary>
        public void SetHighlight(bool on)
        {
            if (highlightImage != null)
                highlightImage.gameObject.SetActive(on);
        }

        /// <summary>玩家标记</summary>
        public void SetPlayerHere(bool here, Sprite portrait)
        {
            if (playerMark == null) return;
            playerMark.gameObject.SetActive(here);
            if (here && portrait != null)
            {
                playerMark.sprite = portrait;
                playerMark.color = Color.white;
            }
        }

        /// <summary>清除格子上的物体图标（如采集完成后）</summary>
        public void ClearObject()
        {
            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.gameObject.SetActive(false);
            }
            if (labelText != null)
            {
                labelText.text = string.Empty;
                labelText.gameObject.SetActive(false);
            }
        }
    }
}
