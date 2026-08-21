using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

namespace CardGame.Story
{
    /// <summary>
    /// 剧情对话控制器 — Yarn Spinner 3.x DialoguePresenterBase实现
    /// 古风对话框：角色名 + 立绘 + 逐字文本 + 选项按钮
    /// 由 StoryService 按需创建（DontDestroyOnLoad），无需场景预置
    /// </summary>
    public class StoryDialoguePresenter : DialoguePresenterBase
    {
        CanvasGroup _canvasGroup;
        TextMeshProUGUI _nameText;
        TextMeshProUGUI _lineText;
        Image _portrait;
        RectTransform _optionsRoot;
        GameObject _panel;

        readonly List<Button> _optionButtons = new List<Button>();

        // 当前行/选项的完成信号
        YarnTaskCompletionSource _lineDone;
        YarnTaskCompletionSource<int> _optionChosen;
        bool _hurryUp;

        Coroutine _typewriterRoutine;

        public static StoryDialoguePresenter Create(Transform parent = null)
        {
            var go = new GameObject("StoryDialogueUI");
            if (parent != null) go.transform.SetParent(parent, false);
            var presenter = go.AddComponent<StoryDialoguePresenter>();
            presenter.BuildUI();
            return presenter;
        }

        void BuildUI()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 150;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            gameObject.AddComponent<GraphicRaycaster>();

            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

            // 底部对话面板
            _panel = new GameObject("Panel");
            _panel.transform.SetParent(transform, false);
            var panelRt = _panel.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.15f, 0.03f);
            panelRt.anchorMax = new Vector2(0.85f, 0.32f);
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;
            var panelImg = _panel.AddComponent<Image>();
            panelImg.color = new Color(0.05f, 0.06f, 0.09f, 0.97f);

            // 点击面板推进（放最底层，按钮在上层）
            var clickCatcher = _panel.AddComponent<Button>();
            clickCatcher.transition = Selectable.Transition.None;
            clickCatcher.onClick.AddListener(OnAdvanceClicked);

