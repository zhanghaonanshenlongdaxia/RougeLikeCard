using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

namespace CardGame.Story
{
    /// <summary>
    /// 气泡对话控制器 — Yarn 3.x DialoguePresenterBase
    /// 对话以气泡形式显示：说话人在左侧→气泡在左（尾巴朝右下），
    /// 说话人在右侧→气泡在右（尾巴朝左下）。打字机效果+表情联动。
    /// 布局：底部左右两个"说话位"（半身立绘+表情叠加），气泡悬于头顶。
    /// </summary>
    public class BubbleDialoguePresenter : DialoguePresenterBase
    {
        CanvasGroup _canvasGroup;

        // 左右说话位（立绘+表情）
        RectTransform _leftSlot;
        RectTransform _rightSlot;
        Image _leftBody;
        Image _rightBody;
        Image _leftFace;
        Image _rightFace;

        // 气泡
        RectTransform _bubbleRt;
        Image _bubbleBg;
        TextMeshProUGUI _bubbleText;
        TextMeshProUGUI _nameText;

        // 选项
        RectTransform _optionsRoot;
        readonly List<Button> _optionButtons = new List<Button>();

        YarnTaskCompletionSource _lineDone;
        YarnTaskCompletionSource<int> _optionChosen;
        bool _hurryUp;
        Coroutine _typewriterRoutine;

        // 当前说话人是否在右侧
        bool _speakerOnRight;

        public static BubbleDialoguePresenter Create(Transform parent = null)
        {
            var go = new GameObject("BubbleDialogueUI");
            if (parent != null) go.transform.SetParent(parent, false);
            var presenter = go.AddComponent<BubbleDialoguePresenter>();
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

            // 全屏点击推进
            var catcherGo = new GameObject("ClickCatcher", typeof(Image), typeof(Button));
            catcherGo.transform.SetParent(transform, false);
            var cRt = catcherGo.transform as RectTransform;
            cRt.anchorMin = Vector2.zero;
            cRt.anchorMax = Vector2.one;
            cRt.offsetMin = cRt.offsetMax = Vector2.zero;
            var cImg = catcherGo.GetComponent<Image>();
            cImg.color = Color.clear;
            var cBtn = catcherGo.GetComponent<Button>();
            cBtn.transition = Selectable.Transition.None;
            cBtn.onClick.AddListener(OnAdvanceClicked);

            // ===== 左说话位（立绘+表情）=====
            _leftSlot = CreateSpeakerSlot("LeftSlot", false, out _leftBody, out _leftFace);

            // ===== 右说话位 =====
            _rightSlot = CreateSpeakerSlot("RightSlot", true, out _rightBody, out _rightFace);

            // ===== 气泡 =====
            var bubbleGo = new GameObject("Bubble", typeof(RectTransform), typeof(Image));
            bubbleGo.transform.SetParent(transform, false);
            _bubbleRt = bubbleGo.transform as RectTransform;
            _bubbleRt.anchorMin = _bubbleRt.anchorMax = new Vector2(0.5f, 0.5f);
            _bubbleRt.sizeDelta = new Vector2(760, 200);
            _bubbleRt.anchoredPosition = new Vector2(0, 180);
            _bubbleBg = bubbleGo.GetComponent<Image>();
            _bubbleBg.sprite = CreateBubbleSprite();
            _bubbleBg.type = Image.Type.Sliced;
            _bubbleBg.color = new Color(0.98f, 0.97f, 0.93f, 0.98f);
            _bubbleBg.raycastTarget = false;

            // 角色名（气泡顶部）
            var nameGo = new GameObject("NameText", typeof(RectTransform), typeof(TextMeshProUGUI));
            nameGo.transform.SetParent(_bubbleRt, false);
            var nRt = nameGo.transform as RectTransform;
            nRt.anchorMin = new Vector2(0, 1);
            nRt.anchorMax = new Vector2(1, 1);
            nRt.pivot = new Vector2(0.5f, 1);
            nRt.anchoredPosition = new Vector2(0, -8);
            nRt.sizeDelta = new Vector2(-40, 40);
            _nameText = nameGo.GetComponent<TextMeshProUGUI>();
            _nameText.fontSize = 28;
            _nameText.fontStyle = FontStyles.Bold;
            _nameText.color = new Color(0.45f, 0.20f, 0.10f);
            _nameText.alignment = TextAlignmentOptions.Center;
            _nameText.raycastTarget = false;
            if (font != null) _nameText.font = font;

            // 气泡正文
            var lineGo = new GameObject("LineText", typeof(RectTransform), typeof(TextMeshProUGUI));
            lineGo.transform.SetParent(_bubbleRt, false);
            var lRt = lineGo.transform as RectTransform;
            lRt.anchorMin = new Vector2(0, 0);
            lRt.anchorMax = new Vector2(1, 1);
            lRt.offsetMin = new Vector2(28, 14);
            lRt.offsetMax = new Vector2(-28, -52);
            _bubbleText = lineGo.GetComponent<TextMeshProUGUI>();
            _bubbleText.fontSize = 30;
            _bubbleText.color = new Color(0.20f, 0.18f, 0.16f);
            _bubbleText.alignment = TextAlignmentOptions.TopLeft;
            _bubbleText.raycastTarget = false;
            _bubbleText.enableWordWrapping = true;
            if (font != null) _bubbleText.font = font;

            // 气泡尾巴（小三角，随左右翻转）
            var tailGo = new GameObject("Tail", typeof(RectTransform), typeof(Image));
            tailGo.transform.SetParent(_bubbleRt, false);
            var tRt = tailGo.transform as RectTransform;
            tRt.anchorMin = tRt.anchorMax = new Vector2(0.5f, 0);
            tRt.pivot = new Vector2(0.5f, 1);
            tRt.anchoredPosition = new Vector2(0, -2);
            tRt.sizeDelta = new Vector2(36, 26);
            tRt.localEulerAngles = new Vector3(0, 0, 180);
            var tailImg = tailGo.GetComponent<Image>();
            tailImg.sprite = CreateBubbleSprite();
            tailImg.type = Image.Type.Sliced;
            tailImg.color = _bubbleBg.color;
            tailImg.raycastTarget = false;
            tailGo.name = "Tail";

            // ===== 选项（底部中间）=====
            var optGo = new GameObject("OptionsRoot", typeof(RectTransform), typeof(VerticalLayoutGroup));
            optGo.transform.SetParent(transform, false);
            _optionsRoot = optGo.transform as RectTransform;
            _optionsRoot.anchorMin = new Vector2(0.3f, 0.06f);
            _optionsRoot.anchorMax = new Vector2(0.7f, 0.42f);
            _optionsRoot.offsetMin = _optionsRoot.offsetMax = Vector2.zero;
            var oLayout = optGo.GetComponent<VerticalLayoutGroup>();
            oLayout.spacing = 12;
            oLayout.childAlignment = TextAnchor.MiddleCenter;
            oLayout.childForceExpandHeight = false;
            oLayout.childControlHeight = false;
            _optionsRoot.gameObject.SetActive(false);
        }

