using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Ustas.RimAI.Communication.Memory.UI
{
    public class VirtualListView<T> where T : class
    {
        private List<T> items = new List<T>();
        private Func<T, float> getItemHeight;
        private Action<Rect, T, int> drawItem;
        private Vector2 scrollPosition;

        private readonly List<float> itemOffsets = new List<float>();
        private readonly List<float> itemHeights = new List<float>();
        private float totalHeight = 0f;

        public float ItemSpacing { get; set; } = 5f;
        public Vector2 ScrollPosition
        {
            get => scrollPosition;
            set => scrollPosition = value;
        }

        public string EmptyLabel { get; set; } = "No items to display";

        public VirtualListView(Func<T, float> getItemHeight, Action<Rect, T, int> drawItem)
        {
            this.getItemHeight = getItemHeight ?? throw new ArgumentNullException(nameof(getItemHeight));
            this.drawItem = drawItem ?? throw new ArgumentNullException(nameof(drawItem));
        }

        public void SetItems(List<T> newItems)
        {
            items = newItems ?? new List<T>();
            RebuildLayout();
        }

        private void RebuildLayout()
        {
            itemOffsets.Clear();
            itemHeights.Clear();
            totalHeight = 0f;

            for (int i = 0; i < items.Count; i++)
            {
                itemOffsets.Add(totalHeight);
                float height = getItemHeight(items[i]);
                itemHeights.Add(height);
                totalHeight += height + ItemSpacing;
            }
        }

        public void Draw(Rect rect)
        {
            if (items.Count == 0)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = new Color(0.6f, 0.6f, 0.6f);
                Widgets.Label(rect, EmptyLabel);
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            Rect viewRect = new Rect(0f, 0f, rect.width - 16f, totalHeight);

            Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);

            float visibleTop = scrollPosition.y;
            float visibleBottom = visibleTop + rect.height;

            GetVisibleIndexRange(visibleTop, visibleBottom, out int startIndex, out int endIndex);

            for (int i = startIndex; i <= endIndex; i++)
            {
                float itemTop = itemOffsets[i];
                float itemHeight = itemHeights[i];
                Rect itemRect = new Rect(0f, itemTop, viewRect.width, itemHeight);
                drawItem(itemRect, items[i], i);
            }

            Widgets.EndScrollView();
        }

        public List<int> FindItemsInRange(float minY, float maxY)
        {
            var indices = new List<int>();

            if (items.Count == 0)
                return indices;

            int start = FindFirstIndexAtOrBefore(minY);
            if (start < 0) start = 0;

            for (int i = start; i < items.Count; i++)
            {
                float itemTop = itemOffsets[i];
                float itemBottom = itemTop + itemHeights[i];

                if (itemTop > maxY)
                    break;

                if (itemBottom >= minY)
                {
                    indices.Add(i);
                }
            }

            return indices;
        }

        public string GetRenderStats(float viewportHeight)
        {
            if (items.Count == 0)
                return "0/0 rendered";

            float visibleTop = scrollPosition.y;
            float visibleBottom = visibleTop + viewportHeight;

            GetVisibleIndexRange(visibleTop, visibleBottom, out int startIndex, out int endIndex);

            int visibleCount = 0;
            if (endIndex >= startIndex)
                visibleCount = endIndex - startIndex + 1;

            return $"{visibleCount}/{items.Count} rendered";
        }

        private void GetVisibleIndexRange(float visibleTop, float visibleBottom, out int startIndex, out int endIndex)
        {
            startIndex = FindFirstIndexAtOrBefore(visibleTop);
            if (startIndex < 0) startIndex = 0;

            endIndex = FindLastIndexAtOrAfter(visibleBottom);
            if (endIndex < 0) endIndex = items.Count - 1;

            if (startIndex < 0) startIndex = 0;
            if (endIndex >= items.Count) endIndex = items.Count - 1;
            if (endIndex < startIndex) endIndex = startIndex;
        }

        private int FindFirstIndexAtOrBefore(float y)
        {
            int low = 0;
            int high = items.Count - 1;
            int result = items.Count; // default: not found

            while (low <= high)
            {
                int mid = (low + high) / 2;
                float top = itemOffsets[mid];
                float bottom = top + itemHeights[mid];

                if (bottom >= y)
                {
                    result = mid;
                    high = mid - 1;
                }
                else
                {
                    low = mid + 1;
                }
            }

            if (result == items.Count)
                return items.Count - 1;

            return result;
        }

        private int FindLastIndexAtOrAfter(float y)
        {
            int low = 0;
            int high = items.Count - 1;
            int result = -1;

            while (low <= high)
            {
                int mid = (low + high) / 2;
                float top = itemOffsets[mid];

                if (top <= y)
                {
                    result = mid;
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }

            if (result < 0)
                return 0;

            return result;
        }
    }
}
