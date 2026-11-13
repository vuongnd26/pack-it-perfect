using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PackItPerfect.UI
{
    /// <summary>Small runtime layout adapter for safe-area scroll cards, adaptive grids and wrapping chip rows.</summary>
    public sealed class ResponsiveUILayout : MonoBehaviour
    {
        enum LayoutKind { Scroll, Grid, Flow }
        LayoutKind kind;
        RectTransform rect;
        RectTransform content;
        RectTransform safe;
        ScrollRect scroll;
        GridLayoutGroup grid;
        float maximumWidth;
        float heightShare;
        float margin;
        float bottomMargin;
        bool bottom;
        float spacing;
        float rowHeight;
        float padding;
        bool measureText;
        Vector2 cellSize;
        int maximumColumns;
        readonly Vector3[] corners = new Vector3[4];
        GameObject lastSelection;
        float lastWidth = -1f;
        float lastHeight = -1f;
        int flowSignature;
        bool hasFlowLayout;
        // Label of each flow child, looked up once per child instead of on every frame.
        readonly Dictionary<Transform, TextMeshProUGUI> flowLabels = new Dictionary<Transform, TextMeshProUGUI>();
        LayoutElement flowElement;
        bool flowElementResolved;

        TextMeshProUGUI LabelOf(Transform child)
        {
            if (!flowLabels.TryGetValue(child, out var label))
            {
                if (flowLabels.Count > 64) flowLabels.Clear(); // destroyed chips: start over rather than grow
                label = child.GetComponentInChildren<TextMeshProUGUI>(true);
                flowLabels[child] = label;
            }
            return label;
        }

        /// <summary>Fits a scroll viewport inside its safe parent; content remains full size and scrolls rather than shrinking.</summary>
        public void ConfigureScroll(RectTransform safeArea, RectTransform scrollContent, ScrollRect scrollView,
            float width, float maxHeightShare, float sideMargin, bool alignBottom, float bottomInset)
        {
            kind = LayoutKind.Scroll;
            rect = (RectTransform)transform;
            safe = safeArea;
            content = scrollContent;
            scroll = scrollView;
            maximumWidth = width;
            heightShare = maxHeightShare;
            margin = sideMargin;
            bottom = alignBottom;
            bottomMargin = bottomInset;
        }

        /// <summary>Fits as many cells as the available width permits, up to the configured column count.</summary>
        public void ConfigureGrid(GridLayoutGroup layout, Vector2 minimumCellSize, int maxColumns)
        {
            kind = LayoutKind.Grid;
            rect = (RectTransform)transform;
            grid = layout;
            cellSize = minimumCellSize;
            maximumColumns = Mathf.Max(1, maxColumns);
        }

        /// <summary>Wraps children by their label width without reducing the label's font size.</summary>
        public void ConfigureFlow(float gap, float minimumHeight, float horizontalPadding, bool sizeToLabel = true)
        {
            kind = LayoutKind.Flow;
            rect = (RectTransform)transform;
            spacing = gap;
            rowHeight = minimumHeight;
            padding = horizontalPadding;
            measureText = sizeToLabel;
        }

        void OnEnable()
        {
            lastSelection = null;
            lastWidth = -1f;
            lastHeight = -1f;
            hasFlowLayout = false;
        }

        void LateUpdate()
        {
            if (rect == null) return;
            if (kind == LayoutKind.Scroll) FitScroll();
            else if (kind == LayoutKind.Grid) FitGrid();
            else FitFlow();
        }

        void FitGrid()
        {
            float width = Mathf.Max(1f, rect.rect.width - grid.padding.horizontal);
            int columns = Mathf.Clamp(Mathf.FloorToInt((width + grid.spacing.x) / (cellSize.x + grid.spacing.x)), 1, maximumColumns);
            float size = Mathf.Min(cellSize.x, width);
            if (grid.constraintCount != columns || grid.cellSize.x != size)
            {
                grid.constraintCount = columns;
                grid.cellSize = new Vector2(size, cellSize.y);
            }
        }

        void FitFlow()
        {
            float width = Mathf.Max(1f, rect.rect.width);
            int signature = width.GetHashCode();
            unchecked
            {
                for (int i = 0; i < rect.childCount; i++)
                {
                    var child = rect.GetChild(i);
                    signature = signature * 31 + child.GetHashCode();
                    signature = signature * 31 + (child.gameObject.activeSelf ? 1 : 0);
                    if (!measureText || !child.gameObject.activeSelf) continue;
                    var label = LabelOf(child);
                    if (label == null) continue;
                    // Strings are immutable: a changed text is always a different instance, so the reference is enough.
                    signature = signature * 31 + RuntimeHelpers.GetHashCode(label.text);
                    signature = signature * 31 + label.fontSize.GetHashCode();
                }
            }
            if (hasFlowLayout && signature == flowSignature) return;
            hasFlowLayout = true;
            flowSignature = signature;
            float x = 0f;
            float y = 0f;
            float lineHeight = 0f;
            int first = 0;
            for (int i = 0; i < rect.childCount; i++)
            {
                var child = (RectTransform)rect.GetChild(i);
                if (!child.gameObject.activeSelf) continue;
                var label = measureText ? LabelOf(child) : null;
                float desiredWidth = label != null ? label.GetPreferredValues(label.text).x + padding * 2f : child.sizeDelta.x;
                float childWidth = Mathf.Min(width, desiredWidth);
                float childHeight = label != null
                    ? Mathf.Max(rowHeight, label.GetPreferredValues(label.text, Mathf.Max(1f, childWidth - padding * 2f), Mathf.Infinity).y)
                    : child.sizeDelta.y;
                if (x > 0f && x + childWidth > width)
                {
                    CentreLine(first, i, width - x + spacing);
                    y += lineHeight + spacing;
                    x = 0f;
                    lineHeight = 0f;
                    first = i;
                }
                UIFactory.Place(child, new Vector2(0f, 1f), new Vector2(x, -y), new Vector2(childWidth, childHeight));
                x += childWidth + spacing;
                lineHeight = Mathf.Max(lineHeight, childHeight);
            }
            CentreLine(first, rect.childCount, width - x + (x > 0f ? spacing : 0f));
            float height = y + lineHeight;
            if (!Mathf.Approximately(rect.sizeDelta.y, height)) rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            if (!flowElementResolved)
            {
                flowElementResolved = true;
                flowElement = rect.GetComponent<LayoutElement>();
            }
            var element = flowElement;
            if (element != null && !Mathf.Approximately(element.preferredHeight, height)) element.preferredHeight = height;
        }

        void CentreLine(int first, int end, float unusedWidth)
        {
            for (int i = first; i < end; i++)
            {
                var child = (RectTransform)rect.GetChild(i);
                if (child.gameObject.activeSelf) child.anchoredPosition += new Vector2(Mathf.Max(0f, unusedWidth) * 0.5f, 0f);
            }
        }

        void FitScroll()
        {
            float width = Mathf.Max(1f, Mathf.Min(maximumWidth, safe.rect.width - margin * 2f));
            float availableHeight = Mathf.Max(1f, safe.rect.height - margin - (bottom ? bottomMargin : margin));
            float preferred = LayoutUtility.GetPreferredHeight(content);
            float height = Mathf.Min(availableHeight * heightShare, Mathf.Max(1f, preferred));
            bool resized = !Mathf.Approximately(width, lastWidth) || !Mathf.Approximately(height, lastHeight);
            if (resized)
            {
                UIFactory.Place(rect, new Vector2(0.5f, bottom ? 0f : 0.5f),
                    new Vector2(0f, bottom ? bottomMargin : 0f), new Vector2(width, height));
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
                lastWidth = width;
                lastHeight = height;
            }
            RevealSelection(resized);
        }

        void RevealSelection(bool resized)
        {
            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (!resized && selected == lastSelection) return;
            lastSelection = selected;
            if (selected == null || !selected.transform.IsChildOf(content)) return;
            var target = selected.transform as RectTransform;
            if (target == null) return;
            target.GetWorldCorners(corners);
            float low = rect.InverseTransformPoint(corners[0]).y;
            float high = rect.InverseTransformPoint(corners[1]).y;
            float shift = low < rect.rect.yMin ? rect.rect.yMin - low : high > rect.rect.yMax ? rect.rect.yMax - high : 0f;
            if (Mathf.Approximately(shift, 0f)) return;
            scroll.StopMovement();
            float maxOffset = Mathf.Max(0f, content.rect.height - rect.rect.height);
            var position = content.anchoredPosition;
            position.y = Mathf.Clamp(position.y + shift, 0f, maxOffset);
            content.anchoredPosition = position;
        }
    }
}
