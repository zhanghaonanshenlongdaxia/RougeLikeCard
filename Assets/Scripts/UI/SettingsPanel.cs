using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CardGame.Audio;

namespace CardGame.UI
{
    /// <summary>
    /// 设置面板：BGM音量、SFX音量、全屏切换、返回。
    /// 运行时创建，无prefab依赖。
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        private TMP_FontAsset _font;
        private Slider _bgmSlider;
        private Slider _sfxSlider;
        private TextMeshProUGUI _bgmValue;
        private TextMeshProUGUI _sfxValue;

        private void Awake()
        {
            _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            BuildUI();
        }

        void BuildUI()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 300;
                var scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                gameObject.AddComponent<GraphicRaycaster>();
            }

            // 半透明遮罩
            var bg = new GameObject("BG");
            bg.transform.SetParent(transform, false);
            var bgRt = bg.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
            bg.AddComponent<Image>().color = new Color(0, 0, 0, 0.7f);

            // 主面板
            var panel = new GameObject("Panel");
            panel.transform.SetParent(transform, false);
            var pRt = panel.AddComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0.3f, 0.15f); pRt.anchorMax = new Vector2(0.7f, 0.85f);
            pRt.offsetMin = Vector2.zero; pRt.offsetMax = Vector2.zero;
            panel.AddComponent<Image>().color = new Color(0.08f, 0.1f, 0.15f, 0.97f);

            var vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 25; vlg.padding = new RectOffset(30, 30, 30, 30);
            vlg.childControlWidth = true; vlg.childControlHeight = false;
            vlg.childAlignment = TextAnchor.UpperCenter;

            // === 标题 ===
            CreateText(panel.transform, "设  置", 32, new Color(0.9f, 0.8f, 0.3f), 45);

            // === BGM音量 ===
            CreateSliderRow(panel.transform, "背景音乐", out _bgmSlider, out _bgmValue, 0f, 1f,
                GameAudioManager.Instance != null ? GameAudioManager.Instance.bgmVolume : 0.5f,
                (v) => { if (GameAudioManager.Instance != null) GameAudioManager.Instance.bgmVolume = v; });

            // === SFX音量 ===
            CreateSliderRow(panel.transform, "音效", out _sfxSlider, out _sfxValue, 0f, 1f,
                GameAudioManager.Instance != null ? GameAudioManager.Instance.sfxVolume : 0.8f,
                (v) => { if (GameAudioManager.Instance != null) GameAudioManager.Instance.sfxVolume = v; });

            // === 分割线 ===
            CreateText(panel.transform, "", 1, Color.clear, 10);

            // === 全屏切换 ===
            CreateToggleRow(panel.transform, "全屏模式", Screen.fullScreen, (v) =>
            {
                Screen.fullScreen = v;
                FloatingTip.Show(v ? "已切换全屏" : "已切换窗口");
            });

            // === 清除存档 ===
            var clearBtn = CreateButton(panel.transform, "清除存档", new Color(0.5f, 0.2f, 0.2f, 1f));
            clearBtn.onClick.AddListener(() =>
            {
                if (SaveSystem.HasSave())
                {
                    SaveSystem.DeleteSave();
                    FloatingTip.ShowSuccess("存档已清除");
                }
                else
                    FloatingTip.ShowWarning("没有存档");
            });

            // === 返回按钮 ===
            var closeBtn = CreateButton(panel.transform, "返回", new Color(0.15f, 0.3f, 0.5f, 1f));
            closeBtn.onClick.AddListener(() =>
            {
                if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick);
                Destroy(gameObject);
            });
        }

        void CreateSliderRow(Transform parent, string label, out Slider slider, out TextMeshProUGUI valueText,
            float min, float max, float current, System.Action<float> onValueChanged)
        {
            TextMeshProUGUI vText = null;
            CreateSliderRowInternal(parent, label, out slider, ref vText, min, max, current, onValueChanged);
            valueText = vText;
        }

        void CreateSliderRowInternal(Transform parent, string label, out Slider slider, ref TextMeshProUGUI valueText,
            float min, float max, float current, System.Action<float> onValueChanged)
        {
            var row = new GameObject($"Row_{label}");
            row.transform.SetParent(parent, false);
            var rowRt = row.AddComponent<RectTransform>();
            var rowLe = row.AddComponent<LayoutElement>();
            rowLe.preferredHeight = 50;

            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 15; hlg.childControlWidth = true; hlg.childForceExpandWidth = true;

            // 标签
            var labelObj = CreateText(row.transform, label, 22, Color.white, 0);
            var labelLe = labelObj.AddComponent<LayoutElement>();
            labelLe.preferredWidth = 120;

            // Slider
            var sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(row.transform, false);
            var sliderRt = sliderObj.AddComponent<RectTransform>();
            var sliderLe = sliderObj.AddComponent<LayoutElement>();
            sliderLe.flexibleWidth = 1;

            // 背景
            var bgObj = new GameObject("BG");
            bgObj.transform.SetParent(sliderObj.transform, false);
            var bgRt = bgObj.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
            bgObj.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 1f);

            // Fill
            var fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(bgObj.transform, false);
            var fillRt = fillObj.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero; fillRt.offsetMax = Vector2.zero;
            fillObj.AddComponent<Image>().color = new Color(0.2f, 0.5f, 0.3f, 1f);

            // Handle
            var handleObj = new GameObject("Handle");
            handleObj.transform.SetParent(fillObj.transform, false);
            var handleRt = handleObj.AddComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(20, 20);
            handleObj.AddComponent<Image>().color = Color.white;

            slider = sliderObj.AddComponent<Slider>();
            slider.fillRect = fillRt;
            slider.targetGraphic = handleObj.GetComponent<Image>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = current;

            // 数值文字（先创建，再绑定slider事件）
            var valObj = new GameObject("Value");
            valObj.transform.SetParent(row.transform, false);
            var valRt = valObj.AddComponent<RectTransform>();
            var valLe = valObj.AddComponent<LayoutElement>();
            valLe.preferredWidth = 60;
            valueText = valObj.AddComponent<TextMeshProUGUI>();
            valueText.text = $"{Mathf.RoundToInt(current * 100)}%";
            valueText.fontSize = 20; valueText.color = new Color(1f, 0.85f, 0f);
            valueText.alignment = TextAlignmentOptions.Center;
            if (_font) valueText.font = _font;

            var capturedText = valueText;
            slider.onValueChanged.AddListener((v) =>
            {
                capturedText.text = $"{Mathf.RoundToInt(v * 100)}%";
                onValueChanged?.Invoke(v);
            });
        }

        void CreateToggleRow(Transform parent, string label, bool isOn, System.Action<bool> onToggle)
        {
            var row = new GameObject($"Row_{label}");
            row.transform.SetParent(parent, false);
            var rowLe = row.AddComponent<LayoutElement>();
            rowLe.preferredHeight = 45;
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 15; hlg.childControlWidth = true; hlg.childForceExpandWidth = true;

            var labelObj = CreateText(row.transform, label, 22, Color.white, 0);
            var labelLe = labelObj.AddComponent<LayoutElement>();
            labelLe.preferredWidth = 120;

            var toggleObj = new GameObject("Toggle");
            toggleObj.transform.SetParent(row.transform, false);
            toggleObj.AddComponent<RectTransform>();
            var toggleLe = toggleObj.AddComponent<LayoutElement>();
            toggleLe.preferredWidth = 60;

            var bgObj = new GameObject("BG");
            bgObj.transform.SetParent(toggleObj.transform, false);
            var bgRt = bgObj.AddComponent<RectTransform>();
            bgRt.sizeDelta = new Vector2(50, 25);
            bgObj.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 1f);

            var checkObj = new GameObject("Checkmark");
            checkObj.transform.SetParent(bgObj.transform, false);
            var checkRt = checkObj.AddComponent<RectTransform>();
            checkRt.sizeDelta = new Vector2(40, 20);
            checkObj.AddComponent<Image>().color = new Color(0.2f, 0.5f, 0.3f, 1f);

            var toggle = toggleObj.AddComponent<Toggle>();
            toggle.targetGraphic = bgObj.GetComponent<Image>();
            toggle.graphic = checkObj.GetComponent<Image>();
            toggle.isOn = isOn;
            toggle.onValueChanged.AddListener((v) =>
            {
                if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick);
                onToggle?.Invoke(v);
            });
        }

        Button CreateButton(Transform parent, string label, Color color)
        {
            var btnObj = new GameObject($"Btn_{label}");
            btnObj.transform.SetParent(parent, false);
            var btnLe = btnObj.AddComponent<LayoutElement>();
            btnLe.preferredHeight = 50;
            btnObj.AddComponent<Image>().color = color;

            var txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            var txtRt = txtObj.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero; txtRt.offsetMax = Vector2.zero;
            var tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label; tmp.fontSize = 24; tmp.color = new Color(0.95f, 0.85f, 0.4f);
            tmp.alignment = TextAlignmentOptions.Center;
            if (_font) tmp.font = _font;

            return btnObj.AddComponent<Button>();
        }

        GameObject CreateText(Transform parent, string text, float size, Color color, float preferredHeight)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            if (preferredHeight > 0)
            {
                var le = go.AddComponent<LayoutElement>();
                le.preferredHeight = preferredHeight;
            }
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = size; tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            if (_font) tmp.font = _font;
            return go;
        }
    }
}
