using TMPro;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using Alchemy.Inspector;

namespace CardGame.UI
{
    public class EventUIController : MonoBehaviour, IController
    {
        [SerializeField] private Image eventImage;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Transform choicesRoot;
        [SerializeField] private GameObject choiceButtonPrefab;

        private EventData _currentEvent;

        public IArchitecture GetArchitecture() => CardGameArchitecture.Interface;

        private void Awake()
        {
            // 运行时自动查找引用（因为 Prefab 的 SerializeField 未赋值）
            if (descriptionText == null)
            {
                var tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var tmp in tmps)
                {
                    if (tmp.gameObject.name == "Description") { descriptionText = tmp; break; }
                }
            }
            if (eventImage == null)
            {
                var imgs = GetComponentsInChildren<Image>(true);
                foreach (var img in imgs)
                {
                    if (img.gameObject.name == "EventImage") { eventImage = img; break; }
                }
            }
            if (choicesRoot == null)
            {
                // 查找名为 Choices 的子物体
                var choices = transform.Find("Panel/Choices");
                if (choices == null)
                {
                    // 递归查找
                    foreach (var t in GetComponentsInChildren<Transform>(true))
                    {
                        if (t.name == "Choices") { choices = t; break; }
                    }
                }
                if (choices != null) choicesRoot = choices;
            }
        }

        public void ShowEvent(EventData eventData)
        {
            _currentEvent = eventData;
            if (descriptionText) descriptionText.text = eventData.description;
            if (eventImage && eventData.eventImage) eventImage.sprite = eventData.eventImage;

            if (choicesRoot == null) return;
            for (int i = choicesRoot.childCount - 1; i >= 0; i--)
                Destroy(choicesRoot.GetChild(i).gameObject);

            for (int i = 0; i < eventData.choices.Count; i++)
            {
                var choice = eventData.choices[i];
                var go = choiceButtonPrefab ? Instantiate(choiceButtonPrefab, choicesRoot) : CreateChoiceButton(choicesRoot);
                var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp) tmp.text = choice.choiceText;

                var btn = go.GetComponentInChildren<Button>();
                if (btn == null) btn = go.AddComponent<Button>();
                var index = i;
                btn.onClick.AddListener(() => OnChoiceSelected(index));
            }

            gameObject.SetActive(true);
        }

        private GameObject CreateChoiceButton(Transform parent)
        {
            var go = new GameObject("Choice");
            go.transform.SetParent(parent);
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 55);
            go.AddComponent<Image>().color = new Color(0.1f, 0.2f, 0.35f, 1);
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(go.transform);
            var rt = textObj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(15, 5); rt.offsetMax = new Vector2(-15, -5);
            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.fontSize = 20;
            tmp.color = Color.white;
            var libSans = UnityEngine.Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (libSans) tmp.font = libSans;
            return go;
        }

        private void OnChoiceSelected(int index)
        {
            if (_currentEvent == null || index >= _currentEvent.choices.Count) return;
            this.GetSystem<IEventSystem>().ExecuteChoice(_currentEvent, index);
            gameObject.SetActive(false);
            NotifyMapRefresh();
        }

        private void NotifyMapRefresh()
        {
            var mapMgr = FindObjectOfType<NueGames.NueDeck.Scripts.Managers.MapManager>();
            if (mapMgr != null) mapMgr.SendMessage("BuildMap");
        }
    }
}
