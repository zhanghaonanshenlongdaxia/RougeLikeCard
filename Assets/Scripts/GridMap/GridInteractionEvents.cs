using System;

namespace CardGame
{
    /// <summary>
    /// 格子交互事件（静态事件总线）
    /// </summary>
    public static class GridInteractionEvents
    {
        /// <summary>玩家进入格子（移动路径的每一步都触发）</summary>
        public static Action<GridCell> OnCellEntered;
        /// <summary>格子被请求交互（敌人/撤离自动触发，或玩家点击本格/按E）</summary>
        public static Action<GridCell> OnCellInteract;
        /// <summary>交互完成（message格式 "类型:信息"，用于UI刷新）</summary>
        public static Action<GridCell, string> OnInteractionComplete;

        public static void TriggerCellEntered(GridCell cell) => OnCellEntered?.Invoke(cell);
        public static void TriggerCellInteract(GridCell cell) => OnCellInteract?.Invoke(cell);
        public static void TriggerInteractionComplete(GridCell cell, string message) => OnInteractionComplete?.Invoke(cell, message);
    }
}
