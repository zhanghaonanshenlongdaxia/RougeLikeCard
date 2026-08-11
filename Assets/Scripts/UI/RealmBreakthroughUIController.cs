using TMPro;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using CardGame.Audio;
using NueGames.NueDeck.Scripts.Enums;

namespace CardGame.UI
{
    /// <summary>
    /// 境界突破面板。运行时创建，无prefab依赖。
    /// </summary>
    public class RealmBreakthroughUIController : MonoBehaviour
    {
        private TMP_FontAsset _font;
        private TextMeshProUGUI _statusText;
        private TextMeshProUGUI _costText;
        private Button _breakthroughBtn;

        public IArchitecture GetArchitecture() => CardGameArchitecture.Interface;

        private void Awake()
        {
            _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            BuildUI();
            Refresh();
        }

        void BuildUI()
        {
            var canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 60;
                var scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                gameObject.AddComponent<GraphicRaycaster>();
            }

            // 清理prefab子物体
            Transform panel = transform.Find("Panel");
            if (panel != null)
            {
                for (int i = panel.childCount - 1; i >= 0; i--)
                    DestroyImmediate(panel.GetChild(i).gameObject);
                var vlg = panel.GetComponent<VerticalLayoutGroup>();
                if (vlg != null) DestroyImmediate(vlg);
                var csf = panel.GetComponent<ContentSizeFitter>();
                if (csf != null) DestroyImmediate(csf);
            }
            else
            {
                var pObj = new GameObject("Panel");
                pObj.transform.SetParent(transform, false);
                var pRt = pObj.AddComponent<RectTransform>();
                pRt.anchorMin = new Vector2(0.2f, 0.1f); pRt.anchorMax = new Vector2(0.8f, 0.9f);
                pRt.offsetMin = Vector2.zero; pRt.offsetMax = Vector2.zero;
                pObj.AddComponent<Image>().color = new Color(0.08f, 0.1f, 0.15f, 0.97f);
                panel = pObj.transform;
            }

            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 15; layout.padding = new RectOffset(25, 25, 25, 25);
            layout.childControlWidth = true; layout.childControlHeight = false;
            layout.childAlignment = TextAnchor.UpperCenter;

            // 标题
            CreateText(panel, "境界突破", 30, new Color(0.9f, 0.8f, 0.3f), 40);

            // 当前境界
            _statusText = CreateText(panel, "", 22, new Color(0.7f, 0.9f, 1f), 30).GetComponent<TextMeshProUGUI>();

            // 需求区
            _costText = CreateText(panel, "", 20, Color.white, 120).GetComponent<TextMeshProUGUI>();
            _costText.alignment = TextAlignmentOptions.Left;

            // 突破按钮
            var btnObj = new GameObject("BreakthroughButton");
            btnObj.transform.SetParent(panel, false);
            btnObj.AddComponent<Image>().color = new Color(0.2f, 0.5f, 0.3f, 1f);
            var btnLe = btnObj.AddComponent<LayoutElement>();
            btnLe.preferredHeight = 55;
            var btnTxt = CreateText(btnObj.transform, "突  破", 26, new Color(0.95f, 0.85f, 0.4f), 0).GetComponent<TextMeshProUGUI>();
            btnTxt.alignment = TextAlignmentOptions.Center;
            _breakthroughBtn = btnObj.AddComponent<Button>();
            _breakthroughBtn.onClick.AddListener(OnBreakthrough);

            // 关闭按钮
            var closeObj = new GameObject("CloseButton");
            closeObj.transform.SetParent(panel, false);
            closeObj.AddComponent<Image>().color = new Color(0.5f, 0.2f, 0.2f, 1f);
            var closeLe = closeObj.AddComponent<LayoutElement>();
            closeLe.preferredHeight = 45;
            var closeTxt = CreateText(closeObj.transform, "返  回", 22, new Color(0.9f, 0.8f, 0.5f), 0).GetComponent<TextMeshProUGUI>();
            closeTxt.alignment = TextAlignmentOptions.Center;
            var closeBtn = closeObj.AddComponent<Button>();
            closeBtn.onClick.AddListener(() =>
            {
                if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick);
                gameObject.SetActive(false);
                var mapMgr = FindObjectOfType<NueGames.NueDeck.Scripts.Managers.MapManager>();
                if (mapMgr != null) mapMgr.SendMessage("BuildMap");
            });
        }

        void Refresh()
        {
            var arch = CardGameArchitecture.Interface;
            var realmSystem = arch.GetSystem<IRealmSystem>();
            var realmModel = arch.GetModel<IRealmModel>();
            var battleModel = arch.GetModel<IBattleModel>();

            string currentName = realmSystem.GetCurrentRealmName();
            _statusText.text = $"当前境界：{currentName}";

            var next = realmSystem.GetNextBreakthrough();
            if (next == null)
            {
                _costText.text = "已达最高境界，无法突破。";
                _breakthroughBtn.interactable = false;
                _breakthroughBtn.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                return;
            }

            bool canBreak = realmSystem.CanBreakthrough();
            _costText.text = $"突破至：{next.realmName}\n\n灵石：{next.goldCost}（持有 {battleModel.CurrentGold.Value}）\n材料：{ItemQualityHelper.GetDisplayName(next.requiredQuality)} × {next.materialCount}\n丹药：{next.requiredPotionId}\n\n奖励：+{next.hpBonus} HP / +{next.shenShiBonus} 神识上限\n\n{next.description}";

            _breakthroughBtn.interactable = canBreak;
            _breakthroughBtn.GetComponent<Image>().color = canBreak
                ? new Color(0.2f, 0.5f, 0.3f, 1f)
                : new Color(0.3f, 0.3f, 0.3f, 0.5f);
        }

        void OnBreakthrough()
        {
            if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick);
            var arch = CardGameArchitecture.Interface;
            var realmSystem = arch.GetSystem<IRealmSystem>();

            if (realmSystem.DoBreakthrough())
            {
                FloatingTip.ShowSuccess("境界突破成功！");
                Refresh();
            }
            else
            {
                FloatingTip.ShowWarning("突破条件不足");
            }
        }

        GameObject CreateText(Transform parent, string text, float size, Color color, float preferredHeight)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            if (preferredHeight > 0)
                go.AddComponent<LayoutElement>().preferredHeight = preferredHeight;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = size; tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            if (_font) tmp.font = _font;
            return go;
        }
    }
}