        /// <summary>创建说话位：半身立绘底图+表情叠加层</summary>
        RectTransform CreateSpeakerSlot(string name, bool onRight, out Image body, out Image face)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var rt = go.transform as RectTransform;
            rt.anchorMin = rt.anchorMax = onRight ? new Vector2(1, 0) : new Vector2(0, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.anchoredPosition = new Vector2(onRight ? -260 : 260, -40);
            rt.sizeDelta = new Vector2(480, 640);

            // 身体立绘（无脸）
            var bodyGo = new GameObject("Body", typeof(RectTransform), typeof(Image));
            bodyGo.transform.SetParent(rt, false);
            var bRt = bodyGo.transform as RectTransform;
            bRt.anchorMin = bRt.anchorMax = Vector2.one;
            bRt.offsetMin = bRt.offsetMax = Vector2.zero;
            body = bodyGo.GetComponent<Image>();
            body.preserveAspect = true;
            body.raycastTarget = false;
            body.color = new Color(1, 1, 1, 0);

            // 表情叠加（头部位置）
            var faceGo = new GameObject("Face", typeof(RectTransform), typeof(Image));
            faceGo.transform.SetParent(rt, false);
            var fRt = faceGo.transform as RectTransform;
            fRt.anchorMin = fRt.anchorMax = new Vector2(0.5f, 1);
            fRt.pivot = new Vector2(0.5f, 1);
            fRt.anchoredPosition = new Vector2(0, -90);
            fRt.sizeDelta = new Vector2(150, 150);
            face = faceGo.GetComponent<Image>();
            face.preserveAspect = true;
            face.raycastTarget = false;
            face.color = new Color(1, 1, 1, 0);

            return rt;
        }

        // ===================== 玩家交互 =====================

