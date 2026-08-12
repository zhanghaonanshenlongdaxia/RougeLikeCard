using TMPro;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using Alchemy.Inspector;
using CardGame.Audio;

namespace CardGame.UI
{
    /// <summary>
    /// 宝箱面板。Prefab 驱动，运行时只刷新数据和处理确认逻辑。
    /// </summary>
    public class TreasureUIController : MonoBehaviour, IController
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI _descriptionText;
        [SerializeField] private Transform _rewardSlotsRoot;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private TextMeshProUGUI _confirmButtonText;

        private bool _rewardClaimed;

        public IArchitecture GetArchitecture() => CardGameArchitecture.Interface;

        private void Awake()
        {
            AutoBindReferences();
            if (_confirmButton != null)
            {
                _confirmButton.onClick.RemoveAllListeners();
                _confirmButton.onClick.AddListener(() => {
                    if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick);
                    OnConfirm();
                });
            }
        }

        private void AutoBindReferences()
        {
            var panel = transform.Find("Panel");
            if (panel == null) return;

            if (_descriptionText == null) _descriptionText = panel.Find("DescriptionText")?.GetComponent<TextMeshProUGUI>();
            if (_rewardSlotsRoot == null) _rewardSlotsRoot = panel.Find("RewardArea");
            if (_confirmButton == null) _confirmButton = panel.Find("ConfirmButton")?.GetComponent<Button>();
            if (_confirmButtonText == null && _confirmButton != null)
                _confirmButtonText = _confirmButton.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
        }

        public void ShowTreasure(string desc)
        {
            _rewardClaimed = false;
            if (_descriptionText) _descriptionText.text = desc;
            if (_rewardSlotsRoot != null)
            {
                for (int i = _rewardSlotsRoot.childCount - 1; i >= 0; i--)
                    Destroy(_rewardSlotsRoot.GetChild(i).gameObject);
            }
            if (_confirmButtonText) _confirmButtonText.text = "确认拾取";
            if (_confirmButton) _confirmButton.interactable = true;
            gameObject.SetActive(true);
        }

        public void OnConfirm()
        {
            if (!_rewardClaimed)
            {
                GrantTreasureReward();
                _rewardClaimed = true;
                if (_confirmButtonText) _confirmButtonText.text = "离开";
                return;
            }

            gameObject.SetActive(false);
            var mapMgr = FindObjectOfType<NueGames.NueDeck.Scripts.Managers.MapManager>();
            if (mapMgr != null) mapMgr.SendMessage("BuildMap");
        }

        private void GrantTreasureReward()
        {
            var gm = NueGames.NueDeck.Scripts.Managers.GameManager.Instance;
            if (gm == null) return;

            var rewards = new System.Collections.Generic.List<string>();

            int gold = Random.Range(30, 80);
            var arch = CardGameArchitecture.Interface;
            arch.GetModel<IBattleModel>().CurrentGold.Value += gold;
            gm.PersistentGameplayData.CurrentGold = arch.GetModel<IBattleModel>().CurrentGold.Value;
            rewards.Add($"+{gold}灵石");

            GrantRandomMaterial(rewards);

            if (Random.value < 0.5f)
            {
                var potion = CardGame.ResourceCache.GetRandomPotion();
                if (potion != null)
                {
                    arch.GetSystem<IPotionSystem>().ObtainPotion(potion);
                    rewards.Add($"药水: {potion.name}");
                }
            }

            if (_rewardSlotsRoot != null)
            {
                var rewardObj = new GameObject("RewardText");
                rewardObj.transform.SetParent(_rewardSlotsRoot, false);
                var rt = rewardObj.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(10, 5); rt.offsetMax = new Vector2(-10, -5);
                var tmp = rewardObj.AddComponent<TextMeshProUGUI>();
                tmp.text = string.Join("\n", rewards);
                tmp.fontSize = 20; tmp.color = new Color(1f, 0.85f, 0f);
                tmp.alignment = TextAlignmentOptions.Center;
            }

            FloatingTip.ShowSuccess("宝箱开启! " + string.Join(" ", rewards));
        }

        private void GrantRandomMaterial(System.Collections.Generic.List<string> rewards)
        {
            var mat = CardGame.ResourceCache.GetRandomMaterial();
            if (mat != null)
            {
                this.GetSystem<IInventorySystem>().AddItem(mat, 1);
                rewards.Add($"灵材: {mat.name}");
            }
        }
    }
}
