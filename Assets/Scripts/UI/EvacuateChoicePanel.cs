using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CardGame.Audio;

namespace CardGame.UI
{
    /// <summary>
    /// 撤离选择面板：Boss通关后弹出，选择撤离(回基地)或继续冒险
    /// </summary>
    public class EvacuateChoicePanel : MonoBehaviour
    {
        private TMP_FontAsset _font;

        public void Init(System.Action onEvacuate, System.Action onContinue)
        {
            _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            BuildUI(onEvacuate, onContinue);
        }

        void BuildUI(System.Action onEvacuate, System.Action onContinue)
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 250;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            gameObject.AddComponent<GraphicRaycaster>();

            // 背景
            var bg = new GameObject("BG");
            bg.transform.SetParent(transform, false);
            var bgRt = bg.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
            bg.AddComponent<Image>().color = new Color(0, 0, 0, 0.8f);

            // 面板
            var panel = new GameObject("Panel");
            panel.transform.SetParent(transform, false);
            var pRt = panel.AddComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0.25f, 0.2f); pRt.anchorMax = new Vector2(0.75f, 0.8f);
            pRt.offsetMin = Vector2.zero; pRt.offsetMax = Vector2.zero;
            panel.AddComponent<Image>().color = new Color(0.08f, 0.1f, 0.15f, 0.98f);

            var vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 20; vlg.padding = new RectOffset(30, 30, 30, 30);
            vlg.childControlWidth = true; vlg.childAlignment = TextAnchor.MiddleCenter;

            // 标题
            var title = CreateText(panel.transform, "大关卡通关!", 32, new Color(0.9f, 0.8f, 0.3f));
            title.AddComponent<LayoutElement>().preferredHeight = 45;

            // 描述
            var desc = CreateText(panel.transform, "你已击败此关Boss。\n是御剑归去(撤离)，还是继续深入？\n\n撤离：所有灵材安全送回乾坤袋\n继续：可能获得更多机缘，但死亡将失去全部携带物品", 20, new Color(0.85f, 0.85f, 0.85f));
            desc.AddComponent<LayoutElement>().preferredHeight = 100;

            // 按钮行
            var btnRow = new GameObject("ButtonRow");
            btnRow.transform.SetParent(panel.transform, false);
            btnRow.AddComponent<RectTransform>();
            var hlg = btnRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 30; hlg.childControlWidth = true; hlg.childForceExpandWidth = true;
            btnRow.AddComponent<LayoutElement>().preferredHeight = 60;

            // 撤离按钮
            var evacBtn = CreateButton(btnRow.transform, "御剑归去", new Color(0.15f, 0.3f, 0.5f, 1f));
            evacBtn.onClick.AddListener(() =>
            {
                if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick);
                Destroy(gameObject);
                onEvacuate?.Invoke();
            });

            // 继续按钮
            var contBtn = CreateButton(btnRow.transform, "继续历练", new Color(0.3f, 0.2f, 0.1f, 1f));
            contBtn.onClick.AddListener(() =>
            {
                if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick);
                Destroy(gameObject);
                onContinue?.Invoke();
            });
        }

        Button CreateButton(Transform parent, string label, Color color)
        {
            var go = new GameObject($"Btn_{label}");
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = color;
            var txtObj = new GameObject("Text");
            txtObj.transform.SetParent(go.transform, false);
            var txtRt = txtObj.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero; txtRt.offsetMax = Vector2.zero;
            var tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label; tmp.fontSize = 24; tmp.color = new Color(0.95f, 0.85f, 0.4f);
            tmp.alignment = TextAlignmentOptions.Center;
            if (_font) tmp.font = _font;
            return go.AddComponent<Button>();
        }

        GameObject CreateText(Transform parent, string text, float size, Color color)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = size; tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            if (_font) tmp.font = _font;
            return go;
        }
    }
}