        void OnAdvanceClicked()
        {
            if (_hurryUp && _typewriterRoutine != null)
            {
                StopCoroutine(_typewriterRoutine);
                _typewriterRoutine = null;
                _bubbleText.maxVisibleCharacters = int.MaxValue;
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

            var characterName = line.CharacterName;

            if (!string.IsNullOrEmpty(characterName))
            {
                // 说话人切换：主角(陈洛)在左，其他人在右
                bool isPlayerChar = characterName.Contains("陈洛") || characterName.Contains("洛");
                _speakerOnRight = !isPlayerChar;

                _nameText.text = characterName;
                _nameText.gameObject.SetActive(true);
                ShowSpeaker(characterName, isPlayerChar);
            }
            else
            {
                _nameText.gameObject.SetActive(false);
                // 旁白：气泡居中灰底
            }

            // 气泡定位：靠说话人一侧
            float bubbleX = _speakerOnRight ? 220 : -220;
            _bubbleRt.anchoredPosition = new Vector2(bubbleX, 180);

            // 尾巴位置跟随
            var tail = _bubbleRt.Find("Tail");
            if (tail != null)
            {
                var tailRt = tail as RectTransform;
                tailRt.anchorMin = tailRt.anchorMax = _speakerOnRight
                    ? new Vector2(0.25f, 0)
                    : new Vector2(0.75f, 0);
                tailRt.anchoredPosition = new Vector2(0, -2);
            }

            var text = line.TextWithoutCharacterName.Text;
            _bubbleText.maxVisibleCharacters = int.MaxValue;
            _bubbleText.text = text;

            if (_typewriterRoutine != null) StopCoroutine(_typewriterRoutine);
            _typewriterRoutine = StartCoroutine(TypewriterRoutine(text.Length));

            _lineDone = new YarnTaskCompletionSource();

            while (!_lineDone.Task.IsCompleted() && !token.NextContentToken.IsCancellationRequested)
                await YarnTask.Yield();

            if (_typewriterRoutine != null) { StopCoroutine(_typewriterRoutine); _typewriterRoutine = null; }
            _bubbleText.maxVisibleCharacters = int.MaxValue;
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

        // ===================== 立绘与表情 =====================

        /// <summary>角色名→立绘资源名映射（资源在Resources/StoryPortraits/）</summary>
        static string MapCharacterToAsset(string characterName)
        {
            if (string.IsNullOrEmpty(characterName)) return null;
            if (characterName.Contains("陈洛") || characterName.Contains("洛")) return "chenluo";
            // 后续角色在此扩展：周半壶→zhoubanhu 等
            return null;
        }

        /// <summary>显示说话人立绘（有新立绘才切，非说话侧淡出）</summary>
        void ShowSpeaker(string characterName, bool isPlayerSide)
        {
            var assetName = MapCharacterToAsset(characterName);
            var body = isPlayerSide ? _leftBody : _rightBody;
            var face = isPlayerSide ? _leftFace : _rightFace;
            var otherBody = isPlayerSide ? _rightBody : _leftBody;
            var otherFace = isPlayerSide ? _rightFace : _leftFace;

            if (assetName != null)
            {
                // 加载身体立绘（无脸）
                var bodySprite = Resources.Load<Sprite>($"StoryPortraits/{assetName}_body");
                if (bodySprite != null)
                {
                    body.sprite = bodySprite;
                    body.color = Color.white;
                }

                // 默认表情
                var faceSprite = Resources.Load<Sprite>($"StoryPortraits/{assetName}_face_normal");
                if (faceSprite != null)
                {
                    face.sprite = faceSprite;
                    face.color = Color.white;
                }
            }

            // 另一侧淡出
            otherBody.color = new Color(1, 1, 1, 0.35f);
            otherFace.color = new Color(1, 1, 1, 0.35f);
        }

        /// <summary>切换表情（外部/指令调用）：emotion如 normal/happy/sad/angry/surprised/shy</summary>
        public void SetEmotion(string characterName, string emotion)
        {
            var assetName = MapCharacterToAsset(characterName);
            if (assetName == null) return;
            var face = characterName.Contains("洛") ? _leftFace : _rightFace;
            var sprite = Resources.Load<Sprite>($"StoryPortraits/{assetName}_face_{emotion}");
            if (sprite != null)
            {
                face.sprite = sprite;
                face.color = Color.white;
            }
            else Debug.LogWarning($"[Bubble] 表情未找到: {assetName}_face_{emotion}");
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
            img.color = new Color(0.10f, 0.13f, 0.18f, 0.95f);
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
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (font != null) tmp.font = font;

            return btn;
        }

        /// <summary>程序化生成9宫格圆角气泡背景</summary>
        Sprite CreateBubbleSprite()
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var radius = 14;
            var center = size / 2;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool inside = true;
                    // 四角圆角检测
                    int dx = Mathf.Min(x, size - 1 - x);
                    int dy = Mathf.Min(y, size - 1 - y);
                    if (dx < radius && dy < radius)
                    {
                        float d = Mathf.Sqrt((dx - radius) * (dx - radius) + (dy - radius) * (dy - radius));
                        inside = d <= radius;
                    }
                    tex.SetPixel(x, y, inside ? Color.white : Color.clear);
                }
            }
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));
            return sprite;
        }

        IEnumerator TypewriterRoutine(int totalChars)
        {
            _bubbleText.maxVisibleCharacters = 0;
            const float charsPerSecond = 28f;
            float shown = 0f;
            while (shown < totalChars)
            {
                shown += charsPerSecond * Time.deltaTime;
                _bubbleText.maxVisibleCharacters = Mathf.Min((int)shown, totalChars);
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
