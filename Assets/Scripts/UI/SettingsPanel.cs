using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CardGame.Audio;

namespace CardGame.UI
{
    /// <summary>
    /// 设置面板：BGM音量、SFX音量、全屏切换、返回。
    /// Prefab 驱动，运行时只绑定事件和刷新数据。
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        [Header("Sliders")]
        [SerializeField] private Slider _bgmSlider;
        [SerializeField] private TextMeshProUGUI _bgmValue;
        [SerializeField] private Slider _sfxSlider;
        [SerializeField] private TextMeshProUGUI _sfxValue;

        [Header("Toggle")]
        [SerializeField] private Toggle _fullscreenToggle;

        [Header("Buttons")]
        [SerializeField] private Button _clearSaveButton;
        [SerializeField] private Button _backButton;

        private void Awake()
        {
            AutoBindReferences();
            WireEvents();
        }

        private void AutoBindReferences()
        {
            var panel = transform.Find("Panel");
            if (panel == null) return;

            if (_bgmSlider == null) _bgmSlider = panel.Find("BGMRow/Slider")?.GetComponent<Slider>();
            if (_bgmValue == null) _bgmValue = panel.Find("BGMRow/Value")?.GetComponent<TextMeshProUGUI>();
            if (_sfxSlider == null) _sfxSlider = panel.Find("SFXRow/Slider")?.GetComponent<Slider>();
            if (_sfxValue == null) _sfxValue = panel.Find("SFXRow/Value")?.GetComponent<TextMeshProUGUI>();
            if (_fullscreenToggle == null) _fullscreenToggle = panel.Find("FullscreenRow/Toggle")?.GetComponent<Toggle>();
            if (_clearSaveButton == null) _clearSaveButton = panel.Find("ClearSaveButton")?.GetComponent<Button>();
            if (_backButton == null) _backButton = panel.Find("BackButton")?.GetComponent<Button>();
        }

        private void WireEvents()
        {
            float bgmVol = GameAudioManager.Instance != null ? GameAudioManager.Instance.bgmVolume : 0.5f;
            float sfxVol = GameAudioManager.Instance != null ? GameAudioManager.Instance.sfxVolume : 0.8f;

            if (_bgmSlider) { _bgmSlider.value = bgmVol; _bgmSlider.onValueChanged.AddListener(v =>
            {
                if (_bgmValue) _bgmValue.text = $"{Mathf.RoundToInt(v * 100)}%";
                if (GameAudioManager.Instance != null) GameAudioManager.Instance.bgmVolume = v;
            }); }
            if (_bgmValue) _bgmValue.text = $"{Mathf.RoundToInt(bgmVol * 100)}%";

            if (_sfxSlider) { _sfxSlider.value = sfxVol; _sfxSlider.onValueChanged.AddListener(v =>
            {
                if (_sfxValue) _sfxValue.text = $"{Mathf.RoundToInt(v * 100)}%";
                if (GameAudioManager.Instance != null) GameAudioManager.Instance.sfxVolume = v;
            }); }
            if (_sfxValue) _sfxValue.text = $"{Mathf.RoundToInt(sfxVol * 100)}%";

            if (_fullscreenToggle) { _fullscreenToggle.isOn = Screen.fullScreen; _fullscreenToggle.onValueChanged.AddListener(v =>
            {
                Screen.fullScreen = v;
                FloatingTip.Show(v ? "已切换全屏" : "已切换窗口");
            }); }

            if (_clearSaveButton) _clearSaveButton.onClick.AddListener(() =>
            {
                if (SaveSystem.HasSave()) { SaveSystem.DeleteSave(); FloatingTip.ShowSuccess("存档已清除"); }
                else FloatingTip.ShowWarning("没有存档");
            });

            if (_backButton) _backButton.onClick.AddListener(() =>
            {
                if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick);
                Destroy(gameObject);
            });
        }
    }
}
