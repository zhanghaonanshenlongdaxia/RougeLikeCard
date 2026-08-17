using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardGame.UI.Common
{
    /// <summary>
    /// 通用弹窗 — 标题+内容区+关闭按钮+确认/取消按钮(可选)。
    /// Prefab驱动，单例，DontDestroyOnLoad。
    /// 普通弹窗只显示标题+内容+关闭按钮；需要二次确认时显示确认/取消按钮。
    /// </summary>
    public class CommonPopup : MonoBehaviour
    {
        public static CommonPopup Instance { get; private set; }

        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private Transform _contentRoot;
        [SerializeField] private GameObject _btnSure;
        [SerializeField] private Button _sureButton;
        [SerializeField] private GameObject _btnCancel;
        [SerializeField] private Button _cancelButton;

        private static GameObject _prefab;
        private bool _initialized;

        private void Awake()
        {
            AutoBindReferences();
        }

        private void AutoBindReferences()
        {
            if (_initialized) return;
            _initialized = true;

            var panel = transform.Find("Panel");
            if (panel == null) return;

            var titleBar = panel.Find("TitleBar");
            if (titleBar != null)
            {
                if (_titleText == null) _titleText = titleBar.Find("Text")?.GetComponent<TextMeshProUGUI>();
            }

            var scroll = panel.Find("ScrollView");
            if (scroll != null)
            {
                var viewport = scroll.Find("Viewport");
                if (viewport != null)
                {
                    var content = viewport.Find("Content");
                    if (content != null) _contentRoot = content;
                }
            }

            var btnSure = panel.Find("btnSure");
            if (btnSure != null)
            {
                _btnSure = btnSure.gameObject;
                if (_sureButton == null) _sureButton = btnSure.GetComponent<Button>();
            }

            var btnCancel = panel.Find("btnCancel");
            if (btnCancel != null)
            {
                _btnCancel = btnCancel.gameObject;
                if (_cancelButton == null) _cancelButton = btnCancel.GetComponent<Button>();
            }

            // 默认隐藏确认/取消按钮
            if (_btnSure != null) _btnSure.SetActive(false);
            if (_btnCancel != null) _btnCancel.SetActive(false);

            // 关闭按钮
            var closeBtn = titleBar?.Find("CloseBtn")?.GetComponent<Button>();
            if (closeBtn != null) closeBtn.onClick.AddListener(Close);
        }

        public static CommonPopup GetOrCreate()
        {
            if (Instance != null) return Instance;

            if (_prefab == null)
                _prefab = Resources.Load<GameObject>("Prefabs/Common/CommonPopup");

            GameObject go;
            if (_prefab != null)
            {
                go = Instantiate(_prefab);
                Instance = go.GetComponent<CommonPopup>();
                if (Instance == null) Instance = go.AddComponent<CommonPopup>();
            }
            else
            {
                go = new GameObject("CommonPopup");
                Instance = go.AddComponent<CommonPopup>();
            }

            DontDestroyOnLoad(go);
            return Instance;
        }

        public Transform ContentRoot => _contentRoot;

        /// <summary>普通弹窗：只显示标题+内容+关闭按钮</summary>
        public void Show(string title)
        {
            if (!_initialized) AutoBindReferences();

            if (_titleText != null) _titleText.text = title;
            if (_btnSure != null) _btnSure.SetActive(false);
            if (_btnCancel != null) _btnCancel.SetActive(false);

            ClearContent();
            gameObject.SetActive(true);
        }

        /// <summary>确认弹窗：显示标题+内容+确认/取消按钮</summary>
        public void ShowConfirm(string title, string message, Action onConfirm, string sureText = "确认", string cancelText = "取消")
        {
            if (!_initialized) AutoBindReferences();

            if (_titleText != null) _titleText.text = title;

            ClearContent();
            AddText(message, 22, new Color(0.85f, 0.85f, 0.9f));

            if (_btnSure != null) _btnSure.SetActive(true);
            if (_btnCancel != null) _btnCancel.SetActive(true);

            // 设置按钮文字
            var sureTmp = _btnSure?.transform.Find("Text (TMP)")?.GetComponent<TextMeshProUGUI>();
            if (sureTmp != null) sureTmp.text = sureText;
            var cancelTmp = _btnCancel?.transform.Find("Text (TMP)")?.GetComponent<TextMeshProUGUI>();
            if (cancelTmp != null) cancelTmp.text = cancelText;

            // 绑定按钮事件
            if (_sureButton != null)
            {
                _sureButton.onClick.RemoveAllListeners();
                _sureButton.onClick.AddListener(() =>
                {
                    onConfirm?.Invoke();
                    Close();
                });
            }
            if (_cancelButton != null)
            {
                _cancelButton.onClick.RemoveAllListeners();
                _cancelButton.onClick.AddListener(Close);
            }

            gameObject.SetActive(true);
        }

        public TextMeshProUGUI AddText(string text, float fontSize, Color color)
        {
            if (_contentRoot == null) return null;
            var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            var go = new GameObject("Text");
            go.transform.SetParent(_contentRoot, false);
            go.AddComponent<RectTransform>();
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.raycastTarget = false;
            if (font) tmp.font = font;
            var le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return tmp;
        }

        public void Close()
        {
            gameObject.SetActive(false);
            ClearContent();
            if (_btnSure != null) _btnSure.SetActive(false);
            if (_btnCancel != null) _btnCancel.SetActive(false);
        }

        private void ClearContent()
        {
            if (_contentRoot == null) return;
            for (int i = _contentRoot.childCount - 1; i >= 0; i--)
                Destroy(_contentRoot.GetChild(i).gameObject);
        }
    }
}
