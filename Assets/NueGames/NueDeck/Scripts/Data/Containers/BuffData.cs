using NueGames.NueDeck.Scripts.Enums;
using UnityEngine;

namespace NueGames.NueDeck.Scripts.Data.Containers
{
    /// <summary>
    /// Buff特效类型：只有需要独特代码逻辑的buff才需要设非None值
    /// </summary>
    public enum BuffSpecialEffect
    {
        None,       // 纯数值修饰（多段倍率），完全数据驱动
        Poison,     // 回合结束时按层数扣血
        Stun,       // 跳过行动
        Thorn       // 受击时反弹伤害
    }

    /// <summary>
    /// Buff数据SO — 一个buff一个asset，在BuffEditorWindow中创建/编辑。
    /// 加新buff只需：① StatusType加枚举值 ② 创建BuffData SO。
    /// 只有独特机制的buff才需要设SpecialEffect并在BuffEffectHandler加case。
    /// </summary>
    [CreateAssetMenu(fileName = "Buff", menuName = "NueDeck/Containers/Buff Data", order = 10)]
    public class BuffData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private StatusType statusType;
        [SerializeField] private string displayName;
        [SerializeField][TextArea] private string description;
        [SerializeField] private Sprite icon;

        [Header("Behavior")]
        [Tooltip("回合结束自动递减1层")]
        [SerializeField] private bool decreaseOverTurn;
        [Tooltip("永久状态，战斗中不可清除（如Thorn/Strength）")]
        [SerializeField] private bool isPermanent;
        [Tooltip("允许负值叠加（如Strength可以为负）")]
        [SerializeField] private bool canNegativeStack;
        [Tooltip("下回合开始时清除（如Block）")]
        [SerializeField] private bool clearAtNextTurn;

        [Header("Multipliers (1 = no effect)")]
        [Tooltip("受到的伤害倍率，如易伤=1.5")]
        [SerializeField] private float damageTakenMult = 1f;
        [Tooltip("造成的伤害倍率，如虚弱=0.75")]
        [SerializeField] private float damageDealtMult = 1f;
        [Tooltip("获得的格挡倍率，如脆弱=0.75")]
        [SerializeField] private float blockMult = 1f;

        [Header("Special Effect")]
        [Tooltip("只有Poison/Stun/Thorn等独特机制需要设置，其余选None")]
        [SerializeField] private BuffSpecialEffect specialEffect = BuffSpecialEffect.None;

        #region Properties
        public StatusType StatusType => statusType;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public bool DecreaseOverTurn => decreaseOverTurn;
        public bool IsPermanent => isPermanent;
        public bool CanNegativeStack => canNegativeStack;
        public bool ClearAtNextTurn => clearAtNextTurn;
        public float DamageTakenMult => damageTakenMult;
        public float DamageDealtMult => damageDealtMult;
        public float BlockMult => blockMult;
        public BuffSpecialEffect SpecialEffect => specialEffect;
        #endregion

        #region Editor Methods
#if UNITY_EDITOR
        public void EditStatusType(StatusType type) => statusType = type;
        public void EditDisplayName(string name) => displayName = name;
        public void EditDescription(string desc) => description = desc;
        public void EditIcon(Sprite sprite) => icon = sprite;
        public void EditDecreaseOverTurn(bool val) => decreaseOverTurn = val;
        public void EditIsPermanent(bool val) => isPermanent = val;
        public void EditCanNegativeStack(bool val) => canNegativeStack = val;
        public void EditClearAtNextTurn(bool val) => clearAtNextTurn = val;
        public void EditDamageTakenMult(float val) => damageTakenMult = val;
        public void EditDamageDealtMult(float val) => damageDealtMult = val;
        public void EditBlockMult(float val) => blockMult = val;
        public void EditSpecialEffect(BuffSpecialEffect effect) => specialEffect = effect;
#endif
        #endregion
    }
}
