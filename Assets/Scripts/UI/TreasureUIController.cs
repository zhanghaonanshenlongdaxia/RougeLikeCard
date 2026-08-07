using TMPro;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using Alchemy.Inspector;
using CardGame.Audio;

namespace CardGame.UI
{
    public class TreasureUIController : MonoBehaviour, IController
    {
        [SerializeField] private Image rewardImage;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Transform rewardSlotsRoot;
        [SerializeField] private Button confirmButton;
        private TMP_FontAsset _font;

        public IArchitecture GetArchitecture() => CardGameArchitecture.Interface;

        private void Awake()
        {
            _font = UnityEngine.Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            BuildUI();
        }

        private void BuildUI()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 50;
                var scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                gameObject.AddComponent<GraphicRaycaster>();
            }

            // 全屏遮罩背景
            var bgObj = new GameObject("Background");
            bgObj.transform.SetParent(transform, false);
            var bgRt = bgObj.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
            bgObj.AddComponent<Image>().color = new Color(0, 0, 0, 0.95f);

            Transform panel = transform.Find("Panel");
            if (panel == null)
            {
                var panelObj = new GameObject("Panel");
                panelObj.transform.SetParent(transform, false);
                var panelRt = panelObj.AddComponent<RectTransform>();
                panelRt.anchorMin = new Vector2(0.2f, 0.15f); panelRt.anchorMax = new Vector2(0.8f, 0.85f);
                panelRt.offsetMin = Vector2.zero; panelRt.offsetMax = Vector2.zero;
                panelObj.AddComponent<Image>().color = new Color(0.08f, 0.1f, 0.15f, 1f);
                panel = panelObj.transform;
            }

            // 标题
            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(panel, false);
            var titleRt = titleObj.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.85f); titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.offsetMin = Vector2.zero; titleRt.offsetMax = Vector2.zero;
            var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "宝箱"; titleTmp.fontSize = 28; titleTmp.color = new Color(0.9f, 0.8f, 0.3f);
            titleTmp.alignment = TextAlignmentOptions.Center;
            if (_font) titleTmp.font = _font;

            // 描述
            if (descriptionText == null)
            {
                var dObj = new GameObject("DescriptionText");
                dObj.transform.SetParent(panel, false);
                var dRt = dObj.AddComponent<RectTransform>();
                dRt.anchorMin = new Vector2(0.05f, 0.25f); dRt.anchorMax = new Vector2(0.95f, 0.85f);
                dRt.offsetMin = Vector2.zero; dRt.offsetMax = Vector2.zero;
                descriptionText = dObj.AddComponent<TextMeshProUGUI>();
                descriptionText.fontSize = 20; descriptionText.color = Color.white;
                descriptionText.alignment = TextAlignmentOptions.Center;
                if (_font) descriptionText.font = _font;
            }

            // 确认按钮
            if (confirmButton == null)
            {
                var cObj = new GameObject("ConfirmButton");
                cObj.transform.SetParent(panel, false);
                var cRt = cObj.AddComponent<RectTransform>();
                cRt.anchorMin = new Vector2(0.35f, 0.05f); cRt.anchorMax = new Vector2(0.65f, 0.2f);
                cRt.offsetMin = Vector2.zero; cRt.offsetMax = Vector2.zero;
                cObj.AddComponent<Image>().color = new Color(0.2f, 0.5f, 0.3f, 1f);

                var ctObj = new GameObject("Text");
                ctObj.transform.SetParent(cObj.transform, false);
                var ctRt = ctObj.AddComponent<RectTransform>();
                ctRt.anchorMin = Vector2.zero; ctRt.anchorMax = Vector2.one;
                ctRt.offsetMin = Vector2.zero; ctRt.offsetMax = Vector2.zero;
                var ctTmp = ctObj.AddComponent<TextMeshProUGUI>();
                ctTmp.text = "确认"; ctTmp.fontSize = 20; ctTmp.color = Color.white;
                ctTmp.alignment = TextAlignmentOptions.Center;
                if (_font) ctTmp.font = _font;

                confirmButton = cObj.AddComponent<Button>();
                confirmButton.onClick.AddListener(() => {
                    if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick);
                    OnConfirm();
                });
            }
        }

        public void ShowTreasure(string desc)
        {
            if (descriptionText) descriptionText.text = desc;
            gameObject.SetActive(true);
        }

        public void OnConfirm()
        {
            gameObject.SetActive(false);
            var mapMgr = FindObjectOfType<NueGames.NueDeck.Scripts.Managers.MapManager>();
            if (mapMgr != null) mapMgr.SendMessage("BuildMap");
        }
    }
}