            // 左侧立绘
            var portraitGo = new GameObject("Portrait");
            portraitGo.transform.SetParent(_panel.transform, false);
            var pRt = portraitGo.AddComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0, 0);
            pRt.anchorMax = new Vector2(0, 1);
            pRt.pivot = new Vector2(0, 0.5f);
            pRt.anchoredPosition = new Vector2(14, 0);
            pRt.sizeDelta = new Vector2(220, -20);
            _portrait = portraitGo.AddComponent<Image>();
            _portrait.preserveAspect = true;
            _portrait.color = new Color(1, 1, 1, 0.25f);

            // 角色名
            var nameGo = new GameObject("NameText");
            nameGo.transform.SetParent(_panel.transform, false);
            var nRt = nameGo.AddComponent<RectTransform>();
            nRt.anchorMin = new Vector2(0, 1);
            nRt.anchorMax = new Vector2(0.5f, 1);
            nRt.pivot = new Vector2(0, 1);
            nRt.anchoredPosition = new Vector2(250, -10);
            nRt.sizeDelta = new Vector2(300, 44);
            _nameText = nameGo.AddComponent<TextMeshProUGUI>();
            _nameText.fontSize = 34;
            _nameText.fontStyle = FontStyles.Bold;
            _nameText.color = new Color(0.95f, 0.87f, 0.6f);
            _nameText.alignment = TextAlignmentOptions.Left;
            _nameText.raycastTarget = false;
            if (font != null) _nameText.font = font;

            // 正文
            var lineGo = new GameObject("LineText");
            lineGo.transform.SetParent(_panel.transform, false);
            var lRt = lineGo.AddComponent<RectTransform>();
            lRt.anchorMin = new Vector2(0, 0);
            lRt.anchorMax = new Vector2(1, 1);
            lRt.offsetMin = new Vector2(255, 56);
            lRt.offsetMax = new Vector2(-40, -60);
            _lineText = lineGo.AddComponent<TextMeshProUGUI>();
            _lineText.fontSize = 30;
            _lineText.color = new Color(0.94f, 0.94f, 0.9f);
            _lineText.alignment = TextAlignmentOptions.TopLeft;
            _lineText.raycastTarget = false;
            _lineText.enableWordWrapping = true;
            if (font != null) _lineText.font = font;

            // 继续提示
            var hintGo = new GameObject("ContinueHint");
            hintGo.transform.SetParent(_panel.transform, false);
            var hRt = hintGo.AddComponent<RectTransform>();
            hRt.anchorMin = new Vector2(1, 0);
            hRt.anchorMax = new Vector2(1, 0);
            hRt.pivot = new Vector2(1, 0);
            hRt.anchoredPosition = new Vector2(-20, 10);
            hRt.sizeDelta = new Vector2(120, 36);
            var hintTmp = hintGo.AddComponent<TextMeshProUGUI>();
            hintTmp.fontSize = 24;
            hintTmp.color = new Color(0.7f, 0.75f, 0.65f);
            hintTmp.alignment = TextAlignmentOptions.Right;
            hintTmp.text = "点击继续 ▼";
            hintTmp.raycastTarget = false;
            if (font != null) hintTmp.font = font;

            // 选项容器
            var optGo = new GameObject("OptionsRoot");
            optGo.transform.SetParent(_panel.transform, false);
            var oRt = optGo.AddComponent<RectTransform>();
            oRt.anchorMin = new Vector2(0.55f, 0);
            oRt.anchorMax = new Vector2(1, 1);
            oRt.offsetMin = new Vector2(0, 10);
            oRt.offsetMax = new Vector2(-30, -10);
            var oLayout = optGo.AddComponent<VerticalLayoutGroup>();
            oLayout.spacing = 12;
            oLayout.childAlignment = TextAnchor.MiddleRight;
            oLayout.childForceExpandHeight = false;
            oLayout.childControlHeight = false;
            _optionsRoot = oRt;
            _optionsRoot.gameObject.SetActive(false);
        }

        // ===================== 玩家交互 =====================

        void OnAdvanceClicked()
        {
            if (_hurryUp && _typewriterRoutine != null)
            {
                // 第一次点击：跳过逐字动画
                StopCoroutine(_typewriterRoutine);
                _typewriterRoutine = null;
                _lineText.maxVisibleCharacters = int.MaxValue;
                _hurryUp = false;
                return;
            }
            _lineDone?.TrySetResult();
        }

        // ===================== DialoguePresenterBase 实现 =====================

        public override YarnTask OnDialogueStartedAsync()
        {
            StartCoroutine(Fade(true));
            return YarnTask.CompletedTask;
        }

        public override YarnTask OnDialogueCompleteAsync()
        {
            StartCoroutine(Fade(false));
            return YarnTask.CompletedTask;
        }

        public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
        {
            _hurryUp = true;
            _optionsRoot.gameObject.SetActive(false);

            // 角色名与立绘
            var nameText = line.CharacterName;
            if (!string.IsNullOrEmpty(nameText))
            {
                _nameText.gameObject.SetActive(true);
                _nameText.text = nameText;
                TrySetPortrait(nameText);
            }
            else
            {
                _nameText.gameObject.SetActive(false);
                _portrait.sprite = null;
                _portrait.color = new Color(1, 1, 1, 0.25f);
            }

            var text = line.TextWithoutCharacterName.Text;
            _lineText.maxVisibleCharacters = int.MaxValue;
            _lineText.text = text;

            if (_typewriterRoutine != null) StopCoroutine(_typewriterRoutine);
            _typewriterRoutine = StartCoroutine(TypewriterRoutine(text.Length));

            _lineDone = new YarnTaskCompletionSource();

            while (!_lineDone.Task.IsCompleted() && !token.NextContentToken.IsCancellationRequested)
                await YarnTask.Yield();

            if (_typewriterRoutine != null) { StopCoroutine(_typewriterRoutine); _typewriterRoutine = null; }
            _lineText.maxVisibleCharacters = int.MaxValue;
            _lineDone = null;
        }

        public override async YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] dialogueOptions, LineCancellationToken cancellationToken)
        {
            var available = new List<DialogueOption>();
            foreach (var o in dialogueOptions)
                if (o.IsAvailable) available.Add(o);
            if (available.Count == 0) return null;

            _optionsRoot.gameObject.SetActive(true);

            while (_optionButtons.Count < available.Count)
                _optionButtons.Add(CreateOptionButton());

            for (int i = 0; i < _optionButtons.Count; i++)
                _optionButtons[i].gameObject.SetActive(i < available.Count);

            _optionChosen = new YarnTaskCompletionSource<int>();

            for (int i = 0; i < available.Count; i++)
            {
                var idx = i;
                var btn = _optionButtons[i];
                var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
                tmp.text = available[i].Line.TextWithoutCharacterName.Text;

                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    _optionsRoot.gameObject.SetActive(false);
                    _optionChosen.TrySetResult(idx);
                });
            }

            while (!_optionChosen.Task.IsCompleted() && !cancellationToken.NextContentToken.IsCancellationRequested)
                await YarnTask.Yield();

            if (_optionChosen.Task.IsCompleted())
            {
                var chosenIdx = await _optionChosen.Task;
                _optionChosen = null;
                return available[chosenIdx];
            }

            _optionChosen = null;
            return null;
        }

        // ===================== 辅助 =====================

        Button CreateOptionButton()
        {
            var go = new GameObject("OptionButton");
            go.transform.SetParent(_optionsRoot, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 58);
            var layout = go.AddComponent<LayoutElement>();
            layout.minHeight = 58;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.16f, 0.2f, 0.28f, 0.95f);
            var btn = go.AddComponent<Button>();

            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(go.transform, false);
            var tRt = txtGo.AddComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.offsetMin = new Vector2(14, 2);
            tRt.offsetMax = new Vector2(-14, -2);
            var tmp = txtGo.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 26;
            tmp.color = new Color(0.95f, 0.95f, 0.9f);
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.raycastTarget = false;
            var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (font != null) tmp.font = font;

            return btn;
        }

        void TrySetPortrait(string characterName)
        {
            var sprite = Resources.Load<Sprite>($"StoryPortraits/{characterName}");
            if (sprite != null)
            {
                _portrait.sprite = sprite;
                _portrait.color = Color.white;
            }
            else
            {
                _portrait.sprite = null;
                _portrait.color = new Color(1, 1, 1, 0.25f);
            }
        }

        IEnumerator TypewriterRoutine(int totalChars)
        {
            _lineText.maxVisibleCharacters = 0;
            const float charsPerSecond = 28f;
            float shown = 0f;
            while (shown < totalChars)
            {
                shown += charsPerSecond * Time.deltaTime;
                _lineText.maxVisibleCharacters = Mathf.Min((int)shown, totalChars);
                yield return null;
            }
            _typewriterRoutine = null;
        }

        IEnumerator Fade(bool on)
        {
            const float duration = 0.22f;
            float start = _canvasGroup.alpha;
            float target = on ? 1f : 0f;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                _canvasGroup.alpha = Mathf.Lerp(start, target, t);
                yield return null;
            }
            _canvasGroup.alpha = target;
            _canvasGroup.blocksRaycasts = on;
            _canvasGroup.interactable = on;
            if (!on) _optionsRoot.gameObject.SetActive(false);
        }
    }
}
