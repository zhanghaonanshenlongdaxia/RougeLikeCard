using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CardGame.Audio;

namespace CardGame.UI
{
    /// <summary>
    /// 通用 UI 辅助方法。
    /// EnsureCloseButton: 在 panel 上查找或创建"离开"按钮并绑定关闭回调，点击播放音效。
    /// </summary>
    public static class UIHelper
    {
        public static Button EnsureCloseButton(MonoBehaviour behaviour, System.Action onClose)
        {
            // 若 panel 没有子物体（transform.GetChild(0) 会越界），先确保至少有一个容器
            Transform container = behaviour.transform;
            if (container.childCount == 0)
            {
                var panelObj = new GameObject("Panel");
                panelObj.transform.SetParent(container, false);
                var panelRt = panelObj.AddComponent<RectTransform>();
                panelRt.anchorMin = Vector2.zero; panelRt.anchorMax = Vector2.one;
                panelRt.offsetMin = Vector2.zero; panelRt.offsetMax = Vector2.zero;
                panelObj.AddComponent<Image>().color = new Color(0.1f, 0.12f, 0.18f, 0.95f);
                container = panelObj.transform;
            }

            // 尝试查找已有返回/关闭按钮
            var buttons = behaviour.GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                var n = btn.gameObject.name;
                if (n.Contains("Back") || n.Contains("Close") || n.Contains("离开"))
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => { PlayClick(); onClose?.Invoke(); });
                    return btn;
                }
            }

            // 创建关闭按钮
            var go = new GameObject("CloseButton");
            go.transform.SetParent(container, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(-10, -10);
            rt.sizeDelta = new Vector2(80, 40);
            go.AddComponent<Image>().color = new Color(0.6f, 0.15f, 0.15f, 1);

            var txtObj = new GameObject("Text");
            txtObj.transform.SetParent(go.transform, false);
            var txtRt = txtObj.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero; txtRt.offsetMax = Vector2.zero;
            var tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "离开";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 18;
            tmp.color = Color.white;
            var libSans = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (libSans) tmp.font = libSans;

            var btn2 = go.AddComponent<Button>();
            btn2.onClick.AddListener(() => { PlayClick(); onClose?.Invoke(); });
            return btn2;
        }

        private static void PlayClick()
        {
            if (GameAudioManager.Instance != null)
                GameAudioManager.Instance.PlaySFX(SFXType.UIClick);
        }
    }
}