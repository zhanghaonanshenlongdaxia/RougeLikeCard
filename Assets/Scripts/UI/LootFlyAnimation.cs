using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace CardGame.UI
{
    /// <summary>
    /// 拾取飞行特效 — 贝塞尔曲线飞行 + 对象池，使用DOTween。
    /// 点击物品后生成小图标，放大→沿弧线飞向目标→缩小消失。
    /// </summary>
    public class LootFlyAnimation : MonoBehaviour
    {
        public static LootFlyAnimation Instance { get; private set; }

        private Canvas _canvas;
        private readonly Queue<RectTransform> _pool = new Queue<RectTransform>();
        private const int PoolSize = 30;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildCanvas();
        }

        private void BuildCanvas()
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 500;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            gameObject.AddComponent<GraphicRaycaster>();

            for (int i = 0; i < PoolSize; i++)
            {
                var item = CreateItem();
                item.gameObject.SetActive(false);
                _pool.Enqueue(item);
            }
        }

        private RectTransform CreateItem()
        {
            var go = new GameObject("LootFlyItem");
            go.transform.SetParent(_canvas.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(64, 64);
            var img = go.AddComponent<Image>();
            img.raycastTarget = false;
            img.preserveAspect = true;
            return rt;
        }

        /// <summary>
        /// 生成一个飞行图标：从 fromPos 出发，沿贝塞尔弧线飞向 targetPos。
        /// </summary>
        public void SpawnAndFly(Sprite icon, Vector2 fromPos, Vector2 targetPos)
        {
            if (icon == null) return;

            var item = _pool.Count > 0 ? _pool.Dequeue() : CreateItem();
            item.gameObject.SetActive(true);
            var img = item.GetComponent<Image>();
            img.sprite = icon;
            img.SetNativeSize();
            item.position = fromPos;
            item.localScale = Vector3.zero;

            // 贝塞尔控制点
            Vector2 mid = (fromPos + targetPos) * 0.5f;
            float arcHeight = Mathf.Min(250f, Vector2.Distance(fromPos, targetPos) * 0.35f);
            Vector2 control = new Vector2(mid.x, mid.y + arcHeight);

            // Phase 1: 放大 (0 → 1.3)，Ease.OutBack有弹性
            item.DOScale(1.3f, 0.2f).SetEase(Ease.OutBack).OnComplete(() =>
            {
                // Phase 2: 贝塞尔飞行 + 缩小
                float flyDuration = 0.6f;
                Sequence flySeq = DOTween.Sequence();

                flySeq.Join(DOTween.To(
                    () => 0f,
                    val =>
                    {
                        // 二次贝塞尔曲线
                        float u = 1f - val;
                        Vector2 pos = u * u * fromPos + 2f * u * val * control + val * val * targetPos;
                        item.position = pos;
                    },
                    1f,
                    flyDuration
                ).SetEase(Ease.InQuad));

                flySeq.Join(item.DOScale(0.3f, flyDuration).SetEase(Ease.InQuad));

                flySeq.OnComplete(() =>
                {
                    item.localScale = Vector3.zero;
                    item.gameObject.SetActive(false);
                    _pool.Enqueue(item);
                });
            });
        }
    }
}
