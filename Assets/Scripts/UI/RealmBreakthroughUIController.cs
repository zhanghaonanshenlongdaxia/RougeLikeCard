using TMPro;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using CardGame.Audio;
using NueGames.NueDeck.Scripts.Enums;

namespace CardGame.UI
{
    /// <summary>
    /// 境界突破面板。Prefab 驱动，运行时只刷新数据。
    /// </summary>
    public class RealmBreakthroughUIController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private TextMeshProUGUI _costText;
        [SerializeField] private Button _breakthroughBtn;
        [SerializeField] private Button _closeButton;

        public IArchitecture GetArchitecture() => CardGameArchitecture.Interface;

        private void Awake()
        {
            AutoBindReferences();
            if (_breakthroughBtn) _breakthroughBtn.onClick.AddListener(OnBreakthrough);
            if (_closeButton) _closeButton.onClick.AddListener(() =>
            {
                if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlaySFX(SFXType.UIClick);
                gameObject.SetActive(false);
                var mapMgr = FindObjectOfType<NueGames.NueDeck.Scripts.Managers.MapManager>();
                if (mapMgr != null) mapMgr.SendMessage("BuildMap");
            });
            Refresh();
        }

        private void AutoBindReferences()
        {
            var panel = transform.Find("Panel");
            if (panel == null) return;

            if (_statusText == null) _statusText = panel.Find("StatusText")?.GetComponent<TextMeshProUGUI>();
            if (_costText == null) _costText = panel.Find("CostText")?.GetComponent<TextMeshProUGUI>();
            if (_breakthroughBtn == null) _breakthroughBtn = panel.Find("BreakthroughButton")?.GetComponent<Button>();
            if (_closeButton == null) _closeButton = panel.Find("CloseButton")?.GetComponent<Button>();
        }

        void Refresh()
        {
            var arch = CardGameArchitecture.Interface;
            var realmSystem = arch.GetSystem<IRealmSystem>();

            string currentName = realmSystem.GetCurrentRealmName();
            if (_statusText) _statusText.text = $"当前境界：{currentName}";

            var next = realmSystem.GetNextBreakthrough();
            if (next == null)
            {
                if (_costText) _costText.text = "已达最高境界，无法突破。";
                if (_breakthroughBtn) { _breakthroughBtn.interactable = false; _breakthroughBtn.image.color = new Color(0.3f, 0.3f, 0.3f, 0.5f); }
                return;
            }

            var battleModel = arch.GetModel<IBattleModel>();
            bool canBreak = realmSystem.CanBreakthrough();
            // 丹药名通过ID查找
            string potionName = next.requiredPotionId;
            if (!string.IsNullOrEmpty(next.requiredPotionId))
            {
                var potion = CardGame.ResourceCache.GetPotions().Find(p => p.potionId == next.requiredPotionId);
                if (potion != null) potionName = potion.name;
            }
            if (_costText) _costText.text = $"突破至：{next.realmName}\n\n灵石：{next.goldCost}（持有 {battleModel.CurrentGold.Value}）\n材料：{ItemQualityHelper.GetDisplayName(next.requiredQuality)} × {next.materialCount}\n丹药：{potionName}\n\n奖励：+{next.hpBonus} HP / +{next.shenShiBonus} 神识上限\n\n{next.description}";

            if (_breakthroughBtn)
            {
                _breakthroughBtn.interactable = canBreak;
                _breakthroughBtn.image.color = canBreak
                    ? new Color(0.2f, 0.5f, 0.3f, 1f)
                    : new Color(0.3f, 0.3f, 0.3f, 0.5f);
            }
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
    }
}
