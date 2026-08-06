using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using NueGames.NueDeck.Scripts.Data.Characters;
using CardGame.Audio;

namespace CardGame.UI
{
    /// <summary>
    /// 战前/战后对话面板。运行时动态创建，不需要预制体。
    /// </summary>
    public class EncounterDialoguePanel : MonoBehaviour
    {
        private Image _bg;
        private TextMeshProUGUI _nameText;
        private TextMeshProUGUI _dialogueText;
        private Button _continueButton;
        private TextMeshProUGUI _continueText;
        private System.Action _onComplete;

        public void Init(string enemyName, string dialogue, string buttonText, System.Action onComplete)
        {
            // Create canvas if not exists
            var canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                var scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                gameObject.AddComponent<GraphicRaycaster>();
            }

            // Background overlay
            var bgObj = new GameObject("BG");
            bgObj.transform.SetParent(transform, false);
            _bg = bgObj.AddComponent<Image>();
            _bg.color = new Color(0, 0, 0, 0.85f);
            var bgRt = bgObj.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;

            // Dialogue box
            var boxObj = new GameObject("DialogueBox");
            boxObj.transform.SetParent(transform, false);
            var boxImg = boxObj.AddComponent<Image>();
            boxImg.color = new Color(0.1f, 0.12f, 0.18f, 0.95f);
            var boxRt = boxObj.GetComponent<RectTransform>();
            boxRt.anchorMin = new Vector2(0.15f, 0.2f);
            boxRt.anchorMax = new Vector2(0.85f, 0.8f);
            boxRt.offsetMin = Vector2.zero; boxRt.offsetMax = Vector2.zero;

            // Enemy name
            var nameObj = new GameObject("NameText");
            nameObj.transform.SetParent(boxObj.transform, false);
            _nameText = nameObj.AddComponent<TextMeshProUGUI>();
            _nameText.text = enemyName;
            _nameText.fontSize = 28;
            _nameText.fontStyle = FontStyles.Bold;
            _nameText.color = new Color(0.9f, 0.8f, 0.3f);
            _nameText.alignment = TextAlignmentOptions.Center;
            var nameRt = nameObj.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f, 0.8f); nameRt.anchorMax = new Vector2(1f, 1f);
            nameRt.offsetMin = new Vector2(20, 5); nameRt.offsetMax = new Vector2(-20, -5);
            var libSans = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (libSans) _nameText.font = libSans;

            // Dialogue text
            var dlgObj = new GameObject("DialogueText");
            dlgObj.transform.SetParent(boxObj.transform, false);
            _dialogueText = dlgObj.AddComponent<TextMeshProUGUI>();
            _dialogueText.text = dialogue;
            _dialogueText.fontSize = 22;
            _dialogueText.color = Color.white;
            _dialogueText.alignment = TextAlignmentOptions.Center;
            _dialogueText.richText = true;
            var dlgRt = dlgObj.GetComponent<RectTransform>();
            dlgRt.anchorMin = new Vector2(0.05f, 0.2f); dlgRt.anchorMax = new Vector2(0.95f, 0.75f);
            dlgRt.offsetMin = Vector2.zero; dlgRt.offsetMax = Vector2.zero;
            if (libSans) _dialogueText.font = libSans;

            // Continue button
            var btnObj = new GameObject("ContinueButton");
            btnObj.transform.SetParent(boxObj.transform, false);
            var btnImg = btnObj.AddComponent<Image>();
            btnImg.color = new Color(0.2f, 0.5f, 0.3f, 1f);
            var btnRt = btnObj.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.35f, 0.03f); btnRt.anchorMax = new Vector2(0.65f, 0.18f);
            btnRt.offsetMin = Vector2.zero; btnRt.offsetMax = Vector2.zero;

            var contTxtObj = new GameObject("ButtonText");
            contTxtObj.transform.SetParent(btnObj.transform, false);
            _continueText = contTxtObj.AddComponent<TextMeshProUGUI>();
            _continueText.text = buttonText;
            _continueText.fontSize = 20;
            _continueText.color = Color.white;
            _continueText.alignment = TextAlignmentOptions.Center;
            var contRt = contTxtObj.GetComponent<RectTransform>();
            contRt.anchorMin = Vector2.zero; contRt.anchorMax = Vector2.one;
            contRt.offsetMin = Vector2.zero; contRt.offsetMax = Vector2.zero;
            if (libSans) _continueText.font = libSans;

            _continueButton = btnObj.AddComponent<Button>();
            _onComplete = onComplete;
            _continueButton.onClick.AddListener(() => {
                if (GameAudioManager.Instance != null)
                    GameAudioManager.Instance.PlaySFX(SFXType.UIClick);
                _onComplete?.Invoke();
                Destroy(gameObject);
            });

            gameObject.SetActive(true);
        }
    }
}
