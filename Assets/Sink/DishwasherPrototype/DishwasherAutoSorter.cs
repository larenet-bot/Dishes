using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// V7: deliberately conservative shelf packing for the dishwasher.
/// Uses whole image bounding rectangles and leaves part of each rack unused;
/// the existing manual polygon validator still makes the final decision.
/// No GameObject or MonoBehaviour is needed for this helper.
/// </summary>
public static class DishwasherAutoSorter
{
    private struct OriginalPose
    {
        public Vector2 position;
        public int turns;
    }

    private sealed class Shelf
    {
        public readonly RectTransform rack;
        public readonly Rect area;
        public float nextX;
        public float rowBottom;
        public float rowHeight;

        public Shelf(RectTransform rack, float coverage, float gap)
        {
            this.rack = rack;
            Rect full = rack.rect;
            float margin = gap * 0.5f;
            float width = Mathf.Max(0f, full.width - 2f * margin) * coverage;
            float height = Mathf.Max(0f, full.height - 2f * margin) * coverage;
            area = new Rect(full.xMin + margin, full.yMin + margin, width, height);
            nextX = area.xMin;
            rowBottom = area.yMin;
            rowHeight = 0f;
        }
    }

    /// <summary>
    /// Pack existing UI dish instances. The caller first moves them into staging.
    /// Returns the number of regular (non-Dishpod) dishes actually placed.
    /// </summary>
    public static int Pack(
        List<DishwasherDraggableDish> pile,
        RectTransform dishLayer,
        List<RectTransform> unlockedRacks,
        float usableFraction,
        float gap)
    {
        if (pile == null || dishLayer == null || unlockedRacks == null || unlockedRacks.Count == 0)
            return 0;

        usableFraction = Mathf.Clamp(usableFraction, 0.5f, 1f);
        gap = Mathf.Max(0f, gap);
        var shelves = new List<Shelf>(unlockedRacks.Count);
        foreach (RectTransform rack in unlockedRacks)
            if (rack != null && rack.rect.width > 1f && rack.rect.height > 1f)
                shelves.Add(new Shelf(rack, usableFraction, gap));
        if (shelves.Count == 0) return 0;

        var ordered = new List<DishwasherDraggableDish>();
        var original = new Dictionary<DishwasherDraggableDish, OriginalPose>();
        foreach (DishwasherDraggableDish dish in pile)
        {
            if (dish == null || dish.Locked) continue;
            ordered.Add(dish);
            original[dish] = new OriginalPose
            {
                position = dish.RectTransform.anchoredPosition,
                turns = dish.GetQuarterTurns()
            };
        }

        // Dishpod first because it multiplies every loaded dish. Then prefer
        // higher-value dishes. This uses profitPerDish for ordering only;
        // ScoreManager calculates final awards and all upgrade multipliers.
        ordered.Sort((a, b) =>
        {
            if (a.IsDishpod != b.IsDishpod) return a.IsDishpod ? -1 : 1;
            float aValue = a.Dish != null ? a.Dish.profitPerDish : 0f;
            float bValue = b.Dish != null ? b.Dish.profitPerDish : 0f;
            return bValue.CompareTo(aValue);
        });

        int regularPlaced = 0;
        foreach (DishwasherDraggableDish dish in ordered)
        {
            bool placed = false;
            foreach (Shelf shelf in shelves)
            {
                // Keep dishes upright when possible. Rotate 90 degrees only
                // if the current shelf cannot accommodate the upright image.
                if (TryPlaceOnShelf(dish, dishLayer, shelf, 0, gap) ||
                    TryPlaceOnShelf(dish, dishLayer, shelf, 1, gap))
                {
                    placed = true;
                    break;
                }
            }

            if (placed)
            {
                if (!dish.IsDishpod) regularPlaced++;
            }
            else
            {
                Restore(dish, original[dish]);
            }
        }

        // If the conservative rectangle method fits no regular dishes, try
        // one real dish using the existing polygon validator. A single large
        // irregular plate should not permanently stall the automatic mode.
        if (regularPlaced == 0)
        {
            foreach (DishwasherDraggableDish dish in ordered)
                Restore(dish, original[dish]);

            foreach (DishwasherDraggableDish dish in ordered)
            {
                if (dish.IsDishpod) continue;
                foreach (Shelf shelf in shelves)
                {
                    if (TryPlaceSingleByShape(dish, dishLayer, shelf.rack))
                        return 1;
                }
                Restore(dish, original[dish]);
            }
        }

        return regularPlaced;
    }

    private static void Restore(DishwasherDraggableDish dish, OriginalPose pose)
    {
        dish.ApplyQuarterTurns(pose.turns);
        dish.RectTransform.anchoredPosition = pose.position;
    }

    private static bool TryPlaceOnShelf(DishwasherDraggableDish dish,
        RectTransform layer, Shelf shelf, int quarterTurns, float gap)
    {
        RectTransform rt = dish.RectTransform;
        dish.ApplyQuarterTurns(quarterTurns);
        rt.anchoredPosition = layer.InverseTransformPoint(
            shelf.rack.TransformPoint(shelf.rack.rect.center));

        // Bounding rectangle of the *entire UI image*, not the irregular
        // physics outline. This leaves gaps that players can exploit manually.
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        float minX = float.PositiveInfinity, minY = float.PositiveInfinity;
        float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;
        foreach (Vector3 corner in corners)
        {
            Vector3 p = shelf.rack.InverseTransformPoint(corner);
            minX = Mathf.Min(minX, p.x);
            maxX = Mathf.Max(maxX, p.x);
            minY = Mathf.Min(minY, p.y);
            maxY = Mathf.Max(maxY, p.y);
        }
        float width = maxX - minX;
        float height = maxY - minY;
        if (width <= 0f || height <= 0f ||
            width > shelf.area.width + 0.01f || height > shelf.area.height + 0.01f)
            return false;

        float x = shelf.nextX;
        float y = shelf.rowBottom;
        float rowHeight = shelf.rowHeight;
        if (x + width > shelf.area.xMax + 0.01f)
        {
            x = shelf.area.xMin;
            y += rowHeight + gap;
            rowHeight = 0f;
        }
        if (y + height > shelf.area.yMax + 0.01f) return false;

        // At the measurement pose, the dish was centered over rack.rect.center.
        // Shift so its bounding rectangle starts at the chosen shelf position.
        Vector2 desiredCenter = shelf.rack.rect.center + new Vector2(x - minX, y - minY);
        rt.anchoredPosition = layer.InverseTransformPoint(shelf.rack.TransformPoint(desiredCenter));
        shelf.nextX = x + width + gap;
        shelf.rowBottom = y;
        shelf.rowHeight = Mathf.Max(rowHeight, height);
        return true;
    }

    private static bool TryPlaceSingleByShape(
        DishwasherDraggableDish dish, RectTransform layer, RectTransform rack)
    {
        for (int turns = 0; turns < 2; turns++)
        {
            dish.ApplyQuarterTurns(turns);
            dish.RectTransform.anchoredPosition = layer.InverseTransformPoint(
                rack.TransformPoint(rack.rect.center));
            var one = new List<DishwasherDraggableDish> { dish };
            var map = new Dictionary<DishwasherDraggableDish, RectTransform> { { dish, rack } };
            if (DishwasherPlacementValidator.FindInvalidLoadedDishes(one, map, layer).Count == 0)
                return true;
        }
        return false;
    }
}
