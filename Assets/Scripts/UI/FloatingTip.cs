using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardGame.UI
{
    /// <summary>
    /// 通用飘字/弹窗提示工具。静态方法可在任何地方调用。
    /// </summary>
    public static class FloatingTip
    {
        private static TMP_FontAsset _font;

        static TMP_FontAsset GetFont()
        {
            if (_font != null) return _font;
            _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            return _font;
        }

        /// <summary>
        /// 在屏幕中央弹出短暂提示文字（1.5秒后消失）
        /// </summary>
        public static void Show(string message, Color? color = null)
        {
            var go = new GameObject("FloatingTip");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            go.AddComponent<GraphicRaycaster>();

            // 半透明背景
            var bg = new GameObject("BG");
            bg.transform.SetParent(go.transform, false);
            var bgRt = bg.AddComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0.3f, 0.45f); bgRt.anchorMax = new Vector2(0.7f, 0.55f);
            bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

            // 文字
            var txt = new GameObject("Text");
            txt.transform.SetParent(bg.transform, false);
            var txtRt = txt.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(10, 5); txtRt.offsetMax = new Vector2(-10, -5);
            var tmp = txt.AddComponent<TextMeshProUGUI>();
            tmp.text = message;
            tmp.fontSize = 24;
            tmp.color = color ?? Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            var font = GetFont();
            if (font) tmp.font = font;

            Object.DontDestroyOnLoad(go);
            var runner = go.AddComponent<FloatingTipRunner>();
            runner.StartCoroutine(AnimateAndDestroy(go, 1.5f));
        }

        /// <summary>灵石不足提示</summary>
        public static void ShowInsufficientGold() => Show("灵石不足!", new Color(1f, 0.3f, 0.3f));

        /// <summary>购买成功提示</summary>
        public static void ShowPurchaseSuccess(string itemName) => Show($"购买成功: {itemName}", new Color(0.3f, 1f, 0.3f));

        /// <summary>通用成功提示</summary>
        public static void ShowSuccess(string msg) => Show(msg, new Color(0.3f, 1f, 0.3f));

        /// <summary>通用警告提示</summary>
        public static void ShowWarning(string msg) => Show(msg, new Color(1f, 0.7f, 0.2f));

        static IEnumerator AnimateAndDestroy(GameObject go, float duration)
        {
            yield return new WaitForSeconds(duration * 0.7f);
            // 淡出
            var canvasGroup = go.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = go.AddComponent<CanvasGroup>();
            float fadeTime = duration * 0.3f;
            float elapsed = 0;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = 1f - (elapsed / fadeTime);
                yield return null;
            }
            Object.Destroy(go);
        }
    }

    /// <summary>用于在静态方法中启动协程的MonoBehaviour</summary>
    public class FloatingTipRunner : MonoBehaviour { }
}
